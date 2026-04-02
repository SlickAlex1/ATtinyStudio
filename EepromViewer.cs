using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace AttinyStudio
{
    public class EepromViewer : Form
    {
        public EepromViewer(byte[] data, Icon icon)
        {
            this.Icon = icon;
            this.Text = "EEPROM Memory Visualizer";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.ClrConsole;

            TextBox box = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10.5f),
                BackColor = Theme.ClrConsole,
                ForeColor = Theme.ClrLime,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(20)
            };

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(" OFFSET | HEXADECIMAL DATA" + new string(' ', 22) + "| ASCII TEXT");
            sb.AppendLine(new string('=', 85));
            for (int i = 0; i < data.Length; i += 16)
            {
                sb.Append(string.Format(" {0:X4}  | ", i));
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < data.Length) sb.Append(string.Format("{0:X2} ", data[i + j]));
                    else sb.Append("   ");
                }
                sb.Append("| ");
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < data.Length)
                    {
                        char c = (char)data[i + j];
                        sb.Append((c < 32 || c > 126) ? '.' : c);
                    }
                }
                sb.AppendLine();
            }
            box.Text = sb.ToString();
            this.Controls.Add(box);
        }
    }
}
