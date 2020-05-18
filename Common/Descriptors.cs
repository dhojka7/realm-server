using Microsoft.VisualBasic.CompilerServices;
using RotMG.Game;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Xml.Linq;

namespace RotMG.Common
{
    public enum ItemData : ulong
    {
        //Tiers
        T0 = 1 << 0,
        T1 = 1 << 1,
        T2 = 1 << 2,
        T3 = 1 << 3,
        T4 = 1 << 4,
        T5 = 1 << 5,
        T6 = 1 << 6,
        T7 = 1 << 7,

        //Bonuses
        MaxHP = 1 << 8,
        MaxMP = 1 << 9,
        Attack = 1 << 10,
        Defense = 1 << 11,
        Speed = 1 << 12,
        Dexterity = 1 << 13,
        Vitality = 1 << 14,
        Wisdom = 1 << 15,
        RateOfFire = 1 << 16,
        Damage = 1 << 17,
        Cooldown = 1 << 18,
        FameBonus = 1 << 19
    }

    [Flags]
    public enum ConditionEffects : ulong
    {
        Nothing = 1 << 0,
        Quiet = 1 << 1,
        Weak = 1 << 2,
        Slowed = 1 << 3,
        Sick = 1 << 4,
        Dazed = 1 << 5,
        Stunned = 1 << 6,
        Blind = 1 << 7,
        Hallucinating = 1 << 8,
        Drunk = 1 << 9,
        Confused = 1 << 10,
        StunImmume = 1 << 11,
        Invisible = 1 << 12,
        Paralyzed = 1 << 13,
        Speedy = 1 << 14,
        Bleeding = 1 << 15,
        Healing = 1 << 16,
        Damaging = 1 << 17,
        Berserk = 1 << 18,
        Stasis = 1 << 19,
        StasisImmune = 1 << 20,
        Invincible = 1 << 21,
        Invulnerable = 1 << 23,
        Armored = 1 << 24,
        ArmorBroken = 1 << 25,
        Hexed = 1 << 26,
        NinjaSpeedy = 1 << 27,
    }

    public enum ConditionEffectIndex
    {
        Nothing = 0,
        Quiet = 1,
        Weak = 2,
        Slowed = 3,
        Sick = 4,
        Dazed = 5,
        Stunned = 6,
        Blind = 7,
        Hallucinating = 8,
        Drunk = 9,
        Confused = 10,
        StunImmune = 11,
        Invisible = 12,
        Paralyzed = 13,
        Speedy = 14,
        Bleeding = 15,
        Healing = 16,
        Damaging = 17,
        Berserk = 18,
        Stasis = 19,
        StasisImmune = 20,
        Invincible = 21,
        Invulnerable = 22,
        Armored = 23,
        ArmorBroken = 24,
        Hexed = 25,
    }

    public enum ActivateEffectIndex
    {
        Create,
        Dye,
        Shoot,
        IncrementStat,
        Heal,
        Magic,
        HealNova,
        StatBoostSelf,
        StatBoostAura,
        BulletNova,
        ConditionEffectSelf,
        ConditionEffectAura,
        Teleport,
        PoisonGrenade,
        VampireBlast,
        Trap,
        StasisBlast,
        Pet,
        Decoy,
        Lightning,
        UnlockPortal,
        MagicNova,
        ClearConditionEffectAura,
        RemoveNegativeConditions,
        ClearConditionEffectSelf,
        ClearConditionsEffectSelf,
        RemoveNegativeConditionsSelf,
        Shuriken,
        DazeBlast,
        Backpack,
        PermaPet
    }

    public enum ShowEffectIndex
    {
        Unknown = 0,
        Heal = 1,
        Teleport = 2,
        Stream = 3,
        Throw = 4,
        Nova = 5,
        Poison = 6,
        Line = 7,
        Burst = 8,
        Flow = 9,
        Ring = 10,
        Lightning = 11,
        Collapse = 12,
        Coneblast = 13,
        Jitter = 14,
        Flash = 15,
        ThrowProjectile = 16
    }

