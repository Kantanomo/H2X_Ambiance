using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace HaloMap
{
    public enum Format : int
    {
        Mono22050kbps = 0,
        Stero44100kbps = 1,
        WMASpecific = 2
    }

    public enum Compression : int
    {
        None = 0,
        XboxAdpmc = 1,
        WMA = 2
    }

    public class Sound
    {
        public Format Format;
        public Compression Compression;
        public SoundPermutation Permutation = new SoundPermutation();
        public MemoryStream snd;
        public byte[] Data;

        public void Parse(Map map)
        {
            Clear();

            //Format
            map.br.BaseStream.Position = map.SelectedTag.Offset + 4;
            Format = (Format)map.br.ReadByte();

            //Compression
            map.br.BaseStream.Position = map.SelectedTag.Offset + 4;
            Compression = (Compression)map.br.ReadByte();

            //Permuation
            Permutation = new SoundPermutation();
            map.br.BaseStream.Position = map.SelectedTag.Offset + 8;
            int Index = map.br.ReadInt16();
            Permutation = Coconuts.Permutations[Index];
            for (int i = 0; i < Permutation.ChunkCount; i++)
            {
                int ChoiceIndex = Permutation.ChoiceIndex + i;
                Permutation.Choices.Add(Coconuts.Choices[ChoiceIndex]);
                for (int x = 0; x < Coconuts.Choices[ChoiceIndex].ChunkCount; x++)
                {
                    int SoundIndex = Coconuts.Choices[ChoiceIndex].SoundIndex + x;
                    Permutation.Choices[i].SoundChunks.Add(Coconuts.SoundChunks[SoundIndex]);
                }
            }

            //Raw Buffer
            foreach (SoundChoice cc in Permutation.Choices)
            {
                foreach (SoundChunk c in cc.SoundChunks)
                {
                    if (c.RawLocation == RawLocation.Internal && c.Offset != -1 && c.Size > 0)
                    {
                        map.br.BaseStream.Position = c.Offset;
                        c.Raw = map.br.ReadBytes(c.Size);
                    }
                    else if (c.RawLocation != RawLocation.Internal && c.Offset != -1 && c.Size > 0)
                    {
                        BinaryReader br = map.OpenMap(c.RawLocation);
                        br.BaseStream.Position = c.Offset;
                        c.Raw = br.ReadBytes(c.Size);
                        br.Close();
                    }
                }
            }
        }

        public void Clear()
        {
            for (int i = 0; i < Coconuts.Permutations.Count; i++)
            {
                Coconuts.Permutations[i].Choices.Clear();
            }
            for (int i = 0; i < Coconuts.Choices.Count; i++)
            {
                Coconuts.Choices[i].SoundChunks.Clear();
            }
        }

        public string LoadPreview(Map map, SoundChunk Chunk)
        {
            string Path = "";
            if (Compression == Compression.None || Compression == Compression.XboxAdpmc)
            {
                FormatFile(map, Chunk);
                Path = Application.StartupPath + "\\TempSound.wav";
            }
            else if (Compression == Compression.WMA)
            {
                FormatFile(map, Chunk);
                Path = Application.StartupPath + "\\TempSound.wma";
            }
            return Path;
        }

        public string PlayAll(Map map, SoundChoice[] Choices)
        {
            string Path = "";
            List<byte> Raw = new List<byte>();
            for (int i = 0; i < Choices.Length; i++)
            {
                for (int x = 0; x < Choices[i].SoundChunks.Count; x++)
                {
                    Raw.AddRange(Choices[i].SoundChunks[x].Raw);
                }
            }
            switch (Compression)
            {
                case Compression.None:
                case Compression.XboxAdpmc:
                    {//Wav
                        WriteADPMC(Format, Raw.ToArray(), Application.StartupPath + "\\TempSound.wav");
                        Path = Application.StartupPath + "\\TempSound.wav";
                        break;
                    }
                case Compression.WMA:
                    {//Wma
                        File.WriteAllBytes(Application.StartupPath + "\\TempSound.wma", Raw.ToArray());
                        Path = Application.StartupPath + "\\TempSound.wma";
                        break;
                    }
            }
            return Path;
        }

        public void FormatFile(Map map, SoundChunk Chunk)
        {
            snd = new MemoryStream();
            switch (Compression)
            {
                case Compression.None:
                case Compression.XboxAdpmc:
                    {//Wav
                        WriteADPMC(Format, Chunk.Raw, Application.StartupPath + "\\TempSound.wav");
                        break;
                    }
                case Compression.WMA:
                    {//Wma
                        File.WriteAllBytes(Application.StartupPath + "\\TempSound.wma", Chunk.Raw);
                        break;
                    }
            }
        }

        public void Extract(string Location, Map map, SoundChunk[] Chunks)
        {
            List<byte> Raw = new List<byte>();
            for (int i = 0; i < Chunks.Length; i++)
            {
                Raw.AddRange(Chunks[i].Raw);
            }
            switch (Compression)
            {
                case Compression.None:
                case Compression.XboxAdpmc:
                    {//Wav
                        WriteADPMC(Format, Raw.ToArray(), Location + ".wav");
                        break;
                    }
                case Compression.WMA:
                    {//Wma
                        File.WriteAllBytes(Location + ".wma", Raw.ToArray());
                        break;
                    }
            }
        }

        public void WriteADPMC(Format Format, byte[] Data, string Location)
        {
            int Channels = 0;
            int SampleRate = 0;
            switch (Format)
            {
                case Format.Mono22050kbps:
                    {
                        Channels = 1;
                        SampleRate = 0x5622;
                        break;
                    }
                case Format.Stero44100kbps:
                    {
                        Channels = 2;
                        SampleRate = 0xAC44;
                        break;
                    }
            }

            byte[] RIFF = { 52, 49, 46, 46 };
            byte[] WAVE = { 57, 41, 56, 45 };
            byte[] fmt = { 66, 0x6D, 74, 20 };
            byte[] data = { 64, 61, 74, 61 };

            //Write
            BinaryWriter bw = new BinaryWriter(new FileStream(Location, FileMode.Create, FileAccess.Write, FileShare.Write));
            bw.Write(("RIFF").ToCharArray());
            bw.Write(Data.Length + 48);
            bw.Write(("WAVE").ToCharArray());
            bw.Write(("fmt ").ToCharArray());
            bw.Write(0x14);
            bw.Write((ushort)0x69);
            bw.Write((ushort)Channels);
            bw.Write(SampleRate);
            bw.Write(Channels * SampleRate * 4 / 8);
            bw.Write((ushort)(Channels * 36));
            bw.Write((ushort)0x4);
            bw.Write((ushort)0x2);
            bw.Write((ushort)0x40);
            bw.Write(("data").ToCharArray());
            bw.Write(Data.Length);
            bw.Write(Data);
            bw.Close();
        }

        public void Internalize(Map map, SoundPermutation Perm)
        {
            int shift = 0;

            #region Find the Model Raw Start
            int ModelRawStart = (int)map.br.BaseStream.Length;
            for (int i = 0; i < map.Index.TagCount; i++)
            {
                if (map.Tags[i].Class == "mode")
                {
                    //Model-1
                    map.br.BaseStream.Position = map.Tags[i].Offset + 36;
                    Reflexive mode = new Reflexive();
                    mode.Read(map, true);
                    for (int x = 0; x < mode.ChunkCount; x++)
                    {
                        map.br.BaseStream.Position = mode.Translation + (x * 92) + 56;
                        int Offset = map.br.ReadInt32();
                        if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset < ModelRawStart)
                        {
                            ModelRawStart = Offset;
                        }
                    }

                    //Model-2
                    map.br.BaseStream.Position = map.Tags[i].Offset + 116;
                    mode.Read(map, true);
                    for (int x = 0; x < mode.ChunkCount; x++)
                    {
                        map.br.BaseStream.Position = mode.Translation + (x * 88) + 52;
                        int Offset = map.br.ReadInt32();
                        if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset < ModelRawStart)
                        {
                            ModelRawStart = Offset;
                        }
                    }
                }
            }
            #endregion

            #region Add Sound Raw with Padding
            //Goto the Model Raw Start and Save EOF
            List<int> Pointers = new List<int>();
            map.br.BaseStream.Position = ModelRawStart;
            byte[] EOF = map.br.ReadBytes((int)(map.br.BaseStream.Length - map.br.BaseStream.Position));
            map.bw.BaseStream.Position = ModelRawStart;

            //Write each Raw Block, then padding
            for (int i = 0; i < Perm.Choices.Count; i++)
            {
                for (int x = 0; x < Perm.Choices[i].SoundChunks.Count; x++)
                {
                    //Save the pointer
                    Pointers.Add((int)map.bw.BaseStream.Position);

                    //Write the Raw
                    map.bw.Write(Perm.Choices[i].SoundChunks[x].Raw);

                    //Write the Padding
                    int Padding = 512 - ((int)map.bw.BaseStream.Position % 512);
                    map.bw.Write(new byte[Padding]);

                    //Add Shift
                    shift += Perm.Choices[i].SoundChunks[x].Raw.Length;
                    shift += Padding;
                }
            }

            //Write EOF
            map.bw.Write(EOF);
            #endregion

            #region Fix All Raw Offsets
            //Fix all the Raw Offsets
            Reflexive temp = new Reflexive();
            for (int i = 0; i < map.Index.TagCount; i++)
            {
                #region Bitmaps
                if (map.Tags[i].Class == "bitm")
                {
                    map.br.BaseStream.Position = map.Tags[i].Offset + 68 + shift;
                    temp.Read(map, true);
                    for (int x = 0; x < temp.ChunkCount; x++)
                    {
                        map.br.BaseStream.Position = temp.Translation + (x * 116) + 28 + shift;
                        int Offset1 = map.br.ReadInt32();
                        int Offset2 = map.br.ReadInt32();
                        int Offset3 = map.br.ReadInt32();
                        if (Offset1 > 0 && Map.RawLocator(ref Offset1) == RawLocation.Internal)
                        {
                            Offset1 += shift;
                            map.bw.BaseStream.Position = temp.Translation + (x * 116) + 28 + shift;
                            map.bw.Write(Offset1);
                        }
                        if (Offset2 > 0 && Map.RawLocator(ref Offset2) == RawLocation.Internal)
                        {
                            Offset2 += shift;
                            map.bw.BaseStream.Position = temp.Translation + (x * 116) + 28 + shift + 4;
                            map.bw.Write(Offset2);
                        }
                        if (Offset3 > 0 && Map.RawLocator(ref Offset3) == RawLocation.Internal)
                        {
                            Offset3 += shift;
                            map.bw.BaseStream.Position = temp.Translation + (x * 116) + 28 + shift + 8;
                            map.bw.Write(Offset3);
                        }
                    }
                }
                #endregion

                #region Decorators
                else if (map.Tags[i].Class == "DECR")
                {
                    map.br.BaseStream.Position = map.Tags[i].Offset + 56 + shift;
                    int Offset = map.br.ReadInt32();
                    if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                    {
                        map.bw.BaseStream.Position = map.Tags[i].Offset + 56 + shift;
                        map.bw.Write(Offset + shift);
                    }
                }
                #endregion

                #region Animations
                else if (map.Tags[i].Class == "jmad")
                {
                    map.br.BaseStream.Position = map.Tags[i].Offset + 172 + shift;
                    temp.Read(map, true);
                    for (int x = 0; x < temp.ChunkCount; x++)
                    {
                        map.br.BaseStream.Position = temp.Translation + (x * 20) + 8 + shift;
                        int Offset = map.br.ReadInt32();
                        if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                        {
                            map.bw.BaseStream.Position = temp.Translation + (x * 20) + 8 + shift;
                            map.bw.Write(Offset + shift);
                        }
                    }
                }
                #endregion

                #region Lightmaps
                else if (map.Tags[i].Class == "ltmp")
                {
                    //Find the magic for it
                    for (int x = 0; x < map.BSPs.Count; x++)
                    {
                        if (map.Tags[i].Ident == map.BSPs[x].LightmapIdent)
                        {
                            //Lightmap Groups
                            int Offset;
                            map.br.BaseStream.Position = map.BSPs[x].LightmapOffset + 128 + shift;
                            Reflexive r = new Reflexive();
                            r.Read(map, map.BSPs[x].Magic, 0);
                            for (int z = 0; z < r.ChunkCount; z++)
                            {
                                //Clustors
                                map.br.BaseStream.Position = r.Translation + (104 * z) + 32 + shift;
                                Reflexive c = new Reflexive();
                                c.Read(map, map.BSPs[x].Magic, 0);
                                for (int a = 0; a < c.ChunkCount; a++)
                                {
                                    map.br.BaseStream.Position = c.Translation + (84 * a) + 40 + shift;
                                    Offset = map.br.ReadInt32();
                                    if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                                    {
                                        map.bw.BaseStream.Position = c.Translation + (84 * a) + 40 + shift;
                                        map.bw.Write(Offset + shift);
                                    }
                                }

                                //Poop Definitions
                                map.br.BaseStream.Position = r.Translation + (104 * z) + 48 + shift;
                                c.Read(map, map.BSPs[x].Magic, 0);
                                for (int a = 0; a < c.ChunkCount; a++)
                                {
                                    map.br.BaseStream.Position = c.Translation + (84 * a) + 40 + shift;
                                    Offset = map.br.ReadInt32();
                                    if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                                    {
                                        map.bw.BaseStream.Position = c.Translation + (84 * a) + 40 + shift;
                                        map.bw.Write(Offset + shift);
                                    }
                                }

                                //GeometryBuckets
                                map.br.BaseStream.Position = r.Translation + (104 * z) + 64 + shift;
                                c.Read(map, map.BSPs[x].Magic, 0);
                                for (int a = 0; a < c.ChunkCount; a++)
                                {
                                    map.br.BaseStream.Position = c.Translation + (56 * a) + 12 + shift;
                                    Offset = map.br.ReadInt32();
                                    if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                                    {
                                        map.bw.BaseStream.Position = c.Translation + (56 * a) + 12 + shift;
                                        map.bw.Write(Offset + shift);
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion

                #region BSPs
                else if (map.Tags[i].Class == "sbsp")
                {
                    for (int x = 0; x < map.BSPs.Count; x++)
                    {
                        if (map.Tags[i].Ident == map.BSPs[x].BSPIdent)
                        {
                            //Detail Object Raw
                            int Offset;
                            map.br.BaseStream.Position = map.BSPs[x].BSPOffset + 172 + shift;
                            Reflexive r = new Reflexive();
                            r.Read(map, map.BSPs[x].Magic, 0);
                            for (int z = 0; z < r.ChunkCount; z++)
                            {
                                map.br.BaseStream.Position = r.Translation + (176 * z) + 40 + shift;
                                Offset = map.br.ReadInt32();
                                if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                                {
                                    map.bw.BaseStream.Position = r.Translation + (176 * z) + 40 + shift;
                                    map.bw.Write(Offset + shift);
                                }
                            }

                            //BSP Permutations
                            map.br.BaseStream.Position = map.BSPs[x].BSPOffset + 328 + shift;
                            r.Read(map, map.BSPs[x].Magic, 0);
                            for (int z = 0; z < r.ChunkCount; z++)
                            {
                                map.br.BaseStream.Position = r.Translation + 40 + (200 * z) + shift;
                                Offset = map.br.ReadInt32();
                                if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                                {
                                    map.bw.BaseStream.Position = r.Translation + 40 + (200 * z) + shift;
                                    map.bw.Write(Offset + shift);
                                }
                            }

                            //Water Definitions
                            map.br.BaseStream.Position = map.BSPs[x].BSPOffset + 548 + shift;
                            r.Read(map, map.BSPs[x].Magic, 0);
                            for (int z = 0; z < r.ChunkCount; z++)
                            {
                                map.br.BaseStream.Position = r.Translation + (172 * z) + 16 + shift;
                                Offset = map.br.ReadInt32();
                                if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                                {
                                    map.bw.BaseStream.Position = r.Translation + (172 * z) + 16 + shift;
                                    map.bw.Write(Offset + shift);
                                }
                            }

                            //Decorator Raw
                            map.br.BaseStream.Position = map.BSPs[x].BSPOffset + 580 + shift;
                            r.Read(map, map.BSPs[x].Magic, 0);
                            for (int z = 0; z < r.ChunkCount; z++)
                            {
                                map.br.BaseStream.Position = r.Translation + (48 * z) + 16 + shift;
                                Reflexive Cache = new Reflexive();
                                Cache.Read(map, map.BSPs[x].Magic, 0);
                                for (int a = 0; a < Cache.ChunkCount; a++)
                                {
                                    map.br.BaseStream.Position = Cache.Translation + (44 * a) + shift;
                                    Offset = map.br.ReadInt32();
                                    if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                                    {
                                        map.bw.BaseStream.Position = Cache.Translation + (44 * a) + shift;
                                        map.bw.Write(Offset + shift);
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion

                #region Models
                else if (map.Tags[i].Class == "mode")
                {
                    //Model-1
                    map.br.BaseStream.Position = map.Tags[i].Offset + 36 + shift;
                    temp.Read(map, true);
                    for (int x = 0; x < temp.ChunkCount; x++)
                    {
                        map.br.BaseStream.Position = temp.Translation + (x * 92) + 56 + shift;
                        int Offset = map.br.ReadInt32();
                        if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                        {
                            map.bw.BaseStream.Position = temp.Translation + (x * 92) + 56 + shift;
                            map.bw.Write(Offset + shift);
                        }
                    }

                    //Model-2
                    map.br.BaseStream.Position = map.Tags[i].Offset + 116 + shift;
                    temp.Read(map, true);
                    for (int x = 0; x < temp.ChunkCount; x++)
                    {
                        map.br.BaseStream.Position = temp.Translation + (x * 88) + 52 + shift;
                        int Offset = map.br.ReadInt32();
                        if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                        {
                            map.bw.BaseStream.Position = temp.Translation + (x * 88) + 52 + shift;
                            map.bw.Write(Offset + shift);
                        }
                    }
                }
                #endregion

                #region Particles
                else if (map.Tags[i].Class == "PRTM")
                {
                    map.br.BaseStream.Position = map.Tags[i].Offset + 160 + shift;
                    int Offset = map.br.ReadInt32();
                    if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                    {
                        map.bw.BaseStream.Position = map.Tags[i].Offset + 160 + shift;
                        map.bw.Write(Offset + shift);
                    }
                }
                #endregion

                #region Weather
                else if (map.Tags[i].Class == "weat")
                {
                    map.br.BaseStream.Position = map.Tags[i].Offset + shift;
                    temp.Read(map, true);
                    for (int x = 0; x < temp.ChunkCount; x++)
                    {
                        map.br.BaseStream.Position = temp.Translation + (x * 140) + 64 + shift;
                        int Offset = map.br.ReadInt32();
                        if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                        {
                            map.bw.BaseStream.Position = temp.Translation + (x * 140) + 64 + shift;
                            map.bw.Write(Offset + shift);
                        }
                    }
                }
                #endregion

                #region Coconuts
                else if (map.Tags[i].Class == "ugh!")
                {
                    //Model Pointers
                    for (int x = 0; x < Coconuts.ModelRawBlocks.Count; x++)
                    {
                        if (Coconuts.ModelRawBlocks[x].RawLocation == RawLocation.Internal && Coconuts.ModelRawBlocks[x].Offset > 0)
                        {
                            map.bw.BaseStream.Position = Coconuts.ModelRawBlocks[x].Pointer + shift;
                            map.bw.Write(Coconuts.ModelRawBlocks[x].Offset + shift);
                        }
                    }
                }
                #endregion
            }
            #endregion

            #region Update
            //Update all header info
            map.Header.IndexOffset += shift;
            map.Header.CrazyOffset += shift;
            map.Header.FileTableIndexOffset += shift;
            map.Header.FileTableOffset += shift;
            map.Header.Script128Offset += shift;
            map.Header.ScriptIndexOffset += shift;
            map.Header.ScriptTableOffset += shift;

            //Shift Unicode
            map.br.BaseStream.Position = map.Tags[0].Offset + 400 + shift;
            for (int x = 0; x < 8; x++)
            {
                map.br.BaseStream.Position += 8;
                int Index = map.br.ReadInt32();
                int Table = map.br.ReadInt32();
                if (Index > 0 && Table > 0)
                {
                    map.bw.BaseStream.Position -= 8;
                    map.bw.Write(Index + shift);
                    map.bw.Write(Table + shift);
                }
                map.br.BaseStream.Position += 12;
            }

            //Shift Crazy Pointers
            map.bw.BaseStream.Position = map.Tags[0].Offset + 632 + shift;
            map.bw.Write(map.Header.CrazyOffset);
            map.bw.Write(map.Header.CrazyOffset);

            //Update BSP Info
            map.br.BaseStream.Position = map.Tags[3].Offset + 528 + shift;
            temp.Read(map, true);
            for (int x = 0; x < temp.ChunkCount; x++)
            {
                //We only need to update the BSP Offset, because it is part of the magic
                map.bw.BaseStream.Position = temp.Translation + (x * 68) + shift;
                map.bw.Write(map.BSPs[x].BSPOffset + shift);
            }

            //Values result in original ones, onyl magic needs to be fixed.
            //Update BSP Header
            //for (int i = 0; i < map.BSPs.Count; i++)
            //{
            //    //Recalculate Magic
            //    map.BSPs[i].Magic = map.BSPs[i].MagicBaseAddress - (map.BSPs[i].BSPOffset + shift);

            //    //Write new Info
            //    map.bw.BaseStream.Position = map.BSPs[i].BSPOffset + shift;
            //    map.bw.Write(map.BSPs[i].BSPSize);
            //    map.bw.Write((map.BSPs[i].BSPOffset + 16 + shift) + map.BSPs[i].Magic);
            //    map.bw.Write((map.BSPs[i].LightmapOffset + shift) + map.BSPs[i].Magic);
            //}
            #endregion

            #region Update the Sound Permuatation
            //Fix the sound perm
            for (int i = 0; i < Perm.Choices.Count; i++)
            {
                for (int x = 0; x < Perm.Choices[i].SoundChunks.Count; x++)
                {
                    map.bw.BaseStream.Position = Perm.Choices[i].SoundChunks[x].ChunkOffset + shift;
                    map.bw.Write(Pointers[i]);
                }
            }
            #endregion

            #region Clean Up
            map.bw.BaseStream.Position = 8;
            map.bw.Write((int)map.bw.BaseStream.Length);
            map.bw.BaseStream.Position += 4;
            map.bw.Write(map.Header.IndexOffset);
            map.bw.BaseStream.Position = 344;
            map.bw.Write(map.Header.CrazyOffset);
            map.bw.BaseStream.Position += 4;
            map.bw.Write(map.Header.Script128Offset);
            map.bw.BaseStream.Position += 8;
            map.bw.Write(map.Header.ScriptIndexOffset);
            map.bw.Write(map.Header.ScriptTableOffset);
            map.bw.BaseStream.Position = 708;
            map.bw.Write(map.Header.FileTableOffset);
            map.bw.BaseStream.Position += 4;
            map.bw.Write(map.Header.FileTableIndexOffset);

            //Sign the map
            int size = (int)map.br.BaseStream.Length - 2048;
            int times = size / 4;
            int result = 0;

            map.br.BaseStream.Position = 2048;
            for (int x = 0; x < times; x++)
            {
                result ^= map.br.ReadInt32();
            }

            map.bw.BaseStream.Position = 720;
            map.bw.Write(result);
            #endregion
        }

        public void Inject(Map map, string Location)
        {
            int shift = 0;

            #region Set up Sound Perm
            //Read the Buffer
            BinaryReader br = new BinaryReader(new FileStream(Location, FileMode.Open, FileAccess.Read, FileShare.Read));
            byte[] Buffer = new byte[0];
            byte Format = 0;
            int BlockCount = 0;
            if (Location.EndsWith(".wav"))
            {
                br.BaseStream.Position = 22;
                int tempf = br.ReadUInt16();
                if (tempf == 1)
                {
                    Format = 0;
                }
                else if (tempf == 2)
                {
                    Format = 1;
                }
                br.BaseStream.Position = 48;
                Buffer = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));
                BlockCount = Buffer.Length / 32760;

            }
            else
            {
                BlockCount = 1;
                Format = 2;
                Buffer = br.ReadBytes((int)br.BaseStream.Length);
            }
            br.Close();

            //Set up our new Sound Permutation
            SoundPermutation Perm = new SoundPermutation();
            Perm.ChoiceIndex = (short)Coconuts.Choices.Count;
            if (BlockCount == 0)
            {
                BlockCount = 1;
            }

            //Choice
            Perm.ChunkCount = 1;
            SoundChoice sc = new SoundChoice();
            sc.ChunkCount = (short)BlockCount;
            sc.NameIndex = 0;
            sc.SoundIndex = (short)(Coconuts.SoundChunks.Count);
            Perm.Choices.Add(sc);
            #endregion

            #region Find the Model Raw Start
            int ModelRawStart = (int)map.br.BaseStream.Length;
            for (int i = 0; i < map.Index.TagCount; i++)
            {
                if (map.Tags[i].Class == "mode")
                {
                    //Model-1
                    map.br.BaseStream.Position = map.Tags[i].Offset + 36;
                    Reflexive mode = new Reflexive();
                    mode.Read(map, true);
                    for (int x = 0; x < mode.ChunkCount; x++)
                    {
                        map.br.BaseStream.Position = mode.Translation + (x * 92) + 56;
                        int Offset = map.br.ReadInt32();
                        if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset < ModelRawStart)
                        {
                            ModelRawStart = Offset;
                        }
                    }

                    //Model-2
                    map.br.BaseStream.Position = map.Tags[i].Offset + 116;
                    mode.Read(map, true);
                    for (int x = 0; x < mode.ChunkCount; x++)
                    {
                        map.br.BaseStream.Position = mode.Translation + (x * 88) + 52;
                        int Offset = map.br.ReadInt32();
                        if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset < ModelRawStart)
                        {
                            ModelRawStart = Offset;
                        }
                    }
                }
            }
            #endregion

            #region Add Sound Raw with Padding
            //Goto the Model Raw Start and Save EOF
            List<int> Pointers = new List<int>();
            List<int> Sizes = new List<int>();
            map.br.BaseStream.Position = ModelRawStart;
            byte[] EOF = map.br.ReadBytes((int)(map.br.BaseStream.Length - map.br.BaseStream.Position));
            map.bw.BaseStream.Position = ModelRawStart;

            //Wav
            if (Format != 2)
            {
                //Write each Raw Block, then padding
                for (int i = 0; i < BlockCount; i++)
                {
                    //Save the pointer
                    Pointers.Add((int)map.bw.BaseStream.Position);

                    //Write the Raw in Blocks of 32760 + remainder
                    byte[] Temp = new byte[0];
                    if (i == BlockCount - 1)
                    {
                        Temp = new byte[Buffer.Length - (32760 * i)];
                        Array.Copy(Buffer, 32760 * i, Temp, 0, Buffer.Length - (32760 * i));
                    }
                    else
                    {
                        Temp = new byte[32760];
                        Array.Copy(Buffer, 32760 * i, Temp, 0, 32760);
                    }
                    map.bw.Write(Temp);

                    //Save the Size
                    Sizes.Add(Temp.Length);

                    //Write the Padding
                    int Padding = 512 - ((int)map.bw.BaseStream.Position % 512);
                    map.bw.Write(new byte[Padding]);

                    //Add Shift
                    shift += Temp.Length;
                    shift += Padding;
                }
            }
            //Wma
            else
            {
                //Save the pointer
                Pointers.Add((int)map.bw.BaseStream.Position);

                //Write the Raw
                map.bw.Write(Buffer);

                //Save the Size
                Sizes.Add(Buffer.Length);

                //Write the Padding
                int Padding = 512 - ((int)map.bw.BaseStream.Position % 512);
                map.bw.Write(new byte[Padding]);

                //Add Shift
                shift += Buffer.Length;
                shift += Padding;
            }

            //Write EOF
            map.bw.Write(EOF);
            #endregion

            #region Fix All Raw Offsets
            //Fix all the Raw Offsets
            Reflexive temp = new Reflexive();
            for (int i = 0; i < map.Index.TagCount; i++)
            {
                #region Bitmaps
                if (map.Tags[i].Class == "bitm")
                {
                    map.br.BaseStream.Position = map.Tags[i].Offset + 68 + shift;
                    temp.Read(map, true);
                    for (int x = 0; x < temp.ChunkCount; x++)
                    {
                        map.br.BaseStream.Position = temp.Translation + (x * 116) + 28 + shift;
                        int Offset1 = map.br.ReadInt32();
                        int Offset2 = map.br.ReadInt32();
                        int Offset3 = map.br.ReadInt32();
                        if (Offset1 > 0 && Map.RawLocator(ref Offset1) == RawLocation.Internal)
                        {
                            Offset1 += shift;
                            map.bw.BaseStream.Position = temp.Translation + (x * 116) + 28 + shift;
                            map.bw.Write(Offset1);
                        }
                        if (Offset2 > 0 && Map.RawLocator(ref Offset2) == RawLocation.Internal)
                        {
                            Offset2 += shift;
                            map.bw.BaseStream.Position = temp.Translation + (x * 116) + 28 + shift + 4;
                            map.bw.Write(Offset2);
                        }
                        if (Offset3 > 0 && Map.RawLocator(ref Offset3) == RawLocation.Internal)
                        {
                            Offset3 += shift;
                            map.bw.BaseStream.Position = temp.Translation + (x * 116) + 28 + shift + 8;
                            map.bw.Write(Offset3);
                        }
                    }
                }
                #endregion

                #region Decorators
                else if (map.Tags[i].Class == "DECR")
                {
                    map.br.BaseStream.Position = map.Tags[i].Offset + 56 + shift;
                    int Offset = map.br.ReadInt32();
                    if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                    {
                        map.bw.BaseStream.Position = map.Tags[i].Offset + 56 + shift;
                        map.bw.Write(Offset + shift);
                    }
                }
                #endregion

                #region Animations
                else if (map.Tags[i].Class == "jmad")
                {
                    map.br.BaseStream.Position = map.Tags[i].Offset + 172 + shift;
                    temp.Read(map, true);
                    for (int x = 0; x < temp.ChunkCount; x++)
                    {
                        map.br.BaseStream.Position = temp.Translation + (x * 20) + 8 + shift;
                        int Offset = map.br.ReadInt32();
                        if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                        {
                            map.bw.BaseStream.Position = temp.Translation + (x * 20) + 8 + shift;
                            map.bw.Write(Offset + shift);
                        }
                    }
                }
                #endregion

                #region Lightmaps
                else if (map.Tags[i].Class == "ltmp")
                {
                    //Find the magic for it
                    for (int x = 0; x < map.BSPs.Count; x++)
                    {
                        if (map.Tags[i].Ident == map.BSPs[x].LightmapIdent)
                        {
                            //Lightmap Groups
                            int Offset;
                            map.br.BaseStream.Position = map.BSPs[x].LightmapOffset + 128 + shift;
                            Reflexive r = new Reflexive();
                            r.Read(map, map.BSPs[x].Magic, 0);
                            for (int z = 0; z < r.ChunkCount; z++)
                            {
                                //Clustors
                                map.br.BaseStream.Position = r.Translation + (104 * z) + 32 + shift;
                                Reflexive c = new Reflexive();
                                c.Read(map, map.BSPs[x].Magic, 0);
                                for (int a = 0; a < c.ChunkCount; a++)
                                {
                                    map.br.BaseStream.Position = c.Translation + (84 * a) + 40 + shift;
                                    Offset = map.br.ReadInt32();
                                    if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                                    {
                                        map.bw.BaseStream.Position = c.Translation + (84 * a) + 40 + shift;
                                        map.bw.Write(Offset + shift);
                                    }
                                }

                                //Poop Definitions
                                map.br.BaseStream.Position = r.Translation + (104 * z) + 48 + shift;
                                c.Read(map, map.BSPs[x].Magic, 0);
                                for (int a = 0; a < c.ChunkCount; a++)
                                {
                                    map.br.BaseStream.Position = c.Translation + (84 * a) + 40 + shift;
                                    Offset = map.br.ReadInt32();
                                    if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                                    {
                                        map.bw.BaseStream.Position = c.Translation + (84 * a) + 40 + shift;
                                        map.bw.Write(Offset + shift);
                                    }
                                }

                                //GeometryBuckets
                                map.br.BaseStream.Position = r.Translation + (104 * z) + 64 + shift;
                                c.Read(map, map.BSPs[x].Magic, 0);
                                for (int a = 0; a < c.ChunkCount; a++)
                                {
                                    map.br.BaseStream.Position = c.Translation + (56 * a) + 12 + shift;
                                    Offset = map.br.ReadInt32();
                                    if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                                    {
                                        map.bw.BaseStream.Position = c.Translation + (56 * a) + 12 + shift;
                                        map.bw.Write(Offset + shift);
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion

                #region BSPs
                else if (map.Tags[i].Class == "sbsp")
                {
                    for (int x = 0; x < map.BSPs.Count; x++)
                    {
                        if (map.Tags[i].Ident == map.BSPs[x].BSPIdent)
                        {
                            //Detail Object Raw
                            int Offset;
                            map.br.BaseStream.Position = map.BSPs[x].BSPOffset + 172 + shift;
                            Reflexive r = new Reflexive();
                            r.Read(map, map.BSPs[x].Magic, 0);
                            for (int z = 0; z < r.ChunkCount; z++)
                            {
                                map.br.BaseStream.Position = r.Translation + (176 * z) + 40 + shift;
                                Offset = map.br.ReadInt32();
                                if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                                {
                                    map.bw.BaseStream.Position = r.Translation + (176 * z) + 40 + shift;
                                    map.bw.Write(Offset + shift);
                                }
                            }

                            //BSP Permutations
                            map.br.BaseStream.Position = map.BSPs[x].BSPOffset + 328 + shift;
                            r.Read(map, map.BSPs[x].Magic, 0);
                            for (int z = 0; z < r.ChunkCount; z++)
                            {
                                map.br.BaseStream.Position = r.Translation + 40 + (200 * z) + shift;
                                Offset = map.br.ReadInt32();
                                if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                                {
                                    map.bw.BaseStream.Position = r.Translation + 40 + (200 * z) + shift;
                                    map.bw.Write(Offset + shift);
                                }
                            }

                            //Water Definitions
                            map.br.BaseStream.Position = map.BSPs[x].BSPOffset + 548 + shift;
                            r.Read(map, map.BSPs[x].Magic, 0);
                            for (int z = 0; z < r.ChunkCount; z++)
                            {
                                map.br.BaseStream.Position = r.Translation + (172 * z) + 16 + shift;
                                Offset = map.br.ReadInt32();
                                if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                                {
                                    map.bw.BaseStream.Position = r.Translation + (172 * z) + 16 + shift;
                                    map.bw.Write(Offset + shift);
                                }
                            }

                            //Decorator Raw
                            map.br.BaseStream.Position = map.BSPs[x].BSPOffset + 580 + shift;
                            r.Read(map, map.BSPs[x].Magic, 0);
                            for (int z = 0; z < r.ChunkCount; z++)
                            {
                                map.br.BaseStream.Position = r.Translation + (48 * z) + 16 + shift;
                                Reflexive Cache = new Reflexive();
                                Cache.Read(map, map.BSPs[x].Magic, 0);
                                for (int a = 0; a < Cache.ChunkCount; a++)
                                {
                                    map.br.BaseStream.Position = Cache.Translation + (44 * a) + shift;
                                    Offset = map.br.ReadInt32();
                                    if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                                    {
                                        map.bw.BaseStream.Position = Cache.Translation + (44 * a) + shift;
                                        map.bw.Write(Offset + shift);
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion

                #region Models
                else if (map.Tags[i].Class == "mode")
                {
                    //Model-1
                    map.br.BaseStream.Position = map.Tags[i].Offset + 36 + shift;
                    temp.Read(map, true);
                    for (int x = 0; x < temp.ChunkCount; x++)
                    {
                        map.br.BaseStream.Position = temp.Translation + (x * 92) + 56 + shift;
                        int Offset = map.br.ReadInt32();
                        if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                        {
                            map.bw.BaseStream.Position = temp.Translation + (x * 92) + 56 + shift;
                            map.bw.Write(Offset + shift);
                        }
                    }

                    //Model-2
                    map.br.BaseStream.Position = map.Tags[i].Offset + 116 + shift;
                    temp.Read(map, true);
                    for (int x = 0; x < temp.ChunkCount; x++)
                    {
                        map.br.BaseStream.Position = temp.Translation + (x * 88) + 52 + shift;
                        int Offset = map.br.ReadInt32();
                        if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                        {
                            map.bw.BaseStream.Position = temp.Translation + (x * 88) + 52 + shift;
                            map.bw.Write(Offset + shift);
                        }
                    }
                }
                #endregion

                #region Particles
                else if (map.Tags[i].Class == "PRTM")
                {
                    map.br.BaseStream.Position = map.Tags[i].Offset + 160 + shift;
                    int Offset = map.br.ReadInt32();
                    if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                    {
                        map.bw.BaseStream.Position = map.Tags[i].Offset + 160 + shift;
                        map.bw.Write(Offset + shift);
                    }
                }
                #endregion

                #region Weather
                else if (map.Tags[i].Class == "weat")
                {
                    map.br.BaseStream.Position = map.Tags[i].Offset + shift;
                    temp.Read(map, true);
                    for (int x = 0; x < temp.ChunkCount; x++)
                    {
                        map.br.BaseStream.Position = temp.Translation + (x * 140) + 64 + shift;
                        int Offset = map.br.ReadInt32();
                        if (Map.RawLocator(ref Offset) == RawLocation.Internal && Offset > 0)
                        {
                            map.bw.BaseStream.Position = temp.Translation + (x * 140) + 64 + shift;
                            map.bw.Write(Offset + shift);
                        }
                    }
                }
                #endregion

                #region Coconuts
                else if (map.Tags[i].Class == "ugh!")
                {
                    //Model Pointers
                    for (int x = 0; x < Coconuts.ModelRawBlocks.Count; x++)
                    {
                        if (Coconuts.ModelRawBlocks[x].RawLocation == RawLocation.Internal && Coconuts.ModelRawBlocks[x].Offset > 0)
                        {
                            map.bw.BaseStream.Position = Coconuts.ModelRawBlocks[x].Pointer + shift;
                            map.bw.Write(Coconuts.ModelRawBlocks[x].Offset + shift);
                        }
                    }
                }
                #endregion
            }
            #endregion

            #region Update
            //Update all header info
            map.Header.IndexOffset += shift;
            map.Header.CrazyOffset += shift;
            map.Header.FileTableIndexOffset += shift;
            map.Header.FileTableOffset += shift;
            map.Header.Script128Offset += shift;
            map.Header.ScriptIndexOffset += shift;
            map.Header.ScriptTableOffset += shift;

            //Shift Unicode
            map.br.BaseStream.Position = map.Tags[0].Offset + 400 + shift;
            for (int x = 0; x < 8; x++)
            {
                map.br.BaseStream.Position += 8;
                int Index = map.br.ReadInt32();
                int Table = map.br.ReadInt32();
                if (Index > 0 && Table > 0)
                {
                    map.bw.BaseStream.Position -= 8;
                    map.bw.Write(Index + shift);
                    map.bw.Write(Table + shift);
                }
                map.br.BaseStream.Position += 12;
            }

            //Shift Crazy Pointers
            map.bw.BaseStream.Position = map.Tags[0].Offset + 632 + shift;
            map.bw.Write(map.Header.CrazyOffset);
            map.bw.Write(map.Header.CrazyOffset);

            //Update BSP Info
            map.br.BaseStream.Position = map.Tags[3].Offset + 528 + shift;
            temp.Read(map, true);
            for (int x = 0; x < temp.ChunkCount; x++)
            {
                //We only need to update the BSP Offset, because it is part of the magic
                map.bw.BaseStream.Position = temp.Translation + (x * 68) + shift;
                map.bw.Write(map.BSPs[x].BSPOffset + shift);
            }
            #endregion

            #region Add new Chunks to Ugh!
            int Ughshift = 0;

            #region Sound Permutations
            //Permutations Reflexive
            map.br.BaseStream.Position = map.Tags[map.Tags.Count - 1].Offset + 32 + shift;
            Reflexive t = new Reflexive();
            t.Read(map, true);

            //Update ChunkCount
            map.bw.BaseStream.Position = map.Tags[map.Tags.Count - 1].Offset + 32 + shift;
            map.bw.Write(t.ChunkCount + 1);

            //Save EOF
            map.br.BaseStream.Position = t.Translation + (t.ChunkCount * 12) + shift;
            EOF = map.br.ReadBytes((int)(map.br.BaseStream.Length - map.br.BaseStream.Position));
            map.bw.BaseStream.Position = t.Translation + (t.ChunkCount * 12) + shift;

            //Sound Permutation
            //map.bw.Write((short)457);
            map.bw.Write(Perm.Unknown1);
            map.bw.Write(Perm.Unknown2);
            map.bw.Write(Perm.Unknown3);
            map.bw.Write(Perm.ChunkNumber);
            //map.bw.Write((short)-1);
            map.bw.Write(Perm.Unknown4);
            map.bw.Write(Perm.ChoiceIndex);
            map.bw.Write(Perm.ChunkCount);
            map.bw.Write(EOF);
            Ughshift += 12;
            #endregion

            #region Sound Choices
            //Choices Reflexive
            map.br.BaseStream.Position = map.Tags[map.Tags.Count - 1].Offset + 40 + shift;
            t.Read(map, true);

            //Update ChunkCount and Translation
            map.bw.BaseStream.Position = map.Tags[map.Tags.Count - 1].Offset + 40 + shift;
            map.bw.Write(t.ChunkCount + Perm.Choices.Count);
            map.bw.Write((t.Translation + Ughshift) + map.Index.SecondaryMagic);

            //Save EOF
            map.br.BaseStream.Position = t.Translation + (t.ChunkCount * 16) + shift + Ughshift;
            EOF = map.br.ReadBytes((int)(map.br.BaseStream.Length - map.br.BaseStream.Position));
            map.bw.BaseStream.Position = t.Translation + (t.ChunkCount * 16) + shift + Ughshift;

            //Write New Chunks
            for (int i = 0; i < Perm.Choices.Count; i++)
            {
                map.bw.Write(Perm.Choices[i].NameIndex);
                map.bw.Write(Perm.Choices[i].Unknown1);
                map.bw.Write(Perm.Choices[i].Unknown2);
                map.bw.Write(Perm.Choices[i].SoundIndex);
                map.bw.Write(Perm.Choices[i].ChunkCount);
                Ughshift += 16;
            }
            map.bw.Write(EOF);
            #endregion

            #region Shift
            //Unknown
            map.br.BaseStream.Position = map.Tags[map.Tags.Count - 1].Offset + 48 + shift;
            t.Read(map, true);
            for (int i = 0; i < t.ChunkCount; i++)
            {
                //Unknown
                map.br.BaseStream.Position = t.Translation + shift + Ughshift + 20;
                Reflexive u = new Reflexive();
                u.Read(map, true);
                u.Translation += Ughshift;
                u.Write(map, true);
            }
            t.Translation += Ughshift;
            t.Write(map, true);

            //Zero
            map.br.BaseStream.Position = map.Tags[map.Tags.Count - 1].Offset + 56 + shift;
            t.Read(map, true);
            t.Translation += Ughshift;
            t.Write(map, true);
            #endregion

            #region Sound Raw Blocks
            //Sound Raw Blocks
            map.br.BaseStream.Position = map.Tags[map.Tags.Count - 1].Offset + 64 + shift;
            t.Read(map, true);

            //Update ChunkCount and Translation
            map.bw.BaseStream.Position = map.Tags[map.Tags.Count - 1].Offset + 64 + shift;
            map.bw.Write(t.ChunkCount + Perm.Choices[0].ChunkCount);
            map.bw.Write((t.Translation + Ughshift) + map.Index.SecondaryMagic);

            //Save EOF
            map.br.BaseStream.Position = t.Translation + (t.ChunkCount * 12) + shift + Ughshift;
            EOF = map.br.ReadBytes((int)(map.br.BaseStream.Length - map.br.BaseStream.Position));
            map.bw.BaseStream.Position = t.Translation + (t.ChunkCount * 12) + shift + Ughshift;

            //Write the new Chunks
            for (int i = 0; i < Perm.Choices[0].ChunkCount; i++)
            {
                map.bw.Write(Pointers[i]);
                if (Format == 2)
                {
                    map.bw.Write((uint)(Sizes[i] | 0x80000000));
                }
                else
                {
                    map.bw.Write(Sizes[i]);
                }
                map.bw.Write(-1);
                Ughshift += 12;
            }
            map.bw.Write(EOF);
            #endregion

            #region Shift
            //Unknown
            map.br.BaseStream.Position = map.Tags[map.Tags.Count - 1].Offset + 72 + shift;
            t.Read(map, true);
            for (int i = 0; i < t.ChunkCount; i++)
            {
                //Unknown
                map.br.BaseStream.Position = t.Translation + shift + Ughshift;
                Reflexive u = new Reflexive();
                u.Read(map, true);
                u.Translation += Ughshift;
                u.Write(map, true);

                //Unknown
                map.br.BaseStream.Position = t.Translation + shift + Ughshift + 8;
                u.Read(map, true);
                u.Translation += Ughshift;
                u.Write(map, true);
            }
            t.Translation += Ughshift;
            t.Write(map, true);

            //Coconut Model
            map.br.BaseStream.Position = map.Tags[map.Tags.Count - 1].Offset + 80 + shift;
            t.Read(map, true);
            for (int i = 0; i < t.ChunkCount; i++)
            {
                //Unknown
                map.br.BaseStream.Position = t.Translation + shift + Ughshift + 24;
                Reflexive u = new Reflexive();
                u.Read(map, true);
                u.Translation += Ughshift;
                u.Write(map, true);
            }
            t.Translation += Ughshift;
            t.Write(map, true);
            #endregion

            #region Fix Ugh MetaSize
            map.bw.BaseStream.Position = map.Tags[map.Tags.Count - 1].IndexOffset + 12 + shift;
            map.bw.Write(map.Tags[map.Tags.Count - 1].Size + Ughshift);
            #endregion
            #endregion

            #region Update Sound Tag
            map.bw.BaseStream.Position = map.SelectedTag.Offset + shift + 4;
            map.bw.Write(Format);
            map.bw.BaseStream.Position = map.SelectedTag.Offset + shift + 8;
            map.bw.Write((short)Coconuts.Permutations.Count);
            map.bw.Write((short)1);
            #endregion

            #region Clean Up
            //Write new Padding
            map.bw.BaseStream.Position = map.Header.MetaTableOffset + map.Header.MetaTableSize + shift + Ughshift;
            int pad = 512 - ((int)map.bw.BaseStream.Position % 512);
            for (int i = 0; i < pad; i++)
            {
                map.bw.Write((byte)205);
            }

            //Header
            map.bw.BaseStream.Position = 8;
            map.bw.Write((int)map.bw.BaseStream.Length);
            map.bw.BaseStream.Position += 4;
            map.bw.Write(map.Header.IndexOffset);
            map.bw.BaseStream.Position += 4;
            map.bw.Write(map.Header.MetaTableSize + Ughshift);
            map.bw.Write(map.Header.NonRawSize + Ughshift);
            map.bw.BaseStream.Position = 344;
            map.bw.Write(map.Header.CrazyOffset);
            map.bw.BaseStream.Position += 4;
            map.bw.Write(map.Header.Script128Offset);
            map.bw.BaseStream.Position += 8;
            map.bw.Write(map.Header.ScriptIndexOffset);
            map.bw.Write(map.Header.ScriptTableOffset);
            map.bw.BaseStream.Position = 708;
            map.bw.Write(map.Header.FileTableOffset);
            map.bw.BaseStream.Position += 4;
            map.bw.Write(map.Header.FileTableIndexOffset);

            //Sign the map
            int size = (int)map.br.BaseStream.Length - 2048;
            int times = size / 4;
            int result = 0;

            map.br.BaseStream.Position = 2048;
            for (int x = 0; x < times; x++)
            {
                result ^= map.br.ReadInt32();
            }

            map.bw.BaseStream.Position = 720;
            map.bw.Write(result);
            #endregion
        }
    }
}