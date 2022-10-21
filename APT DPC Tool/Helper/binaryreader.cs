using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace Helper.binaryreader
{
    public class binaryreader:BinaryReader
    {

        public binaryreader(Stream stream) : base(stream)
        {

        }

        public string GetStringValue(int StringLenght)
        {
            return Encoding.ASCII.GetString(this.ReadBytes(StringLenght));
        }

        public string GetStringValueN(bool SavePosition = true, int SeekAndRead = -1, Encoding encoding = null)
        {
            List<byte> StringValues = new List<byte>();
            while (true)
            {
                StringValues.Add(this.ReadByte());
                if (StringValues[StringValues.Count - 1] == 0)
                {
                    break;
                }

            }
            return Encoding.ASCII.GetString(StringValues.ToArray()).TrimEnd('\0');
        }
    }
}
