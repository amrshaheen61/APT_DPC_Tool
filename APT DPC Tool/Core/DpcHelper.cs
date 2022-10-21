using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Helper.binaryreader;
using Helper.MemoryList;
using System.IO.Compression;
using System.Xml;

namespace APT_DPC_Tool.Core
{
    public enum Game
    {
        Innocence,
        Requiem
    }
    public struct Header
    {
        public string versionString { get; set; }
        public bool isNotRTC { get; set; }
        public long BlockDescriptionOffet { get; set; }
        public long FileSize { get; set; }
        public long UFileSize { get; set; }
        public int FilesCount { get; set; }
        //Requiem
        public long FilesBlockSize { get; set; }
        public long FilesBlockOffset { get; set; }
        public long MapSize { get; set; }
        public long MapOffset { get; set; }


    }


    public struct BlockDescription
    {
        public string unknown { get; set; }
        public long FilesMapOffet { get; set; }
        public long DataFilesOffet { get; set; }
    }






    public struct FileDescription
    {
        public ulong ID { get; set; }
        public ulong ID2 { get; set; }
        public ulong FileType { get; set; }
        public long Offset { get; set; }
        public long CompressSize { get; set; }
        public long UcompressSize { get; set; }
    }
    public struct FileDataBlockDescription
    {
        public long BlockOffset { get; set; }
        public int FilesCount { get; set; }
        public long BlockSizePlusPadding { get; set; }
        public long BlockSize { get; set; }
        public long Crc { get; set; }
    }
    public struct FilesMap
    {
        public BlockDescription blockDescription { get; set; }
        public string unknown { get; set; }
        public List<FileDataBlockDescription> DataFiles { get; set; }
        public List<FileDescription> FileMap { get; set; }
    }



    public static class DpcHelper
    {
        public static long FixedOffset(this int value, Game game)
        {
            switch (game)
            {
                case Game.Innocence:
                    {
                        return (long)value << 11;
                    }
                case Game.Requiem:
                    {
                        return (long)value << 4;
                    }
            }
            return value;
        }




        public static Header ReadHeader(this binaryreader binaryReader, Game game)
        {
            Header header = new Header();

            switch (game)
            {
                case Game.Innocence:
                    {
                        header.versionString = binaryReader.GetStringValue(260);
                        header.isNotRTC = binaryReader.ReadInt32() == 1;
                        header.BlockDescriptionOffet = binaryReader.ReadInt32().FixedOffset(game);
                        break;
                    }
                case Game.Requiem:
                    {
                        header.versionString = binaryReader.GetStringValue(257);
                        binaryReader.ReadInt32();//Unkown
                        header.BlockDescriptionOffet = binaryReader.ReadInt32().FixedOffset(game);
                        header.FilesBlockSize = binaryReader.ReadInt32().FixedOffset(game);
                        header.FilesBlockOffset = binaryReader.ReadInt32().FixedOffset(game);
                        header.MapSize = binaryReader.ReadInt32().FixedOffset(game);
                        header.MapOffset = binaryReader.ReadInt32().FixedOffset(game);
                        break;
                    }
            }
            return header;
        }





        public static List<BlockDescription> ReadBlockDescription(this binaryreader binaryReader, Header header, Game game)
        {

            List<BlockDescription> BlockDescriptions = new List<BlockDescription>();
            binaryReader.BaseStream.Position = header.BlockDescriptionOffet;

            int BlockCount = binaryReader.ReadInt32();
            for (int i = 0; i < BlockCount; i++)
            {
                BlockDescription blockDescription = new BlockDescription();
                blockDescription.unknown = Convert.ToBase64String(binaryReader.ReadBytes(8 * 3)); //crc ??
                blockDescription.FilesMapOffet = binaryReader.ReadInt32().FixedOffset(game);
                blockDescription.DataFilesOffet = blockDescription.FilesMapOffet + binaryReader.ReadInt32().FixedOffset(game);
                BlockDescriptions.Add(blockDescription);
            }
            return BlockDescriptions;
        }



