using RotMG.Game;
using RotMG.Game.Entities;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RotMG.Common
{
    //XML/Text files combined storage system
    public static class Database
    {
        private const int MaxLegends = 20;
        private const int MinFameRequiredToEnterLegends = 0;
        private static readonly Dictionary<string, TimeSpan> TimeSpans = new Dictionary<string, TimeSpan>()
        {
            {"week", TimeSpan.FromDays(7) },
            {"month", TimeSpan.FromDays(30) },
            {"all", TimeSpan.MaxValue }
        };

        private static readonly Dictionary<string, XElement> FameLists = new Dictionary<string, XElement>
        {
            { "week", null },
            { "month", null },
            { "all", null }
        };

        private static readonly HashSet<int> Legends = new HashSet<int>();

        private const int MaxInvalidLoginAttempts = 5;
        private static Dictionary<string, byte> InvalidLoginAttempts;

        private const int MaxRegisteredAccounts = 1;
        private static Dictionary<string, byte> RegisteredAccounts;

        private const int ResetCooldown = 60000 * 5; //5 minutes
        private static int ResetTime;

        private const int CharSlotPrice = 2000; //Fame
        private const int SkinPrice = 1000; //Credits

        public static void Init()
        {
            InvalidLoginAttempts = new Dictionary<string, byte>();
            RegisteredAccounts = new Dictionary<string, byte>();
            if (!Directory.Exists(Settings.DatabaseDirectory))
                Directory.CreateDirectory(Settings.DatabaseDirectory);

            CreateKey("nextAccId", "0", true);
            CreateKey("news", "", true);

            foreach (string span in TimeSpans.Keys)
                CreateKey($"legends.{span}", "", true);

            FlushLegends();
        }

        public static void Tick()
        {
            if (Environment.TickCount - ResetTime >= ResetCooldown)
            {
#if DEBUG
                Program.Print(PrintType.Debug, "Database reset");
#endif
                RegisteredAccounts.Clear();
                InvalidLoginAttempts.Clear();
                FlushLegends();
                ResetTime = Environment.TickCount;
            }
        }

        private static void AddRegisteredAccount(string ip)
        {
            if (RegisteredAccounts.ContainsKey(ip))
                RegisteredAccounts[ip]++;
            else RegisteredAccounts[ip] = 1;
        }

        private static void AddInvalidLoginAttempt(string ip)
        {
            if (InvalidLoginAttempts.ContainsKey(ip))
                InvalidLoginAttempts[ip]++;
            else InvalidLoginAttempts[ip] = 1;
        }

        private static void CreateKey(string path, string contents, bool global = false)
        {
            string combined = CombineKeyPath(path, global);
            if (!File.Exists(combined))
                File.WriteAllText(combined, contents);
        }

        public static void DeleteKey(string path, bool global = false)
        {
            File.Delete(CombineKeyPath(path, global));
        }

        private static void SetKey(string path, string contents, bool global = false)
        {
            File.WriteAllText(CombineKeyPath(path, global), contents);
        }

        private static void SetKeyLines(string path, string[] contents, bool global = false)
        {
            File.WriteAllLines(CombineKeyPath(path, global), contents);
        }

        private static string GetKey(string path, bool global = false)
        {
            string combined = CombineKeyPath(path, global);
            if (!File.Exists(combined))
                return null;
            return File.ReadAllText(combined);
        }

        private static string[] GetKeyLines(string path, bool global = false)
        {
            string combined = CombineKeyPath(path, global);
            if (!File.Exists(combined))
                return null;
            return File.ReadAllLines(combined);
        }

        public static string CombineKeyPath(string path, bool global = false)
        {
            return $"{Settings.DatabaseDirectory}/{(global ? "@" : "")}{path}.file";
        }

        public static bool CanRegisterAccount(string ip)
        {
            if (RegisteredAccounts.TryGetValue(ip, out byte attempts) && attempts >= MaxRegisteredAccounts)
                return false;
            return true;
        }

        public static bool CanAttemptLogin(string ip)
        {
            if (InvalidLoginAttempts.TryGetValue(ip, out byte attempts) && attempts >= MaxInvalidLoginAttempts)
                return false;
            return true;
        }

        public static AccountModel GuestAccount()
        {
            return new AccountModel() 
            {
                MaxNumChars = 1, 
                Stats = new StatsInfo() { ClassStats = new ClassStatsInfo[0] },
                AliveChars = new List<int>(),
                DeadChars = new List<int>(),
                OwnedSkins = new List<int>(),
                LockedIds = new List<int>(),
                IgnoredIds = new List<int>()
            };
        }

        public static int GetStars(AccountModel acc)
        {
            int stars = 0;
            foreach (ClassStatsInfo classStat in acc.Stats.ClassStats)
                for (int i = 0; i < Player.Stars.Length; i++)
                {
                    if (classStat.BestFame >= Player.Stars[i])
                        stars++;
                }
            return stars;
        }

        public static bool IsValidPassword(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            if (input.Length < 9) return false;
            return true;
        }

        public static bool IsValidUsername(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            if (input.Length < 1 || input.Length > 12) return false;
            return Regex.IsMatch(input, @"^[a-zA-Z0-9]+$");
        }

        public static int IdFromUsername(string username)
        {
            string value = GetKey($"login.username.{username}");
            return string.IsNullOrWhiteSpace(value) ? -1 : int.Parse(value);
        }

        public static string UsernameFromId(int id)
        {
            string value = GetKey($"login.id.{id}");
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        public static RegisterStatus RegisterAccount(string username, string password, string ip)
        {
            if (!CanRegisterAccount(ip))
                return RegisterStatus.TooManyRegisters;

            if (!IsValidUsername(username))
                return RegisterStatus.InvalidUsername;

            if (!IsValidPassword(password))
                return RegisterStatus.InvalidPassword;

            if (IdFromUsername(username) != -1)
                return RegisterStatus.UsernameTaken;

            int id = int.Parse(GetKey("nextAccId", true));
            string salt = MathUtils.GenerateSalt();
            SetKey("nextAccId", (id + 1).ToString(), true);

            SetKey($"login.username.{username}", id.ToString());
            SetKey($"login.id.{id}", username);
            SetKey($"login.hash.{id}", (password + salt).ToSHA1());
            SetKey($"login.salt.{id}", salt);

            AccountModel acc = new AccountModel(id)
            {
                Stats = new StatsInfo
                {
                    BestCharFame = 0,
                    TotalFame = 0,
                    Fame = 0,
                    TotalCredits = 0,
                    Credits = 0,
                    ClassStats = CreateClassStats()
                },

                MaxNumChars = 1,
                NextCharId = 0,
                AliveChars = new List<int>(),
                DeadChars = new List<int>(),
                OwnedSkins = new List<int>(),
                Ranked = false,
                Muted = false,
                Banned = false,
                GuildName = null,
                GuildRank = 0,
                Connected = false,
                LockedIds = new List<int>(),
                IgnoredIds = new List<int>(),
                AllyDamage = true,
                AllyShots = true,
                Effects = true,
                Sounds = true,
                Notifications = true,
                RegisterTime = UnixTime()
            };

            acc.Save();
            AddRegisteredAccount(ip);
            return RegisterStatus.Success;
        }

        public static int UnixTime()
        {
            return (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static bool IsAccountInUse(AccountModel acc)
        {
            bool accountInUse = acc.Connected && Manager.GetClient(acc.Id) != null;
            if (!accountInUse && acc.Connected)
            {
                acc.Connected = false;
                acc.Save();
            }
            return accountInUse;
        }

        public static AccountModel Verify(string username, string password, string ip)
        {
            if (!CanAttemptLogin(ip))
                return null;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            int id = IdFromUsername(username);
            if (id == -1) return null;

            string hash = GetKey($"login.hash.{id}");
            string match = (password + GetKey($"login.salt.{id}")).ToSHA1();

            AccountModel acc = hash.Equals(match) ? new AccountModel(id) : null;
            if (acc == null) AddInvalidLoginAttempt(ip);
            else acc.Load();
            return acc;
        }

        public static bool DeleteCharacter(AccountModel acc, int charId)
        {
            if (!acc.AliveChars.Contains(charId))
                return false;

            CharacterModel character = new CharacterModel(acc.Id, charId);
            character.Load();

            character.Deleted = true;
            character.Save();

            acc.AliveChars.Remove(charId);
            acc.Save();
            return true;
        }

        public static bool ChangePassword(AccountModel acc, string newPassword) 
        {
#if DEBUG
            if (acc == null)
                throw new Exception("Undefined account");
#endif
            if (!IsValidPassword(newPassword))
                return false;

            string salt = MathUtils.GenerateSalt();
            SetKey($"login.hash.{acc.Id}", (newPassword + salt).ToSHA1());
            SetKey($"login.salt.{acc.Id}", salt);
            return true;
        }

        public static bool BuyCharSlot(AccountModel acc)
        {
#if DEBUG
            if (acc == null)
                throw new Exception("Undefined account");
#endif
            if (acc.Stats.Fame < CharSlotPrice)
                return false;
            acc.Stats.Fame -= CharSlotPrice;
            acc.MaxNumChars++;
            acc.Save();
            return true;
        }

        public static bool BuySkin(AccountModel acc, int skinType)
        {
#if DEBUG
            if (acc == null)
                throw new Exception("Undefined account");
#endif
            if (!Resources.Type2Skin.ContainsKey((ushort)skinType))
                return false;
            if (acc.OwnedSkins.Contains(skinType))
                return false;
            if (acc.Stats.Credits < SkinPrice)
                return false;
            acc.Stats.Credits -= SkinPrice;
            acc.OwnedSkins.Add(skinType);
            acc.Save();
            return true;
        }

        public static XElement GetNews(AccountModel acc)
        {
            int maxNews = 7;
            int newsCount = 0;
            XElement news = new XElement("News");
            foreach (XElement item in Resources.News)
            {
                if (++newsCount > maxNews)  break;
                news.Add(item);
            }
            foreach (int d in acc.DeadChars)
            {
                if (++newsCount > maxNews) break;
                CharacterModel character = LoadCharacter(acc, d);
                character.Load();
                news.Add(new XElement("Item",
                    new XElement("Icon", "fame"),
                    new XElement("Title", $"Your {Resources.Type2Player[(ushort)character.ClassType].DisplayId} died at Level {character.Level}"),
                    new XElement("TagLine", $"Earning {character.Fame} Base Fame and {character.DeathFame} Total Fame"),
                    new XElement("Link", $"fame:{character.Id}"),
                    new XElement("Date", character.DeathTime)));
            }
            return news;
        }

        public static CharacterModel LoadCharacter(AccountModel acc, int charId)
        {
            CharacterModel character = new CharacterModel(acc.Id, charId);
            if (!character.IsNull)
                character.Load();
            return character;
        }

        public static void SaveCharacter(CharacterModel character)
        {
            character.Save();
        }

        public static void Death(string killer, AccountModel acc, CharacterModel character)
        {
#if DEBUG
            if (character == null)
                throw new Exception("Undefined character model");
#endif
            acc.AliveChars.Remove(character.Id);
            if (acc.DeadChars.Count == AccountModel.MaxDeadCharsStored)
                acc.DeadChars.RemoveAt(AccountModel.MaxDeadCharsStored - 1);
            acc.DeadChars.Insert(0, character.Id);

            int deathTime = UnixTime();
            int baseFame = character.Fame;
            int totalFame = character.Fame;

            XElement fame = new XElement("Fame");
            XElement ce = character.ExportFame();
            ce.Add(new XAttribute("id", character.Id));
            ce.Add(new XElement("Account", new XElement("Name", acc.Name)));
            fame.Add(ce);
            character.FameStats.ExportTo(fame);

            ClassStatsInfo classStats = acc.Stats.GetClassStats(character.ClassType);
            FameStats fameStats = CalculateStats(acc, character, killer);
            totalFame = fameStats.TotalFame;
            foreach (FameBonus bonus in fameStats.Bonuses)
                fame.Add(new XElement("Bonus", new XAttribute("id", bonus.Name), bonus.Fame));

            fame.Add(new XElement("CreatedOn", character.CreationTime));
            fame.Add(new XElement("KilledOn", deathTime));
            fame.Add(new XElement("KilledBy", killer));
            fame.Add(new XElement("BaseFame", baseFame));
            fame.Add(new XElement("TotalFame", totalFame));

            if (classStats.BestFame < baseFame)
                classStats.BestFame = baseFame;

            if (classStats.BestLevel < character.Level)
                classStats.BestLevel = character.Level;


            character.Dead = true;
            character.DeathTime = UnixTime();
            character.DeathFame = totalFame;
            character.Save();

            acc.Stats.Fame += totalFame;
            acc.Stats.TotalCredits += totalFame;
            acc.Save();

            if (character.Fame >= MinFameRequiredToEnterLegends)
                PushLegend(acc.Id, character.Id, totalFame, deathTime);

            CreateKey($"death.{acc.Id}.{character.Id}", fame.ToString());
        }

        public static FameStats CalculateStats(AccountModel acc, CharacterModel character, string killer = "")
        {
            int baseFame = character.Fame;
            int totalFame = baseFame;

            FameStats stats = new FameStats
            {
                BaseFame = baseFame,
                Bonuses = new List<FameBonus>()
            }; ClassStatsInfo classStats = acc.Stats.GetClassStats(character.ClassType);

            //Ancestor
            if (acc.Stats.GetClassStats(character.ClassType).BestLevel == 0)
            {
                int bonus = (int)(baseFame * .2f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Ancestor", Fame = bonus });
            }

            //First Born
            if (acc.Stats.BestCharFame < baseFame)
            {
                int bonus = (int)(baseFame * .2f);
                totalFame += bonus;
                acc.Stats.BestCharFame = baseFame;
                stats.Bonuses.Add(new FameBonus { Name = "First Born", Fame = bonus });
            }

            //Pacifist
            if (character.FameStats.DamageDealt == 0 && character.Level == 20)
            {
                int bonus = (int)(baseFame * .25f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Pacifist", Fame = bonus });
            }

            //Thirsty
            if (character.FameStats.PotionsDrank == 0 && character.Level == 20)
            {
                int bonus = (int)(baseFame * .25f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Thirsty", Fame = bonus });
            }

            //Mundane
            if (character.FameStats.AbilitiesUsed == 0 && character.Level == 20)
            {
                int bonus = (int)(baseFame * .25f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Mundane", Fame = bonus });
            }

            //Boots On The Ground
            if (character.FameStats.Teleports == 0 && character.Level == 20)
            {
                int bonus = (int)(baseFame * .25f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Boots On The Ground", Fame = bonus });
            }

            //Tunnel Rat
            if (character.FameStats.PirateCavesCompleted >= 1 &&
                character.FameStats.AbyssOfDemonsCompleted >= 1 &&
                character.FameStats.SnakePitsCompleted >= 1 &&
                character.FameStats.SpiderDensCompleted >= 1 &&
                character.FameStats.SpriteWorldsCompleted >= 1 &&
                character.FameStats.TombsCompleted >= 1 &&
                character.FameStats.UndeadLairsCompleted >= 1)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Tunnel Rat", Fame = bonus });
            }

            //Dungeon Master
            if (character.FameStats.PirateCavesCompleted >= 20 &&
                character.FameStats.AbyssOfDemonsCompleted >= 20 &&
                character.FameStats.SnakePitsCompleted >= 20 &&
                character.FameStats.SpiderDensCompleted >= 20 &&
                character.FameStats.SpriteWorldsCompleted >= 20 &&
                character.FameStats.TombsCompleted >= 20 &&
                character.FameStats.UndeadLairsCompleted >= 20)
            {
                int bonus = (int)(baseFame * .2f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Dungeon Master", Fame = bonus });
            }

            //Enemy Of The Gods
            if (((float)character.FameStats.GodKills / character.FameStats.MonsterKills) >= 0.1f)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Enemy Of The Gods", Fame = bonus });
            }

            //Slayer Of The Gods
            if (((float)character.FameStats.GodKills / character.FameStats.MonsterKills) >= 0.5f)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Slayer Of The Gods", Fame = bonus });
            }

            //Oryx Slayer
            if (character.FameStats.OryxKills >= 1)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Oryx Slayer", Fame = bonus });
            }

            //Dominator Of Realms
            if (character.FameStats.OryxKills >= 1000)
            {
                int bonus = (int)(baseFame * .25f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Dominator Of Realms", Fame = bonus });
            }

            //Accurate
            if (((float)character.FameStats.ShotsThatDamage / character.FameStats.Shots) >= .25f)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Accurate", Fame = bonus });
            }

            //Sharpshooter
            if (((float)character.FameStats.ShotsThatDamage / character.FameStats.Shots) >= .5f)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Sharpshooter", Fame = bonus });
            }

            //Sniper
            if (((float)character.FameStats.ShotsThatDamage / character.FameStats.Shots) >= .75f)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Sniper", Fame = bonus });
            }

            //Explorer
            if (character.FameStats.TilesUncovered >= 1000000)
            {
                int bonus = (int)(baseFame * .05f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Explorer", Fame = bonus });
            }

            //Cartographer
            if (character.FameStats.TilesUncovered >= 4000000)
            {
                int bonus = (int)(baseFame * .05f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Cartographer", Fame = bonus });
            }

            //Pathfinder
            if (character.FameStats.TilesUncovered >= 20000000)
            {
                int bonus = (int)(baseFame * .05f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Pathfinder", Fame = bonus });
            }

            //Team Player
            if (character.FameStats.LevelUpAssists >= 100)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Team Player", Fame = bonus });
            }

            //Leader Of Men
            if (character.FameStats.LevelUpAssists >= 1000)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Leader Of Men", Fame = bonus });
            }

            //Friend Of The Cubes
            if (character.FameStats.CubeKills == 0 && character.Level == 20)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Friend Of The Cubes", Fame = bonus });
            }

            //Careless
            if (character.FameStats.DamageTaken >= 100000)
            {
                int bonus = (int)(baseFame * .05f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Careless", Fame = bonus });
            }

            //Expert Manoeuvres
            if (character.FameStats.DamageTaken == 0 && character.Level == 20)
            {
                int bonus = (int)(baseFame * .5f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Expert Manoeuvres", Fame = bonus });
            }

            //Beginner's Luck
            if (character.FameStats.NearDeathEscapes >= 1)
            {
                int bonus = (int)(baseFame * .05f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Beginner's Luck", Fame = bonus });
            }

            //Living On The Edge
            if (character.FameStats.NearDeathEscapes >= 50)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Living On The Edge", Fame = bonus });
            }

            //Living In The Nexus
            if (character.FameStats.Escapes >= 1000)
            {
                int bonus = (int)(baseFame * .05f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Living In The Nexus", Fame = bonus });
            }

            //Seal The Deal
            if (((float)character.FameStats.MonsterKills / (character.FameStats.MonsterKills + character.FameStats.MonsterAssists) >= 0.5f) && character.Level == 20)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Seal The Deal", Fame = bonus });
            }

            //Realm Riches
            if (character.FameStats.WhiteBags >= 100)
            {
                int bonus = (int)(baseFame * .15f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Realm Riches", Fame = bonus });
            }

            //Devil's Advocate
            if (character.FameStats.AbyssOfDemonsCompleted >= 1000 &&
                character.FameStats.CubeKills >= 50000 &&
                character.FameStats.OryxKills >= 666 &&
                killer == "Lava")
            {
                int bonus = (int)(baseFame * 1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Devil's Advocate", Fame = bonus });
            }

            //Well Equipped
            int wellEquipped = 0;
            for (int k = 0; k < 4; k++)
                if (character.Inventory[k] != -1)
                {
                    wellEquipped += Resources.Type2Item[(ushort)character.Inventory[k]].FameBonus;
                    wellEquipped += (int)ItemDesc.GetStat(character.ItemDatas[k], ItemData.FameBonus, 1);
                }
            if (wellEquipped > 0)
            {
                int bonus = (int)(baseFame * (wellEquipped / 100.0f));
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Well Equipped", Fame = bonus });
            }

            stats.TotalFame = totalFame;
            return stats;
        }

        public static void FlushLegends()
        {
            int time = UnixTime();
            Legends.Clear();
            foreach (KeyValuePair<string, TimeSpan> span in TimeSpans)
            {
                string[] legends = GetKeyLines($"legends.{span.Key}", true);
                if (span.Key != "all")
                {
                    legends = legends.Where(k => !((time - span.Value.TotalSeconds) > int.Parse(k.Split(':')[3]))).ToArray();
                    legends = legends.OrderByDescending(k => int.Parse(k.Split(':')[2])).ToArray();
                    SetKeyLines($"legends.{span.Key}", legends.ToArray(), true);
                }

                //Update famelist
                XElement list = new XElement("FameList");
                list.Add(new XAttribute("timespan", span.Key));

                foreach (string i in legends.Take(20))
                {
                    string[] s = i.Split(':');
                    int accId = int.Parse(s[0]);
                    int charId = int.Parse(s[1]);
                    int totalFame = int.Parse(s[2]);
                    int deathTime = int.Parse(s[3]);

                    AccountModel acc = new AccountModel(accId);
                    //acc.Load(); Only name is accessed so no load needed

                    CharacterModel character = new CharacterModel(accId, charId);
                    character.Load();

                    list.Add(
                        new XElement("FameListElem",
                        new XAttribute("accountId", accId),
                        new XAttribute("charId", charId),
                        new XElement("Name", acc.Name),
                        new XElement("ObjectType", character.ClassType),
                        new XElement("Tex1", character.Tex1),
                        new XElement("Tex2", character.Tex2),
                        new XElement("Texture", character.SkinType),
                        new XElement("Equipment", string.Join(",", character.Inventory)),
                        new XElement("ItemDatas", string.Join(",", character.ItemDatas)),
                        new XElement("TotalFame", totalFame)));
                    Legends.Add(accId);
                }

                FameLists[span.Key] = list
;            }
        }

        public static void PushLegend(int accId, int charId, int totalFame, int deathTime)
        {
            foreach (var span in TimeSpans)
            {
                List<string> legends = GetKeyLines($"legends.{span.Key}", true).ToList();

                string entry = $"{accId}:{charId}:{totalFame}:{deathTime}";
                legends.Add(entry);
                legends = legends.OrderByDescending(k => int.Parse(k.Split(':')[2])).ToList();

                if (span.Key == "all")
                    legends = legends.Take(MaxLegends).ToList();

                SetKeyLines($"legends.{span.Key}", legends.ToArray(), true);
            }
            FlushLegends();
        }

        public static XElement GetLegends(string timespan)
        {
            if (!FameLists.ContainsKey(timespan))
                return null;
            return FameLists[timespan];
        }

        public static string GetLegend(int accId, int charId)
        {
            return GetKey($"death.{accId}.{charId}");
        }

        public static bool IsLegend(int accountId)
        {
            return Legends.Contains(accountId);
        }

        public static ClassStatsInfo[] CreateClassStats()
        {
            List<ClassStatsInfo> classStats = new List<ClassStatsInfo>();
            foreach (PlayerDesc player in Resources.Type2Player.Values) 
            {
                classStats.Add(new ClassStatsInfo
                {
                    BestFame = 0,
                    BestLevel = 0,
                    ObjectType = (int)player.Type
                });
            }
            return classStats.ToArray();
        }

        public static CharacterModel CreateCharacter(AccountModel acc, int classType, int skinType)
        {
#if DEBUG
            if (acc == null)
                throw new Exception("Account is null.");
#endif
            if (!HasEnoughCharacterSlots(acc))
                return null;

            if (!Resources.Type2Player.TryGetValue((ushort)classType, out PlayerDesc player))
                return null;

            if (skinType != 0)
            {
                if (!Resources.Type2Skin.TryGetValue((ushort)skinType, out SkinDesc skin))
                    return null;
                if (skin.PlayerClassType != classType)
                    return null;
            }

            int newId = acc.NextCharId;
            acc.NextCharId++;
            acc.AliveChars.Add(newId);
            acc.Save();

            CharacterModel character = new CharacterModel(acc.Id, newId)
            {
                ClassType = classType,
                Level = 1,
                Experience = 0,
                Fame = 0,
                Inventory = player.Equipment.ToArray(),
                ItemDatas = player.ItemDatas.ToArray(),
                Stats = player.StartingValues.ToArray(),
                HP = player.StartingValues[0],
                MP = player.StartingValues[1],
                Tex1 = 0,
                Tex2 = 0,
                SkinType = skinType,
                HasBackpack = false,
                HealthPotions = Player.MaxPotions,
                MagicPotions = Player.MaxPotions,
                CreationTime = UnixTime(),
                Deleted = false,
                Dead = false,
                DeathFame = -1,
                DeathTime = -1,
                FameStats = new FameStatsInfo(),
                PetId = -1
            };

            character.Save();
            return character;
        }

        public static bool HasEnoughCharacterSlots(AccountModel acc)
        {
#if DEBUG
            if (acc == null)
                throw new Exception("Account is null.");
#endif
            return acc.AliveChars.Count + 1 <= acc.MaxNumChars;
        }
    }
}
