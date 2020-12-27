using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic;
using System.Windows.Forms;

namespace HaloMap
{
    public class Header
    {
        public string Head;
        public int Version, FileSize;
        public int IndexOffset, IndexSize, MetaTableOffset, MetaTableSize, NonRawSize;
        public string MapBuild, MapName, MapScenario;
        public MapType Type;
        public int CrazyOffset, CrazySize;
        public int Script128Offset, ScriptCount, ScriptTableSize, ScriptIndexOffset, ScriptTableOffset;
        public int TagCount, FileTableOffset, FileTableSize, FileTableIndexOffset;
        public int MapChecksum;

        public void Read(Map map)
        {
            map.br.BaseStream.Position = 0;
            Head = Strings.StrReverse(new string(map.br.ReadChars(4)));
            Version = map.br.ReadInt32();
            if (Head != "head" && Version != 8)
            {
                throw new Exception("Error: Invalid Map File!");
            }
            FileSize = map.br.ReadInt32();
            map.br.BaseStream.Position += 4;
            IndexOffset = map.br.ReadInt32();
            IndexSize = map.br.ReadInt32();
            MetaTableOffset = IndexOffset + IndexSize;
            MetaTableSize = map.br.ReadInt32();
            NonRawSize = map.br.ReadInt32();
            map.br.BaseStream.Position = 288;
            MapBuild = new string(map.br.ReadChars(32));
            Type = (MapType)map.br.ReadInt32();
            map.br.BaseStream.Position += 16;
            CrazySize = map.br.ReadInt32();
            CrazyOffset = map.br.ReadInt32();
            map.br.BaseStream.Position += 4;
            Script128Offset = map.br.ReadInt32();
            ScriptCount = map.br.ReadInt32();
            ScriptTableSize = map.br.ReadInt32();
            ScriptIndexOffset = map.br.ReadInt32();
            ScriptTableOffset = map.br.ReadInt32();
            map.br.BaseStream.Position = 408;
            MapName = new string(map.br.ReadChars(36));
            MapScenario = new string(map.br.ReadChars(256));
            map.br.BaseStream.Position += 4;
            TagCount = map.br.ReadInt32();
            FileTableOffset = map.br.ReadInt32();
            FileTableSize = map.br.ReadInt32();
            FileTableIndexOffset = map.br.ReadInt32();
            MapChecksum = map.br.ReadInt32();
        }
    }
}