        public static List<FilesMap> ReadFilesMap(this binaryreader binaryReader, List<BlockDescription> blockDescriptions, Game game)
        {

            List<FilesMap> filesMaps = new List<FilesMap>();

            foreach (BlockDescription blockDescription in blockDescriptions)
            {
                FilesMap filesMap = new FilesMap();

                filesMap.blockDescription = blockDescription;

                #region FileDataBlockDescription
                binaryReader.BaseStream.Position = blockDescription.FilesMapOffet;
                filesMap.DataFiles = new List<FileDataBlockDescription>();
                int BlocksCount = binaryReader.ReadInt32();
                int BlockOffset = binaryReader.ReadInt32();
                filesMap.unknown = Convert.ToBase64String(binaryReader.ReadBytes(32));//crc


                for (int i = 0; i < BlocksCount; i++)
                {
                    FileDataBlockDescription fileDataBlockDescription = new FileDataBlockDescription();
                    fileDataBlockDescription.BlockOffset = BlockOffset.FixedOffset(game);
                    fileDataBlockDescription.FilesCount = binaryReader.ReadInt32();
                    fileDataBlockDescription.BlockSizePlusPadding = binaryReader.ReadInt64();
                    fileDataBlockDescription.BlockSize = binaryReader.ReadInt64();
                    fileDataBlockDescription.Crc = binaryReader.ReadInt64();
                    filesMap.DataFiles.Add(fileDataBlockDescription);
                }
                #endregion

                #region Files

                binaryReader.BaseStream.Position = blockDescription.FilesMapOffet + 1496;
                filesMap.FileMap = new List<FileDescription>();
                int FilesCount = binaryReader.ReadInt32();

                for (int i = 0; i < FilesCount; i++)
                {
                    FileDescription fileDescription = new FileDescription();

                    fileDescription.ID = binaryReader.ReadUInt64();
                    fileDescription.FileType = binaryReader.ReadUInt64();
                    fileDescription.Offset = binaryReader.ReadInt32().FixedOffset(game);

                    switch (game)
                    {
                        case Game.Innocence:
                            {
                                fileDescription.CompressSize = binaryReader.ReadInt64();
                                fileDescription.UcompressSize = binaryReader.ReadInt64();
                                break;
                            }
                        case Game.Requiem:
                            {
                                fileDescription.CompressSize = binaryReader.ReadInt32();
                                binaryReader.BaseStream.Position += 8; //unkown
                                fileDescription.UcompressSize = binaryReader.ReadInt32();
                                break;
                            }
                    }
                    filesMap.FileMap.Add(fileDescription);
                }
                if (game == Game.Requiem)
                {
                    binaryReader.BaseStream.Position += 8; //unkown
                    int Num = binaryReader.ReadInt32();
                    binaryReader.BaseStream.Position += 16 * Num; //id + index + unkown [+4] 
                    Num = binaryReader.ReadInt32();
                    binaryReader.BaseStream.Position += 4 * Num; //index 
                    FilesCount = binaryReader.ReadInt32();

                    for (int i = 0; i < FilesCount; i++)
                    {
                        FileDescription fileDescription = new FileDescription();
                        fileDescription.ID = binaryReader.ReadUInt64();
                        fileDescription.FileType = binaryReader.ReadUInt64();
                        fileDescription.Offset = binaryReader.ReadInt32().FixedOffset(game);
                        fileDescription.CompressSize = binaryReader.ReadInt32();
                        binaryReader.BaseStream.Position += 8; //unkown
                        fileDescription.UcompressSize = binaryReader.ReadInt32();
                        filesMap.FileMap.Add(fileDescription);
                    }

                }


                filesMaps.Add(filesMap);
                #endregion
            }

            return filesMaps;
        }



