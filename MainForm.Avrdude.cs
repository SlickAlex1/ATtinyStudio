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
        private string FuseWriteCmd(FuseOption opt) { return FuseWriteCmd(opt.L, opt.H, opt.E); }
        private string FuseWriteCmd(string l, string h, string e) {
            bool hasE = chipData.ContainsKey(selPart) && chipData[selPart].HasEfuse;
            return string.Format("-U lfuse:w:{0}:m -U hfuse:w:{1}:m", l, h) + (hasE ? string.Format(" -U efuse:w:{0}:m", e) : "");
        }

        private async void RefreshChipDynamicInfo() {
            if (cbPorts.SelectedItem == null) { lblChipDynamicInfo.Text = "NO PORT SELECTED"; lblChipDynamicInfo.ForeColor = Color.Firebrick; return; }
            lblChipDynamicInfo.Text = "ATTEMPTING DETECTION..."; lblChipDynamicInfo.ForeColor = Color.Cyan; btnRetryDetection.Enabled = false;
            if (extractTask != null) await extractTask;
            string extra = (s_Force ? " -F" : "") + (!string.IsNullOrWhiteSpace(s_BitClock) ? " -B " + s_BitClock : "");
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
            string single = ExtractResource(folderPrefix);
            return File.Exists(single) ? single : null;
        }


        private async void ViewEeprom() {
            if (cbPorts.SelectedItem == null) return;
            string op = Path.Combine(tempDir, "live_eeprom.bin");
            try { if (File.Exists(op)) File.Delete(op); } catch { }
            lblStatus.Text = "  READING..."; lblStatus.BackColor = Theme.ClrAccent; progress.Style = ProgressBarStyle.Marquee; SetAbortState(true);
            var res = await ExecuteAvrdudeTaskWithOutput($"-U eeprom:r:\"{op}\":r");
            if (res.Success && File.Exists(op)) { lblStatus.Text = "  READY"; lblStatus.BackColor = Theme.ClrAccent; new EepromViewer(File.ReadAllBytes(op), this.Icon).Show(this); }
            else { lblStatus.Text = "  FAILED"; lblStatus.BackColor = Color.Firebrick; Log("[ERROR] EEPROM read failed."); }
            progress.Style = ProgressBarStyle.Continuous; SetAbortState(false);
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
            if (extractTask != null) await extractTask;
            if (avrdudePath == null || !File.Exists(avrdudePath)) { Log("[ERROR] avrdude.exe could not be extracted. Check disk space / antivirus quarantine."); return (false, ""); }
            this.Invoke((System.Windows.Forms.MethodInvoker)delegate { progress.Style = ProgressBarStyle.Marquee; });
            string extra = (s_Verbose ? " -v" : "") + (s_Force ? " -F" : "") + (!string.IsNullOrWhiteSpace(s_BitClock) ? " -B " + s_BitClock : "");
            string full = string.Format("-C \"{0}\" -c {1} -p {2} -P {3} -b {4} {5} {6}", avrdudeConfPath, selProg, selPart, cbPorts.SelectedItem, selBaud, extra, args);
            Log("> avrdude " + args); StringBuilder raw = new StringBuilder();
            bool ok = await Task.Run(() => {
                try {
                    currentProcess = new Process { StartInfo = new ProcessStartInfo { FileName = avrdudePath, Arguments = full, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = input != null, CreateNoWindow = true, WorkingDirectory = tempDir } };
                    DataReceivedEventHandler onLine = (s, e) => {
                        if (e.Data == null) return;
                        lock (raw) raw.AppendLine(e.Data);
                        try { this.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate { Log(e.Data); }); } catch { }
                    };
                    currentProcess.OutputDataReceived += onLine; currentProcess.ErrorDataReceived += onLine;
                    currentProcess.Start();
                    currentProcess.BeginOutputReadLine(); currentProcess.BeginErrorReadLine();
                    if (input != null) { currentProcess.StandardInput.Write(input); currentProcess.StandardInput.Flush(); currentProcess.StandardInput.Close(); }
                    currentProcess.WaitForExit(); return currentProcess.ExitCode == 0;
                } catch { return false; }
            });
            lock (raw) return (ok, raw.ToString());
        }
    }
}
