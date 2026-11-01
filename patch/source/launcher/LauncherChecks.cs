using System;
using System.IO;
using System.Linq;

public static class LauncherChecks
{
    private static void Reject(byte[] bytes, string hash, string mode)
    {
        try { Launcher.PrepareMode(bytes, hash, mode); }
        catch (ArgumentException) { return; }
        catch (InvalidDataException) { return; }
        throw new Exception("An incompatible executable or mode was accepted.");
    }
    public static int Main(string[] args)
    {
        var canonical = File.ReadAllBytes(args[0]);
        string hash = Launcher.Hash(canonical);
        foreach (string mode in new[] { "Borderless", "Windowed", "Fullscreen" })
        {
            var selected = Launcher.PrepareMode(canonical, hash, mode);
            var restored = Launcher.PrepareMode(selected, hash, "Fullscreen");
            if (!restored.SequenceEqual(canonical)) throw new Exception("Mode round-trip failed.");
            if (selected[0x1B16CC] != (mode == "Fullscreen" ? 1 : 0)) throw new Exception("Window mode did not apply.");
            if (selected[0x2477A] != (mode == "Borderless" ? 0 : 0xCA)) throw new Exception("Border mode did not apply.");
        }
        var corrupted = (byte[])canonical.Clone(); corrupted[0x1000] ^= 1;
        Reject(corrupted, hash, "Borderless");
        var unknownMode = (byte[])canonical.Clone(); unknownMode[0x2477A] = 0xCC;
        Reject(unknownMode, hash, "Borderless");
        Reject(new byte[64], hash, "Borderless");
        Reject(canonical, hash, "invalid");
        Reject(File.ReadAllBytes(args[1]), hash, "Borderless");
        if (Launcher.Hash(canonical) != hash) throw new Exception("Input bytes were mutated.");
        Console.WriteLine("PASS: three display modes round-trip; corrupt, short, unrecognized and original retail executables rejected; input unchanged.");
        return 0;
    }
}