        public static List<FilesMap> ReadFilesMapOnly(this binaryreader binaryReader, Header header)
        {


            binaryReader.BaseStream.Position = header.MapOffset;
            List<FilesMap> filesMaps = new List<FilesMap>();

            FilesMap filesMap = new FilesMap();
            filesMap.DataFiles = new List<FileDataBlockDescription>();
            filesMap.FileMap = new List<FileDescription>();
            int FilesCount = binaryReader.ReadInt32();

            for (int i = 0; i < FilesCount; i++)
            {
                FileDescription fileDescription = new FileDescription();

                fileDescription.ID = binaryReader.ReadUInt64();
                fileDescription.ID2 = binaryReader.ReadUInt64();
                fileDescription.FileType = binaryReader.ReadUInt64();
                fileDescription.Offset = binaryReader.ReadInt32().FixedOffset(Game.Requiem);
                fileDescription.CompressSize = binaryReader.ReadInt32();
                binaryReader.BaseStream.Position += 8; //unkown
                fileDescription.UcompressSize = binaryReader.ReadInt32();

                binaryReader.BaseStream.Position += 8 * 2; //unkown
                filesMap.FileMap.Add(fileDescription);
            }
            filesMaps.Add(filesMap);

            return filesMaps;
        }

        public static void ReadPadding(this binaryreader binaryReader, Game game)
        {

            switch (game)
            {
                case Game.Innocence:
                    {
                        binaryReader.BaseStream.Position += ((long)1 << 11) - (binaryReader.BaseStream.Position % (long)1 << 11);
                        break;
                    }
                case Game.Requiem:
                    {
                        binaryReader.BaseStream.Position += ((long)1 << 4) - (binaryReader.BaseStream.Position % (long)1 << 4);
                        break;
                    }
            }

        }

        public static (string, byte[]) UCompressFile(this binaryreader binaryReader, bool IsDataFile, Game game)
        {
            long StartPosition = binaryReader.BaseStream.Position;

            MemoryList memoryList = new MemoryList();
            ulong Type = binaryReader.ReadUInt64();
            memoryList.Write(Type);
            ulong ID = binaryReader.ReadUInt64();
            memoryList.Write(ID);
            memoryList.Write(binaryReader.ReadInt64());
            int PBufferSize = memoryList.GetPosition();
            int BufferSize = binaryReader.ReadInt32();
            int InfoBufferSize = binaryReader.ReadInt32();
            int OriginalSize = binaryReader.ReadInt32();
            memoryList.Write(BufferSize);
            memoryList.Write(InfoBufferSize);
            memoryList.Write(OriginalSize);

            string FileName = GetStringName(ID) + "." + GetStringName(Type);
            Log.Print("-Exporting file: " + FileName);
                     

            switch (game)
            {
                case Game.Innocence:
                    {
                        int IsCompressed = binaryReader.ReadInt32();
                        memoryList.Write(IsCompressed);//Uncomperss
                        int PCompressSize = memoryList.GetPosition();
                        long CompressSize = binaryReader.ReadInt64();     
                        memoryList.Write(CompressSize);//CompressSize

                        Console.WriteLine(PBufferSize);
                        Console.WriteLine(PCompressSize);
                        if (IsCompressed == 0)
                        {
                            memoryList.Write(binaryReader.ReadBytes(BufferSize));
                        }
                        else
                        {


                            memoryList.Write(binaryReader.ReadBytes(InfoBufferSize));

                            byte[] UCompress = new byte[OriginalSize];
                            Lz4.Decompress(binaryReader.ReadBytes(BufferSize - InfoBufferSize), 0, ref UCompress);
                            memoryList.Add(UCompress);
                         
                            memoryList.SetIntValue((UCompress.Length + InfoBufferSize), false, PBufferSize);
                            memoryList.SetIntValue((UCompress.Length), false, PCompressSize);
                        }
                        break;
                    }
                case Game.Requiem:
                    {
                        int PCompressSize = memoryList.GetPosition();
                        int CompressSize = binaryReader.ReadInt32() - 8;
                        memoryList.Write(CompressSize);
                        short Padding = binaryReader.ReadInt16();
                       // memoryList.Write((short)0);
                        byte IsCompressed = binaryReader.ReadByte();
                        //memoryList.Write(IsCompressed);//unkown

                        //use Innocence statuc
                        memoryList.Write(0L);


                        if (IsCompressed == 0)
                        {
                            memoryList.Write(binaryReader.ReadBytes(BufferSize));
                        }
                        else
                        {


                            memoryList.Write(binaryReader.ReadBytes(InfoBufferSize));

                            binaryReader.ReadBytes(Padding);//no need
                            int _UCompressSize = binaryReader.ReadInt32();
                            int _CompressSize = binaryReader.ReadInt32();
                            int FileLenght = BufferSize - InfoBufferSize - 8 - Padding;
                            byte[] UCompress = binaryReader.ReadBytes(FileLenght);
                            UCompress = Ionic.Zlib.ZlibStream.UncompressBuffer(UCompress);
                            memoryList.Add(UCompress);
                            memoryList.SetIntValue((UCompress.Length + InfoBufferSize), false, PBufferSize);
                            memoryList.SetIntValue((UCompress.Length + 8), false, PCompressSize);
                        }
                        break;
                    }
            }
            return (FileName, memoryList.ToArray());
        }



