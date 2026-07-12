using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Runtime.InteropServices;


namespace AttinyStudio
{
    public partial class MainForm
    {
        private async void RefreshChipDynamicInfo() {
            if (cbPorts.SelectedItem == null) { lblChipDynamicInfo.Text = "NO PORT SELECTED"; lblChipDynamicInfo.ForeColor = Color.Firebrick; return; }
            lblChipDynamicInfo.Text = "ATTEMPTING DETECTION..."; lblChipDynamicInfo.ForeColor = Color.Cyan; btnRetryDetection.Enabled = false;
            string extra = (s_Verbose ? " -v" : "") + (s_Force ? " -F" : "") + (!string.IsNullOrWhiteSpace(s_BitClock) ? " -B " + s_BitClock : "");
            string full = string.Format("-C \"{0}\" -c {1} -p {2} -P {3} -b {4} {5} -v", avrdudeConfPath, selProg, selPart, cbPorts.SelectedItem, selBaud, extra);
            await Task.Run(() => {
                try {
                    Process p = new Process { StartInfo = new ProcessStartInfo { FileName = avrdudePath, Arguments = full, UseShellExecute = false, RedirectStandardError = true, CreateNoWindow = true, WorkingDirectory = tempDir } };
                    p.Start(); string output = p.StandardError.ReadToEnd(); p.WaitForExit();
                    this.Invoke((System.Windows.Forms.MethodInvoker)delegate {
                        if (p.ExitCode == 0) { lblChipDynamicInfo.ForeColor = Color.Lime; lblChipDynamicInfo.Text = "CHIP OK\n\n" + output; }
                        else { lblChipDynamicInfo.ForeColor = Color.Firebrick; lblChipDynamicInfo.Text = "DETECTION FAILED\n\n- No Chip\n- Loose Pins\n- Unpowered\n- Wrong Baud"; }
                        btnRetryDetection.Enabled = true;
                    });
                } catch { this.Invoke((System.Windows.Forms.MethodInvoker)delegate { btnRetryDetection.Enabled = true; }); }
            });
        }

        private void RefreshPorts() {
            string current = cbPorts.SelectedItem?.ToString();
            cbPorts.Items.Clear();
            foreach (string p in SerialPort.GetPortNames()) cbPorts.Items.Add(p);
            if (current != null && cbPorts.Items.Contains(current)) cbPorts.SelectedItem = current;
            else if (cbPorts.Items.Count > 0) cbPorts.SelectedIndex = 0;
        }
        private void Log(string m) { if(this.InvokeRequired) { this.Invoke(new Action<string>(Log), m); return; } txtConsole.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + m + Environment.NewLine); }
        private bool Confirm(string m) { return MessageBox.Show(m, "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes; }

        private string ExtractResource(string resourceName) {
            if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
            string outPath = Path.Combine(tempDir, resourceName.Replace("/", "\\"));
            if (File.Exists(outPath)) return outPath;
            try {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)) {
                    if (s != null) {
                        string dir = Path.GetDirectoryName(outPath);
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        using (FileStream fs = new FileStream(outPath, FileMode.Create)) { s.CopyTo(fs); }
                    }
                }
            } catch { }
            return outPath;
        }

        private void ExtractAllAvrdudeFiles() {
            if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
            foreach (string res in Assembly.GetExecutingAssembly().GetManifestResourceNames()) {
                string rLower = res.ToLower();
                if (rLower.StartsWith("drivers")) continue;
                if ((rLower.EndsWith(".exe")) || 
                    rLower.EndsWith(".conf") || rLower.EndsWith(".dll")) {
                    string ext = ExtractResource(res);
                    if (rLower.EndsWith("avrdude.exe")) avrdudePath = ext;
                    if (rLower.EndsWith("avrdude.conf")) avrdudeConfPath = ext;
                }
            }
        }

