using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaloMap
{
    public class Index
    {
        public int PrimaryMagicConstant, PrimaryMagic, SecondaryMagicConstant, SecondaryMagic;
        public int TagStartModifier, TagStart;
        public int ScenarioIdent, GlobalsIdent;
        public int TagCount;

        public void Read(Map map)
        {
            map.br.BaseStream.Position = map.Header.IndexOffset;
            PrimaryMagicConstant = map.br.ReadInt32();
            PrimaryMagic = PrimaryMagicConstant - (map.Header.IndexOffset + 32);
            map.br.BaseStream.Position += 4;
            TagStartModifier = map.br.ReadInt32();
            TagStart = TagStartModifier - PrimaryMagic;
            ScenarioIdent = map.br.ReadInt32();
            GlobalsIdent = map.br.ReadInt32();
            map.br.BaseStream.Position += 4;
            TagCount = map.br.ReadInt32();
            map.br.BaseStream.Position = TagStart + 8;
            SecondaryMagicConstant = map.br.ReadInt32();
            SecondaryMagic = SecondaryMagicConstant - map.Header.MetaTableOffset;
        }
    }
}