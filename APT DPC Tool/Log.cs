using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APT_DPC_Tool
{
    public static class Log
    {
       public static IProgress<object> progress;
        public static void Print(object text)
        {
            progress.Report(text);
        }
        public static void PrintLine(object text)
        {
            progress.Report(text+"\n");
        }
    }
}
