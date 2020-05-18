using Ionic.Zlib;
using Newtonsoft.Json;
using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace RotMG.Game
{
    public enum Region
    {
        None,
        Spawn,
        Regen,
        Blocks_Sight,
        Note,
        Enemy_1,
        Enemy_2,
        Enemy_3,
        Enemy_4,
        Enemy_5,
        Enemy_6,
        Decoration_1,
        Decoration_2,
        Decoration_3,
        Decoration_4,
        Decoration_5,
        Decoration_6,
        Trigger_1,
        Callback_1,
        Trigger_2,
        Callback_2,
        Trigger_3,
        Callback_3,
        Trigger_4,
        Callback_4,
        Store_1,
        Store_2,
        Store_3,
        Store_4
    }

    public struct JSTile
    {
        public ushort GroundType;
        public ushort ObjectType;
        public Region Region;
        public string Key;
    }

    public class JSMap
    {
        public JSTile[,] Tiles;
        public int Width;
        public int Height;
        public Dictionary<Region, List<IntPoint>> Regions;

        public JSMap(string data)
        {
            json_dat json = JsonConvert.DeserializeObject<json_dat>(data);
            byte[] buffer = ZlibStream.UncompressBuffer(json.data);
            Dictionary<ushort, JSTile> dict = new Dictionary<ushort, JSTile>();
            JSTile[,] tiles = new JSTile[json.width, json.height];

            for (int i = 0; i < json.dict.Length; i++)
            {
                loc o = json.dict[i];
                dict[(ushort)i] = new JSTile
                {
                    GroundType = o.ground == null ? (ushort)255 : Resources.Id2Tile[o.ground].Type,
                    ObjectType = o.objs == null ? (ushort)255 : Resources.Id2Object[o.objs[0].id].Type,
                    Key = o.objs == null ? null : o.objs[0].name,
                    Region = o.regions == null ? Region.None : (Region)Enum.Parse(typeof(Region), o.regions[0].id.Replace(' ', '_'))
                };
            }

            using (PacketReader rdr = new PacketReader(new MemoryStream(buffer)))
            {
                for (int y = 0; y < json.height; y++)
                    for (int x = 0; x < json.width; x++)
                        tiles[x, y] = dict[(ushort)rdr.ReadInt16()];
            }

            //Add composite under cave walls
            for (int x = 0; x < json.width; x++)
            {
                for (int y = 0; y < json.height; y++)
                {
                    if (tiles[x, y].ObjectType != 255)
                    {
                        ObjectDesc desc = Resources.Type2Object[tiles[x, y].ObjectType];
                        if ((desc.CaveWall || desc.ConnectedWall) && tiles[x, y].GroundType == 255)
                        {
                            tiles[x, y].GroundType = 0xfd;
                        }
                    }
                }
            }

            Tiles = tiles;
            Width = json.width;
            Height = json.height;

            InitRegions();
        } 

        public void InitRegions()
        {
            Regions = new Dictionary<Region, List<IntPoint>>();
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    JSTile tile = Tiles[x, y];
                    if (!Regions.ContainsKey(tile.Region))
                        Regions[tile.Region] = new List<IntPoint>();
                    Regions[tile.Region].Add(new IntPoint(x, y));
                }
        }

        private struct json_dat
        {
            public byte[] data { get; set; }
            public loc[] dict { get; set; }
            public int height { get; set; }
            public int width { get; set; }
        }

        private struct loc
        {
            public string ground { get; set; }
            public obj[] objs { get; set; }
            public obj[] regions { get; set; }
        }

        private struct obj
        {
            public string id { get; set; }
            public string name { get; set; }
        }
    }

}