        public static void ExportFiles(this binaryreader binaryReader, string Folder, Game game)
        {

            Header header = binaryReader.ReadHeader(game);
            List<BlockDescription> blockDescriptions;
            List<FilesMap> filesMaps = new List<FilesMap>(); ;
            if (header.BlockDescriptionOffet != 0)
            {
                blockDescriptions = binaryReader.ReadBlockDescription(header, game);
                filesMaps = binaryReader.ReadFilesMap(blockDescriptions, game);

            }

            if (game == Game.Requiem && header.MapOffset != 0)
            {
                filesMaps = binaryReader.ReadFilesMapOnly(header);
            }


            if (!Directory.Exists(Folder))
            {
                Directory.CreateDirectory(Folder);
            }

            //Create Xml file
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            XmlWriter XmlWriter = XmlWriter.Create(Path.Combine(Folder, "FilesMap.Xml"), settings);
            XmlWriter.WriteStartDocument();
            XmlWriter.WriteComment(
                "\nAPT DPC TOOL\n" +
                "Author: Amr Shaheen\n" +
                "Version: v1.0" +
                     "\n" +
                  "DON'T EDIT ANYTHING IN THIS FILE TO AVOID PROBLEMS!\n");

            XmlWriter.WriteStartElement("DPCFile");



            int filesMapIndex = 0;
            foreach (FilesMap filesMap in filesMaps)
            {
                XmlWriter.WriteStartElement("Block");

                XmlWriter.WriteAttributeString("unknown", filesMap.blockDescription.unknown);

                XmlWriter.WriteStartElement("BlockFiles");
                XmlWriter.WriteAttributeString("unknown", filesMap.unknown);

                filesMapIndex++;
                int FileIndex = 0;
                XmlWriter.WriteStartElement("FirstBlock");
                foreach (FileDescription FileDescription in filesMap.FileMap)
                {
                    FileIndex++;

                    if (header.FilesBlockOffset == 0 && header.FilesBlockSize == 0 && game == Game.Requiem)
                    {
                        if (Dpc.CommonDpc == null)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Log.PrintLine("Can't export this file: " + (GetStringName(FileDescription.ID) + "." + GetStringName(FileDescription.FileType)) + "\n Need \"COMMON.DPC\"");
                            Console.ResetColor();
                            continue;
                        }
                    }

                    string FileName;
                    byte[] FileData;

                    if ((header.FilesBlockOffset != 0 && header.FilesBlockSize != 0 && header.MapOffset == 0 && header.MapSize == 0) || game == Game.Innocence)
                    {
                        binaryReader.BaseStream.Position = FileDescription.Offset;
                        (FileName, FileData) = binaryReader.UCompressFile(false, game);
                    }
                    else
                    {
                        Dpc.CommonDpc.BaseStream.Position = FileDescription.Offset;
                        (FileName, FileData) = Dpc.CommonDpc.UCompressFile(false, game);
                    }


                    XmlWriter.WriteStartElement("File");
                    XmlWriter.WriteAttributeString("FileName", FileName);
                    XmlWriter.WriteEndElement();



                    File.WriteAllBytes(Path.Combine(Folder, FileName), FileData);
                    Log.Print(" Done\r\n");
                }
                XmlWriter.WriteEndElement();
                XmlWriter.WriteStartElement("SecondBlock");

                if (filesMap.DataFiles.Count != 0)
                    binaryReader.BaseStream.Position = filesMap.DataFiles[0].BlockOffset;
                foreach (FileDataBlockDescription fileDataBlockDescription in filesMap.DataFiles)
                {
                    XmlWriter.WriteStartElement("FilesBlock");
                    XmlWriter.WriteAttributeString("unknown", fileDataBlockDescription.Crc.ToString());
                    for (int i = 0; i < fileDataBlockDescription.FilesCount; i++)
                    {
                        string FileName;
                        byte[] FileData;
                        (FileName, FileData) = binaryReader.UCompressFile(true, game);

                        XmlWriter.WriteStartElement("File");
                        XmlWriter.WriteAttributeString("FileName", FileName);
                        XmlWriter.WriteEndElement();

                        File.WriteAllBytes(Path.Combine(Folder, FileName), FileData);
                        Log.Print(" Done\r\n");
                    }
                    XmlWriter.WriteEndElement();
                    binaryReader.BaseStream.Position += fileDataBlockDescription.BlockSizePlusPadding - fileDataBlockDescription.BlockSize;
                }
                XmlWriter.WriteEndElement();

                XmlWriter.WriteEndElement();//BlockFiles
                XmlWriter.WriteEndElement();//Block


            }
            XmlWriter.WriteEndElement();
            XmlWriter.Close();
        }