        public string ExtractDriverFolder(string folderPrefix) {
            if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
            bool found = false;
            foreach (string res in Assembly.GetExecutingAssembly().GetManifestResourceNames()) {
                if (res.StartsWith(folderPrefix + "\\", StringComparison.OrdinalIgnoreCase) || res.StartsWith(folderPrefix + "/", StringComparison.OrdinalIgnoreCase)) {
                    ExtractResource(res);
                    found = true;
                }
            }
            if (found) return Path.Combine(tempDir, folderPrefix.Replace("/", "\\"));
            return ExtractResource(folderPrefix);
        }


        private async void ViewEeprom() {
            if (cbPorts.SelectedItem == null) return;
            string op = Path.Combine(tempDir, "live_eeprom.bin");
            lblStatus.Text = "  READING..."; lblStatus.BackColor = Theme.ClrAccent; progress.Style = ProgressBarStyle.Marquee; SetAbortState(true);
            await ExecuteAvrdudeTaskWithOutput($"-U eeprom:r:\"{op}\":r");
            if (File.Exists(op)) new EepromViewer(File.ReadAllBytes(op), this.Icon).Show();
            lblStatus.Text = "  READY"; progress.Style = ProgressBarStyle.Continuous; SetAbortState(false);
        }

        private async void RunAvrdude(string args, bool isFile, string filter, string input = null) {
            if (cbPorts.SelectedItem == null) return;
            string fArgs = args;
            if (args.Contains("$FILE$")) {
                if (isFile) { var ofd = new OpenFileDialog { Filter = filter }; if (ofd.ShowDialog() == DialogResult.OK) fArgs = args.Replace("$FILE$", ofd.FileName); else return; }
                else { var sfd = new SaveFileDialog { Filter = filter }; if (sfd.ShowDialog() == DialogResult.OK) fArgs = args.Replace("$FILE$", sfd.FileName); else return; }
            }
            lblStatus.Text = "  PROCESSING..."; SetAbortState(true); progress.Style = ProgressBarStyle.Marquee;
            var result = await ExecuteAvrdudeTaskWithOutput(fArgs, input);
            lblStatus.Text = result.Success ? "  SUCCESS" : "  FAILED"; lblStatus.BackColor = result.Success ? Color.ForestGreen : Color.Firebrick;
            progress.Style = ProgressBarStyle.Continuous; SetAbortState(false); currentProcess = null;
        }

        private async Task<(bool Success, string Output)> ExecuteAvrdudeTaskWithOutput(string args, string input = null) {
            this.Invoke((System.Windows.Forms.MethodInvoker)delegate { progress.Style = ProgressBarStyle.Marquee; });
            string extra = (s_Verbose ? " -v" : "") + (s_Force ? " -F" : "") + (!string.IsNullOrWhiteSpace(s_BitClock) ? " -B " + s_BitClock : "");
            string full = string.Format("-C \"{0}\" -c {1} -p {2} -P {3} -b {4} {5} {6}", avrdudeConfPath, selProg, selPart, cbPorts.SelectedItem, selBaud, extra, args);
            Log("> avrdude " + args); StringBuilder raw = new StringBuilder();
            bool ok = await Task.Run(() => {
                try {
                    currentProcess = new Process { StartInfo = new ProcessStartInfo { FileName = avrdudePath, Arguments = full, UseShellExecute = false, RedirectStandardError = true, RedirectStandardInput = input != null, CreateNoWindow = true, WorkingDirectory = tempDir } };
                    currentProcess.Start(); if (input != null) { currentProcess.StandardInput.Write(input); currentProcess.StandardInput.Flush(); }
                    StreamReader sr = currentProcess.StandardError;
                    while (!sr.EndOfStream) { string line = sr.ReadLine(); if (line == null) break; raw.AppendLine(line); this.Invoke((System.Windows.Forms.MethodInvoker)delegate { Log(line); }); }
                    currentProcess.WaitForExit(); return currentProcess.ExitCode == 0;
                } catch { return false; }
            });
            return (ok, raw.ToString());
        }
    }
}
