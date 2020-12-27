using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaloMap
{
    public class Reflexive
    {
        #region Fields
        public int Offset;
        public int ChunkCount;
        public int ChunkSize;
        public int RawTranslation;
        public int Translation;
        #endregion

        #region Methods
        public void Read(Map map)
        {
            map.br.BaseStream.Position = map.SelectedTag.Offset + Offset;
            ChunkCount = map.br.ReadInt32();
            RawTranslation = map.br.ReadInt32();
            Translation = RawTranslation - map.Index.SecondaryMagic;
        }

        public void Read(Map map, bool b)
        {
            Offset = (int)map.br.BaseStream.Position;
            ChunkCount = map.br.ReadInt32();
            RawTranslation = map.br.ReadInt32();
            Translation = RawTranslation - map.Index.SecondaryMagic;
        }

        public void Read(Map map, int Offset)
        {
            this.Offset = Offset;
            Read(map);
        }

        public void Read(Map map, int Offset, bool b)
        {
            map.br.BaseStream.Position = Offset;
            Read(map, b);
        }

        public void Read(Map map, int Magic, int i)
        {
            Offset = (int)map.br.BaseStream.Position;
            ChunkCount = map.br.ReadInt32();
            RawTranslation = map.br.ReadInt32();
            Translation = RawTranslation - Magic;
        }

        public void Write(Map map, int Offset)
        {
            this.Offset = Offset;
            Write(map);
        }
        public void Write(Map map)
        {
            map.bw.BaseStream.Position = map.SelectedTag.Offset + Offset;
            map.bw.Write(ChunkCount);
            map.bw.Write(RawTranslation);
        }
        public void Write(Map map, bool b)
        {
            map.bw.BaseStream.Position = Offset;
            map.bw.Write(ChunkCount);
            map.bw.Write((Translation + map.Index.SecondaryMagic));
        }
        public void Fix(Map map, int Magic)
        {
            map.bw.BaseStream.Position = Offset;
            map.bw.Write(ChunkCount);
            map.bw.Write((Translation + Magic));
        }
        #endregion
    }

    public class BSPBlock
    {
        public int BSPOffset, BSPSize, BSPIdent;
        public int LightmapOffset, LightmapSize, LightmapIdent;
        public int Magic, MagicBaseAddress;
    }
}