        public static string GetStringName(ulong Val)
        {
            int[] Indexs = Dpc.Names.Select((Value, Index) => (Value.ID == Val.ToString("X2")) ? Index : -1).Where(index => index != -1).ToArray();
            if (Indexs.Length != 0)
            {
                return Dpc.Names[Indexs[0]].FileName;
            }

            switch (Val)
            {
                case 0xE9659CD1C3F3326D:
                    return "Texture";
                case 0x87218B06F6FE91FD:
                    return "FontMap";
                case 0x1E1E2446DCB3072A:
                    return "Script";
            }
            return Val.ToString("X2");
        }





        //Repack


        public static void WriteHeader(this BinaryWriter binaryWriter, Header header, Game game)
        {
            binaryWriter.BaseStream.Position = 0;
            switch (game)
            {
                case Game.Innocence:
                    {
                        binaryWriter.Write(Encoding.ASCII.GetBytes("v2.128.92.19 - Asobo Studio - Internal Cross Technology - APT DPC Tool By Amr Shahaen(@amrshaheen61)".PadRight(260, '\0')));
                        binaryWriter.Write(1);
                        binaryWriter.Write((int)header.BlockDescriptionOffet);
                        binaryWriter.Write(Enumerable.Repeat((Byte)0, 20).ToArray()); //unkown
                        binaryWriter.Write(0L); //unkown
                        binaryWriter.Write(0L); //unkown
                        binaryWriter.Write(binaryWriter.BaseStream.Length); //unkown
                        binaryWriter.Write(0L); //unkown
                        binaryWriter.Write(0L); //unkown
                        binaryWriter.Write(header.FilesCount); //unkown
                        break;
                    }
                case Game.Requiem:
                    {
                        binaryWriter.Write(Encoding.ASCII.GetBytes("v2.128.52.19 - Asobo Studio - Internal Cross Technology - APT DPC Tool By Amr Shahaen(@amrshaheen61)".PadRight(256, '\0')));
                        binaryWriter.Write((byte)0);
                        binaryWriter.Write(3);
                        binaryWriter.Write((int)header.BlockDescriptionOffet);
                        binaryWriter.Write((int)header.FilesBlockSize);
                        binaryWriter.Write((int)header.FilesBlockOffset);
                        binaryWriter.Write(0L);
                        break;
                    }
            }


        }

