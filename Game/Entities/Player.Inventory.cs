using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RotMG.Game.Entities
{
    public partial class Player
    {
        private const float ContainerMinimumDistance = 2f;

        private const int MaxSlotsWithoutBackpack = 12;
        private const int MaxSlotsWithBackpack = MaxSlotsWithoutBackpack + 8;

        private const byte HealthPotionSlotId = 254;
        private const byte MagicPotionSlotId = 255;

        private const int HealthPotionItemType = 2594;
        private const int MagicPotionItemType = 2595;

        private static byte[] InvalidInvSwap = GameServer.InvResult(1);
        private static byte[] ValidInvSwap = GameServer.InvResult(0);

        //From IContainer :)
        public int[] Inventory { get; set; }
        public int[] ItemDatas { get; set; }

        public void InitInventory(CharacterModel character)
        {
            Inventory = character.Inventory.ToArray();
            ItemDatas = character.ItemDatas.ToArray();
            UpdateInventory();
        }

        public void RecalculateEquipBonuses()
        {
            for (int i = 0; i < 8; i++)
                Boosts[i] = 0;

            for (int i = 0; i < 4; i++)
            {
                if (Inventory[i] == -1)
                    continue;

                ItemDesc item = Resources.Type2Item[(ushort)Inventory[i]];
                foreach (KeyValuePair<int, int> s in item.StatBoosts)
                    Boosts[s.Key] += s.Value;

                int data = ItemDatas[i];
                if (data == -1)
                    continue;

                Boosts[0] += (int)(ItemDesc.GetStat(data, ItemData.MaxHP, 5));
                Boosts[1] += (int)(ItemDesc.GetStat(data, ItemData.MaxMP, 5));
                Boosts[2] += (int)(ItemDesc.GetStat(data, ItemData.Attack, 1));
                Boosts[3] += (int)(ItemDesc.GetStat(data, ItemData.Defense, 1));
                Boosts[4] += (int)(ItemDesc.GetStat(data, ItemData.Speed, 1));
                Boosts[5] += (int)(ItemDesc.GetStat(data, ItemData.Dexterity, 1));
                Boosts[6] += (int)(ItemDesc.GetStat(data, ItemData.Vitality, 1));
                Boosts[7] += (int)(ItemDesc.GetStat(data, ItemData.Wisdom, 1));
            }

            UpdateStats();
        }

        public ItemDesc GetItem(int index)
        {
#if DEBUG
            if (index < 0 || index > (HasBackpack ? MaxSlotsWithBackpack : MaxSlotsWithoutBackpack))
                throw new Exception("GetItem index out of bounds");
#endif
            if (Inventory[index] == -1)
                return null;
            return Resources.Type2Item[(ushort)Inventory[index]];
        }

        public bool GiveItem(ushort type)
        {
            int slot = GetFreeInventorySlot();
            if (slot == -1)
                return false;
            Inventory[slot] = type;
            UpdateInventorySlot(slot);
            return true;
        }

        public int GetFreeInventorySlot()
        {
            int maxSlots = HasBackpack ? MaxSlotsWithBackpack : MaxSlotsWithoutBackpack;
            for (int i = 4; i < maxSlots; i++)
                if (Inventory[i] == -1)
                    return i;
            return -1;
        }

        public void DropItem(byte slot)
        {
            UpdateInventorySlot(slot);

            if (!ValidSlot(slot))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Invalid slot");
#endif
                return;
            }

            int item = Inventory[slot];
            int data = ItemDatas[slot];
            if (item == -1)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Nothing to drop");
#endif
                return;
            }

            Inventory[slot] = -1;
            ItemDatas[slot] = -1;
            UpdateInventorySlot(slot);

            Container container = new Container(Container.PurpleBag, Id, 120000);
            container.Inventory[0] = item;
            container.ItemDatas[0] = data;
            container.UpdateInventorySlot(0);

            RecalculateEquipBonuses();
            Parent.AddEntity(container, Position + MathUtils.Position(.2f, .2f));
        }

        public void SwapItem(SlotData slot1, SlotData slot2)
        {
            Entity en1 = Parent.GetEntity(slot1.ObjectId);
            Entity en2 = Parent.GetEntity(slot2.ObjectId);

            (en1 as IContainer)?.UpdateInventorySlot(slot1.SlotId);
            (en2 as IContainer)?.UpdateInventorySlot(slot2.SlotId);
            
            //Undefined entities
            if (en1 == null || en2 == null)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Undefined entities");
#endif
                Client.Send(InvalidInvSwap);
                return;
            }
            
            //Entities which are not containers???
            if (!(en1 is IContainer) || !(en2 is IContainer))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Not containers");
