// Local launcher for the checked Freelancer test build. No PowerShell dependency.
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.ComponentModel;
using System.Security.Principal;
using System.Text;

// Shared by setup and the launcher. Never changes folder ACLs or system policy.
public static class GameAccess
{
    public static bool IsAdministrator()
    {
        using (var identity = WindowsIdentity.GetCurrent())
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static string Quote(string value)
    {
        var result = new StringBuilder("\""); int slashes = 0;
        foreach (char c in value)
        {
            if (c == '\\') { slashes++; continue; }
            result.Append('\\', c == '"' ? slashes * 2 + 1 : slashes);
            result.Append(c); slashes = 0;
        }
        return result.Append('\\', slashes * 2).Append('"').ToString();
    }

    public static string FindDeniedDirectory(IEnumerable<string> folders)
    {
        foreach (string folder in folders.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(folder)) throw new DirectoryNotFoundException("Game folder not found: " + folder);
            string probe = Path.Combine(folder, ".modern-patch-access-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose)) { }
            }
            catch (UnauthorizedAccessException) { return folder; }
        }
        return null;
    }

    public static string FindDeniedFile(IEnumerable<string> files)
    {
        foreach (string file in files.Where(File.Exists))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReadOnly) != 0)
                throw new IOException("This game file is read-only: " + file + ". Clear its read-only attribute or use a writable game copy.");
            try { using (new FileStream(file, FileMode.Open, FileAccess.Write, FileShare.ReadWrite)) { } }
            catch (UnauthorizedAccessException) { return file; }
        }
        return null;
    }

    public static ProcessStartInfo ElevatedStart(string executable, params string[] arguments)
    {
        return new ProcessStartInfo(executable, String.Join(" ", arguments.Select(Quote)))
        { UseShellExecute = true, Verb = "runas", WorkingDirectory = Path.GetDirectoryName(executable) };
    }

    public static bool RequestAdministrator(string deniedPath, params string[] arguments)
    {
        if (IsAdministrator())
            throw new UnauthorizedAccessException("Windows still denied access with administrator permission: " + deniedPath +
                ". Check this folder's permissions and your security software's protection history, or use a writable game folder such as C:\\Games\\Freelancer.");
        try
        {
            using (var process = Process.Start(ElevatedStart(Application.ExecutablePath, arguments)))
                if (process == null) throw new IOException("Windows could not restart with administrator permission.");
            return true;
        }
        catch (Win32Exception error)
        {
            if (error.NativeErrorCode != 1223) throw;
            return false; // User declined UAC; no automatic retry or game-file changes.
        }
    }
}

public sealed class ManifestEntry
{
    public string path { get; set; }
    public string sha256 { get; set; }
}

public static class Launcher
{
    private static readonly string PackageFolder = AppDomain.CurrentDomain.BaseDirectory;