        public static int SetPadding(this BinaryWriter binaryWriter, Game game, byte PaddingByte = 0xff)
        {
            int padding = 0;


            switch (game)
            {
                case Game.Innocence:
                    {
                        padding = (int)((long)(1 << 11) - binaryWriter.BaseStream.Length % (long)(1 << 11));
                        break;
                    }
                case Game.Requiem:
                    {
                        padding = (int)((long)(1 << 4) - binaryWriter.BaseStream.Length % (long)(1 << 4));
                        break;
                    }
            }

            binaryWriter.Write(Enumerable.Repeat(PaddingByte, padding).ToArray());
            return padding;
        }

        public static int FixedOffset(this BinaryWriter value, Game game)
        {
            switch (game)
            {
                case Game.Innocence:
                    {
                        return (int)(value.BaseStream.Position >> 11);
                    }
                case Game.Requiem:
                    {
                        return (int)(value.BaseStream.Position >> 4);
                    }
            }
            return (int)value.BaseStream.Position;
        }


        public static (int, byte[]) CompressFile(string Path, Game game)
        {
            
            MemoryList memoryList = new MemoryList(Path);
            int Size=0;
            ulong Type = memoryList.GetUInt64Value();
            ulong ID = memoryList.GetUInt64Value();
            memoryList.Skip(8);
            int PBufferSize = memoryList.GetPosition();

            int BufferSize = memoryList.GetIntValue();
            int InfoBufferSize = memoryList.GetIntValue();
            memoryList.Skip(4);

            string FileName = GetStringName(ID) + "." + GetStringName(Type);
            Log.Print("-Importing file: " + FileName);


            switch (game)
            {
                case Game.Innocence:
                    {

                        memoryList.Write(2);
                        int PCompressSize = memoryList.GetPosition();
                        memoryList.Skip(8);
                        memoryList.Skip(InfoBufferSize);
                        int Position = memoryList.GetPosition();
                        Size = BufferSize - InfoBufferSize;
                        byte[] Compress=new byte[0];
                        if(BufferSize - InfoBufferSize!=0)
                        Compress = Lz4.CompressBytes(memoryList.GetBytes(BufferSize - InfoBufferSize), Lz4Mode.Fast);
                        memoryList.Seek(Position);
                        memoryList.SetSize(Position);
                        memoryList.Add(Compress);
                        memoryList.SetIntValue((Compress.Length+ InfoBufferSize), false, PBufferSize);
                        memoryList.SetIntValue((Compress.Length), false, PCompressSize);

                        break;
                    }
                case Game.Requiem:
                    {
                        int PCompressSize = memoryList.GetPosition();
                        memoryList.Skip(4);
                        memoryList.SetShortValue(0);//Padding
                        memoryList.SetByteValue(4); //IsCompressed

                        memoryList.DeleteBytes(5);

                        memoryList.Skip(InfoBufferSize);
                        int Position = memoryList.GetPosition();
                        byte[] Compress=new byte[0];
                        Size = BufferSize - InfoBufferSize;
                        if (BufferSize - InfoBufferSize != 0)
                            Compress = Ionic.Zlib.ZlibStream.CompressBuffer(memoryList.GetBytes(BufferSize - InfoBufferSize));
                        memoryList.Seek(Position);
                        memoryList.SetSize(Position);
                        memoryList.Write((int)(BufferSize - InfoBufferSize));
                        memoryList.Add((int)Compress.Length);
                        memoryList.Add(Compress);
                        memoryList.SetIntValue((Compress.Length + 8 + InfoBufferSize), false, PBufferSize);
                        memoryList.SetIntValue((Compress.Length + 8), false, PCompressSize);
                        break;
                    }
            }
            return (Size, memoryList.ToArray());
        }
        public static (int, byte[]) GetFile(string FilePath,Game game)
        {
            return CompressFile(FilePath, game);
        }



