using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RotMG.Game.Entities
{
    public partial class Player
    {
        private const int ChatCooldownMS = 200;

        public int LastChatTime;

        public void SendInfo(string text) => Client.Send(GameServer.Text("", 0, -1, 0, "", text));
        public void SendError(string text) => Client.Send(GameServer.Text("*Error*", 0, -1, 0, "", text));
        public void SendHelp(string text) => Client.Send(GameServer.Text("*Help*", 0, -1, 0, "", text));
        public void SendClientText(string text) => Client.Send(GameServer.Text("*Client*", 0, -1, 0, "", text));

        public void Chat(string text)
        {
            if (text.Length <= 0 || text.Length > 128)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Text too short or too long");
#endif
                Client.Disconnect();
                return;
            }

            string validText = Regex.Replace(text, @"[^a-zA-Z0-9`!@#$%^&* ()_+|\-=\\{}\[\]:"";'<>?,./]", "");
            if (validText.Length <= 0)
            {
                SendError("Invalid text.");
                return;
            }

            if (LastChatTime + ChatCooldownMS > Manager.TotalTimeUnsynced)
            {
                SendError("Message sent too soon after previous one.");
                return;
            }

            LastChatTime = Manager.TotalTimeUnsynced;

            if (validText[0] == '/')
            {
                string[] s = validText.Split(' ');
                string[] j = new string[s.Length - 1];
                for (int i = 1; i < s.Length; i++)
                    j[i - 1] = s[i];
                string command = s[0];
                string input = string.Join(' ', j);
                switch (command.ToLower())
                {
                    case "/legendary":
                        if (Client.Account.Ranked)
                        {
                            int slot = int.Parse(j[0]);
                            if (Inventory[slot] != -1)
                            {
                                Tuple<bool, ItemData> roll = Resources.Type2Item[(ushort)Inventory[slot]].Roll();
                                while ((roll.Item2 & ItemData.T7) == 0)
                                    roll = Resources.Type2Item[(ushort)Inventory[slot]].Roll();
                                ItemDatas[slot] = !roll.Item1 ? -1 : (int)roll.Item2;
                                UpdateInventorySlot(slot);
                                RecalculateEquipBonuses();
                            }
                        }
                        break;
                    case "/roll":
                        if (Client.Account.Ranked)
                        {
                            for (int k = 0; k < 20; k++)
                            {
                                if (Inventory[k] != -1)
                                {
                                    Tuple<bool, ItemData> roll = Resources.Type2Item[(ushort)Inventory[k]].Roll();
                                    ItemDatas[k] = !roll.Item1 ? -1 : (int)roll.Item2;
                                    UpdateInventorySlot(k);
                                    RecalculateEquipBonuses();
                                }
                            }
                        }
                        break;
                    case "/disconnect":
                    case "/dcAll":
                    case "/dc":
                        if (Client.Account.Ranked)
                        {
                            foreach (Client c in Manager.Clients.Values.ToArray())
                            {
                                try { c.Disconnect(); }
                                catch { }
                            }
                        }
                        break;
                    case "/terminate":
                    case "/stop":
                        if (Client.Account.Ranked)
                        {
                            Program.StartTerminating();
                            return;
                        }
                        break;
                    case "/gimme":
                    case "/give":
                        if (!Client.Account.Ranked)
                        {
                            SendError("Not ranked");
                            return;
                        }
                        if (Resources.IdLower2Item.TryGetValue(input.ToLower(), out ItemDesc item))
                        {
                            if (GiveItem(item.Type))
                                SendInfo("Success");
                            else SendError("No inventory slots");
                        }
                        else SendError($"Item <{input}> not found in GameData");
                        break;
                    case "/create":
                    case "/spawn":
                        if (!Client.Account.Ranked)
                        {
                            SendError("Not ranked");
                            return;
                        }
                        if (string.IsNullOrWhiteSpace(input))
                        {
                            SendHelp("/spawn <count> <entity>");
                            return;
                        }
                        int spawnCount;
                        if (!int.TryParse(j[0], out spawnCount))
                            spawnCount = -1;
                        if (Resources.IdLower2Object.TryGetValue((spawnCount == -1 ? input : string.Join(' ', j.Skip(1))).ToLower(), out ObjectDesc desc))
                        {
                            if (spawnCount == -1) spawnCount = 1;
                            if (desc.Player || desc.Static)
                            {
                                SendError("Can't spawn this entity");
                                return;
                            }
                            SendInfo($"Spawning <{spawnCount}> <{desc.DisplayId}> in 2 seconds");
                            Position pos = Position;
                            Manager.AddTimedAction(2000, () =>
                            {
                                for (int i = 0; i < spawnCount; i++)
                                {
                                    Entity entity = Resolve(desc.Type);
                                    Parent?.AddEntity(entity, pos);
                                }
                            });
                        }
                        else
                        {
                            SendError($"Entity <{input}> not found in Game Data");
                        }
                        break;
                    case "/max":
                        if (!Client.Account.Ranked)
                        {
                            SendError("Not ranked");
                            return;
                        }
                        for (int i = 0; i < Stats.Length; i++)
                            Stats[i] = (Desc as PlayerDesc).Stats[i].MaxValue;
                        UpdateStats();
                        SendInfo("Maxed");
                        break;
                    case "/god":
                        if (!Client.Account.Ranked)
                        {
                            SendError("Not ranked");
                            return;
                        }
                        ApplyConditionEffect(ConditionEffectIndex.Invincible, HasConditionEffect(ConditionEffectIndex.Invincible) ? 0 : -1);
                        SendInfo($"Godmode set to {HasConditionEffect(ConditionEffectIndex.Invincible)}");
                        break;
                    case "/allyshots":
                        Client.Account.AllyShots = !Client.Account.AllyShots;
                        SendInfo($"Ally shots set to {Client.Account.AllyShots}");
                        break;
                    case "/allydamage":
                        Client.Account.AllyDamage = !Client.Account.AllyDamage;
                        SendInfo($"Ally damage set to {Client.Account.AllyDamage}");
                        break;
                    case "/effects":
                        Client.Account.Effects = !Client.Account.Effects;
                        SendInfo($"Effects set to {Client.Account.Effects}");
                        break;
                    case "/sounds":
                        Client.Account.Sounds = !Client.Account.Sounds;
                        SendInfo($"Sounds set to {Client.Account.Sounds}");
                        break;
                    case "/notifications":
                        Client.Account.Notifications = !Client.Account.Notifications;
                        SendInfo($"Notifications set to {Client.Account.Notifications}");
                        break;
                    case "/online":
                    case "/who":
                        SendInfo($"" +
                            $"<{Manager.Clients.Values.Count(k => k.Player != null)} Player(s)> " +
                            $"<{string.Join(", ", Manager.Clients.Values.Where(k => k.Player != null).Select(k => k.Player.Name))}>");
                        break;
                    case "/server":
                    case "/pos":
                    case "/loc":
                        SendInfo(this.ToString());
                        break;
                    case "/where":
                    case "/find":
                        Player findTarget = Manager.GetPlayer(input);
                        if (findTarget == null) SendError("Couldn't find player");
                        else SendInfo(findTarget.ToString());
                        break;
                    case "/fame":
                    case "/famestats":
                    case "/stats":
                        SaveToCharacter();
                        FameStats fameStats = Database.CalculateStats(Client.Account, Client.Character, "");
                        SendInfo($"Active: {FameStats.MinutesActive} minutes");
                        SendInfo($"Shots: {FameStats.Shots}");
                        SendInfo($"Accuracy: {(int)(((float)FameStats.ShotsThatDamage / FameStats.Shots) * 100f)}% ({FameStats.ShotsThatDamage}/{FameStats.Shots})");
                        SendInfo($"Abilities Used: {FameStats.AbilitiesUsed}");
                        SendInfo($"Tiles Seen: {FameStats.TilesUncovered}");
                        SendInfo($"Monster Kills: {FameStats.MonsterKills} ({FameStats.MonsterAssists} Assists, {(int)(((float)FameStats.MonsterKills / (FameStats.MonsterKills + FameStats.MonsterAssists)) * 100f)}% Final Blows)");
                        SendInfo($"God Kills: {FameStats.GodKills} ({(int)(((float)FameStats.GodKills / FameStats.MonsterKills) * 100f)}%) ({FameStats.GodKills}/{FameStats.MonsterKills})");
                        SendInfo($"Oryx Kills: {FameStats.OryxKills} ({(int)(((float)FameStats.OryxKills / FameStats.MonsterKills) * 100f)}%) ({FameStats.OryxKills}/{FameStats.MonsterKills})");
                        SendInfo($"Cube Kills: {FameStats.CubeKills} ({(int)(((float)FameStats.CubeKills / FameStats.MonsterKills) * 100f)}%) ({FameStats.CubeKills}/{FameStats.MonsterKills})");
                        SendInfo($"Cyan Bags: {FameStats.CyanBags}");
                        SendInfo($"Blue Bags: {FameStats.BlueBags}");
                        SendInfo($"White Bags: {FameStats.WhiteBags}");
                        SendInfo($"Damage Taken: {FameStats.DamageTaken}");
                        SendInfo($"Damage Dealt: {FameStats.DamageDealt}");
                        SendInfo($"Teleports: {FameStats.Teleports}");
                        SendInfo($"Potions Drank: {FameStats.PotionsDrank}");
                        SendInfo($"Quests Completed: {FameStats.QuestsCompleted}");
                        SendInfo($"Pirate Caves Completed: {FameStats.PirateCavesCompleted}");
                        SendInfo($"Spider Dens Completed: {FameStats.SpiderDensCompleted}");
                        SendInfo($"Snake Pits Completed: {FameStats.SnakePitsCompleted}");
                        SendInfo($"Sprite Worlds Completed: {FameStats.SpriteWorldsCompleted}");
                        SendInfo($"Undead Lairs Completed: {FameStats.UndeadLairsCompleted}");
                        SendInfo($"Abyss Of Demons Completed: {FameStats.AbyssOfDemonsCompleted}");
                        SendInfo($"Tombs Completed: {FameStats.TombsCompleted}");
                        SendInfo($"Escapes: {FameStats.Escapes}");
                        SendInfo($"Near Death Escapes: {FameStats.NearDeathEscapes}");
                        SendInfo($"Party Member Level Ups: {FameStats.LevelUpAssists}");
                        foreach (FameBonus bonus in fameStats.Bonuses)
                            SendHelp($"{bonus.Name}: +{bonus.Fame}");
                        SendInfo($"Base Fame: {fameStats.BaseFame}");
                        SendInfo($"Total Fame: {fameStats.TotalFame}");
                        break;
                    default:
                        SendError("Unknown command");
                        break;
                }
                return;
            }

            if (Client.Account.Muted)
            {
                SendError("You are muted");
                return;
            }

            byte[] packet = GameServer.Text(Name, Id, NumStars, 5, "", validText);

            foreach (Player player in Parent.Players.Values)
                if (!player.Client.Account.IgnoredIds.Contains(AccountId))
                    player.Client.Send(packet);
        }
    }
}
