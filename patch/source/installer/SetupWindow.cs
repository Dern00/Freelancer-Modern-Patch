// SPDX-License-Identifier: MIT
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FreelancerModernSetup
{
    sealed class SetupWindow : Form
    {
        readonly TextBox game = new TextBox(); readonly Button browse = new Button(), install = new Button(), undo = new Button(), undoHd = new Button(), play = new Button();
        readonly CheckBox hd = new CheckBox();
        readonly Label status = new Label(); readonly ProgressBar bar = new ProgressBar(); readonly BackgroundWorker worker = new BackgroundWorker();
        bool restoring, restoringHd, wantHd;
        string installed;
        public SetupWindow(string initial)
        {
            Text = "Freelancer Modern Patch Setup"; ClientSize = new Size(700, 410); Font = new Font("Segoe UI", 10);
            AutoScaleMode = AutoScaleMode.Dpi; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; StartPosition = FormStartPosition.CenterScreen;
            Add(new Label { Text = "Install Freelancer Modern Patch", Font = new Font("Segoe UI", 17, FontStyle.Bold) }, 24, 20, 592, 38);
            Add(new Label { Text = "Choose your English retail game folder. Existing Modern Patch installations\ncan add HD textures here. Original files are backed up; saves stay untouched." }, 24, 68, 652, 52);
            Add(new Label { Text = "Freelancer game folder (contains EXE and DATA)" }, 24, 126, 580, 24);
            Add(game, 24, 155, 484, 28); Add(browse, 518, 152, 98, 32); browse.Text = "Browse..."; game.Text = initial ?? "";
            bool available = File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hd-manifest.json"));
            Add(hd, 24, 200, 652, 34); hd.Text = available ? "Install HD textures: interiors, characters and HUD (about 2.4 GB)" : "HD textures available in the optional HD download"; hd.Checked = available; hd.Enabled = available;
            Add(status, 24, 248, 652, 48); status.Text = "Choose the game folder, then click Install.\nUndo HD Textures keeps the core patch. Undo Patch restores both.";
            Add(bar, 24, 306, 652, 18);
            Add(undo, 24, 357, 116, 34); undo.Text = "Undo Patch";
            Add(undoHd, 150, 357, 210, 34); undoHd.Text = "Undo HD Textures";
            Add(play, 388, 357, 142, 34); play.Text = "Play Freelancer"; play.Visible = false;
            Add(install, 540, 357, 136, 34); install.Text = "Install"; AcceptButton = install;
            browse.Click += delegate {
                using (var picker = new FolderBrowserDialog { Description = "Select the Freelancer game folder containing EXE and DATA.", ShowNewFolderButton = false })
                    if (picker.ShowDialog(this) == DialogResult.OK) game.Text = picker.SelectedPath;
            };
            install.Click += delegate { if (installed != null) { Close(); return; } Start(false, false); };
            undo.Click += delegate {
                if (MessageBox.Show(this, "Restore the original game files and remove the core patch and HD textures from this folder?\n\nYour saves will be kept. Current patch files and settings will be retained in the backup folders.", "Undo Modern Patch", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) Start(true, false);
            };
            undoHd.Click += delegate {
                if (MessageBox.Show(this, "Restore the original textures while keeping the core patch, FPS settings and saves?", "Undo HD Textures", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) Start(true, true);
            };
            play.Click += delegate {
                try { Process.Start(new ProcessStartInfo(Path.Combine(installed, (wantHd ? "ModernTextures" : "ModernPatch") + "\\Freelancer-Launcher.exe"), "\"" + installed + "\" Borderless") { WorkingDirectory = installed }); }
                catch (Exception error) { MessageBox.Show(this, error.Message, "Unable to launch"); }
            };
            worker.WorkerReportsProgress = true;
            worker.DoWork += delegate(object sender, DoWorkEventArgs args) {
                string path = (string)args.Argument; int last = -1;
                Action<int, string> progress = delegate(int value, string message) { if (value != last) { worker.ReportProgress(value, message); last = value; } };
                string package = AppDomain.CurrentDomain.BaseDirectory;
                if (restoringHd) PatchEngine.UndoTextures(path, progress);
                else if (restoring) PatchEngine.Undo(path, progress);
                else
                {
                    bool already = PatchEngine.IsModernGame(package, path);
                    if (already && !wantHd) throw new IOException("The core Modern Patch is already installed. Select HD textures to add them, or close setup.");
                    if (!already) PatchEngine.Install(package, path, progress);
                    if (wantHd)
                    {
                        try { PatchEngine.InstallTextures(package, path, progress); }
                        catch (Exception error) { throw new IOException("The core patch is installed. HD textures did not finish: " + error.Message); }
                    }
                }
                args.Result = Installer.Full(path);
            };
            worker.ProgressChanged += delegate(object sender, ProgressChangedEventArgs args) { bar.Value = args.ProgressPercentage; status.Text = (string)args.UserState; };
            worker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs args) {
                EnableInputs(true);
                if (args.Error != null) { status.Text = "The operation did not finish. See the message for details."; MessageBox.Show(this, args.Error.Message, "Setup needs attention", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                installed = (string)args.Result; install.Text = "Close"; game.ReadOnly = true; browse.Enabled = false; play.Visible = !restoring;
                status.Text = restoringHd ? "Original textures restored. Core patch, FPS settings and saves are kept." : restoring ? "Original game files restored. Your saves are kept.\nBackups and the Undo tools remain for reference." : wantHd ? "HD textures are ready! Use Play Freelancer HD.cmd in your game folder.\nUndo HD Textures.cmd restores textures while keeping the core patch." : "Ready to play! Use Play Freelancer.cmd in your game folder next time.\nUndo Modern Patch.cmd restores the original files. Saves stay untouched.";
            };
            FormClosing += delegate(object sender, FormClosingEventArgs args) { if (worker.IsBusy) args.Cancel = true; };
        }
        void Start(bool restore, bool textures) { restoring = restore; restoringHd = textures; wantHd = hd.Checked; EnableInputs(false); play.Visible = false; bar.Value = 0; worker.RunWorkerAsync(game.Text); }
        void EnableInputs(bool value) { game.Enabled = browse.Enabled = install.Enabled = undo.Enabled = undoHd.Enabled = value; hd.Enabled = value && File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hd-manifest.json")); }
        void Add(Control control, int x, int y, int width, int height) { control.SetBounds(x, y, width, height); Controls.Add(control); }
    }
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length == 3 && (args[0] == "--install-check" || args[0] == "--undo-check" || args[0] == "--install-hd-check" || args[0] == "--undo-hd-check"))
            {
                try {
                    if (args[0] == "--install-check") PatchEngine.Install(AppDomain.CurrentDomain.BaseDirectory, args[1], delegate { });
                    else if (args[0] == "--install-hd-check") PatchEngine.InstallTextures(AppDomain.CurrentDomain.BaseDirectory, args[1], delegate { });
                    else if (args[0] == "--undo-hd-check") PatchEngine.UndoTextures(args[1], delegate { });
                    else PatchEngine.Undo(args[1], delegate { });
                    File.WriteAllText(args[2], "OK"); return 0;
                } catch (Exception error) { File.WriteAllText(args[2], error.Message); return 1; }
            }
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupWindow(args.Length == 2 && (args[0] == "--undo" || args[0] == "--undo-hd") ? args[1] : null)); return 0;
        }
    }
}