#endif
                Client.Send(InvalidInvSwap);
                return;
            }

            if (en1.Position.Distance(en2) > ContainerMinimumDistance)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Too far away from container");
#endif
                Client.Send(InvalidInvSwap);
                return;
            }

            //Player manipulation attempt
            if ((en1 is Player && slot1.ObjectId != Id) ||
                (en2 is Player && slot2.ObjectId != Id))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Player manipulation attempt");
#endif
                Client.Send(InvalidInvSwap);
                return;
            }

            //Container manipulation attempt
            if ((en1 is Container && 
                (en1 as Container).OwnerId != -1 && 
                Id != (en1 as Container).OwnerId) ||
             (en2 is Container && 
             (en2 as Container).OwnerId != -1 && 
             Id != (en2 as Container).OwnerId))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Container manipulation attempt");
#endif
                Client.Send(InvalidInvSwap);
                return;
            }

            IContainer con1 = en1 as IContainer;
            IContainer con2 = en2 as IContainer;

            //Invalid slots
            if (!con1.ValidSlot(slot1.SlotId) || !con2.ValidSlot(slot2.SlotId))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Invalid inv swap");
#endif
                Client.Send(InvalidInvSwap);
                return;
            }

            //Invalid slot types
            int item1 = con1.Inventory[slot1.SlotId];
            int data1 = con1.ItemDatas[slot1.SlotId];
            int item2 = con2.Inventory[slot2.SlotId];
            int data2 = con2.ItemDatas[slot2.SlotId];
            PlayerDesc d = Desc as PlayerDesc;
            ItemDesc d1;
            ItemDesc d2;
            Resources.Type2Item.TryGetValue((ushort)item1, out d1);
            Resources.Type2Item.TryGetValue((ushort)item2, out d2);

            if (con1 is Player)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (slot1.SlotId == i)
                    {
                        if ((d1 != null && d.SlotTypes[i] != d1.SlotType) ||
                            (d2 != null && d.SlotTypes[i] != d2.SlotType))
                        {
#if DEBUG
                            Program.Print(PrintType.Error, "Invalid slot type");
#endif
                            Client.Send(InvalidInvSwap);
                            return;
                        }
                    }
                }
            }

            if (con2 is Player)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (slot2.SlotId == i)
                    {
                        if ((d1 != null && d.SlotTypes[i] != d1.SlotType) ||
                            (d2 != null && d.SlotTypes[i] != d2.SlotType))
                        {
#if DEBUG
                            Program.Print(PrintType.Error, "Invalid slot type");
#endif
                            Client.Send(InvalidInvSwap);
                            return;
                        }
                    }
                }
            }

            con1.Inventory[slot1.SlotId] = item2;
            con1.ItemDatas[slot1.SlotId] = data2;
            con2.Inventory[slot2.SlotId] = item1;
            con2.ItemDatas[slot2.SlotId] = data1;
            con1.UpdateInventorySlot(slot1.SlotId);
            con2.UpdateInventorySlot(slot2.SlotId);
            RecalculateEquipBonuses();
            Client.Send(ValidInvSwap);
        }

        public bool ValidSlot(int slot)
        {
            int maxSlots = HasBackpack ? MaxSlotsWithBackpack : MaxSlotsWithoutBackpack;
            if (slot < 0 || slot >= maxSlots)
                return false;
            return true;
        }

        public void UpdateInventory()
        {
            int length = HasBackpack ? MaxSlotsWithBackpack : MaxSlotsWithoutBackpack;
            for (int k = 0; k < length; k++)
                UpdateInventorySlot(k);
        }

        public void UpdateInventorySlot(int slot)
        {
#if DEBUG
            if (!HasBackpack && slot >= MaxSlotsWithoutBackpack)
                throw new Exception("Should not be updating backpack stats when there is no backpack present.");
            if (slot < 0 || slot >= MaxSlotsWithBackpack)
                throw new Exception("Out of bounds slot update attempt.");
#endif
            switch (slot)
            {
                case 0: 
                    SetSV(StatType.Inventory_0, Inventory[0]);
                    SetPrivateSV(StatType.ItemData_0, ItemDatas[0]);
                    break;
                case 1: 
                    SetSV(StatType.Inventory_1, Inventory[1]);
                    SetPrivateSV(StatType.ItemData_1, ItemDatas[1]);
                    break;
                case 2: 
                    SetSV(StatType.Inventory_2, Inventory[2]);
                    SetPrivateSV(StatType.ItemData_2, ItemDatas[2]);
                    break;
                case 3: 
                    SetSV(StatType.Inventory_3, Inventory[3]);
                    SetPrivateSV(StatType.ItemData_3, ItemDatas[3]);
                    break;
                case 4: 
                    SetPrivateSV(StatType.Inventory_4, Inventory[4]);
                    SetPrivateSV(StatType.ItemData_4, ItemDatas[4]);
                    break;
                case 5: 
                    SetPrivateSV(StatType.Inventory_5, Inventory[5]);
                    SetPrivateSV(StatType.ItemData_5, ItemDatas[5]);
                    break;
                case 6: 
                    SetPrivateSV(StatType.Inventory_6, Inventory[6]);
                    SetPrivateSV(StatType.ItemData_6, ItemDatas[6]);
                    break;
                case 7: 
                    SetPrivateSV(StatType.Inventory_7, Inventory[7]);
                    SetPrivateSV(StatType.ItemData_7, ItemDatas[7]);
                    break;
                case 8: 
                    SetPrivateSV(StatType.Inventory_8, Inventory[8]);
                    SetPrivateSV(StatType.ItemData_8, ItemDatas[8]);
                    break;
                case 9: 
                    SetPrivateSV(StatType.Inventory_9, Inventory[9]);
                    SetPrivateSV(StatType.ItemData_9, ItemDatas[9]);
                    break;
                case 10: 
                    SetPrivateSV(StatType.Inventory_10, Inventory[10]);
                    SetPrivateSV(StatType.ItemData_10, ItemDatas[10]);
                    break;
                case 11: 
                    SetPrivateSV(StatType.Inventory_11, Inventory[11]);
                    SetPrivateSV(StatType.ItemData_11, ItemDatas[11]);
                    break;
                case 12: 
                    SetPrivateSV(StatType.Backpack_0, Inventory[12]);
                    SetPrivateSV(StatType.ItemData_12, ItemDatas[12]);
                    break;
                case 13: 
                    SetPrivateSV(StatType.Backpack_1, Inventory[13]);
                    SetPrivateSV(StatType.ItemData_13, ItemDatas[13]);
                    break;
                case 14: 
                    SetPrivateSV(StatType.Backpack_2, Inventory[14]);
                    SetPrivateSV(StatType.ItemData_14, ItemDatas[14]);
                    break;
                case 15: 
                    SetPrivateSV(StatType.Backpack_3, Inventory[15]);
                    SetPrivateSV(StatType.ItemData_15, ItemDatas[15]);
                    break;
                case 16: 
                    SetPrivateSV(StatType.Backpack_4, Inventory[16]);
                    SetPrivateSV(StatType.ItemData_16, ItemDatas[16]);
                    break;
                case 17: 
                    SetPrivateSV(StatType.Backpack_5, Inventory[17]);
                    SetPrivateSV(StatType.ItemData_17, ItemDatas[17]);
                    break;
                case 18: 
                    SetPrivateSV(StatType.Backpack_6, Inventory[18]);
                    SetPrivateSV(StatType.ItemData_18, ItemDatas[18]);
                    break;
                case 19: 
                    SetPrivateSV(StatType.Backpack_7, Inventory[19]);
                    SetPrivateSV(StatType.ItemData_19, ItemDatas[19]);
                    break;
            }
        }
    }
}
