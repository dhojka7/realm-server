using RotMG.Common;
using RotMG.Game.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.Logic.Loots
{
    public class ItemLoot : Loot
    {
        public readonly ushort Item;
        public readonly float Threshold;
        public readonly float Chance;
        public readonly int Min;

        public ItemLoot(string item, float chance, float threshold = 0, int min = 0)
        {
            Item = Resources.IdLower2Item[item.ToLower()].Type;
            Threshold = threshold;
            Chance = chance;
            Min = min;
        }

        public override int TryObtainItem(Entity host, Player player, int position, float threshold)
        {
            return Item;
        }
    }
}
