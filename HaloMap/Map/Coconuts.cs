using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaloMap
{
    public class SoundName
    {
        public short Index;
    }

    public class SoundChoice
    {
        public int NameIndex, Unknown1, Unknown2;
        public short SoundIndex, ChunkCount;
        public List<SoundChunk> SoundChunks = new List<SoundChunk>();
    }

    public class SoundPermutation
    {
        public short Unknown1, Unknown2;
        public byte Unknown3, ChunkNumber;
        public short Unknown4, ChoiceIndex, ChunkCount;
        public List<SoundChoice> Choices = new List<SoundChoice>();
    }

    public class SoundChunk
    {
        public int Offset, Size, ChunkOffset;
        public RawLocation RawLocation;
        public byte[] Raw;
    }

    public class ModelBlock
    {
        public int Offset, Size, HeaderSize, DataSize, Pointer;
        public RawLocation RawLocation;
    }

    public class Coconuts
    {
        public static List<SoundName> Names = new List<SoundName>();
        public static List<SoundChoice> Choices = new List<SoundChoice>();
        public static List<SoundPermutation> Permutations = new List<SoundPermutation>();
        public static List<SoundChunk> SoundChunks = new List<SoundChunk>();
        public static List<ModelBlock> ModelRawBlocks = new List<ModelBlock>();

        public static void Read(Map map)
        {
            Tag Coco = map.Tags[map.Tags.Count - 1];

            //SoundNames
            map.br.BaseStream.Position = Coco.Offset + 16;
            Reflexive r = new Reflexive();
            r.Read(map, true);
            for (int i = 0; i < r.ChunkCount; i++)
            {
                map.br.BaseStream.Position = r.Translation + (i * 4);
                SoundName name = new SoundName();
                name.Index = map.br.ReadInt16();
                Names.Add(name);
            }

            //Permutations
            map.br.BaseStream.Position = Coco.Offset + 32;
            r.Read(map, true);
            for (int i = 0; i < r.ChunkCount; i++)
            {
                map.br.BaseStream.Position = r.Translation + (i * 12);
                SoundPermutation Perm = new SoundPermutation();
                Perm.Unknown1 = map.br.ReadInt16();
                Perm.Unknown2 = map.br.ReadInt16();
                Perm.Unknown3 = map.br.ReadByte();
                Perm.ChunkNumber = map.br.ReadByte();
                Perm.Unknown4 = map.br.ReadInt16();
                Perm.ChoiceIndex = map.br.ReadInt16();
                Perm.ChunkCount = map.br.ReadInt16();
                Permutations.Add(Perm);
            }

            //Choices
            map.br.BaseStream.Position = Coco.Offset + 40;
            r.Read(map, true);
            for (int i = 0; i < r.ChunkCount; i++)
            {
                map.br.BaseStream.Position = r.Translation + (i * 16);
                SoundChoice Choice = new SoundChoice();
                Choice.NameIndex = map.br.ReadInt32();
                Choice.Unknown1 = map.br.ReadInt32();
                Choice.Unknown2 = map.br.ReadInt32();
                Choice.SoundIndex = map.br.ReadInt16();
                Choice.ChunkCount = map.br.ReadInt16();
                Choices.Add(Choice);
            }

            //Sound Chunks
            map.br.BaseStream.Position = Coco.Offset + 64;
            r.Read(map, true);
            for (int i = 0; i < r.ChunkCount; i++)
            {
                map.br.BaseStream.Position = r.Translation + (i * 12);
                SoundChunk Chunk = new SoundChunk();
                Chunk.ChunkOffset = (int)map.br.BaseStream.Position;
                Chunk.Offset = map.br.ReadInt32();
                Chunk.Size = map.br.ReadInt32() & 0x7FFFFFFF;
                Chunk.RawLocation = Map.RawLocator(ref Chunk.Offset);
                SoundChunks.Add(Chunk);
            }

            //Model Raw Blocks
            map.br.BaseStream.Position = Coco.Offset + 80;
            r.Read(map, true);
            for (int i = 0; i < r.ChunkCount; i++)
            {
                map.br.BaseStream.Position = r.Translation + (i * 44) + 8;
                ModelBlock m = new ModelBlock();
                m.Pointer = (int)map.br.BaseStream.Position;
                m.Offset = map.br.ReadInt32();
                m.Size = map.br.ReadInt32();
                m.HeaderSize = map.br.ReadInt32();
                m.DataSize = map.br.ReadInt32();
                m.RawLocation = Map.RawLocator(ref m.Offset);
                ModelRawBlocks.Add(m);
            }
        }

        public static void Clear()
        {
            Names.Clear();
            Choices.Clear();
            Permutations.Clear();
            SoundChunks.Clear();
            ModelRawBlocks.Clear();
        }
    }
}