    public class ObjectDesc
    {
        public readonly string Id;
        public readonly ushort Type;

        public readonly string DisplayId;

        public readonly bool Static;
        public readonly bool CaveWall;
        public readonly bool ConnectedWall;
        public readonly bool BlocksSight;

        public readonly bool OccupySquare;
        public readonly bool FullOccupy;
        public readonly bool EnemyOccupySquare;

        public readonly bool ProtectFromGroundDamage;
        public readonly bool ProtectFromSink;

        public readonly bool Player;
        public readonly bool Enemy;

        public readonly bool God;
        public readonly bool Cube;
        public readonly bool Quest;
        public readonly bool Hero;
        public readonly int Level;
        public readonly bool Oryx;
        public readonly float XpMult;

        public readonly int Size;
        public readonly int MinSize;
        public readonly int MaxSize;

        public readonly int MaxHP;
        public readonly int Defense;

        public readonly Dictionary<int, ProjectileDesc> Projectiles;

        public ObjectDesc(XElement e, string id, ushort type)
        {
            Id = id;
            Type = type;

            DisplayId = e.ParseString("DisplayId", Id);

            Static = e.ParseBool("Static");
            CaveWall = e.ParseString("Class") == "CaveWall";
            ConnectedWall = e.ParseString("Class") == "ConnectedWall";
            BlocksSight = e.ParseBool("BlocksSight");

            OccupySquare = e.ParseBool("OccupySquare");
            FullOccupy = e.ParseBool("FullOccupy");
            EnemyOccupySquare = e.ParseBool("EnemyOccupySquare");

            ProtectFromGroundDamage = e.ParseBool("ProtectFromGroundDamage");
            ProtectFromSink = e.ParseBool("ProtectFromSink");

            Enemy = e.ParseBool("Enemy");
            Player = e.ParseBool("Player");

            God = e.ParseBool("God");
            Cube = e.ParseBool("Cube");
            Quest = e.ParseBool("Quest");
            Hero = e.ParseBool("Hero");
            Level = e.ParseInt("Level", -1);
            Oryx = e.ParseBool("Oryx");
            XpMult = e.ParseFloat("XpMult", 1);

            Size = e.ParseInt("Size", 100);
            MinSize = e.ParseInt("MinSize", Size);
            MaxSize = e.ParseInt("MaxSize", Size);

            MaxHP = e.ParseInt("MaxHitPoints");
            Defense = e.ParseInt("Defense");

            Projectiles = new Dictionary<int, ProjectileDesc>();
            foreach (XElement k in e.Elements("Projectile"))
            {
                ProjectileDesc desc = new ProjectileDesc(k, Type);
#if DEBUG
                if (Projectiles.ContainsKey(desc.BulletType))
                    throw new Exception("Duplicate bullet type");
#endif
                Projectiles[desc.BulletType] = desc;
            }
        }
    }

    public class PlayerDesc : ObjectDesc
    {
        public readonly int[] SlotTypes;
        public readonly int[] Equipment;
        public readonly int[] ItemDatas;
        public readonly StatDesc[] Stats;
        public readonly int[] StartingValues;

        public PlayerDesc(XElement e, string id, ushort type) : base(e, id, type)
        {
            SlotTypes = e.ParseIntArray("SlotTypes", ",");

            int[] equipment = e.ParseUshortArray("Equipment", ",").Select(k => (int)(k == 0xffff ? -1 : k)).ToArray();
            Equipment = new int[20];
            for (int k = 0; k < 20; k++)
                Equipment[k] = k >= equipment.Length ? -1 : equipment[k];

            ItemDatas = new int[20];
            for (int k = 0; k < 20; k++)
                ItemDatas[k] = -1;

            Stats = new StatDesc[8];
            for (int i = 0; i < Stats.Length; i++)
                Stats[i] = new StatDesc(i, e);
            Stats = Stats.OrderBy(k => k.Index).ToArray();

            StartingValues = Stats.Select(k => k.StartingValue).ToArray();
        }
    }

