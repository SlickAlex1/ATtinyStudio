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
    public class FuseOption {
        public string Name;
        public string L, H, E;
        public bool IsInternal;
    }

    public class ChipConfig {
        public List<FuseOption> Fuses = new List<FuseOption>();
        public int EepSize;
        public string Flash, Sram, Pins, Speed, Layout;
    }
}
