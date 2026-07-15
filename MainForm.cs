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
    public partial class MainForm : Form
    {
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private string tempDir = Path.Combine(Path.GetTempPath(), AppMetadata.Title.Replace(" ", "") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        private string avrdudePath;
        private string avrdudeConfPath;
        private string packedAvrVer = "8.1";
        private Process currentProcess = null;
        private Task extractTask;
        public Icon AppIcon = null;

        private string selPart = "attiny85", selProg = "stk500v1";
        private int selBaud = 19200;
        private int currentEepromSize = 512;
        private bool s_Verbose = false, s_Force = false;
        private string s_BitClock = "";

        private ComboBox cbPorts, cbParts, cbProgs, cbBauds;
        private TextBox txtConsole, txtL, txtH, txtE, txtTerminal, txtBitClock;
        private Label lblStatus, lblHeaderTitle, lblMadeBy, lblChipDynamicInfo, lblChipStaticInfo;
        private ProgressBar progress;
        private PictureBox picPinout;
        private Button btnStop, btnExit, btnMin, btnMax, btnRetryDetection;
        private TabControl tabs;
        private Panel pnlHeader;
        private TableLayoutPanel mainLayout, ctrlBar;
        private CheckBox chkVerbose, chkForce;

        private DataGridView dgvBatch;
        private CheckBox chkBatchVerify, chkBatchErase, chkBatchAuto, chkBatchLogFull, chkBatchSaveHistory, chkBatchSetFuses;
        private ComboBox cbBatchTargetFreq;
        private NumericUpDown numBatchDelay;
        private Rectangle dragBoxFromMouseDown;
        private int rowIndexFromMouseDown;
        private bool isBatchAborted = false;
        private System.Threading.CancellationTokenSource batchCts = null;

        private Dictionary<string, ChipConfig> chipData = new Dictionary<string, ChipConfig>();

        public MainForm() {
            InitChipData();
            this.Opacity = 0; this.DoubleBuffered = true;
            this.BackColor = Theme.ClrBack; this.ForeColor = Theme.ClrText;
            this.Size = new Size(1150, 1000); this.MinimumSize = new Size(1000, 850);
            this.FormBorderStyle = FormBorderStyle.None;
            this.Text = AppMetadata.Title; this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9);
            LoadSettings(); SetupIcon();
            extractTask = Task.Run(() => ExtractAllAvrdudeFiles());
            InitializeComponent(); RefreshPorts(); ApplyChipSettings(selPart);
            Log($"[SYSTEM] App Initialized.");
            Log($"[SYSTEM] Temp Directory: {tempDir}");
            Timer startFade = new Timer { Interval = 15 };
            startFade.Tick += (s, e) => { if (this.Opacity < 1) this.Opacity += 0.05; else startFade.Stop(); };
            startFade.Start();
        }

        private void InitChipData() {
            var t13 = new ChipConfig { EepSize=64, HasEfuse=false, Flash="1 KB", Sram="64B", Pins="8", Speed="9.6MHz", Layout="1:RST 8:VCC\n2:PB3 7:PB2\n3:PB4 6:PB1\n4:GND 5:PB0" };
            t13.Fuses.Add(new FuseOption { Name="1.2 MHz", L="0x6A", H="0xFF", E="0xFF", IsInternal=true });
            t13.Fuses.Add(new FuseOption { Name="4.8 MHz", L="0x79", H="0xFF", E="0xFF", IsInternal=true });
            t13.Fuses.Add(new FuseOption { Name="9.6 MHz", L="0x7A", H="0xFF", E="0xFF", IsInternal=true });
            t13.Fuses.Add(new FuseOption { Name="Ext. Clock", L="0x60", H="0xFF", E="0xFF", IsInternal=false });
            chipData["attiny13"] = t13;

            var t85 = new ChipConfig { EepSize=512, Flash="8 KB", Sram="512B", Pins="8", Speed="20MHz", Layout="1:RST 8:VCC\n2:PB3 7:PB2\n3:PB4 6:PB1\n4:GND 5:PB0" };
            t85.Fuses.Add(new FuseOption { Name="1 MHz Internal", L="0x62", H="0xDF", E="0xFF", IsInternal=true });
            t85.Fuses.Add(new FuseOption { Name="8 MHz Internal", L="0xE2", H="0xDF", E="0xFF", IsInternal=true });
            t85.Fuses.Add(new FuseOption { Name="16 MHz PLL (4ms Startup)", L="0xF1", H="0xDF", E="0xFF", IsInternal=true });
            t85.Fuses.Add(new FuseOption { Name="16.5 MHz PLL (64ms Startup)", L="0xE1", H="0xDF", E="0xFF", IsInternal=true });
            t85.Fuses.Add(new FuseOption { Name="Ext. Clock", L="0x60", H="0xDF", E="0xFF", IsInternal=false });
            t85.Fuses.Add(new FuseOption { Name="Ext. Crystal", L="0xEF", H="0xDF", E="0xFF", IsInternal=false });
            chipData["attiny85"] = t85;
            var t45 = new ChipConfig { EepSize=256, Flash="4 KB", Sram="256B", Pins=t85.Pins, Speed=t85.Speed, Layout=t85.Layout };
            t45.Fuses.AddRange(t85.Fuses);
            chipData["attiny45"] = t45;
            var t25 = new ChipConfig { EepSize=128, Flash="2 KB", Sram="128B", Pins=t85.Pins, Speed=t85.Speed, Layout=t85.Layout };
            t25.Fuses.AddRange(t85.Fuses);
            chipData["attiny25"] = t25;

            var m328 = new ChipConfig { EepSize=1024, Flash="32 KB", Sram="2KB", Pins="28", Speed="20MHz", Layout="1:RST 7:VCC\n8:GND 17:MOSI\n18:MISO 19:SCK" };
            m328.Fuses.Add(new FuseOption { Name="1 MHz", L="0x62", H="0xD9", E="0xFF", IsInternal=true });
            m328.Fuses.Add(new FuseOption { Name="8 MHz", L="0xE2", H="0xD9", E="0xFF", IsInternal=true });
            m328.Fuses.Add(new FuseOption { Name="Ext. Crystal", L="0xFF", H="0xD9", E="0xFF", IsInternal=false });
            chipData["atmega328p"] = m328;
            var m168 = new ChipConfig { EepSize=512, Flash="16 KB", Sram="1KB", Pins=m328.Pins, Speed=m328.Speed, Layout=m328.Layout };
            m168.Fuses.AddRange(m328.Fuses);
            chipData["atmega168"] = m168;
            var m88 = new ChipConfig { EepSize=512, Flash="8 KB", Sram="1KB", Pins=m328.Pins, Speed=m328.Speed, Layout=m328.Layout };
            m88.Fuses.AddRange(m328.Fuses);
            chipData["atmega88"] = m88;
            var m48 = new ChipConfig { EepSize=256, Flash="4 KB", Sram="512B", Pins=m328.Pins, Speed=m328.Speed, Layout=m328.Layout };
            m48.Fuses.AddRange(m328.Fuses);
            chipData["atmega48"] = m48;

            var m2560 = new ChipConfig { EepSize=4096, Flash="256 KB", Sram="8KB", Pins="100", Speed="16MHz", Layout="30:RST 10:VCC\n11:GND 21:MOSI\n22:MISO 20:SCK" };
            m2560.Fuses.Add(new FuseOption { Name="8 MHz", L="0xE2", H="0xD9", E="0xFD", IsInternal=true });
            m2560.Fuses.Add(new FuseOption { Name="Ext. Crystal", L="0xFF", H="0xD9", E="0xFD", IsInternal=false });
            chipData["atmega2560"] = m2560;

            var m32u4 = new ChipConfig { EepSize=1024, Flash="32 KB", Sram="2.5KB", Pins="44", Speed="16MHz", Layout="13:RST 14:VCC\n15:GND 10:MOSI\n11:MISO 9:SCK" };
            m32u4.Fuses.Add(new FuseOption { Name="8 MHz Internal", L="0xE2", H="0xD9", E="0xF3", IsInternal=true });
            m32u4.Fuses.Add(new FuseOption { Name="Ext. Crystal", L="0xFF", H="0xD9", E="0xF3", IsInternal=false });
            chipData["atmega32u4"] = m32u4;
        }

        protected override void OnFormClosing(FormClosingEventArgs e) { SaveSettings(); CleanupAndExit(); base.OnFormClosing(e); }
        private void CleanupAndExit() { try { if (currentProcess != null && !currentProcess.HasExited) currentProcess.Kill(); GC.Collect(); GC.WaitForPendingFinalizers(); if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { } }
        
        private bool IsInstalled() { 
            try {
                string dir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                string stateFile = Path.Combine(dir, "app.state");
                if (File.Exists(stateFile)) return File.ReadAllText(stateFile).ToLower().Contains("installed");
            } catch { }
            return false;
        }

        private void LoadSettings() {
            if (!IsInstalled()) return;
            try {
                string ini = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "settings.ini");
                if (File.Exists(ini)) {
                    foreach (var line in File.ReadAllLines(ini)) {
                        var p = line.Split('=');
                        if (p.Length == 2) {
                            if (p[0] == "Chip") selPart = p[1]; else if (p[0] == "Prog") selProg = p[1];
                            else if (p[0] == "Baud") int.TryParse(p[1], out selBaud); else if (p[0] == "Verbose") bool.TryParse(p[1], out s_Verbose);
                            else if (p[0] == "Force") bool.TryParse(p[1], out s_Force); else if (p[0] == "BitClock") s_BitClock = p[1];
                        }
                    }
                }
            } catch { }
            if (!chipData.ContainsKey(selPart)) selPart = "attiny85";
            if (selBaud <= 0) selBaud = 19200;
        }

        private void SaveSettings() {
            if (!IsInstalled()) return;
            try {
                string ini = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "settings.ini");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Chip=" + selPart); sb.AppendLine("Prog=" + selProg); sb.AppendLine("Baud=" + selBaud);
                sb.AppendLine("Verbose=" + s_Verbose); sb.AppendLine("Force=" + s_Force); sb.AppendLine("BitClock=" + s_BitClock);
                File.WriteAllText(ini, sb.ToString());
            } catch { }
        }

        private void SetupIcon() { 
            try { 
                // HD Icon Extraction
                AppIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); 
                this.Icon = AppIcon; 
            } catch { } 
        }

    }
}
