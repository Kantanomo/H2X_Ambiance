using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaloMap
{
    public class StringID
    {
        public void Read(Map map)
        {
            map.br.BaseStream.Position = map.Header.ScriptTableOffset;
            for (int i = 0; i < map.Header.ScriptCount; i++)
            {
                char c;
                string s = "";
                while ((c = map.br.ReadChar()) != '\0')
                    s += c;
                map.SIDs.Add(s);
            }
        }
    }
}
