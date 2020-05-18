using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Security;
using System.Security;
using System.Text;
using System.Xml.Linq;

namespace RotMG.Common
{
    public interface IDatabaseInfo
    {
        XElement Export(bool appExport = true);
    }

    public abstract class DatabaseModel : IDatabaseInfo
    {
        public XElement Data;
        public readonly string Path;
        public DatabaseModel(string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                Path = Database.CombineKeyPath(key);
                Reload();
            }
        }

        public void Reload()
        {
            if (File.Exists(Path))
                Data = XElement.Parse(File.ReadAllText(Path));
        }

        public void Save()
        {
            Data = Export(false);
            File.WriteAllText(Path, Data.ToString());
        }

        public bool IsNull => Data == null;

        public abstract void Load();
        public abstract XElement Export(bool appExport = true);
    }

    public class CharacterModel : DatabaseModel
    {
        public readonly int Id;
        public int Experience;
        public int Level;
        public int ClassType;
        public int HP;
        public int MP;
        public int[] Stats;
        public int[] Inventory;
        public int[] ItemDatas;
        public int Fame;
        public int Tex1;
        public int Tex2;
        public int SkinType;
        public int HealthPotions;
        public int MagicPotions;
        public int CreationTime;
        public bool Deleted;
        public bool Dead;
        public int DeathFame;
        public int DeathTime;
        public bool HasBackpack;
        public FameStatsInfo FameStats;
        public int PetId;

        public CharacterModel(int accountId, int key) : base($"char.{accountId}.{key}") 
        {
            Id = key;
        }

        public override void Load()
        {
            Level = Data.ParseInt("Level");
            Experience = Data.ParseInt("Experience");
            ClassType = Data.ParseInt("ClassType");
            HP = Data.ParseInt("HP");
            MP = Data.ParseInt("MP");
            Stats = Data.ParseIntArray("Stats", ",");
            Inventory = Data.ParseIntArray("Inventory", ",");
            ItemDatas = Data.ParseIntArray("ItemDatas", ",");
            Fame = Data.ParseInt("Fame");
            Tex1 = Data.ParseInt("Tex1");
            Tex2 = Data.ParseInt("Tex2");
            SkinType = Data.ParseInt("SkinType");
            HealthPotions = Data.ParseInt("HealthPotions");
            MagicPotions = Data.ParseInt("MagicPotions");
            CreationTime = Data.ParseInt("CreationTime");
            Deleted = Data.ParseBool("Deleted");
            Dead = Data.ParseBool("Dead");
            DeathFame = Data.ParseInt("DeathFame");
            DeathTime = Data.ParseInt("DeathTime");
            HasBackpack = Data.ParseBool("HasBackpack");
            FameStats = new FameStatsInfo(Data.Element("FameStats"));
            PetId = Data.ParseInt("PetId");
        }

        public XElement ExportFame()
        {
            XElement data = new XElement("Char");
            data.Add(new XElement("ObjectType", ClassType));
            data.Add(new XElement("Level", Level));
            data.Add(new XElement("Exp", Experience));
            data.Add(new XElement("CurrentFame", Fame));
            data.Add(new XElement("Equipment", string.Join(",", Inventory)));
            data.Add(new XElement("ItemDatas", string.Join(",", ItemDatas)));
            data.Add(new XElement("MaxHitPoints", Stats[0]));
            data.Add(new XElement("HitPoints", HP));
            data.Add(new XElement("MaxMagicPoints", Stats[1]));
            data.Add(new XElement("MagicPoints", MP));
            data.Add(new XElement("Attack", Stats[2]));
            data.Add(new XElement("Defense", Stats[3]));
            data.Add(new XElement("Speed", Stats[4]));
            data.Add(new XElement("Dexterity", Stats[5]));
            data.Add(new XElement("HpRegen", Stats[6]));
            data.Add(new XElement("MpRegen", Stats[7]));
            data.Add(new XElement("Tex1", Tex1));
            data.Add(new XElement("Tex2", Tex2));
            data.Add(new XElement("Texture", SkinType));
            return data;
        }

        public override XElement Export(bool appExport = true)
        {
            XElement data = new XElement("Char");
            if (appExport) //char/list export
            {
                data.Add(new XElement("ObjectType", ClassType));
                data.Add(new XElement("Level", Level));
                data.Add(new XElement("Exp", Experience));
                data.Add(new XElement("CurrentFame", Fame));
                data.Add(new XElement("Equipment", string.Join(",", Inventory)));
                data.Add(new XElement("ItemDatas", string.Join(",", ItemDatas)));
                data.Add(new XElement("MaxHitPoints", Stats[0]));
                data.Add(new XElement("HitPoints", HP));
                data.Add(new XElement("MaxMagicPoints", Stats[1]));
                data.Add(new XElement("MagicPoints", MP));
                data.Add(new XElement("Attack", Stats[2]));
                data.Add(new XElement("Defense", Stats[3]));
                data.Add(new XElement("Speed", Stats[4]));
                data.Add(new XElement("Dexterity", Stats[5]));
                data.Add(new XElement("HpRegen", Stats[6]));
                data.Add(new XElement("MpRegen", Stats[7]));
                data.Add(new XElement("Tex1", Tex1));
                data.Add(new XElement("Tex2", Tex2));
                data.Add(new XElement("Texture", SkinType));
            }
            else //database export
            {
                data.Add(new XElement("Level", Level));
                data.Add(new XElement("Experience", Experience));
                data.Add(new XElement("ClassType", ClassType));
                data.Add(new XElement("HP", HP));
                data.Add(new XElement("MP", MP));
                data.Add(new XElement("Stats", string.Join(",", Stats)));
                data.Add(new XElement("Inventory", string.Join(",", Inventory)));
                data.Add(new XElement("ItemDatas", string.Join(",", ItemDatas)));
                data.Add(new XElement("Fame", Fame));
                data.Add(new XElement("Tex1", Tex1));
                data.Add(new XElement("Tex2", Tex2));
                data.Add(new XElement("SkinType", SkinType));
                data.Add(new XElement("HealthPotions", HealthPotions));
                data.Add(new XElement("MagicPotions", MagicPotions));
                data.Add(new XElement("HasBackpack", HasBackpack));
                data.Add(new XElement("CreationTime", CreationTime));
                data.Add(new XElement("Deleted", Deleted));
                data.Add(new XElement("Dead", Dead));
                data.Add(new XElement("DeathFame", DeathFame));
                data.Add(new XElement("DeathTime", DeathTime));
                data.Add(new XElement("PetId", PetId));
                data.Add(FameStats.Export(appExport));
            }
            return data;
        }
    }

    public class FameStatsInfo : IDatabaseInfo
    {
        public int Shots;
        public int ShotsThatDamage;
        public int TilesUncovered;
        public int QuestsCompleted;
        public int Escapes;
        public int NearDeathEscapes;
        public int MinutesActive;

        public int LevelUpAssists;
        public int PotionsDrank;
        public int Teleports;
        public int AbilitiesUsed;

        public int DamageTaken;
        public int DamageDealt;

        public int MonsterKills;
        public int MonsterAssists;
        public int GodKills;
        public int GodAssists;
        public int OryxKills;
        public int OryxAssists;
        public int CubeKills;
        public int CubeAssists;
        public int BlueBags;
        public int CyanBags;
        public int WhiteBags;

        public int PirateCavesCompleted;
        public int UndeadLairsCompleted;
        public int AbyssOfDemonsCompleted;
        public int SnakePitsCompleted;
        public int SpiderDensCompleted;
        public int SpriteWorldsCompleted;
        public int TombsCompleted;

        public FameStatsInfo() { }
        public FameStatsInfo(XElement data)
        {
            Shots = data.ParseInt("Shots");
            ShotsThatDamage = data.ParseInt("ShotsThatDamage");
            TilesUncovered = data.ParseInt("TilesUncovered");
            QuestsCompleted = data.ParseInt("QuestsCompleted");
            Escapes = data.ParseInt("Escapes");
            NearDeathEscapes = data.ParseInt("NearDeathEscapes");
            MinutesActive = data.ParseInt("MinutesActive");

            LevelUpAssists = data.ParseInt("LevelUpAssists");
            PotionsDrank = data.ParseInt("PotionsDrank");
            Teleports = data.ParseInt("Teleports");
            AbilitiesUsed = data.ParseInt("AbilitiesUsed");

            DamageTaken = data.ParseInt("DamageTaken");
            DamageDealt = data.ParseInt("DamageDealt");

            MonsterKills = data.ParseInt("MonsterKills");
            MonsterAssists = data.ParseInt("MonsterAssists");
            GodKills = data.ParseInt("GodKills");
            GodAssists = data.ParseInt("GodAssists");
            OryxKills = data.ParseInt("OryxKills");
            OryxAssists = data.ParseInt("OryxAssists");
            CubeKills = data.ParseInt("CubeKills");
            CubeAssists = data.ParseInt("CubeAssists");
            CyanBags = data.ParseInt("CyanBags");
            BlueBags = data.ParseInt("BlueBags");
            WhiteBags = data.ParseInt("WhiteBags");

            PirateCavesCompleted = data.ParseInt("PirateCavesCompleted");
            UndeadLairsCompleted = data.ParseInt("UndeadLairsCompleted");
            AbyssOfDemonsCompleted = data.ParseInt("AbyssOfDemonsCompleted");
            SnakePitsCompleted = data.ParseInt("SnakePitsCompleted");
            SpiderDensCompleted = data.ParseInt("SpiderDensCompleted");
            SpriteWorldsCompleted = data.ParseInt("SpriteWorldsCompleted");
            TombsCompleted = data.ParseInt("TombsCompleted");
        }

        public XElement Export(bool appExport = true)
        {
            XElement data = new XElement("FameStats");
            data.Add(new XElement("Shots", Shots));
            data.Add(new XElement("ShotsThatDamage", ShotsThatDamage));
            data.Add(new XElement("TilesUncovered", TilesUncovered));
            data.Add(new XElement("QuestsCompleted", QuestsCompleted));
            data.Add(new XElement("PirateCavesCompleted", PirateCavesCompleted));
            data.Add(new XElement("UndeadLairsCompleted", UndeadLairsCompleted));
            data.Add(new XElement("AbyssOfDemonsCompleted", AbyssOfDemonsCompleted));
            data.Add(new XElement("SnakePitsCompleted", SnakePitsCompleted));
            data.Add(new XElement("SpiderDensCompleted", SpiderDensCompleted));
            data.Add(new XElement("SpriteWorldsCompleted", SpriteWorldsCompleted));
            data.Add(new XElement("Escapes", Escapes));
            data.Add(new XElement("NearDeathEscapes", NearDeathEscapes));
            data.Add(new XElement("LevelUpAssists", LevelUpAssists));
            data.Add(new XElement("DamageTaken", DamageTaken));
            data.Add(new XElement("DamageDealt", DamageDealt));
            data.Add(new XElement("Teleports", Teleports));
            data.Add(new XElement("PotionsDrank", PotionsDrank));
            data.Add(new XElement("MonsterKills", MonsterKills));
            data.Add(new XElement("MonsterAssists", MonsterAssists));
            data.Add(new XElement("GodKills", GodKills));
            data.Add(new XElement("GodAssists", GodAssists));
            data.Add(new XElement("OryxKills", OryxKills));
            data.Add(new XElement("OryxAssists", OryxAssists));
            data.Add(new XElement("CubeKills", CubeKills));
            data.Add(new XElement("CubeAssists", CubeAssists));
            data.Add(new XElement("CyanBags", CyanBags));
            data.Add(new XElement("BlueBags", BlueBags));
            data.Add(new XElement("WhiteBags", WhiteBags));
            data.Add(new XElement("MinutesActive", MinutesActive));
            data.Add(new XElement("AbilitiesUsed", AbilitiesUsed));
            return data;
        }

        public void ExportTo(XElement e)
        {
            e.Add(new XElement("Shots", Shots));
            e.Add(new XElement("ShotsThatDamage", ShotsThatDamage));
            e.Add(new XElement("TilesUncovered", TilesUncovered));
            e.Add(new XElement("QuestsCompleted", QuestsCompleted));
            e.Add(new XElement("PirateCavesCompleted", PirateCavesCompleted));
            e.Add(new XElement("UndeadLairsCompleted", UndeadLairsCompleted));
            e.Add(new XElement("AbyssOfDemonsCompleted", AbyssOfDemonsCompleted));
            e.Add(new XElement("SnakePitsCompleted", SnakePitsCompleted));
            e.Add(new XElement("SpiderDensCompleted", SpiderDensCompleted));
            e.Add(new XElement("SpriteWorldsCompleted", SpriteWorldsCompleted));
            e.Add(new XElement("Escapes", Escapes));
            e.Add(new XElement("NearDeathEscapes", NearDeathEscapes));
            e.Add(new XElement("LevelUpAssists", LevelUpAssists));
            e.Add(new XElement("DamageTaken", DamageTaken));
            e.Add(new XElement("DamageDealt", DamageDealt));
            e.Add(new XElement("Teleports", Teleports));
            e.Add(new XElement("PotionsDrank", PotionsDrank));
            e.Add(new XElement("MonsterKills", MonsterKills));
            e.Add(new XElement("MonsterAssists", MonsterAssists));
            e.Add(new XElement("GodKills", GodKills));
            e.Add(new XElement("GodAssists", GodAssists));
            e.Add(new XElement("OryxKills", OryxKills));
            e.Add(new XElement("OryxAssists", OryxAssists));
            e.Add(new XElement("CubeKills", CubeKills));
            e.Add(new XElement("CubeAssists", CubeAssists));
            e.Add(new XElement("CyanBags", CyanBags));
            e.Add(new XElement("BlueBags", BlueBags));
            e.Add(new XElement("WhiteBags", WhiteBags));
            e.Add(new XElement("MinutesActive", MinutesActive));
            e.Add(new XElement("AbilitiesUsed", AbilitiesUsed));
        }
    }

    public class AccountModel : DatabaseModel
    {
        public const int MaxDeadCharsStored = 20;

        public readonly int Id; //Taken from database.
        public readonly string Name; //Taken from database.

        public int NextCharId;
        public int MaxNumChars;
        public List<int> AliveChars;
        public List<int> DeadChars;
        public List<int> OwnedSkins;
        public bool Ranked;
        public bool Muted;
        public bool Banned;
        public string GuildName;
        public int GuildRank;
        public StatsInfo Stats;
        public bool Connected;
        public int RegisterTime;
        public List<int> LockedIds;
        public List<int> IgnoredIds;
        public bool AllyShots;
        public bool AllyDamage;
        public bool Effects;
        public bool Sounds;
        public bool Notifications;

        public AccountModel() : base(null) { }
        public AccountModel(int key) : base($"account.{key}")
        {
            Id = key;

            if (Data != null)
                Name = Database.UsernameFromId(key);
        }

        public override void Load()
        {
            NextCharId = Data.ParseInt("NextCharId");
            MaxNumChars = Data.ParseInt("MaxNumChars");
            AliveChars = Data.ParseIntList("AliveChars", ",", new List<int>());
            DeadChars = Data.ParseIntList("DeadChars", ",", new List<int>());
            OwnedSkins = Data.ParseIntList("OwnedSkins", ",", new List<int>());
            Ranked = Data.ParseBool("Ranked");
            Muted = Data.ParseBool("Muted");
            Banned = Data.ParseBool("Banned");
            GuildName = Data.ParseString("GuildName");
            GuildRank = Data.ParseInt("GuildRank");
            Connected = Data.ParseBool("Connected");
            RegisterTime = Data.ParseInt("RegisterTime");
            LockedIds = Data.ParseIntList("LockedIds", ",", new List<int>());
            IgnoredIds = Data.ParseIntList("IgnoredIds", ",", new List<int>());
            AllyShots = Data.ParseBool("AllyShots", true);
            AllyDamage = Data.ParseBool("AllyDamage", true);
            Effects = Data.ParseBool("Effects", true);
            Sounds = Data.ParseBool("Sounds", true);
            Notifications = Data.ParseBool("Notifications", true);

            Stats = new StatsInfo
            {
                BestCharFame = Data.Element("Stats").ParseInt("BestCharFame"),
                TotalFame = Data.Element("Stats").ParseInt("TotalFame"),
                Fame = Data.Element("Stats").ParseInt("Fame"),
                TotalCredits = Data.Element("Stats").ParseInt("TotalCredits"),
                Credits = Data.Element("Stats").ParseInt("Credits")
            };

            List<ClassStatsInfo> classStats = new List<ClassStatsInfo>();
            foreach (XElement e in Data.Element("Stats").Elements("ClassStats"))
            {
                classStats.Add(new ClassStatsInfo
                {
                    ObjectType = e.ParseInt("@objectType"),
                    BestFame = e.ParseInt("BestFame"),
                    BestLevel = e.ParseInt("BestLevel")
                });
            }
            Stats.ClassStats = classStats.ToArray();
        }

        public override XElement Export(bool appExport = true)
        {
            XElement data = new XElement("Account");
            data.Add(new XElement("AccountId", Id));

            if (appExport)
            {
                data.Add(new XElement("Name", Name));
                data.Add(new XElement("Guild", new XElement("Name", GuildName), new XElement("Rank", GuildRank)));
            }
            else
            {
                data.Add(new XElement("AliveChars", string.Join(",", AliveChars)));
                data.Add(new XElement("DeadChars", string.Join(",", DeadChars)));
                data.Add(new XElement("OwnedSkins", string.Join(",", OwnedSkins)));
                data.Add(new XElement("Ranked", Ranked));
                data.Add(new XElement("Muted", Muted));
                data.Add(new XElement("Banned", Banned));
                data.Add(new XElement("Connected", Connected));
                data.Add(new XElement("NextCharId", NextCharId));
                data.Add(new XElement("MaxNumChars", MaxNumChars));
                data.Add(new XElement("GuildName", GuildName));
                data.Add(new XElement("GuildRank", GuildRank));
                data.Add(new XElement("RegisterTime", RegisterTime));
                data.Add(new XElement("LockedIds", string.Join(",", LockedIds)));
                data.Add(new XElement("IgnoredIds", string.Join(",", IgnoredIds)));
                data.Add(new XElement("AllyShots", AllyShots));
                data.Add(new XElement("AllyDamage", AllyDamage));
                data.Add(new XElement("Effects", Effects));
                data.Add(new XElement("Sounds", Sounds));
                data.Add(new XElement("Notifications", Notifications));
            }

            data.Add(Stats.Export(appExport));

            return data;
        }
    }

    public class ClassStatsInfo : IDatabaseInfo
    {
        public int ObjectType;
        public int BestLevel;
        public int BestFame;

        public XElement Export(bool appExport = true)
        {
            XElement data = new XElement("ClassStats");
            data.Add(new XAttribute("objectType", ObjectType));
            data.Add(new XElement("BestLevel", BestLevel));
            data.Add(new XElement("BestFame", BestFame));
            return data;
        }
    }

    public class StatsInfo : IDatabaseInfo
    {
        public int BestCharFame;
        public int TotalFame;
        public int Fame;
        public int Credits;
        public int TotalCredits;
        public ClassStatsInfo[] ClassStats;

        public XElement Export(bool appExport = true)
        {
            XElement data = new XElement("Stats");
            data.Add(new XElement("BestCharFame", BestCharFame));
            data.Add(new XElement("TotalFame", TotalFame));
            data.Add(new XElement("Fame", Fame));
            data.Add(new XElement("TotalCredits", TotalCredits));
            data.Add(new XElement("Credits", Credits));
            foreach (ClassStatsInfo k in ClassStats)
                data.Add(k.Export(appExport));
            return data;
        }

        public ClassStatsInfo GetClassStats(int type)
        {
            foreach (ClassStatsInfo s in ClassStats)
                if (s.ObjectType == type)
                    return s;
            return null;
        }
    }
}
