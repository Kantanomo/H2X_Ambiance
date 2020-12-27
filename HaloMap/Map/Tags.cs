using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic;
using System.Windows.Forms;

namespace HaloMap
{
    public class Tag
    {
        public string Class, Path;
        public int Ident, Offset, Size, IndexOffset;
    }

    public class Tags
    {
        public void Read(Map map, TreeView tv)
        {
            map.br.BaseStream.Position = map.Index.TagStart;
            for (int i = 0; i < map.Index.TagCount; i++)
            {
                Tag t = new Tag();
                t.IndexOffset = (int)map.br.BaseStream.Position;
                t.Class = Strings.StrReverse(new string(map.br.ReadChars(4)));
                t.Ident = map.br.ReadInt32();
                t.Offset = map.br.ReadInt32() - map.Index.SecondaryMagic;
                t.Size = map.br.ReadInt32();
                map.Tags.Add(t);
            }

            map.br.BaseStream.Position = map.Header.FileTableOffset;
            for (int i = 0; i < map.Index.TagCount; i++)
            {
                char c;
                while ((c = map.br.ReadChar()) != '\0')
                    map.Tags[i].Path += c;
            }

            //TreeView
            for (int i = 0; i < map.Index.TagCount; i++)
            {
                if (map.Tags[i].Class == "snd!")
                {
                    tv.Nodes.Add(map.Tags[i].Path);
                }
            }
            tv.Sort();
        }

        public Tag FindTag(Map map, string Class, string Path)
        {
            for (int i = 0; i < map.Index.TagCount; i++)
            {
                if (map.Tags[i].Class == Class && map.Tags[i].Path == Path)
                {
                    return map.Tags[i];
                }
            }
            return null;
        }
    }
}
