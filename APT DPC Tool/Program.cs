using APT_DPC_Tool.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace APT_DPC_Tool
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {

           Log.progress=new Progress<object>(Text=>Console.Write(Text));
           Application.EnableVisualStyles();
           Application.SetCompatibleTextRenderingDefault(false);
           Application.Run(new FrmMain());
     

        }
    }
}
