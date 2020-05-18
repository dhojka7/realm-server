using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.Entities
{
    public interface IContainer
    {
        public int[] Inventory { get; set; }
        public int[] ItemDatas { get; set; }
        public void UpdateInventory();
        public void UpdateInventorySlot(int slot);
        public bool ValidSlot(int slot);
    }

    public class Container : Entity, IContainer
    {
        public const int MaxSlots = 8;

        public const ushort BrownBag = 0x0500;
        public const ushort PurpleBag = 0x0506;
        public const ushort CyanBag = 0x0507;
        public const ushort BlueBag = 0x0508;
        public const ushort WhiteBag = 0x0509;
        public static ushort FromBagType(int bagType) 
        {
            switch (bagType) 
            {
                case 0: return BrownBag;
                case 1: return PurpleBag;
                case 2: return CyanBag;
                case 3: return BlueBag;
                case 4: return WhiteBag;
            }
            throw new Exception("Invalid bag type");
        }

        public int OwnerId = -1;
        public int[] Inventory { get; set; }
        public int[] ItemDatas { get; set; }

        public Container(ushort type, int ownerId, int? lifetime) : base(type, lifetime)
        {
            OwnerId = ownerId;
            Inventory = new int[MaxSlots];
            ItemDatas = new int[MaxSlots];
            for (int i = 0; i < MaxSlots; i++)
            {
                Inventory[i] = -1;
                ItemDatas[i] = -1;
            }
        }

        public override void Tick()
        {
            bool disappear = true;
            for (int i = 0; i < MaxSlots; i++)
                if (Inventory[i] != -1)
                {
                    disappear = false;
                    break;
                }

            if (disappear)
            {
                Parent.RemoveEntity(this);
                return;
            }

            base.Tick();
        }

        public bool ValidSlot(int slot)
        {
            if (slot < 0 || slot >= MaxSlots)
                return false;
            return true;
        }

        public void UpdateInventory()
        {
            for (int k = 0; k < MaxSlots; k++)
                UpdateInventorySlot(k);
        }

        public void UpdateInventorySlot(int slot)
        {
#if DEBUG
            if (slot < 0 || slot >= MaxSlots)
                throw new Exception("Out of bounds slot update attempt.");
#endif
            switch (slot)
            {
                case 0: 
                    SetSV(StatType.Inventory_0, Inventory[0]);
                    SetSV(StatType.ItemData_0, ItemDatas[0]);
                    break;
                case 1: 
                    SetSV(StatType.Inventory_1, Inventory[1]);
                    SetSV(StatType.ItemData_1, ItemDatas[1]);
                    break;
                case 2: 
                    SetSV(StatType.Inventory_2, Inventory[2]);
                    SetSV(StatType.ItemData_2, ItemDatas[2]);
                    break;
                case 3: 
                    SetSV(StatType.Inventory_3, Inventory[3]);
                    SetSV(StatType.ItemData_3, ItemDatas[3]);
                    break;
                case 4: 
                    SetSV(StatType.Inventory_4, Inventory[4]);
                    SetSV(StatType.ItemData_4, ItemDatas[4]);
                    break;
                case 5: 
                    SetSV(StatType.Inventory_5, Inventory[5]);
                    SetSV(StatType.ItemData_5, ItemDatas[5]);
                    break;
                case 6: 
                    SetSV(StatType.Inventory_6, Inventory[6]);
                    SetSV(StatType.ItemData_6, ItemDatas[6]);
                    break;
                case 7:
                    SetSV(StatType.Inventory_7, Inventory[7]);
                    SetSV(StatType.ItemData_7, ItemDatas[7]);
                    break;
            }
        }
    }
}