    public static string Hash(byte[] bytes)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    // Validate the complete executable after normalizing only the three known
    // display-mode locations. An unrelated byte change must always be rejected.
    public static byte[] PrepareMode(byte[] input, string expectedHash, string mode)
    {
        if (input.Length <= 0x1B16CC) throw new InvalidDataException("The game executable is incomplete.");
        if (mode != "Borderless" && mode != "Fullscreen" && mode != "Windowed")
            throw new ArgumentException("Choose Borderless, Fullscreen, or Windowed mode.");
        bool normal = input[0x2477A] == 0xCA && input[0x2477B] == 2;
        bool borderless = input[0x2477A] == 0 && input[0x2477B] == 0;
        if ((!normal && !borderless) || input[0x2490D] != input[0x2477A] ||
            input[0x2490E] != input[0x2477B] || input[0x1B16CC] > 1)
            throw new InvalidDataException("This game has unrecognized display-mode changes. No changes were made.");
        var result = (byte[])input.Clone();
        result[0x2477A] = result[0x2490D] = 0xCA;
        result[0x2477B] = result[0x2490E] = 2;
        result[0x1B16CC] = 1;
        if (!String.Equals(Hash(result), expectedHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Select the patched game copy. Its executable does not match this patch; no changes were made.");
        if (mode != "Fullscreen") result[0x1B16CC] = 0;
        if (mode == "Borderless")
            result[0x2477A] = result[0x2477B] = result[0x2490D] = result[0x2490E] = 0;
        return result;
    }

    private static void Log(string message)
    {
        try { File.AppendAllText(Path.Combine(PackageFolder, "Launcher.log"),
            DateTimeOffset.Now.ToString("o") + " " + message + Environment.NewLine); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void EnsureClosed(string exe)
    {
        foreach (var process in Process.GetProcessesByName("Freelancer"))
        {
            using (process)
            {
                string runningPath;
                try { runningPath = process.MainModule.FileName; }
                catch (InvalidOperationException) { continue; } // It has already exited.
                catch (System.ComponentModel.Win32Exception)
                {
                    throw new InvalidOperationException("A Freelancer process is already running and could not be checked. Close it, then try again without administrator mode.");
                }
                if (String.Equals(runningPath, exe, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("This copy of Freelancer is already running. Switch back to its window, or close it before launching again.");
            }
        }
    }

    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            Application.EnableVisualStyles();
            if (args.Length > 2) throw new ArgumentException("Usage: Freelancer-Launcher.exe \"patched game folder\" [Borderless|Fullscreen|Windowed]");
            string gameFolder;
            if (args.Length == 0)
            {
                using (var picker = new OpenFileDialog())
                {
                    picker.Title = "Select the patched copy's EXE\\Freelancer.exe";
                    picker.Filter = "Freelancer game|Freelancer.exe";
                    picker.CheckFileExists = true;
                    if (picker.ShowDialog() != DialogResult.OK) return 0;
                    gameFolder = Path.GetDirectoryName(Path.GetDirectoryName(picker.FileName));
                }
            }
            else gameFolder = Path.GetFullPath(args[0]);
            string mode = args.Length == 2 ? args[1] : "Borderless";
            string exe = Path.GetFullPath(Path.Combine(gameFolder, "EXE", "Freelancer.exe"));
            EnsureClosed(exe);
            string manifestFile = Path.Combine(PackageFolder, "manifest.json");
            var entries = new JavaScriptSerializer().Deserialize<List<ManifestEntry>>(File.ReadAllText(manifestFile));
            var matches = entries.Where(e => String.Equals(e.path, "EXE/Freelancer.exe", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1) throw new InvalidDataException("Keep the launcher beside this patch's manifest.json file.");
            byte[] before = File.ReadAllBytes(exe);
            byte[] after = PrepareMode(before, matches[0].sha256, mode);
            // Portable saves and FPS settings need write access even when the
            // selected display mode already matches the executable.
            var folders = new List<string> { gameFolder, Path.GetDirectoryName(exe) };
            string saves = Path.Combine(gameFolder, "SAVE");
            if (Directory.Exists(saves)) folders.Add(saves);
            foreach (string child in new[] { "SinglePlayer", "MultiPlayer" })
                if (Directory.Exists(Path.Combine(saves, child))) folders.Add(Path.Combine(saves, child));
            var writableFiles = new List<string> { Path.Combine(gameFolder, "EXE", "flplusplus.ini") };
            writableFiles.Add(Path.Combine(saves, "PerfOptions.ini"));
            if (!before.SequenceEqual(after)) writableFiles.Add(exe);
            string denied = GameAccess.FindDeniedDirectory(folders) ?? GameAccess.FindDeniedFile(writableFiles);
            if (denied != null)
            {
                if (!GameAccess.RequestAdministrator(denied, Path.GetFullPath(gameFolder), mode))
                    MessageBox.Show("Launch cancelled. This game folder needs administrator permission to save settings and progress. You can try again or use a writable folder such as C:\\Games\\Freelancer.", "Freelancer launch cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 0;
            }
            if (!before.SequenceEqual(after))
            {
                string backup = exe + ".before-display-mode.bak";
                if (!File.Exists(backup)) File.Copy(exe, backup);
                File.WriteAllBytes(exe, after);
            }
            var start = new ProcessStartInfo(exe)
            {
                WorkingDirectory = Path.GetDirectoryName(exe),
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Normal
            };
            start.EnvironmentVariables["__COMPAT_LAYER"] = "HIGHDPIAWARE";
            // FPS, saves and the user's system settings are deliberately untouched.
            using (var game = Process.Start(start))
            {
                if (game == null) throw new InvalidOperationException("Windows did not start the game.");
                Log("Started " + exe + " in " + mode + " mode; PID " + game.Id + ".");
                if (game.WaitForExit(3000))
                    throw new InvalidOperationException("Freelancer closed immediately (exit code " + game.ExitCode + "). See Launcher.log for the launch details.");
            }
            return 0;
        }
        catch (Exception error)
        {
            Log("Launch failed: " + error.ToString());
            MessageBox.Show(error.Message, "Freelancer could not start", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