    public class StatDesc
    {
        public readonly string Type;
        public readonly int Index;
        public readonly int MaxValue;
        public readonly int StartingValue;
        public readonly int MinIncrease;
        public readonly int MaxIncrease;

        public StatDesc(int index, XElement e)
        {
            Index = index;
            Type = StatIndexToName(index);

            StartingValue = e.ParseInt(Type);
            MaxValue = e.Element(Type).ParseInt("@max");

            foreach (XElement stat in e.Elements("LevelIncrease"))
            {
                if (stat.Value == Type)
                {
                    MinIncrease = stat.ParseInt("@min");
                    MaxIncrease = stat.ParseInt("@max");
                    break;
                }
            }
        }

        public static string StatIndexToName(int index)
        {
            switch (index)
            {
                case 0: return "MaxHitPoints";
                case 1: return "MaxMagicPoints";
                case 2: return "Attack";
                case 3: return "Defense";
                case 4: return "Speed";
                case 5: return "Dexterity";
                case 6: return "HpRegen";
                case 7: return "MpRegen";
            }
            return null;
        }

        public static int StatNameToIndex(string name)
        {
            switch (name)
            {
                case "MaxHitPoints": return 0;
                case "MaxMagicPoints": return 1;
                case "Attack": return 2;
                case "Defense": return 3;
                case "Speed": return 4;
                case "Dexterity": return 5;
                case "HpRegen": return 6;
                case "MpRegen": return 7;
            }
            return -1;
        }
    }

    public class SkinDesc
    {
        public readonly string Id;
        public readonly ushort Type;

        public readonly ushort PlayerClassType;

        public SkinDesc(XElement e, string id, ushort type)
        {
            Id = id;
            Type = type;
            PlayerClassType = e.ParseUshort("PlayerClassType");
        }
    }

    public class ActivateEffectDesc
    {
        public readonly ActivateEffectIndex Index;
        public readonly ConditionEffectDesc[] Effects;
        public readonly ConditionEffectIndex Effect;
        public readonly int DurationMS;
        public readonly float Range;
        public readonly int Amount;
        public readonly int TotalDamage;
        public readonly float Radius;
        public readonly uint? Color;
        public readonly int MaxTargets;
        
        public ActivateEffectDesc(XElement e)
        {
            Index = (ActivateEffectIndex)Enum.Parse(typeof(ActivateEffectIndex), e.Value.Replace(" ", ""));
            Effect = e.ParseConditionEffect("@effect");
            DurationMS = (int)(e.ParseFloat("@duration", 0) * 1000);
            Range = e.ParseFloat("@range");
            Amount = e.ParseInt("@amount");
            TotalDamage = e.ParseInt("@totalDamage");
            Radius = e.ParseFloat("@radius");
            MaxTargets = e.ParseInt("@maxTargets");

            Effects = new ConditionEffectDesc[1]
            {
                new ConditionEffectDesc(Effect, DurationMS)
            };

            if (e.Attribute("color") != null)
                Color = e.ParseUInt("@color");
        }
    }
    
    public class ItemDesc
    {
        public const float RateOfFireMultiplier = 0.05f;
        public const float DamageMultiplier = 0.05f;
        public const float CooldownMultiplier = 0.05f;

        enum ItemType
        {
            All,
            Sword,
            Dagger,
            Bow,
            Tome,
            Shield,
            Leather,
            Plate,
            Wand,
            Ring,
            Potion,
            Spell,
            Seal,
            Cloak,
            Robe,
            Quiver,
            Helm,
            Staff,
            Poison,
            Skull,
            Trap,
            Orb,
            Prism,
            Scepter,
            Katana,
            Shuriken,
        }

