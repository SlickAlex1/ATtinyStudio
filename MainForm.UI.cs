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
        private void InitializeComponent() {
            mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, ColumnCount = 1, BackColor = Theme.ClrBack, Padding = new Padding(0), Margin = new Padding(0) };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 175)); mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 75));  
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180)); 
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12)); mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  

            pnlHeader = new Panel { Name = "pnlHeader", Dock = DockStyle.Fill, BackColor = Theme.ClrHeader, Margin = new Padding(0) };
            pnlHeader.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); } };
            
            PictureBox picIcon = new PictureBox { Size = new Size(110, 110), Left = 35, Top = 25, SizeMode = PictureBoxSizeMode.Zoom };
            try { using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("icon.ico")) { if (s != null) picIcon.Image = new Icon(s, new Size(256, 256)).ToBitmap(); } } catch { }
            picIcon.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); } };

            lblHeaderTitle = new Label { Text = AppMetadata.Title.ToUpper(), Left = 160, Top = 35, AutoSize = true, Font = new Font("Segoe UI Semibold", 48, FontStyle.Bold), ForeColor = Color.White };
            lblHeaderTitle.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); } };
            lblMadeBy = new Label { Text = "BY " + AppMetadata.Author, Left = 168, Top = 125, AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Theme.ClrAccent };
            
            btnExit = new Button { Text = "✕", FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = Color.White, Font = new Font("Segoe UI", 14, FontStyle.Bold), Size = new Size(45, 45), Top = 0, Cursor = Cursors.Hand };
            btnExit.FlatAppearance.BorderSize = 0; btnExit.FlatAppearance.MouseOverBackColor = Color.Firebrick; btnExit.Click += (s, e) => this.Close();
            btnMax = new Button { Text = "▢", FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = Color.White, Font = new Font("Segoe UI", 14, FontStyle.Bold), Size = new Size(45, 45), Top = 0, Cursor = Cursors.Hand };
            btnMax.FlatAppearance.BorderSize = 0; btnMax.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 60); btnMax.Click += (s, e) => { this.WindowState = (this.WindowState == FormWindowState.Maximized) ? FormWindowState.Normal : FormWindowState.Maximized; };
            btnMin = new Button { Text = "—", FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = Color.White, Font = new Font("Segoe UI", 14, FontStyle.Bold), Size = new Size(45, 45), Top = 0, Cursor = Cursors.Hand };
            btnMin.FlatAppearance.BorderSize = 0; btnMin.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 60); btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            this.SizeChanged += (s, e) => { btnExit.Left = this.Width - 45; btnMax.Left = this.Width - 90; btnMin.Left = this.Width - 135; };
            pnlHeader.Controls.AddRange(new Control[] { picIcon, lblHeaderTitle, lblMadeBy, btnExit, btnMax, btnMin });

            ctrlBar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = Color.FromArgb(18, 18, 22), Margin = new Padding(0) };
            ctrlBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); ctrlBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); 
            ctrlBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); ctrlBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  
            cbPorts = new ComboBox { Width = 170, Margin = new Padding(40, 22, 0, 0), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.ClrPanel, ForeColor = Theme.ClrText, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10) };
            Button btnR = CreateButton("REFRESH", 0, 0, 110, Theme.ClrAccent, Color.White); btnR.Margin = new Padding(15, 21, 0, 0); btnR.Click += (s, e) => RefreshPorts();
            btnStop = CreateButton("ABORT", 0, 0, 110, Color.FromArgb(60, 60, 60), Color.White); btnStop.Margin = new Padding(15, 21, 0, 0); btnStop.Enabled = false;
            btnStop.Click += (s, e) => { if (currentProcess != null && !currentProcess.HasExited) try { currentProcess.Kill(); } catch { } isBatchAborted = true; batchCts?.Cancel(); };
            ctrlBar.Controls.AddRange(new Control[] { cbPorts, btnR, btnStop });

            tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(15, 12), DrawMode = TabDrawMode.OwnerDrawFixed, ItemSize = new Size(100, 45), Margin = new Padding(0) };
            tabs.DrawItem += (s, e) => {
                Graphics g = e.Graphics; Rectangle rect = tabs.GetTabRect(e.Index); bool selected = tabs.SelectedIndex == e.Index;
                using (SolidBrush sb = new SolidBrush(selected ? Theme.ClrAccent : Theme.ClrHeader)) g.FillRectangle(sb, rect);
                TextRenderer.DrawText(g, tabs.TabPages[e.Index].Text, new Font("Segoe UI Semibold", 9.5f), rect, selected ? Color.White : Color.Gray, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            };
            tabs.SelectedIndexChanged += (s, e) => { if (tabs.SelectedTab.Text == "CHIP INFO") RefreshChipDynamicInfo(); };
            string[] tNames = { "FLASH", "FUSES", "EEPROM", "BATCH", "TERMINAL", "SNIPPETS", "CHIP INFO", "DRIVERS", "SETTINGS", "PINOUT", "ABOUT" };
            foreach(var n in tNames) tabs.TabPages.Add(new TabPage(n) { BackColor = Theme.ClrBack, Padding = new Padding(0) });
            SetupFlashTab(tabs.TabPages[0]); SetupFusesTab(tabs.TabPages[1]); SetupEEPROMTab(tabs.TabPages[2]); SetupBatchTab(tabs.TabPages[3]); SetupTerminalTab(tabs.TabPages[4]);
            SetupSnippetsTab(tabs.TabPages[5]); SetupChipInfoTab(tabs.TabPages[6]); SetupDriversTab(tabs.TabPages[7]); SetupSettingsTab(tabs.TabPages[8]); SetupPinoutTab(tabs.TabPages[9]); SetupAboutTab(tabs.TabPages[10]);

            txtConsole = new TextBox { Name = "txtConsole", Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Theme.ClrConsole, ForeColor = Theme.ClrLime, Font = new Font("Consolas", 10.5f), BorderStyle = BorderStyle.None, Margin = new Padding(0) };
            progress = new ProgressBar { Dock = DockStyle.Fill, Style = ProgressBarStyle.Continuous, BackColor = Color.Black, ForeColor = Theme.ClrAccent, Margin = new Padding(0) };
            lblStatus = new Label { Dock = DockStyle.Fill, Text = "  SYSTEM READY", TextAlign = ContentAlignment.MiddleLeft, BackColor = Theme.ClrAccent, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(0) };
            mainLayout.Controls.AddRange(new Control[] { pnlHeader, ctrlBar, tabs, txtConsole, progress, lblStatus });
            this.Controls.Add(mainLayout);
        }

        private void SetAbortState(bool active) { 
            if (this.InvokeRequired) { this.Invoke(new Action<bool>(SetAbortState), active); return; } 
            btnStop.Enabled = active; btnStop.BackColor = active ? Color.FromArgb(180, 20, 20) : Color.FromArgb(60, 60, 60); 
            ToggleUI(!active);
            if (!active) { progress.Style = ProgressBarStyle.Continuous; progress.Value = 0; }
        }

        private void ToggleUI(bool enabled) {
            if (this.InvokeRequired) { this.Invoke(new Action<bool>(ToggleUI), enabled); return; }
            tabs.Enabled = enabled;
            cbPorts.Enabled = enabled;
            foreach (Control c in ctrlBar.Controls) if (c != btnStop) c.Enabled = enabled;
            UpdateControlColors(this);
        }

        private void UpdateControlColors(Control parent) {
            foreach (Control c in parent.Controls) {
                if (c is CheckBox || c is Label || c is RadioButton) {
                    if (!c.Enabled) {
                        if (c.Tag == null || !(c.Tag is Color)) c.Tag = c.ForeColor;
                        c.ForeColor = Color.Gray;
                    } else if (c.Tag is Color) {
                        c.ForeColor = (Color)c.Tag;
                    }
                }
                if (c.HasChildren) UpdateControlColors(c);
            }
        }

        private Button CreateButton(string txt, int x, int y, int w, Color bg, Color fg) { Button b = new Button { Text = txt, Left = x, Top = y, Width = w, Height = 34, FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = fg, Font = new Font("Segoe UI", 9, FontStyle.Bold) }; b.FlatAppearance.BorderSize = 0; b.Cursor = Cursors.Hand; return b; }
        
        private Button CreateTile(string t1, string t2, EventHandler onClick) { 
            Button b = new Button { Tag = "tile", Width = 250, Height = 170, Margin = new Padding(20), BackColor = Theme.ClrPanel, ForeColor = Theme.ClrText, FlatStyle = FlatStyle.Flat, Text = t1 + "\n\n" + t2, Cursor = Cursors.Hand, Font = new Font("Segoe UI Semibold", 11.5f), TextAlign = ContentAlignment.MiddleCenter }; 
            b.FlatAppearance.BorderSize = 1; b.FlatAppearance.BorderColor = Color.FromArgb(45, 45, 50); 
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, Theme.ClrAccent); 
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(80, Theme.ClrAccent); 
            b.Click += onClick; return b; 
        }

        private void ApplyChipSettings(string chip) {
            if (chipData.ContainsKey(chip)) {
                var c = chipData[chip]; currentEepromSize = c.EepSize;
                if (lblChipStaticInfo != null) lblChipStaticInfo.Text = string.Format("{0} TECHNICAL SPECIFICATIONS:\n\nCore: 8-bit AVR\nPins: {1}\nFlash Memory: {2}\nEEPROM: {3} Bytes\nSRAM: {4}\nClock Speed: Up to {5}\nVoltage: 2.7V - 5.5V\n\nBasic Pinout Header:\n{6}", chip.ToUpper(), c.Pins, c.Flash, c.EepSize, c.Sram, c.Speed, c.Layout);
                SetupFusesTab(tabs.TabPages[1]);
                UpdateBatchFreqs();
                LoadPinoutImage(chip);
                Log("[CONFIG] Applied settings for " + chip);
            }
        }

        private void UpdateBatchFreqs() {
            if (cbBatchTargetFreq == null) return;
            cbBatchTargetFreq.Items.Clear();
            if (chipData.ContainsKey(selPart)) {
                foreach (var f in chipData[selPart].Fuses) cbBatchTargetFreq.Items.Add(f.Name);
                if (cbBatchTargetFreq.Items.Count > 0) cbBatchTargetFreq.SelectedIndex = 0;
            }
        }

        private void SetupFlashTab(TabPage tp) {
            FlowLayoutPanel f = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(35), BackColor = Theme.ClrBack, WrapContents = true, AutoScroll = true };
            f.Controls.Add(CreateTile("WRITE FLASH", "Upload .HEX to chip", (s, e) => RunAvrdude("-U flash:w:\"$FILE$\":i", true, "HEX|*.hex")));
            f.Controls.Add(CreateTile("READ FLASH", "Backup .HEX from chip", (s, e) => RunAvrdude("-U flash:r:\"$FILE$\":i", false, "HEX|*.hex")));
            f.Controls.Add(CreateTile("VERIFY FLASH", "Compare file vs chip", (s, e) => RunAvrdude("-U flash:v:\"$FILE$\":i", true, "HEX|*.hex")));
            f.Controls.Add(CreateTile("CHIP STATUS", "Read ID and tables", (s, e) => RunAvrdude("-v -U lfuse:r:-:h -U hfuse:r:-:h -U efuse:r:-:h", false, null)));
            f.Controls.Add(CreateTile("FULL ERASE", "Wipe all and reset", (s, e) => { if(Confirm("Erase everything?")) RunAvrdude("-e", false, null); }));
            tp.Controls.Add(f);
        }

        private void SetupFusesTab(TabPage tp) {
            tp.Controls.Clear();
            FlowLayoutPanel f = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(35), BackColor = Theme.ClrBack, WrapContents = true, AutoScroll = true };
            f.Controls.Add(CreateTile("UNLOCK CHIP", "Full Access (0xFF)", (s, e) => RunAvrdude("-U lock:w:0xFF:m", false, null)));
            f.Controls.Add(CreateTile("LOCK CHIP", "Protected (0xFC)", (s, e) => RunAvrdude("-U lock:w:0xFC:m", false, null)));
            if (chipData.ContainsKey(selPart)) {
                var c = chipData[selPart];
                foreach (var opt in c.Fuses.OrderByDescending(x => x.IsInternal)) {
                    string type = opt.IsInternal ? "[INTERNAL]" : "[EXTERNAL]";
                    f.Controls.Add(CreateTile(opt.Name, type, (s, e) => RunAvrdude(string.Format("-U lfuse:w:{0}:m -U hfuse:w:{1}:m -U efuse:w:{2}:m", opt.L, opt.H, opt.E), false, null)));
                }
            }
            Panel cnl = new Panel { Width = 920, Height = 140, Margin = new Padding(30), BackColor = Theme.ClrPanel };
            txtL = new TextBox { Top = 55, Left = 30, Width = 100, Text = "0x62", BackColor = Theme.ClrBack, ForeColor = Theme.ClrText, Font = new Font("Consolas", 12) };
            txtH = new TextBox { Top = 55, Left = 140, Width = 100, Text = "0xDF", BackColor = Theme.ClrBack, ForeColor = Theme.ClrText, Font = new Font("Consolas", 12) };
            txtE = new TextBox { Top = 55, Left = 250, Width = 100, Text = "0xFF", BackColor = Theme.ClrBack, ForeColor = Theme.ClrText, Font = new Font("Consolas", 12) };
            Button bW = CreateButton("WRITE FUSES", 380, 52, 160, Color.FromArgb(45, 45, 55), Color.White);
            bW.Click += (s,e) => RunAvrdude(string.Format("-U lfuse:w:{0}:m -U hfuse:w:{1}:m -U efuse:w:{2}:m", txtL.Text, txtH.Text, txtE.Text), false, null);
            cnl.Controls.AddRange(new Control[] { txtL, txtH, txtE, bW }); f.Controls.Add(cnl); tp.Controls.Add(f);
        }

        private void SetupEEPROMTab(TabPage tp) {
            FlowLayoutPanel f = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(35), BackColor = Theme.ClrBack, WrapContents = true, AutoScroll = true };
            f.Controls.Add(CreateTile("WRITE EEPROM", "Upload .BIN to chip", (s, e) => RunAvrdude("-U eeprom:w:\"$FILE$\":r", true, "BIN|*.bin")));
            f.Controls.Add(CreateTile("READ EEPROM", "Backup .BIN from chip", (s, e) => RunAvrdude("-U eeprom:r:\"$FILE$\":r", false, "BIN|*.bin")));
            f.Controls.Add(CreateTile("LIVE VIEWER", "Hex/ASCII Visualizer", (s, e) => ViewEeprom()));
            f.Controls.Add(CreateTile("WIPE EEPROM", "Reset all to 0xFF", (s, e) => { string wPath = Path.Combine(tempDir, "w.bin"); File.WriteAllBytes(wPath, Enumerable.Repeat((byte)255, currentEepromSize).ToArray()); RunAvrdude("-U eeprom:w:\"" + wPath + "\":r", false, null); }));
            tp.Controls.Add(f);
        }

        private void SetupBatchTab(TabPage tp) {
            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 100 };
            Button btnSearch = CreateButton("SELECT FOLDER", 20, 10, 150, Theme.ClrAccent, Color.White);
            chkBatchVerify = new CheckBox { Text = "Verify", AutoSize = true, ForeColor = Color.White, Top = 15, Left = 180, Checked = true };
            chkBatchErase = new CheckBox { Text = "Erase", AutoSize = true, ForeColor = Color.White, Top = 15, Left = 250, Checked = true };
            chkBatchAuto = new CheckBox { Text = "Auto-Next", AutoSize = true, ForeColor = Color.White, Top = 15, Left = 320, Checked = true };
            Label lbD = new Label { Text = "Delay (s):", ForeColor = Color.Gray, Top = 18, Left = 410, AutoSize = true };
            numBatchDelay = new NumericUpDown { Top = 15, Left = 475, Width = 50, Minimum = 0, Maximum = 60, Value = 2, BackColor = Theme.ClrPanel, ForeColor = Color.White };
            Button btnStartBatch = CreateButton("START BATCH", 550, 10, 120, Color.ForestGreen, Color.White);
            Button btnNextFile = CreateButton("NEXT FILE", 680, 10, 100, Color.DarkCyan, Color.White);
            Button btnDelete = CreateButton("DELETE", 790, 10, 80, Color.Firebrick, Color.White);
            
            chkBatchSaveHistory = new CheckBox { Text = "SAVE BATCH HISTORY", AutoSize = true, ForeColor = Color.White, Top = 55, Left = 20, Checked = false };
            chkBatchLogFull = new CheckBox { Text = "INCLUDE RAW CONSOLE OUTPUT", AutoSize = true, ForeColor = Color.Gray, Top = 55, Left = 200, Checked = false, Enabled = false };
            chkBatchSaveHistory.CheckedChanged += (s, e) => { chkBatchLogFull.Enabled = chkBatchSaveHistory.Checked; chkBatchLogFull.ForeColor = chkBatchSaveHistory.Checked ? Color.White : Color.Gray; };
            
            chkBatchSetFuses = new CheckBox { Text = "SET FUSES/FREQ", AutoSize = true, ForeColor = Theme.ClrAccent, Top = 55, Left = 420, Checked = false };
            cbBatchTargetFreq = new ComboBox { Top = 52, Left = 550, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.ClrPanel, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            UpdateBatchFreqs();

            pnlTop.Controls.AddRange(new Control[] { btnSearch, chkBatchVerify, chkBatchErase, chkBatchAuto, lbD, numBatchDelay, btnStartBatch, btnNextFile, btnDelete, chkBatchSaveHistory, chkBatchLogFull, chkBatchSetFuses, cbBatchTargetFreq });

            dgvBatch = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Theme.ClrPanel, ForeColor = Color.Black, AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, EnableHeadersVisualStyles = false, BorderStyle = BorderStyle.None, GridColor = Color.FromArgb(45, 45, 50), AllowDrop = true, AllowUserToResizeColumns = false, AllowUserToResizeRows = false };
            dgvBatch.DefaultCellStyle.BackColor = Theme.ClrBack; dgvBatch.DefaultCellStyle.ForeColor = Color.White; dgvBatch.ColumnHeadersDefaultCellStyle.BackColor = Theme.ClrHeader; dgvBatch.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvBatch.Columns.Add("ColName", "File Name"); dgvBatch.Columns.Add("ColStatus", "Status"); dgvBatch.Columns.Add("ColPath", "Path"); dgvBatch.Columns["ColPath"].Visible = false;

            dgvBatch.MouseDown += (s, e) => { rowIndexFromMouseDown = dgvBatch.HitTest(e.X, e.Y).RowIndex; if (rowIndexFromMouseDown != -1) dragBoxFromMouseDown = new Rectangle(new Point(e.X - (SystemInformation.DragSize.Width / 2), e.Y - (SystemInformation.DragSize.Height / 2)), SystemInformation.DragSize); else dragBoxFromMouseDown = Rectangle.Empty; };
            dgvBatch.MouseMove += (s, e) => { if ((e.Button & MouseButtons.Left) == MouseButtons.Left && dragBoxFromMouseDown != Rectangle.Empty && !dragBoxFromMouseDown.Contains(e.X, e.Y)) dgvBatch.DoDragDrop(dgvBatch.Rows[rowIndexFromMouseDown], DragDropEffects.Move); };
            dgvBatch.DragOver += (s, e) => e.Effect = DragDropEffects.Move;
            dgvBatch.DragDrop += (s, e) => { Point p = dgvBatch.PointToClient(new Point(e.X, e.Y)); int dropIndex = dgvBatch.HitTest(p.X, p.Y).RowIndex; if (e.Effect == DragDropEffects.Move && dropIndex != -1) { DataGridViewRow r = e.Data.GetData(typeof(DataGridViewRow)) as DataGridViewRow; dgvBatch.Rows.RemoveAt(rowIndexFromMouseDown); dgvBatch.Rows.Insert(dropIndex, r); } };

            btnSearch.Click += (s, e) => { using (FolderBrowserDialog fbd = new FolderBrowserDialog()) if (fbd.ShowDialog() == DialogResult.OK) foreach(var f in Directory.GetFiles(fbd.SelectedPath, "*.hex", SearchOption.AllDirectories)) dgvBatch.Rows.Add(Path.GetFileName(f), "PENDING", f); };
            btnDelete.Click += (s, e) => { if (dgvBatch.SelectedRows.Count > 0 && Confirm("Delete selected rows?")) foreach (DataGridViewRow row in dgvBatch.SelectedRows) dgvBatch.Rows.Remove(row); };

            Func<DataGridViewRow, Task<(bool, string)>> flashRow = async (row) => {
                string path = row.Cells[2].Value.ToString();
                row.Cells[1].Value = "FLASHING..."; row.DefaultCellStyle.BackColor = Color.DarkGoldenrod;
                string fuseCmd = "";
                if (chkBatchSetFuses.Checked && cbBatchTargetFreq.SelectedItem != null) {
                    var opt = chipData[selPart].Fuses.FirstOrDefault(x => x.Name == cbBatchTargetFreq.Text);
                    if (opt != null) fuseCmd = string.Format("-U lfuse:w:{0}:m -U hfuse:w:{1}:m -U efuse:w:{2}:m ", opt.L, opt.H, opt.E);
                }
                string cmd = (chkBatchErase.Checked ? "-e " : "") + fuseCmd + $"-U flash:w:\"{path}\":i " + (chkBatchVerify.Checked ? $"-U flash:v:\"{path}\":i " : "");
                var res = await ExecuteAvrdudeTaskWithOutput(cmd);
                if (res.Success) { row.Cells[1].Value = "SUCCESS"; row.DefaultCellStyle.BackColor = Color.ForestGreen; }
                else { row.Cells[1].Value = "FAILED"; row.DefaultCellStyle.BackColor = Color.Firebrick; }
                return (res.Success, res.Output);
            };

            btnStartBatch.Click += async (s, e) => {
                if (cbPorts.SelectedItem == null || dgvBatch.Rows.Count == 0) return;
                isBatchAborted = false; batchCts = new System.Threading.CancellationTokenSource(); SetAbortState(true);
                StringBuilder log = new StringBuilder(); log.AppendLine($"{AppMetadata.Title} Batch - {DateTime.Now}");
                int start = dgvBatch.SelectedRows.Count > 0 ? dgvBatch.SelectedRows[0].Index : 0;
                for (int i = start; i < dgvBatch.Rows.Count; i++) {
                    if (isBatchAborted) break;
                    lblStatus.Text = $"  FLASHING: {dgvBatch.Rows[i].Cells[0].Value}..."; lblStatus.BackColor = Theme.ClrAccent;
                    dgvBatch.ClearSelection(); dgvBatch.Rows[i].Selected = true;
                    dgvBatch.FirstDisplayedScrollingRowIndex = i;
                    var res = await flashRow(dgvBatch.Rows[i]);
                    log.AppendLine($"[{(res.Item1 ? "OK" : "FAIL")}] {dgvBatch.Rows[i].Cells[0].Value}");
                    if (chkBatchLogFull.Checked) log.AppendLine(res.Item2);
                    if (i < dgvBatch.Rows.Count - 1 && chkBatchAuto.Checked) {
                        lblStatus.Text = $"  WAITING {numBatchDelay.Value}s..."; progress.Style = ProgressBarStyle.Marquee;
                        try { await Task.Delay((int)numBatchDelay.Value * 1000, batchCts.Token); } catch (TaskCanceledException) { break; }
                    } else if (!chkBatchAuto.Checked) break;
                }
                if (chkBatchSaveHistory.Checked) {
                    try { string logDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName); File.WriteAllText(Path.Combine(logDir, "batch_history_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt"), log.ToString()); } catch { }
                }
                lblStatus.Text = isBatchAborted ? "  BATCH ABORTED" : "  BATCH FINISHED";
                lblStatus.BackColor = isBatchAborted ? Color.Firebrick : Color.ForestGreen;
                SetAbortState(false);
            };

            btnNextFile.Click += async (s, e) => {
                if (dgvBatch.Rows.Count == 0) return;
                int idx = 0;
                if (dgvBatch.SelectedRows.Count > 0) idx = (dgvBatch.SelectedRows[0].Index + 1) % dgvBatch.Rows.Count;
                dgvBatch.ClearSelection(); dgvBatch.Rows[idx].Selected = true;
                dgvBatch.FirstDisplayedScrollingRowIndex = idx;
                if (cbPorts.SelectedItem != null) { SetAbortState(true); await flashRow(dgvBatch.Rows[idx]); SetAbortState(false); }
            };
            tp.Controls.AddRange(new Control[] { dgvBatch, pnlTop });
        }

        private void SetupAboutTab(TabPage tp) {
            tp.Controls.Clear();
            FlowLayoutPanel f = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(35), BackColor = Theme.ClrBack, WrapContents = true, AutoScroll = true };
            
            Panel infoBox = new Panel { Width = 900, Height = 820, BackColor = Theme.ClrPanel, Padding = new Padding(40) };
            infoBox.Paint += (s, e) => { ControlPaint.DrawBorder(e.Graphics, infoBox.ClientRectangle, Color.FromArgb(45, 45, 50), ButtonBorderStyle.Solid); };

            StringBuilder sbHead = new StringBuilder();
            sbHead.AppendLine(AppMetadata.Title.ToUpper());
            sbHead.AppendLine("Version " + AppMetadata.Version);
            sbHead.AppendLine("Developer: " + AppMetadata.Author);
            sbHead.AppendLine("License: " + AppMetadata.License);
            sbHead.AppendLine("──────────────────────────────────────────────────────────────");

            Label lblHead = new Label { AutoSize = true, ForeColor = Theme.ClrAccent, Font = new Font("Segoe UI Semibold", 14), Text = sbHead.ToString(), Location = new Point(40, 40), MaximumSize = new Size(820, 0) };
            
            Button btnInstall = CreateButton("INSTALL NOW", 40, 185, 300, Theme.ClrAccent, Color.White);
            btnInstall.Height = 50; btnInstall.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnInstall.Click += (s, e) => { using(var f = new InstallerForm()) { f.ShowDialog(); } };
            if (IsInstalled()) btnInstall.Visible = false;

            StringBuilder sbBody = new StringBuilder();
            sbBody.AppendLine("\nDESCRIPTION:");
            sbBody.AppendLine(AppMetadata.Description);
            sbBody.AppendLine("\nLEGAL POLICY & THIRD-PARTY LICENSES:");
            sbBody.AppendLine("\n1. AVRDUDE");
            sbBody.AppendLine("This software interfaces with AVRDUDE (v" + packedAvrVer + ") for hardware communication. AVRDUDE is an open-source utility licensed under the GNU General Public License (GPL) v2.0 or later. Source code is available at: https://github.com/avrdudes/avrdude");
            sbBody.AppendLine("\n2. .NET RUNTIME & LIBRARIES");
            sbBody.AppendLine("Built on the .NET 10 Framework. System libraries are provided by Microsoft Corporation under the MIT License.");
            sbBody.AppendLine("\n3. HARDWARE DRIVERS");
            sbBody.AppendLine("Drivers for CH340, FTDI, CP210x, and Zadig/libusb are property of their respective manufacturers. These are provided as-is for convenience and subject to their own proprietary or open-source licenses.");
            sbBody.AppendLine("\n4. PROJECT LICENSE (GPLv3)");
            sbBody.AppendLine("ATtiny Studio is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.");
            sbBody.AppendLine("\n" + AppMetadata.Copyright);

            Label lblBody = new Label { AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 10.5f), Text = sbBody.ToString(), Location = new Point(40, btnInstall.Visible ? 245 : 185), MaximumSize = new Size(820, 0) };
            
            infoBox.Controls.AddRange(new Control[] { lblHead, btnInstall, lblBody });
            f.Controls.Add(infoBox); tp.Controls.Add(f);
        }

        private void SetupChipInfoTab(TabPage tp) {
            TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            Panel pL = new Panel { Dock = DockStyle.Fill, Padding = new Padding(40), AutoScroll = true };
            lblChipStaticInfo = new Label { Dock = DockStyle.Top, AutoSize = true, ForeColor = Theme.ClrText, Font = new Font("Consolas", 11), Text = "Loading..." };
            pL.Controls.Add(lblChipStaticInfo);
            
            Panel pR = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.FromArgb(20, 20, 25) };

            btnRetryDetection = CreateButton("DETECT CHIP NOW", 0, 0, 0, Theme.ClrAccent, Color.White);
            btnRetryDetection.Dock = DockStyle.Bottom; btnRetryDetection.Height = 50;
            btnRetryDetection.Click += (s, e) => RefreshChipDynamicInfo();
            
            Panel pScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10) };
            lblChipDynamicInfo = new Label { Dock = DockStyle.Top, AutoSize = true, ForeColor = Color.Yellow, Font = new Font("Consolas", 11), Text = "STATUS: WAITING FOR DETECTION" };
            pScroll.Controls.Add(lblChipDynamicInfo);
            
            pR.Controls.Add(pScroll); pR.Controls.Add(btnRetryDetection);
            t.Controls.AddRange(new Control[] { pL, pR }); tp.Controls.Add(t);
        }

        private void SetupDriversTab(TabPage tp) {
            FlowLayoutPanel f = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(35), WrapContents = true, AutoScroll = true };
            Action<string, string, string, string, bool> ad = (n, d, l, p, e) => f.Controls.Add(CreateTile(n, d + "\n" + l, (s, ev) => {
                try { 
                    string fp = ExtractDriverFolder("Drivers\\" + p); 
                    if (e) Process.Start(new ProcessStartInfo { FileName = fp, UseShellExecute = true, Verb = "runas" }); 
                    else Process.Start("explorer.exe", fp); 
                }
                catch { MessageBox.Show("Failed to launch driver."); }
            }));
            ad("CH340", "Serial Driver", "wch-ic.com", "CH341SER.EXE", true); 
            ad("ZADIG", "USB Filter", "zadig.akeo.ie", "zadig-2.9.exe", true);
            ad("FTDI", "VCP Drivers", "ftdichip.com", "CDM-v2.12.36.20-WHQL-Certified", false); 
            ad("CP210x", "Silicon Labs", "silabs.com", "CP210x_VCP_Windows", false);
            tp.Controls.Add(f);
        }

        private void SetupTerminalTab(TabPage tp) {
            Panel p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(50) };
            txtTerminal = new TextBox { Dock = DockStyle.Top, Height = 40, BackColor = Theme.ClrPanel, ForeColor = Color.White, Font = new Font("Consolas", 14), BorderStyle = BorderStyle.FixedSingle, Text = "-U flash:w:file.hex:i" };
            Button b = CreateButton("EXECUTE RAW COMMAND", 0, 0, 250, Theme.ClrAccent, Color.White);
            b.Dock = DockStyle.Top; b.Height = 50; b.Margin = new Padding(0, 20, 0, 0);
            b.Click += (s, e) => RunAvrdude(txtTerminal.Text, false, null);
            p.Controls.AddRange(new Control[] { b, txtTerminal }); tp.Controls.Add(p);
        }

        private void SetupSnippetsTab(TabPage tp) {
            FlowLayoutPanel f = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(35), BackColor = Theme.ClrBack, WrapContents = true, AutoScroll = true };
            Action<string, string> add = (t, c) => { Button b = CreateTile(t, "Click to copy", (s, e) => { Clipboard.SetText(c); MessageBox.Show("Copied!"); }); f.Controls.Add(b); };
            add("BASIC BLINK", "void setup() {\n  pinMode(0, OUTPUT);\n}\n\nvoid loop() {\n  digitalWrite(0, HIGH);\n  delay(1000);\n  digitalWrite(0, LOW);\n  delay(1000);\n}");
            add("PWM DIMMING", "void setup() {\n  pinMode(1, OUTPUT);\n}\n\nvoid loop() {\n  for (int i = 0; i <= 255; i++) {\n    analogWrite(1, i);\n    delay(10);\n  }\n  for (int i = 255; i >= 0; i--) {\n    analogWrite(1, i);\n    delay(10);\n  }\n}");
            add("ADC READ", "void setup() {\n  pinMode(0, OUTPUT);\n}\n\nvoid loop() {\n  int val = analogRead(A1);\n  digitalWrite(0, HIGH);\n  delay(val);\n  digitalWrite(0, LOW);\n  delay(val);\n}");
            add("SOFT SERIAL", "#include <SoftwareSerial.h>\n\nSoftwareSerial mySerial(3, 4); // RX, TX\n\nvoid setup() {\n  mySerial.begin(9600);\n  mySerial.println(\"ATtiny85 Serial Initialized\");\n}\n\nvoid loop() {\n  mySerial.println(\"Hello from ATtiny85\");\n  delay(1000);\n}");
            add("I2C MASTER", "#include <TinyWireM.h>\n\n#define ADDR 0x27\n\nvoid setup() {\n  TinyWireM.begin();\n}\n\nvoid loop() {\n  TinyWireM.beginTransmission(ADDR);\n  TinyWireM.send(0x01);\n  TinyWireM.endTransmission();\n  delay(500);\n}");
            add("INTERNAL TEMP", "long readTemp() {\n  ADMUX = _BV(REFS1) | _BV(MUX3) | _BV(MUX2) | _BV(MUX1) | _BV(MUX0);\n  delay(2);\n  ADCSRA |= _BV(ADSC);\n  while (bit_is_set(ADCSRA, ADSC));\n  return (high << 8) | low;\n}\n\nvoid setup() {}\n\nvoid loop() {\n  long raw = readTemp();\n  delay(1000);\n}");
            add("EEPROM", "#include <EEPROM.h>\n\nvoid setup() {\n  EEPROM.write(0, 123);\n  byte val = EEPROM.read(0);\n}\n\nvoid loop() {}");
            add("SLEEP WAKE", "#include <avr/sleep.h>\n#include <avr/interrupt.h>\n\nvoid setup() {\n  pinMode(0, OUTPUT); pinMode(2, INPUT_PULLUP);\n  GIMSK |= _BV(PCIE); PCMSK |= _BV(PCINT2); sei();\n}\n\nvoid loop() {\n  digitalWrite(0, HIGH); delay(1000); digitalWrite(0, LOW);\n  set_sleep_mode(SLEEP_MODE_PWR_DOWN);\n  sleep_enable(); sleep_cpu(); sleep_disable();\n}\n\nISR(PCINT0_vect) {}");
            add("WATCHDOG", "#include <avr/wdt.h>\n#include <avr/sleep.h>\n\nvoid setup() {\n  MCUSR &= ~(1 << WDRF);\n  WDTCR |= (1 << WDCE) | (1 << WDE);\n  WDTCR = (1 << WDP3) | (1 << WDP0) | (1 << WDIE);\n}\n\nvoid loop() {\n  pinMode(0, OUTPUT); digitalWrite(0, HIGH); delay(100); digitalWrite(0, LOW); pinMode(0, INPUT);\n  set_sleep_mode(SLEEP_MODE_PWR_DOWN);\n  sleep_enable(); sleep_cpu();\n}\n\nISR(WDT_vect) {}");
            add("NEOPIXEL", "#include <Adafruit_NeoPixel.h>\n\nAdafruit_NeoPixel p(8, 0, NEO_GRB + NEO_KHZ800);\n\nvoid setup() { p.begin(); }\n\nvoid loop() {\n  for(int i=0; i<8; i++) {\n    p.setPixelColor(i, p.Color(150, 0, 0)); p.show(); delay(100);\n  }\n}");
            add("CAP TOUCH", "int readCap(int p) {\n  pinMode(p, OUTPUT); digitalWrite(p, LOW); delay(1);\n  pinMode(p, INPUT); return analogRead(p);\n}\n\nvoid loop() {\n  if (readCap(3) > 500) { /* Touched */ }\n}");
            add("TONE BEEP", "void play(int p, int f, int d) {\n  long prd = 1000000L / f; long cyc = (long)f * d / 1000;\n  for (long i = 0; i < cyc; i++) {\n    digitalWrite(p, HIGH); delayMicroseconds(prd / 2);\n    digitalWrite(p, LOW); delayMicroseconds(prd / 2);\n  }\n}\n\nvoid setup() { pinMode(1, OUTPUT); }\nvoid loop() { play(1, 440, 500); delay(1000); }");
            tp.Controls.Add(f);
        }

        private void SetupSettingsTab(TabPage tp) {
            Panel p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(80), AutoScroll = true };
            int y = 30;
            Action<string, Control> add = (l, c) => { Label lbl = new Label { Text = l, Top = y + 8, Left = 0, Width = 200, ForeColor = Color.Gray, Font = new Font("Segoe UI Semibold", 11) }; c.Top = y; c.Left = 220; c.Width = 380; p.Controls.AddRange(new Control[] { lbl, c }); y += 55; };
            cbProgs = new ComboBox { Width = 380, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.ClrPanel, ForeColor = Theme.ClrText, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10) };
            cbProgs.Items.AddRange(new string[] { "stk500v1", "stk500v2", "usbasp", "usbtiny", "arduino", "avrispmkII", "buspirate", "picoprog", "jtagice" }); cbProgs.SelectedItem = selProg;
            cbProgs.SelectedIndexChanged += (s, e) => { selProg = cbProgs.Text; Log($"[SETTING] Programmer changed to: {selProg}"); };
            cbParts = new ComboBox { Width = 380, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.ClrPanel, ForeColor = Theme.ClrText, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10) };
            cbParts.Items.AddRange(chipData.Keys.ToArray()); cbParts.SelectedItem = selPart;
            cbParts.SelectedIndexChanged += (s, e) => { selPart = cbParts.Text; ApplyChipSettings(selPart); Log($"[SETTING] Chip changed to: {selPart}"); };
            cbBauds = new ComboBox { Width = 380, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.ClrPanel, ForeColor = Theme.ClrText, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10) };
            cbBauds.Items.AddRange(new string[] { "9600", "19200", "38400", "57600", "115200", "250000", "500000" }); cbBauds.SelectedItem = selBaud.ToString();
            cbBauds.SelectedIndexChanged += (s, e) => { int.TryParse(cbBauds.Text, out selBaud); Log($"[SETTING] Baud rate changed to: {selBaud}"); };
            chkVerbose = new CheckBox { Text = "Verbose (-v)", ForeColor = Color.White, Checked = s_Verbose, AutoSize = true }; chkVerbose.CheckedChanged += (s, e) => s_Verbose = chkVerbose.Checked;
            chkForce = new CheckBox { Text = "Force (-F)", ForeColor = Color.White, Checked = s_Force, AutoSize = true }; chkForce.CheckedChanged += (s, e) => s_Force = chkForce.Checked;
            txtBitClock = new TextBox { Width = 100, BackColor = Theme.ClrPanel, ForeColor = Theme.ClrText, Text = s_BitClock, Font = new Font("Consolas", 11) }; txtBitClock.TextChanged += (s, e) => s_BitClock = txtBitClock.Text;
            add("PROGRAMMER:", cbProgs); add("TARGET CHIP:", cbParts); add("BAUD RATE:", cbBauds); add("VERBOSE:", chkVerbose); add("FORCE:", chkForce); add("BIT CLOCK:", txtBitClock);
            tp.Controls.Add(p);
        }

        private void SetupPinoutTab(TabPage tp) {
            picPinout = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black, Padding = new Padding(20) };
            LoadPinoutImage("default");
            tp.Controls.Add(picPinout);
        }

        private void LoadPinoutImage(string chip) {
            if (picPinout == null) return;
            string folder = chip;
            if (chip == "attiny85" || chip == "attiny45" || chip == "attiny25") folder = "attiny85_45_25";
            try {
                string foundRes = null;
                foreach (string res in Assembly.GetExecutingAssembly().GetManifestResourceNames()) {
                    if (res.StartsWith($"Pinouts\\{folder}\\", StringComparison.OrdinalIgnoreCase)) { foundRes = res; break; }
                }
                if (foundRes != null) {
                    Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(foundRes);
                    if (s != null) {
                        if (picPinout.Image != null) picPinout.Image.Dispose();
                        picPinout.Image = Image.FromStream(s);
                    }
                } else {
                    if (picPinout.Image != null) { picPinout.Image.Dispose(); picPinout.Image = null; }
                }
            } catch { }
        }
    }
}
