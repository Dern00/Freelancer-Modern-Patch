// SPDX-License-Identifier: MIT
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Web.Script.Serialization;

public static class AccessChecks
{
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    public static int Main(string[] args)
    {
        if (args[0] == "--arguments")
        { File.WriteAllText(args[1], new JavaScriptSerializer().Serialize(args.Skip(2).ToArray())); return 0; }
        if (args[0] == "--elevated-write")
        {
            if (!GameAccess.IsAdministrator()) return 2;
            File.WriteAllText(Path.Combine(args[1], "elevated.txt"), "administrator access confirmed"); return 0;
        }
        string root = Path.Combine(Path.GetFullPath(args[0]), "access-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string file = Path.Combine(root, "settings.ini"); File.WriteAllText(file, "preserve settings");
        Check(GameAccess.FindDeniedDirectory(new[] { root, root }) == null, "Writable folder rejected");
        Check(Directory.GetFiles(root).Length == 1, "Probe was not removed");
        Check(GameAccess.FindDeniedFile(new[] { file }) == null, "Writable file rejected");
        Check(File.ReadAllText(file) == "preserve settings", "File probe changed contents");
        File.SetAttributes(file, FileAttributes.ReadOnly);
        try { GameAccess.FindDeniedFile(new[] { file }); throw new Exception("Read-only file accepted"); }
        catch (IOException error) { Check(error.Message.Contains("read-only"), "Read-only error unclear"); }
        finally { File.SetAttributes(file, FileAttributes.Normal); }
        try { GameAccess.FindDeniedDirectory(new[] { Path.Combine(root, "missing") }); throw new Exception("Missing folder accepted"); }
        catch (DirectoryNotFoundException) { checks++; }

        string executable = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string output = Path.Combine(root, "arguments.json");
        string[] values = { "C:\\Game Folder\\", "", "with\"quote", "x\\\"y", "C:\\Games\\Freelancer & more", "%PATH%", "Unicode é" };
        var start = new ProcessStartInfo(executable, String.Join(" ", new[] { "--arguments", output }.Concat(values).Select(GameAccess.Quote)))
            { UseShellExecute = false, CreateNoWindow = true };
        using (var child = Process.Start(start)) { child.WaitForExit(); Check(child.ExitCode == 0, "Argument child failed"); }
        Check(new JavaScriptSerializer().Deserialize<string[]>(File.ReadAllText(output)).SequenceEqual(values), "Windows argument round-trip failed");
        var elevated = GameAccess.ElevatedStart(executable, "--elevated-write", root);
        Check(elevated.UseShellExecute && elevated.Verb == "runas" && elevated.FileName == executable, "Elevation does not use direct Windows consent");

        // Restrict only this disposable directory; restore its original ACL in finally.
        string restricted = Path.Combine(root, "Protected Game"); Directory.CreateDirectory(restricted);
        var original = Directory.GetAccessControl(restricted);
        try
        {
            var acl = new DirectorySecurity(); acl.SetAccessRuleProtection(true, false);
            foreach (var sid in new[] { WellKnownSidType.BuiltinAdministratorsSid, WellKnownSidType.LocalSystemSid })
                acl.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(sid, null), FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            using (var identity = WindowsIdentity.GetCurrent())
                acl.AddAccessRule(new FileSystemAccessRule(identity.User, FileSystemRights.ReadAndExecute | FileSystemRights.ChangePermissions, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            Directory.SetAccessControl(restricted, acl);
            if (!GameAccess.IsAdministrator())
                Check(GameAccess.FindDeniedDirectory(new[] { restricted }) == restricted, "Protected directory did not report denial");
            if (args.Length > 1 && args[1] == "--uac")
            {
                using (var child = Process.Start(GameAccess.ElevatedStart(executable, "--elevated-write", restricted)))
                { child.WaitForExit(); Check(child.ExitCode == 0, "Elevated child failed"); }
                Check(File.ReadAllText(Path.Combine(restricted, "elevated.txt")) == "administrator access confirmed", "Elevated write not confirmed");
            }
            Check(Directory.GetFiles(restricted, ".modern-patch-access-*").Length == 0, "Denied probe left a file");
        }
        finally { Directory.SetAccessControl(restricted, original); }
        Console.WriteLine("PASS: " + checks + " permission, file-preservation and Windows argument checks.");
        return 0;
    }
}