        static ItemData[] GlobalModifiers =
        {
            ItemData.MaxHP, 
            ItemData.MaxMP, 
            ItemData.Attack, 
            ItemData.Defense, 
            ItemData.Speed, 
            ItemData.Dexterity, 
            ItemData.Vitality, 
            ItemData.Wisdom, 
            ItemData.FameBonus,
        };

        static ItemData[] AbilityModifiers = GlobalModifiers.Concat(
            new ItemData[]
            {
                ItemData.Cooldown, 
                ItemData.Damage, 
            }).ToArray();

        static ItemData[] WeaponModifiers = GlobalModifiers.Concat(
            new ItemData[]
            {
                ItemData.RateOfFire, 
                ItemData.Damage, 
            }).ToArray();

        static ItemType[] WeaponTypes =
        {
            ItemType.Sword,
            ItemType.Dagger,
            ItemType.Staff,
            ItemType.Wand,
            ItemType.Katana,
            ItemType.Bow
        };

        static ItemType[] ArmorTypes =
        {
            ItemType.Robe,
            ItemType.Plate,
            ItemType.Leather
        };

        static ItemType[] RingTypes =
        {
            ItemType.Ring,
        };

        static ItemType[] AbilityTypes =
        {
            ItemType.Cloak,
            ItemType.Spell,
            ItemType.Tome,
            ItemType.Helm,
            ItemType.Quiver,
            ItemType.Seal,
            ItemType.Poison,
            ItemType.Skull,
            ItemType.Shield,
            ItemType.Trap,
            ItemType.Orb,
            ItemType.Shuriken,
            ItemType.Prism,
            ItemType.Scepter
        };

        static ItemType[] ModifiableTypes = WeaponTypes.Concat(ArmorTypes).Concat(RingTypes).Concat(AbilityTypes).ToArray();

        public static float GetStat(int data, ItemData i, float multiplier)
        {
            int rank = GetRank(data);
            if (rank == -1)
                return 0;
            int value = 0;
            if (HasStat(data, i))
            {
                value += rank;
            }
            return value * multiplier;
        }

        public static int GetRank(int data)
        {
            if (data == -1)
                return -1;
            if (HasStat(data, ItemData.T0))
                return 1;
            if (HasStat(data, ItemData.T1))
                return 2;
            if (HasStat(data, ItemData.T2))
                return 3;
            if (HasStat(data, ItemData.T3))
                return 4;
            if (HasStat(data, ItemData.T4))
                return 5;
            if (HasStat(data, ItemData.T5))
                return 6;
            if (HasStat(data, ItemData.T6))
                return 7;
            if (HasStat(data, ItemData.T7))
                return 8;
            return -1;
        }

        public static bool HasStat(int data, ItemData i)
        {
            if (data == -1)
                return false;
            return ((ItemData)data & i) != 0;
        }

        public Tuple<bool, ItemData> Roll()
        {
            ItemData data = 0;
            if (!ModifiableTypes.Contains((ItemType)SlotType))
                return Tuple.Create(false, data);

            if (!MathUtils.Chance(.5f))
                return Tuple.Create(false, data);

            int rank = -1;
            float chance = .5f;
            for (int i = 0; i < 8; i++)
            {
                if (MathUtils.Chance(chance))
                    rank++;
                else break;
            }
            if (rank == -1) 
                return Tuple.Create(false, data);

            data |= (ItemData)((ulong)1 << rank);

            ItemData[] modifiers = GlobalModifiers;
            if (WeaponTypes.Contains((ItemType)SlotType))
                modifiers = WeaponModifiers;
            else if (AbilityTypes.Contains((ItemType)SlotType))
                modifiers = AbilityModifiers;

            int bonuses = MathUtils.NextInt(2, 3);
            if ((data & ItemData.T7) != 0) //T7s can have 4 bonuses
                if (MathUtils.Chance(0.5f))
                    bonuses++;

            List<ItemData> s = new List<ItemData>();
            while (s.Count < bonuses)
            {
                ItemData k = modifiers[MathUtils.Next(modifiers.Length)];
                if (s.Contains(k))
                    continue;
                if ((k == ItemData.Damage) 
                    && Projectile == null)
                    continue;
                s.Add(k);
                data |= k;
            }

            return Tuple.Create(true, data);
        }