        public static void ImportFiles(string DpcPath, string XmlFilesMap, Game game)
        {

            XmlDocument doc = new XmlDocument();
            doc.Load(XmlFilesMap);

            Header header = new Header();
            List<BlockDescription> BlockDescriptions = new List<BlockDescription>();

        
            List<List<FileDescription>> FileDescriptionS = new List<List<FileDescription>>();

          
            List<long> FileMapStartOffset = new List<long>();
            BinaryWriter DPCFILE = new BinaryWriter(new FileStream(DpcPath, FileMode.Create, FileAccess.Write));

            DPCFILE.Write(Enumerable.Repeat((byte)0xff, 4096).ToArray());



            XmlNodeList BlockElements = doc.GetElementsByTagName("Block");

            for (int i = 0; i < BlockElements.Count; i++)
            {

                List<FileDataBlockDescription> DataFiles = new List<FileDataBlockDescription>();

                BlockDescription blockDescription = new BlockDescription();
                blockDescription.unknown = BlockElements[i].Attributes["unknown"].Value;
                blockDescription.FilesMapOffet = DPCFILE.FixedOffset(game);
                long DataStartPosition = DPCFILE.BaseStream.Position;

                DPCFILE.Write(Enumerable.Repeat((byte)0, 1496).ToArray()); //DataBlock info

                int FilesCount = BlockElements[i].ChildNodes[0].ChildNodes[0].ChildNodes.Count;
                DPCFILE.Write(FilesCount); //FilesCount

                FileMapStartOffset.Add(DPCFILE.BaseStream.Position);

                DPCFILE.Write(Enumerable.Repeat((byte)0, FilesCount * 36).ToArray()); //DataBlock info

                switch (game)
                {
                    case Game.Innocence:
                        {
                            DPCFILE.Write(Enumerable.Repeat((byte)0, 8).ToArray());
                            break;
                        }
                    case Game.Requiem:
                        {
                            DPCFILE.Write(Enumerable.Repeat((byte)0, 20).ToArray());
                            break;
                        }
                }

                DPCFILE.SetPadding(game);
                blockDescription.DataFilesOffet = DPCFILE.FixedOffset(game) - blockDescription.FilesMapOffet;


                XmlNodeList BlockFiles = BlockElements[i].ChildNodes[0].ChildNodes[1].ChildNodes;

                for (int n = 0; n < BlockFiles.Count; n++)
                {
                    FileDataBlockDescription dataBlockDescription = new FileDataBlockDescription();
                    dataBlockDescription.FilesCount = BlockFiles[n].ChildNodes.Count;
                    dataBlockDescription.Crc = long.Parse(BlockFiles[n].Attributes["unknown"].Value);
                    for (int t = 0; t < BlockFiles[n].ChildNodes.Count; t++)
                    {

                        string FileName = BlockFiles[n].ChildNodes[t].Attributes["FileName"].Value;
                        Byte[] FileBytes;
                        (_, FileBytes) = GetFile(Path.Combine(Path.GetDirectoryName(XmlFilesMap), FileName),game);
                        dataBlockDescription.BlockSize += FileBytes.Length;
                        DPCFILE.Write(FileBytes);
                        Log.Print(" Done\r\n");
                    }
                    dataBlockDescription.BlockSizePlusPadding += DPCFILE.SetPadding(game,0);
                    DataFiles.Add(dataBlockDescription);
                }


                /*
                string unknownVal= BlockElements[i].ChildNodes[0].Attributes["unknown"].Value;
                int DataFilesCount = BlockElements[i].ChildNodes[0].ChildNodes[1].ChildNodes.Count; //SecondBlock
                */
                long CurrentPosition = DPCFILE.BaseStream.Position;
                DPCFILE.BaseStream.Position = DataStartPosition;
                string unknown1 = BlockElements[i].ChildNodes[0].Attributes["unknown"].Value;

                DPCFILE.Write(DataFiles.Count);
                DPCFILE.Write((int)(blockDescription.DataFilesOffet + blockDescription.FilesMapOffet));
                DPCFILE.Write(Convert.FromBase64String(unknown1));
                foreach (FileDataBlockDescription fileDataBlockDescription in DataFiles)
                {
                    DPCFILE.Write(fileDataBlockDescription.FilesCount);
                    DPCFILE.Write(fileDataBlockDescription.BlockSizePlusPadding + fileDataBlockDescription.BlockSize);
                    DPCFILE.Write(fileDataBlockDescription.BlockSize);
                    DPCFILE.Write(fileDataBlockDescription.Crc);
                }
                DPCFILE.BaseStream.Position = CurrentPosition;

                BlockDescriptions.Add(blockDescription);
            }




            //Set BlockDescriptions 
            header.BlockDescriptionOffet = DPCFILE.FixedOffset(game);
            DPCFILE.Write(BlockDescriptions.Count);
            foreach (BlockDescription block in BlockDescriptions)
            {
                DPCFILE.Write(Convert.FromBase64String(block.unknown));
                DPCFILE.Write((int)block.FilesMapOffet);
                DPCFILE.Write((int)block.DataFilesOffet);
            }
            DPCFILE.SetPadding(game);



            //Set File Map Files
            header.FilesBlockOffset = DPCFILE.FixedOffset(game);
            for (int i = 0; i < BlockElements.Count; i++)
            {
                XmlNodeList BlockFiles = BlockElements[i].ChildNodes[0].ChildNodes[0].ChildNodes;
                List<FileDescription> FileDescription = new List<FileDescription>();
                for (int n = 0; n < BlockFiles.Count; n++)
                {
                    FileDescription fileDescription = new FileDescription();

                    string FileName = BlockFiles[n].Attributes["FileName"].Value;
                    Byte[] FileBytes;
                    int DataCompressSize;
                    (DataCompressSize, FileBytes) = GetFile(Path.Combine(Path.GetDirectoryName(XmlFilesMap), FileName),game);

                    fileDescription.FileType = BitConverter.ToUInt64(FileBytes, 0);
                    fileDescription.ID = BitConverter.ToUInt64(FileBytes, 8);
                    fileDescription.CompressSize = FileBytes.Length;
                    fileDescription.UcompressSize = DataCompressSize;
                    fileDescription.Offset = DPCFILE.FixedOffset(game);
                    DPCFILE.Write(FileBytes);
                    DPCFILE.SetPadding(game);
                    FileDescription.Add(fileDescription);
                    Log.Print(" Done\r\n");
                }
                FileDescriptionS.Add(FileDescription);
            }
            header.FilesBlockSize = DPCFILE.FixedOffset(game) - header.FilesBlockOffset;




            for (int i=0; i< FileDescriptionS.Count;i++)
            {
                DPCFILE.BaseStream.Position = FileMapStartOffset[i];
                foreach (FileDescription fileDescription in  FileDescriptionS[i])
                {
                    switch (game)
                    {
                        case Game.Innocence:
                            {
                                DPCFILE.Write(fileDescription.ID);
                                DPCFILE.Write(fileDescription.FileType);
                                DPCFILE.Write((int)fileDescription.Offset);
                                DPCFILE.Write(fileDescription.CompressSize);
                                DPCFILE.Write(fileDescription.UcompressSize);
                               break;
                            }
                        case Game.Requiem:
                            {
                                DPCFILE.Write(fileDescription.ID);
                                DPCFILE.Write(fileDescription.FileType);
                                DPCFILE.Write((int)fileDescription.Offset);
                                DPCFILE.Write((int)fileDescription.CompressSize);
                                DPCFILE.Write(0L);
                                DPCFILE.Write((int)fileDescription.UcompressSize);
                                break;
                            }
                    }
                }

            }

           


            header.FilesCount = doc.GetElementsByTagName("File").Count;
            DPCFILE.WriteHeader(header, game);
            DPCFILE.Close();






        }








    }
}
