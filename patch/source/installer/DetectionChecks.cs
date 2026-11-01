// SPDX-License-Identifier: MIT
using System;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using FreelancerModernSetup;

static class DetectionChecks
{
    static int checks;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
    static int Main(string[] args)
    {
        try
        {
            string package = Path.GetFullPath(args[0]), retail = Path.GetFullPath(args[1]);
            string originalExe = Path.Combine(retail, "EXE/Freelancer.exe");
            string before = Installer.Hash(originalExe);
            Check(!PatchEngine.IsModernGame(package, retail), "Retail game incorrectly recognized as already patched");
            Check(Installer.Hash(originalExe) == before, "Detection changed the retail executable");
            string game = Path.Combine(Path.GetFullPath(args[2]), "detection-check-" + Guid.NewGuid().ToString("N"));
            var entries = new JavaScriptSerializer().Deserialize<Entry[]>(File.ReadAllText(Path.Combine(package, "manifest.json")));
            foreach (Entry entry in entries.Where(e => e.path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || e.path.EndsWith("Freelancer.exe", StringComparison.OrdinalIgnoreCase)))
            {
                string target = Path.Combine(game, entry.path); Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(Path.Combine(package, "payload", entry.path), target);
            }
            string exe = Path.Combine(game, "EXE/Freelancer.exe");
            byte[] patched = File.ReadAllBytes(exe);
            string expected = entries.Single(e => e.path.Equals("EXE/Freelancer.exe", StringComparison.OrdinalIgnoreCase)).sha256;
            foreach (string mode in new[] { "Fullscreen", "Windowed", "Borderless" })
            {
                File.WriteAllBytes(exe, Launcher.PrepareMode(patched, expected, mode));
                string hash = Installer.Hash(exe);
                Check(PatchEngine.IsModernGame(package, game), "Supported display mode not recognized: " + mode);
                Check(Installer.Hash(exe) == hash, "Detection changed selected display mode");
            }
            byte[] damaged = (byte[])patched.Clone(); damaged[0x2477A] ^= 1;
            File.WriteAllBytes(exe, damaged);
            Check(!PatchEngine.IsModernGame(package, game), "Unknown display changes accepted");
            Check(Installer.Hash(exe) == Launcher.Hash(damaged), "Detection rewrote unknown display changes");
            damaged = (byte[])patched.Clone(); damaged[0x1000] ^= 1; File.WriteAllBytes(exe, damaged);
            Check(!PatchEngine.IsModernGame(package, game), "Unrelated EXE corruption accepted");
            File.WriteAllBytes(exe, new byte[20]);
            Check(!PatchEngine.IsModernGame(package, game), "Truncated EXE accepted");
            File.Delete(exe);
            Check(!PatchEngine.IsModernGame(package, game), "Missing EXE accepted");
            File.WriteAllBytes(exe, patched);
            var dll = entries.First(e => e.path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
            File.WriteAllText(Path.Combine(game, dll.path), "modified");
            Check(!PatchEngine.IsModernGame(package, game), "Modified core DLL accepted");
            Console.WriteLine("Passed " + checks + " installation detection assertions.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("Detection check failed: " + error.GetType().Name + ": " + error.Message); return 1; }
    }
}
