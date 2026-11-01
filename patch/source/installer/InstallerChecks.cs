// SPDX-License-Identifier: MIT
using System;
using System.IO;
using FreelancerModernSetup;
static class InstallerChecks
{
    static int checks;
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
    static void Put(string path, string content) { Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, content); }
    static void Reject(Action action, string expected)
    {
        try { action(); throw new Exception("Unexpected success"); }
        catch (IOException error) { Require(error.Message.Contains(expected), error.Message); }
    }
    static void Main(string[] args)
    {
        string root = Path.Combine(Path.GetFullPath(args[0]), "inplace-check-" + Guid.NewGuid().ToString("N"));
        string game = Path.Combine(root, "Game Folder"), package = Path.Combine(root, "Patch é");
        string exe = Path.Combine(game, "EXE/Freelancer.exe"), payload = Path.Combine(package, "payload/EXE/Freelancer.exe");
        Put(exe, "original"); Put(payload, "patched"); Put(Path.Combine(package, "payload/EXE/new.ini"), "config");
        Put(Path.Combine(game, "SAVE/player.fl"), "keep progress");
        Put(Path.Combine(game, "DATA/untouched.ini"), "keep data");
        Put(Path.Combine(package, "Freelancer-Setup.exe"), "setup"); Put(Path.Combine(package, "Freelancer-Launcher.exe"), "launcher");
        string manifest = "[{\"path\":\"EXE/Freelancer.exe\",\"sha256\":\"" + Installer.Hash(payload) + "\",\"original_sha256\":\"" + Installer.Hash(exe) + "\"}," +
            "{\"path\":\"EXE/new.ini\",\"sha256\":\"" + Installer.Hash(Path.Combine(package, "payload/EXE/new.ini")) + "\",\"original_sha256\":null}]";
        string manifestPath = Path.Combine(package, "manifest.json"); Put(manifestPath, manifest);
        Action install = delegate { PatchEngine.Install(package, game, delegate { }); };
        Reject(delegate { PatchEngine.Install(package, Path.Combine(root, "missing"), delegate { }); }, "Select the Freelancer");
        Reject(delegate { PatchEngine.Install(game, game, delegate { }); }, "separate folder");
        Put(Path.Combine(root, "Game é/EXE/Freelancer.exe"), "original");
        Reject(delegate { PatchEngine.Install(package, Path.Combine(root, "Game é"), delegate { }); }, "cannot reliably start");
        Put(payload, "corrupt"); Reject(install, "Patch file check failed"); Put(payload, "patched");
        Put(exe, "modified"); Reject(install, "Unsupported or modified"); Put(exe, "original");
        Put(manifestPath, manifest.Replace("EXE/Freelancer.exe", "../escape.exe")); Reject(install, "Invalid file path");
        Put(manifestPath, manifest.Replace("EXE/new.ini", "EXE/Freelancer.exe")); Reject(install, "Duplicate");
        Put(manifestPath, manifest); Put(Path.Combine(package, "payload/extra.txt"), "extra"); Reject(install, "unexpected files"); File.Delete(Path.Combine(package, "payload/extra.txt"));
        Require(!Directory.Exists(Path.Combine(game, "ModernPatchBackup")), "Preflight wrote backup");
        Require(File.ReadAllText(exe) == "original", "Preflight changed original");
        install();
        Require(File.ReadAllText(exe) == "patched", "Not patched");
        Require(File.ReadAllText(Path.Combine(game, "ModernPatchBackup/originals/EXE/Freelancer.exe")) == "original", "Original backup wrong");
        Require(File.Exists(Path.Combine(game, "Play Freelancer.cmd")), "Play entry missing");
        Require(File.Exists(Path.Combine(game, "Undo Modern Patch.cmd")), "Undo entry missing");
        Require(File.ReadAllText(Path.Combine(game, "SAVE/player.fl")) == "keep progress", "Save changed");
        Reject(install, "already has a patch backup");
        string originalBackup = Path.Combine(game, "ModernPatchBackup/originals/EXE/Freelancer.exe");
        Put(originalBackup, "bad backup"); Reject(delegate { PatchEngine.Undo(game, delegate { }); }, "Backup file check failed");
        Require(File.ReadAllText(exe) == "patched", "Bad backup changed game"); Put(originalBackup, "original");
        Put(Path.Combine(game, "EXE/new.ini"), "changed settings");
        File.Delete(exe); PatchEngine.Undo(game, delegate { });
        Require(File.ReadAllText(exe) == "original", "Original not restored");
        Require(!File.Exists(Path.Combine(game, "EXE/new.ini")), "Added file not removed");
        Require(File.ReadAllText(Path.Combine(game, "ModernPatchBackup/removed-patch-files/EXE/new.ini")) == "changed settings", "Changed settings not retained");
        Require(!File.Exists(Path.Combine(game, "Play Freelancer.cmd")), "Play entry not removed");
        Require(File.ReadAllText(Path.Combine(game, "SAVE/player.fl")) == "keep progress", "Undo changed save");
        Require(File.ReadAllText(Path.Combine(game, "DATA/untouched.ini")) == "keep data", "Undo changed unrelated data");
        Reject(delegate { PatchEngine.Undo(game, delegate { }); }, "already been restored");
        install(); Require(File.ReadAllText(exe) == "patched", "Reinstall after undo failed");
        PatchEngine.Undo(game, delegate { });
        Reject(delegate { PatchEngine.Install(package, game, delegate(int percent, string message) { if (percent > 0) throw new IOException("simulated write interruption"); }); }, "original game files were restored");
        Require(File.ReadAllText(exe) == "original", "Rollback failed");
        Require(!File.Exists(Path.Combine(game, "EXE/new.ini")), "Rollback left added file");
        Require(File.ReadAllText(Path.Combine(game, "SAVE/player.fl")) == "keep progress", "Rollback changed save");
        Console.WriteLine("Passed " + checks + " in-place installer and undo assertions.");
    }
}
