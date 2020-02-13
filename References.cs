using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KB.Configuration;

namespace Com0com.Redirector
{
    public class References
    {
        public static References Default { get; set; } = Ini.Default.LoadProperties(new References()) as References;
        public string Setupc { get; set; } = @"C:\Program Files (x86)\com0com\setupc.exe";
        public string Setupg { get; set; } = @"C:\Program Files (x86)\com0com\setupg.exe";
        public string RFC2217 { get; set; } = @"C:\Program Files (x86)\com0com\hub4com\com2tcp-rfc2217.bat";
        public string Com2tcp { get; set; } = @"C:\Program Files (x86)\com0com\hub4com\com2tcp.exe";
        public string Hub4com { get; set; } = @"C:\Program Files (x86)\com0com\hub4com\hub4com.exe";
    }
}