        public readonly string Id;
        public readonly ushort Type;

        public readonly int SlotType;
        public readonly int Tier;
        public readonly string Description;
        public readonly float RateOfFire;
        public readonly bool Usable;
        public readonly int BagType;
        public readonly int MpCost;
        public readonly int FameBonus;
        public readonly int NumProjectiles;
        public readonly float ArcGap;
        public readonly bool Consumable;
        public readonly bool Potion;
        public readonly string DisplayId;
        public readonly string SuccessorId;
        public readonly bool Soulbound;
        public readonly int CooldownMS;
        public readonly bool Resurrects;
        public readonly int Tex1;
        public readonly int Tex2;
        public readonly int Doses;

        public readonly KeyValuePair<int, int>[] StatBoosts;
        public readonly ActivateEffectDesc[] ActivateEffects;
        public readonly ProjectileDesc Projectile;

        public ItemDesc(XElement e, string id, ushort type)
        {
            Id = id;
            Type = type;

            SlotType = e.ParseInt("SlotType");
            Tier = e.ParseInt("Tier", -1);
            Description = e.ParseString("Description");
            RateOfFire = e.ParseFloat("RateOfFire", 1);
            Usable = e.ParseBool("Usable");
            BagType = e.ParseInt("BagType");
            MpCost = e.ParseInt("MpCost");
            FameBonus = e.ParseInt("FameBonus");
            NumProjectiles = e.ParseInt("NumProjectiles", 1);
            ArcGap = e.ParseFloat("ArcGap", 11.25f);
            Consumable = e.ParseBool("Consumable");
            Potion = e.ParseBool("Potion");
            DisplayId = e.ParseString("DisplayId", Id);
            Doses = e.ParseInt("Doses");
            SuccessorId = e.ParseString("SuccessorId", null);
            Soulbound = e.ParseBool("Soulbound");
            CooldownMS = (int)(e.ParseFloat("Cooldown", .2f) * 1000);
            Resurrects = e.ParseBool("Resurrects");
            Tex1 = (int)e.ParseUInt("Tex1", 0);
            Tex2 = (int)e.ParseUInt("Tex2", 0);

            List<KeyValuePair<int, int>> stats = new List<KeyValuePair<int, int>>();
            foreach (XElement s in e.Elements("ActivateOnEquip"))
                stats.Add(new KeyValuePair<int, int>(
                    s.ParseInt("@stat"),
                    s.ParseInt("@amount")));
            StatBoosts = stats.ToArray();

            List<ActivateEffectDesc> activate = new List<ActivateEffectDesc>();
            foreach (XElement i in e.Elements("Activate"))
                activate.Add(new ActivateEffectDesc(i));
            ActivateEffects = activate.ToArray();

            if (e.Element("Projectile") != null)
                Projectile = new ProjectileDesc(e.Element("Projectile"), Type);
        }
    }

    public class TileDesc
    {
        public readonly string Id;
        public readonly ushort Type;
        public readonly bool NoWalk;
        public readonly int Damage;
        public readonly float Speed;
        public readonly bool Sinking;
        public readonly bool Push;
        public readonly float DX;
        public readonly float DY;

        public TileDesc(XElement e, string id, ushort type)
        {
            Id = id;
            Type = type;
            NoWalk = e.ParseBool("NoWalk");
            Damage = e.ParseInt("Damage");
            Speed = e.ParseFloat("Speed", 1.0f);
            Sinking = e.ParseBool("Sinking");
            if (Push = e.ParseBool("Push"))
            {
                DX = e.Element("Animate").ParseFloat("@dx") / 1000f;
                DY = e.Element("Animate").ParseFloat("@dy") / 1000f;
            }
        }
    }

