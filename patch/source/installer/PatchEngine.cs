// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace FreelancerModernSetup
{
    public sealed class Change
    {
        public string Path { get; set; }
        public string OriginalHash { get; set; }
        public string InstalledHash { get; set; }
        public bool KeepOnUndo { get; set; }
    }
    public sealed class BackupState
    {
        public int Format { get; set; }
        public string Status { get; set; }
        public List<Change> Changes { get; set; }
    }
    public static class PatchEngine
    {
        const string BackupName = "ModernPatchBackup";
        const string HdBackupName = "ModernTexturesBackup";
        static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
        static bool Equal(string a, string b) { return String.Equals(a, b, StringComparison.OrdinalIgnoreCase); }
        static string Safe(string root, string relative) { return Installer.SafeEntry(root, relative); }
        static bool Exists(string path) { return File.Exists(path) || Directory.Exists(path); }
        static string Hash(string path) { return Installer.Hash(path); }
        static void WriteState(string root, BackupState state)
        {
            string path = Path.Combine(root, "state.json"), pending = path + ".new";
            File.WriteAllText(pending, Json.Serialize(state), Encoding.UTF8);
            if (File.Exists(path)) File.Replace(pending, path, null); else File.Move(pending, path);
        }
        static BackupState ReadState(string backup)
        {
            Installer.NoLinks(backup);
            var state = Json.Deserialize<BackupState>(File.ReadAllText(Path.Combine(backup, "state.json")));
            if (state == null || state.Format != 1 || state.Changes == null || state.Changes.Count == 0 ||
                !new[] { "Installing", "Installed", "Restoring", "Restored" }.Contains(state.Status))
                throw new IOException("The patch backup record is invalid. No game files were changed.");
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Change entry in state.Changes)
            {
                if (entry == null) throw new IOException("Invalid backup entry.");
                string normalized = Safe(backup, entry.Path);
                string first = entry.Path.Replace('/', '\\').Split('\\')[0];
                if (!seen.Add(normalized) || Equal(first, "SAVE") || Equal(first, "SCREENSHOTS") ||
                    first.StartsWith(BackupName, StringComparison.OrdinalIgnoreCase) || first.StartsWith(HdBackupName, StringComparison.OrdinalIgnoreCase) ||
                    !Regex.IsMatch(entry.InstalledHash ?? "", "^[0-9a-fA-F]{64}$") ||
                    (entry.OriginalHash != null && !Regex.IsMatch(entry.OriginalHash, "^[0-9a-fA-F]{64}$")))
                    throw new IOException("Invalid backup entry. No game files were changed.");
            }
            return state;
        }
        static void Copy(string source, string target)
        {
            Installer.NoLinks(source); Installer.NoLinks(target);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            if (File.Exists(target)) File.SetAttributes(target, FileAttributes.Normal);
            File.Copy(source, target, true); File.SetAttributes(target, FileAttributes.Normal);
        }
        static void CheckGame(string game)
        {
            Installer.CheckStopped(); Installer.NoLinks(game);
            if (!File.Exists(Path.Combine(game, "EXE\\Freelancer.exe")))
                throw new IOException("Select the Freelancer folder containing EXE and DATA.");
        }

        public static void Install(string package, string game, Action<int, string> progress)
        { InstallComponent(package, game, progress, false); }

        public static void InstallTextures(string package, string game, Action<int, string> progress)
        {
            if (!IsModernGame(package, game)) throw new IOException("Install the core Modern Patch before adding HD textures. The required game files did not match.");
            InstallComponent(package, game, progress, true);
        }

        public static bool IsModernGame(string package, string game)
        {
            try
            {
                var entries = Json.Deserialize<Entry[]>(File.ReadAllText(Path.Combine(package, "manifest.json")));
                var exe = entries.Single(e => Equal(e.path.Replace('\\', '/'), "EXE/Freelancer.exe"));
                Launcher.PrepareMode(File.ReadAllBytes(Safe(game, exe.path)), exe.sha256, "Fullscreen");
                foreach (Entry entry in entries.Where(e => e.path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
                    if (!Equal(Hash(Safe(game, entry.path)), entry.sha256)) return false;
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        static void InstallComponent(string package, string game, Action<int, string> progress, bool textures)
        {
            package = Installer.Full(package); game = Installer.Full(game); CheckGame(game);
            if (game.Any(c => c > 127))
                throw new IOException("Freelancer cannot reliably start from a path containing accented or other non-English characters. Move the game to a path using ordinary English letters and numbers, such as C:\\Games\\Freelancer. Spaces are fine.");
            if (Equal(package, game) || package.StartsWith(game + "\\", StringComparison.OrdinalIgnoreCase) ||
                game.StartsWith(package + "\\", StringComparison.OrdinalIgnoreCase))
                throw new IOException("Extract the patch into a separate folder outside your game folder.");
            Installer.NoLinks(package);
            string backup = Path.Combine(game, textures ? HdBackupName : BackupName);
            BackupState previous = Directory.Exists(backup) ? ReadState(backup) : null;
            if (previous != null && previous.Status != "Restored")
                throw new IOException(textures ? "This folder already has an HD texture backup. Use Undo HD Textures before installing them again." : "This folder already has a patch backup. Use Undo Patch to restore it before installing again.");
            if (File.Exists(backup)) throw new IOException("A file conflicts with the patch backup folder.");
            progress(0, "Checking the game and patch files...");
            Installer.Files(game); // Reject junctions before touching any game files.
            string payload = Path.Combine(package, textures ? "hd-payload" : "payload");
            var payloadFiles = Installer.Files(payload);
            var entries = Json.Deserialize<Entry[]>(File.ReadAllText(Path.Combine(package, textures ? "hd-manifest.json" : "manifest.json")));
            if (entries == null || entries.Length == 0) throw new IOException("The patch manifest is empty.");
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var state = new BackupState { Format = 1, Status = "Installing", Changes = new List<Change>() };
            foreach (Entry entry in entries)
            {
                if (entry == null || !Regex.IsMatch(entry.sha256 ?? "", "^[0-9a-fA-F]{64}$")) throw new IOException("Invalid patch checksum.");
                string file = Safe(payload, entry.path), target = Safe(game, entry.path);
                if (textures && !entry.path.Replace('\\', '/').StartsWith("DATA/", StringComparison.OrdinalIgnoreCase) &&
                    !entry.path.Replace('\\', '/').StartsWith("credits/HD/", StringComparison.OrdinalIgnoreCase))
                    throw new IOException("HD texture entries must be game assets or their credits.");
                if (!seen.Add(file)) throw new IOException("Duplicate patch file in manifest.");
                if (!Equal(Hash(file), entry.sha256)) throw new IOException("Patch file check failed: " + entry.path + ". Extract a fresh download.");
                if (!String.IsNullOrEmpty(entry.original_sha256))
                {
                    if (!File.Exists(target) || !Equal(Hash(target), entry.original_sha256))
                        throw new IOException("Unsupported or modified original: " + entry.path + ". Use an unmodified English retail installation.");
                }
                else if (Exists(target)) throw new IOException("An existing patch file conflicts: " + entry.path);
                sources.Add(entry.path, file);
                state.Changes.Add(new Change { Path = entry.path, OriginalHash = entry.original_sha256, InstalledHash = entry.sha256 });
            }
            if (seen.Count != payloadFiles.Count || payloadFiles.Any(f => !seen.Contains(f))) throw new IOException("The payload contains unexpected files.");
            string supportFolder = textures ? "ModernTextures" : "ModernPatch";
            string playCommand = textures ? "Play Freelancer HD.cmd" : "Play Freelancer.cmd";
            string undoCommand = textures ? "Undo HD Textures.cmd" : "Undo Modern Patch.cmd";
            foreach (string reserved in new[] { supportFolder, playCommand, undoCommand })
                if (Exists(Path.Combine(game, reserved)) && previous == null)
                    throw new IOException("An existing file or folder conflicts: " + reserved);
            foreach (string name in new[] { "Freelancer-Setup.exe", "Freelancer-Launcher.exe", "manifest.json" })
            {
                string file = Path.Combine(package, name);
                string relative = supportFolder + "/" + name;
                sources.Add(relative, file);
                state.Changes.Add(new Change { Path = relative, InstalledHash = Hash(file), KeepOnUndo = true });
            }
            var commands = new Dictionary<string, string> {
                { playCommand, "@echo off\r\nsetlocal DisableDelayedExpansion\r\nstart \"\" \"%~dp0" + supportFolder + "\\Freelancer-Launcher.exe\" \"%~dp0.\" Borderless\r\n" },
                { undoCommand, "@echo off\r\nsetlocal DisableDelayedExpansion\r\nstart \"\" \"%~dp0" + supportFolder + "\\Freelancer-Setup.exe\" " + (textures ? "--undo-hd" : "--undo") + " \"%~dp0.\"\r\n" }
            };
            // Route older core uninstall shortcuts through this version so HD is restored first.
            if (textures && Directory.Exists(Path.Combine(game, BackupName)) && File.Exists(Path.Combine(game, "Undo Modern Patch.cmd")))
                commands.Add("Undo Modern Patch.cmd", "@echo off\r\nsetlocal DisableDelayedExpansion\r\nstart \"\" \"%~dp0ModernTextures\\Freelancer-Setup.exe\" --undo \"%~dp0.\"\r\n");
            // Prepare and verify every backup before replacing the first game file.
            string stage = backup + ".preparing-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(stage);
            foreach (var command in commands)
            {
                string file = Path.Combine(stage, "commands", command.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(file)); File.WriteAllText(file, command.Value, Encoding.ASCII);
                sources.Add(command.Key, file);
                state.Changes.Add(new Change { Path = command.Key, InstalledHash = Hash(file), KeepOnUndo = command.Key == undoCommand });
            }
            foreach (Change entry in state.Changes)
            {
                string target = Safe(game, entry.Path);
                if (File.Exists(target))
                {
                    string original = Safe(Path.Combine(stage, "originals"), entry.Path);
                    Copy(target, original);
                    string hash = Hash(original);
                    if (entry.OriginalHash != null && !Equal(hash, entry.OriginalHash)) throw new IOException("An original file changed during backup: " + entry.Path);
                    entry.OriginalHash = hash;
                }
            }
            WriteState(stage, state);
            // Validate the record using the same path and checksum rules used by Undo.
            ReadState(stage);
            if (previous != null) Directory.Move(backup, backup + ".previous-" + Guid.NewGuid().ToString("N"));
            Directory.Move(stage, backup);
            try
            {
                int done = 0;
                foreach (Change entry in state.Changes)
                {
                    string file = sources[entry.Path];
                    if (file.StartsWith(stage + "\\", StringComparison.OrdinalIgnoreCase)) file = backup + file.Substring(stage.Length);
                    Copy(file, Safe(game, entry.Path));
                    if (!Equal(Hash(Safe(game, entry.Path)), entry.InstalledHash)) throw new IOException("Installed file verification failed: " + entry.Path);
                    progress(10 + (++done * 85 / state.Changes.Count), textures ? "Installing HD textures..." : "Installing the patch...");
                }
                state.Status = "Installed"; WriteState(backup, state); progress(100, "Installed. Your original files are backed up.");
            }
            catch (Exception error)
            {
                try { UndoComponent(game, delegate { }, textures); }
                catch (Exception restoreError) { throw new IOException("Installation stopped: " + error.Message + "\nUse Undo Patch to recover. Backup: " + backup + "\n" + restoreError.Message); }
                throw new IOException("Installation stopped and the original game files were restored. " + error.Message);
            }
        }

        public static void Undo(string game, Action<int, string> progress)
        {
            if (Directory.Exists(Path.Combine(game, HdBackupName)) && ReadState(Path.Combine(game, HdBackupName)).Status != "Restored")
                UndoTextures(game, progress);
            UndoComponent(game, progress, false);
        }

        public static void UndoTextures(string game, Action<int, string> progress)
        { UndoComponent(game, progress, true); }

        static void UndoComponent(string game, Action<int, string> progress, bool textures)
        {
            game = Installer.Full(game); Installer.CheckStopped(); Installer.NoLinks(game);
            if (!Directory.Exists(game)) throw new IOException("The game folder does not exist.");
            string backup = Path.Combine(game, textures ? HdBackupName : BackupName);
            var state = ReadState(backup);
            if (state.Status == "Restored") throw new IOException("The original game has already been restored.");
            Installer.Files(backup);
            foreach (Change entry in state.Changes)
            {
                Installer.NoLinks(Safe(game, entry.Path));
                if (entry.OriginalHash != null && !Equal(Hash(Safe(Path.Combine(backup, "originals"), entry.Path)), entry.OriginalHash))
                    throw new IOException("Backup file check failed: " + entry.Path + ". No files were restored.");
            }
            // Preserve installed files, including changed settings, before removing anything.
            foreach (Change entry in state.Changes)
            {
                string current = Safe(game, entry.Path), retained = Safe(Path.Combine(backup, "removed-patch-files"), entry.Path);
                if (File.Exists(current) && !File.Exists(retained))
                {
                    Copy(current, retained);
                    if (!Equal(Hash(current), Hash(retained))) throw new IOException("Could not preserve current file: " + entry.Path);
                }
            }
            state.Status = "Restoring"; WriteState(backup, state);
            int done = 0;
            foreach (Change entry in state.Changes)
            {
                if (!entry.KeepOnUndo)
                {
                    string target = Safe(game, entry.Path);
                    if (entry.OriginalHash != null)
                    {
                        Copy(Safe(Path.Combine(backup, "originals"), entry.Path), target);
                        if (!Equal(Hash(target), entry.OriginalHash)) throw new IOException("Restore verification failed: " + entry.Path);
                    }
                    else if (File.Exists(target)) { File.SetAttributes(target, FileAttributes.Normal); File.Delete(target); }
                }
                progress(++done * 100 / state.Changes.Count, "Restoring the original game files...");
            }
            state.Status = "Restored"; WriteState(backup, state);
        }
    }
}
