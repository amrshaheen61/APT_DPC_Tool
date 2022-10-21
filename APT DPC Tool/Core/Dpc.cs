using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helper.binaryreader;
namespace APT_DPC_Tool.Core
{

    public class Dpc
    {
        public struct Name
        {
            public string ID;
            public string FileName;
        }
        public static List<Name> Names = new List<Name>();

        Game game;
        string Path;
        string CommonPath;
        public static binaryreader CommonDpc;


        public Dpc(string Path, Game game,string CommonPath="")
        {
            this.Path = Path;
            this.game = game;
            this.CommonPath = CommonPath;
            if (File.Exists("Names.FileList"))
            {
              foreach(string line in File.ReadAllLines("Names.FileList"))
                {
                    if (line.Contains("="))
                    {

                        string[] parts = line.Split('=');

                        Name  name = new Name();
                        name.ID = parts[0].Trim();
                        name.FileName = parts[1].Trim();
                        Names.Add(name);
                    }
                }
               
            }

        }

      public void Unpack(string Folder)
        {
            if (!string.IsNullOrEmpty(CommonPath))
            {
                CommonDpc = new binaryreader(new FileStream(CommonPath, FileMode.Open, FileAccess.Read));
            }

            binaryreader DPCFILE = new binaryreader(new FileStream(this.Path, FileMode.Open, FileAccess.Read));

            switch (game)
            {
                case Game.Innocence:
                    {
                        DpcHelper.ExportFiles(DPCFILE, Folder, Game.Innocence);
                        break;
                    }
                case Game.Requiem:
                    {
                        DpcHelper.ExportFiles(DPCFILE, Folder, Game.Requiem);
                        break;
                    }
            }

            DPCFILE.Close();

        }




        public void Pack(string Xml)
        {


            switch (game)
            {
                case Game.Innocence:
                    {
                        DpcHelper.ImportFiles(this.Path, Xml, Game.Innocence);
                        break;
                    }
                case Game.Requiem:
                    {
                        DpcHelper.ImportFiles(this.Path, Xml, Game.Requiem);
                        break;
                    }
            }



        }


    }
}