    public class ProjectileDesc
    {
        public readonly byte BulletType;
        public readonly string ObjectId;
        public readonly int LifetimeMS;
        public readonly float Speed;

        public readonly int Damage;
        public readonly int MinDamage; //Only for players
        public readonly int MaxDamage;

        public readonly ConditionEffectDesc[] Effects;

        public readonly bool MultiHit;
        public readonly bool PassesCover;
        public readonly bool ArmorPiercing;
        public readonly bool Wavy;
        public readonly bool Parametric;
        public readonly bool Boomerang;

        public readonly float Amplitude;
        public readonly float Frequency;
        public readonly float Magnitude;

        public readonly bool Accelerate;
        public readonly bool Decelerate;

        public readonly ushort ContainerType;

        public ProjectileDesc(XElement e, ushort containerType)
        {
            ContainerType = containerType;
            BulletType = (byte)e.ParseInt("@id");
            ObjectId = e.ParseString("ObjectId");
            LifetimeMS = e.ParseInt("LifetimeMS");
            Speed = e.ParseFloat("Speed");
            Damage = e.ParseInt("Damage");
            MinDamage = e.ParseInt("MinDamage", Damage);
            MaxDamage = e.ParseInt("MaxDamage", Damage);

            List<ConditionEffectDesc> effects = new List<ConditionEffectDesc>();
            foreach (XElement k in e.Elements("ConditionEffect"))
                effects.Add(new ConditionEffectDesc(k));
            Effects = effects.ToArray();

            MultiHit = e.ParseBool("MultiHit");
            PassesCover = e.ParseBool("PassesCover");
            ArmorPiercing = e.ParseBool("ArmorPiercing");
            Wavy = e.ParseBool("Wavy");
            Parametric = e.ParseBool("Parametric");
            Boomerang = e.ParseBool("Boomerang");

            Amplitude = e.ParseFloat("Amplitude", 0);
            Frequency = e.ParseFloat("Frequency", 1);
            Magnitude = e.ParseFloat("Magnitude", 3);

            Accelerate = e.ParseBool("Accelerate", false);
            Decelerate = e.ParseBool("Decelerate", false);
        }
    }

    public class ConditionEffectDesc
    {
        public readonly ConditionEffectIndex Effect;
        public readonly int DurationMS;

        public ConditionEffectDesc(ConditionEffectIndex effect, int durationMs)
        {
            Effect = effect;
            DurationMS = durationMs;
        }

        public ConditionEffectDesc(XElement e)
        {
            Effect = (ConditionEffectIndex)Enum.Parse(typeof(ConditionEffectIndex), e.Value.Replace(" ", ""));
            DurationMS = (int)(e.ParseFloat("@duration") * 1000);
        }
    }

    public class QuestDesc
    {
        public readonly int Level;
        public readonly int Priority;

        public QuestDesc(int level, int priority)
        {
            Level = level;
            Priority = priority;
        }
    }

    public class WorldDesc
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly int Background;
        public readonly bool ShowDisplays;
        public readonly bool AllowTeleport;
        public readonly int BlockSight;
        public readonly JSMap[] Maps;

        public WorldDesc(XElement e)
        {
            Id = e.ParseString("@id");
            DisplayName = e.ParseString("@display", Id);
            Background = e.ParseInt("Background");
            ShowDisplays = e.ParseBool("ShowDisplays");
            AllowTeleport = e.ParseBool("AllowTeleport");
            BlockSight = e.ParseInt("BlockSight");

            string[] maps = e.ParseStringArray("Maps", ";", new string[0]);
            Maps = new JSMap[maps.Length];
            for (int i = 0; i < maps.Length; i++)
                Maps[i] = new JSMap(File.ReadAllText(Resources.CombineResourcePath($"Worlds/{maps[i]}")));
        }
    }
}
