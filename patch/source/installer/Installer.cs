// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace FreelancerModernSetup
{
    public sealed class Entry
    {
        public string path { get; set; }
        public string sha256 { get; set; }
        public string original_sha256 { get; set; }
    }

    public static class Installer
    {
        public static string Full(string value)
        {
            if (String.IsNullOrWhiteSpace(value) || !Path.IsPathRooted(value))
                throw new IOException("Please select a full folder path.");
            string result = Path.GetFullPath(value);
            if (result.TrimEnd('\\').Length == Path.GetPathRoot(result).TrimEnd('\\').Length)
                throw new IOException("Choose a game folder, not a drive root.");
            return result.TrimEnd('\\');
        }

        static bool Inside(string child, string parent)
        {
            return child.Equals(parent, StringComparison.OrdinalIgnoreCase) ||
                child.StartsWith(parent + "\\", StringComparison.OrdinalIgnoreCase);
        }

        internal static void NoLinks(string path)
        {
            for (string p = path; !String.IsNullOrEmpty(p); p = Path.GetDirectoryName(p))
                if ((Directory.Exists(p) || File.Exists(p)) &&
                    (File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Please use a normal local folder, without folder links: " + p);
        }

        internal static List<string> Files(string root)
        {
            NoLinks(root);
            var files = new List<string>();
            var pending = new Stack<string>(); pending.Push(root);
            while (pending.Count != 0)
            {
                string dir = pending.Pop();
                foreach (string item in Directory.GetFileSystemEntries(dir))
                {
                    FileAttributes attr = File.GetAttributes(item);
                    if ((attr & FileAttributes.ReparsePoint) != 0)
                        throw new IOException("Folder links are not supported: " + item);
                    if ((attr & FileAttributes.Directory) != 0) pending.Push(item);
                    else files.Add(item);
                }
            }
            return files;
        }

        public static string Hash(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        static string Relative(string root, string file) { return file.Substring(root.Length + 1); }

        internal static string SafeEntry(string root, string relative)
        {
            if (String.IsNullOrEmpty(relative) || Path.IsPathRooted(relative))
                throw new IOException("Invalid file path in the patch manifest.");
            string[] parts = relative.Replace('/', '\\').Split('\\');
            foreach (string part in parts)
                if (String.IsNullOrEmpty(part) || part == "." || part == ".." ||
                    part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                    part.EndsWith(".") || part.EndsWith(" ") ||
                    Regex.IsMatch(part, @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(\.|$)", RegexOptions.IgnoreCase))
                    throw new IOException("Invalid file path in the patch manifest.");
            string result = Path.GetFullPath(Path.Combine(root, String.Join("\\", parts)));
            if (!Inside(result, root)) throw new IOException("Invalid patch path.");
            return result;
        }

        internal static void CheckStopped()
        {
            foreach (Process game in Process.GetProcessesByName("Freelancer"))
            {
                game.Dispose();
                throw new IOException("Please save and close Freelancer before installing.");
            }
        }

    }
}
