using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace HaloMap
{
    public enum MapType : int
    {
        Multiplayer = 0,
        Singleplayer = 1,
        Shared = 2,
        Mainmenu = 3,
        SingleplayerShared = 4
    }

    public enum RawLocation : long
    {
        Internal = 0X00000000,
        Mainmenu = 0XC0000000,
        Shared = 0X80000000,
        SingleplayerShared = 0X40000000,
        Unknown
    }

    public class Map
    {
        public FileStream fs;
        public BinaryReader br;
        public BinaryWriter bw;

        public Header Header = new Header();
        public Index Index = new Index();
        public Tags TagIndex = new Tags();
        public StringID StringId = new StringID();

        public List<string> SIDs = new List<string>();
        public List<Tag> Tags = new List<Tag>();
        public Tag SelectedTag;

        public List<BSPBlock> BSPs = new List<BSPBlock>();

        public string MainMenu, Shared, SinglePlayerShared;

        public Map(string Location)
        {
            fs = new FileStream(Location, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            br = new BinaryReader(fs);
            bw = new BinaryWriter(fs);
        }

        public void Read(TreeView tv)
        {
            Header.Read(this);
            Index.Read(this);
            StringId.Read(this);
            TagIndex.Read(this, tv);
            Coconuts.Read(this);
            ReadBSP();
            ReadSettings();
        }

        public void ReadSettings()
        {
            try
            {
                StreamReader sr = new StreamReader(Application.StartupPath + "\\Settings.cfg");
                MainMenu = sr.ReadLine();
                Shared = sr.ReadLine();
                SinglePlayerShared = sr.ReadLine();
                sr.Close();
            }
            catch { }
        }

        public void ReadBSP()
        {
            br.BaseStream.Position = Tags[3].Offset + 528;
            Reflexive r = new Reflexive();
            r.Read(this, true);
            for (int x = 0; x < r.ChunkCount; x++)
            {
                br.BaseStream.Position = r.Translation + (x * 68);
                BSPBlock b = new BSPBlock();
                b.BSPOffset = br.ReadInt32();
                b.BSPSize = br.ReadInt32();
                b.MagicBaseAddress = br.ReadInt32();
                b.Magic = b.MagicBaseAddress - b.BSPOffset;
                br.BaseStream.Position += 8;
                b.BSPIdent = br.ReadInt32();
                br.BaseStream.Position += 4;
                b.LightmapIdent = br.ReadInt32();
                br.BaseStream.Position = b.BSPOffset + 8;
                b.LightmapOffset = br.ReadInt32() + -b.Magic;
                b.LightmapSize = b.BSPOffset + b.BSPSize - b.LightmapOffset;
                BSPs.Add(b);
            }
        }

        public void Reload(TreeView tv)
        {
            //Clear
            Header = new Header();
            Index = new Index();
            TagIndex = new Tags();
            StringId = new StringID();
            Coconuts.Clear();
            Tags.Clear();
            SelectedTag = null;
            SIDs.Clear();
            BSPs.Clear();

            //Read
            Header.Read(this);
            Index.Read(this);
            StringId.Read(this);
            TagIndex.Read(this, tv);
            Coconuts.Read(this);
            ReadBSP();
        }

        public static RawLocation RawLocator(ref int Offset)
        {
            long Map = Offset & 0xC0000000;
            Offset &= 0x3FFFFFFF;
            switch (Map)
            {
                case 0:
                    {
                        return RawLocation.Internal;
                    }
                case 0x80000000:
                    {
                        return RawLocation.Shared;
                    }
                case 0xC0000000:
                    {
                        return RawLocation.SingleplayerShared;
                    }
                case 0x40000000:
                    {
                        return RawLocation.Mainmenu;
                    }
            }
            return RawLocation.Unknown;
        }

        public BinaryReader OpenMap(RawLocation Type)
        {
            string Location = "";
            if (Type == RawLocation.Shared)
            {
                Location = Shared;
            }
            else if (Type == RawLocation.Mainmenu)
            {
                Location = MainMenu;
            }
            else if (Type == RawLocation.SingleplayerShared)
            {
                Location = SinglePlayerShared;
            }
            BinaryReader nbr = new BinaryReader(new FileStream(Location, FileMode.Open, FileAccess.Read, FileShare.Read));
            return nbr;
        }
    }
}
