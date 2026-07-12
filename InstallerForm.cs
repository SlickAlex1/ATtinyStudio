using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AttinyStudio
{
    public class InstallerForm : Form
    {
        private ProgressBar pb;
        private Label lblStatus;
        private Button btnFinish;

        public InstallerForm() {
            this.Size = new Size(500, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(25, 25, 30);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 10);

            Label lblTitle = new Label { Text = "ATtiny Studio Setup", Font = new Font("Segoe UI Semibold", 16), ForeColor = Color.FromArgb(0, 180, 220), AutoSize = true, Top = 20, Left = 30 };
            
            lblStatus = new Label { Text = "Initializing...", Top = 80, Left = 30, AutoSize = true, ForeColor = Color.LightGray };
            
            pb = new ProgressBar { Top = 120, Left = 30, Width = 440, Height = 10, Style = ProgressBarStyle.Marquee };

            btnFinish = new Button { Text = "FINISH", Top = 160, Left = 175, Width = 150, Height = 40, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 180, 220), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand, Visible = false };
            btnFinish.FlatAppearance.BorderSize = 0;
            btnFinish.Click += (s, e) => {
                this.Close();
                Application.Exit();
            };

            this.Controls.AddRange(new Control[] { lblTitle, lblStatus, pb, btnFinish });
            this.Paint += (s, e) => { ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, Color.FromArgb(45, 45, 50), ButtonBorderStyle.Solid); };
            
            this.Load += async (s, e) => await RunInstall();
        }

        private async Task RunInstall() {
            await Task.Delay(500); // Allow UI to render
            try {
                lblStatus.Text = "Copying application files...";
                string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string target = Path.Combine(baseDir, AppMetadata.Title);
                if (!Directory.Exists(target)) Directory.CreateDirectory(target);
                
                string currentPath = Process.GetCurrentProcess().MainModule.FileName;
                string finalExe = Path.Combine(target, AppMetadata.ExeName);
                
                await Task.Run(() => {
                    File.Copy(currentPath, finalExe, true);
                    File.WriteAllText(Path.Combine(target, "app.state"), "installed");
                });

                lblStatus.Text = "Registering Uninstaller...";
                await Task.Run(() => {
                    try {
                        using (Stream s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("uninstall.bat")) {
                            if (s != null) {
                                using (FileStream fs = new FileStream(Path.Combine(target, "uninstall.bat"), FileMode.Create)) { s.CopyTo(fs); }
                            }
                        }
                    } catch { }
                });

                lblStatus.Text = "Creating registry keys...";
                await Task.Run(() => {
                    string regPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\ATtinyStudio";
                    using (var key = Registry.CurrentUser.CreateSubKey(regPath)) {
                        key.SetValue("DisplayName", AppMetadata.Title);
                        key.SetValue("DisplayVersion", AppMetadata.Version);
                        key.SetValue("Publisher", AppMetadata.Author);
                        key.SetValue("InstallLocation", target);
                        key.SetValue("DisplayIcon", finalExe);
                        key.SetValue("UninstallString", "cmd.exe /c \"" + Path.Combine(target, "uninstall.bat") + "\"");
                        key.SetValue("NoModify", 1);
                        key.SetValue("NoRepair", 1);
                    }
                });

                lblStatus.Text = "Creating Desktop Shortcut...";
                await Task.Run(() => {
                    string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string ps = string.Format("$s=(New-Object -ComObject WScript.Shell).CreateShortcut('{0}\\{1}.lnk');$s.TargetPath='{2}';$s.Save()", desktop, AppMetadata.Title, finalExe);
                    ProcessStartInfo psi = new ProcessStartInfo("powershell", "-NoProfile -Command \"" + ps + "\"") { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
                    using(Process p = Process.Start(psi)) { p.WaitForExit(); }
                });

                lblStatus.Text = "Installation Successful!";
                lblStatus.ForeColor = Color.Lime;
                pb.Style = ProgressBarStyle.Continuous;
                pb.Value = 100;
                btnFinish.Visible = true;
            } catch (Exception ex) { 
                lblStatus.Text = "Install failed: " + ex.Message;
                lblStatus.ForeColor = Color.Firebrick;
                pb.Style = ProgressBarStyle.Continuous;
                pb.Value = 0;
                btnFinish.Text = "CLOSE";
                btnFinish.Visible = true;
            }
        }
    }
}
