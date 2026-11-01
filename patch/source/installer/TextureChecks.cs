// SPDX-License-Identifier: MIT
using System;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using FreelancerModernSetup;
static class TextureChecks
{
    static int checks;
    static void Check(bool ok, string why) { if (!ok) throw new Exception(why); checks++; }
    static void Put(string path, string content) { Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, content); }
    static void Reject(Action run, string message) { try { run(); throw new Exception("Unexpected success"); } catch (IOException e) { Check(e.Message.Contains(message), e.Message); } }
    static void Main(string[] args)
    {
        string source = Path.GetFullPath(args[0]), root = Path.Combine(Path.GetFullPath(args[1]), "texture-check-" + Guid.NewGuid().ToString("N"));
        string package = Path.Combine(root, "package"), game = Path.Combine(root, "game"); Directory.CreateDirectory(package);
        File.Copy(Path.Combine(source, "manifest.json"), Path.Combine(package, "manifest.json"));
        var entries = new JavaScriptSerializer().Deserialize<Entry[]>(File.ReadAllText(Path.Combine(package, "manifest.json")));
        foreach (Entry entry in entries.Where(e => e.path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || e.path.EndsWith("Freelancer.exe", StringComparison.OrdinalIgnoreCase)))
        {
            string target = Path.Combine(game, entry.path); Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(Path.Combine(source, "payload", entry.path), target);
        }
        foreach (string name in new[] { "Freelancer-Setup.exe", "Freelancer-Launcher.exe" }) File.Copy(Path.Combine(source, name), Path.Combine(package, name));
        string original = Path.Combine(game, "DATA/test.txm"), texture = Path.Combine(package, "hd-payload/DATA/test.txm");
        Put(original, "original texture"); Put(texture, "HD texture"); Put(Path.Combine(game, "SAVE/test.txt"), "save"); Put(Path.Combine(game, "EXE/flplusplus.ini"), "max_fps = 144");
        string manifest = new JavaScriptSerializer().Serialize(new[] { new Entry { path = "DATA/test.txm", sha256 = Installer.Hash(texture), original_sha256 = Installer.Hash(original) } });
        string manifestPath = Path.Combine(package, "hd-manifest.json"); Put(manifestPath, manifest);
        string coreHash = Installer.Hash(Path.Combine(game, "EXE/Freelancer.exe"));
        Action install = delegate { PatchEngine.InstallTextures(package, game, delegate { }); };
        Check(PatchEngine.IsModernGame(package, game), "Modern core was not recognized");
        Put(texture, "corrupt"); Reject(install, "Patch file check failed"); Put(texture, "HD texture");
        Put(original, "modded texture"); Reject(install, "Unsupported or modified"); Put(original, "original texture");
        Put(manifestPath, manifest.Replace("DATA/test.txm", "EXE/not-an-asset.dll")); Reject(install, "must be game assets"); Put(manifestPath, manifest);
        Check(!Directory.Exists(Path.Combine(game, "ModernTexturesBackup")), "Preflight made backup");
        install();
        Check(File.ReadAllText(original) == "HD texture", "HD missing");
        Check(Installer.Hash(Path.Combine(game, "EXE/Freelancer.exe")) == coreHash, "HD changed game EXE");
        Check(File.ReadAllText(Path.Combine(game, "EXE/flplusplus.ini")) == "max_fps = 144", "HD reset FPS");
        Reject(install, "already has an HD texture backup");
        PatchEngine.UndoTextures(game, delegate { });
        Check(File.ReadAllText(original) == "original texture", "Texture restore failed");
        Check(PatchEngine.IsModernGame(package, game), "HD Undo removed core");
        Check(File.ReadAllText(Path.Combine(game, "SAVE/test.txt")) == "save", "Save lost");
        Check(!File.Exists(Path.Combine(game, "Play Freelancer HD.cmd")), "HD Play entry not removed");
        install(); Check(File.ReadAllText(original) == "HD texture", "HD reinstall failed"); PatchEngine.UndoTextures(game, delegate { });
        Reject(delegate { PatchEngine.InstallTextures(package, game, delegate(int value, string message) { if (value > 0) throw new IOException("simulated failure"); }); }, "original game files were restored");
        Check(File.ReadAllText(original) == "original texture", "HD rollback failed");
        Check(PatchEngine.IsModernGame(package, game), "Rollback removed core");
        Console.WriteLine("Passed " + checks + " HD installer assertions.");
    }
}
