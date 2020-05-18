using RotMG.Common;
using RotMG.Game;
using RotMG.Game.Entities;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace RotMG.Networking
{
    public static partial class GameServer
    {
        public enum PacketId
        {
            Failure,
            CreateSuccess,
            Create,
            PlayerShoot,
            Move,
            PlayerText,
            Text,
            ServerPlayerShoot,
            Damage,
            Update,
            Notification,
            NewTick,
            InvSwap,
            UseItem,
            ShowEffect,
            Hello,
            Goto,
            InvDrop,
            InvResult,
            Reconnect,
            MapInfo,
            Load,
            Teleport,
            UsePortal,
            Death,
            Buy,
            BuyResult,
            Aoe,
            PlayerHit,
            EnemyHit,
            AoeAck,
            ShootAck,
            SquareHit,
            EditAccountList,
            AccountList,
            QuestObjId,
            CreateGuild,
            GuildResult,
            GuildRemove,
            GuildInvite,
            AllyShoot,
            EnemyShoot,
            Escape,
            InvitedToGuild,
            JoinGuild,
            ChangeGuildRank,
            PlaySound,
            Reskin,
            GotoAck
        }

        public static void Read(Client client, int id, byte[] data)
        {
#if DEBUG
            Program.Print(PrintType.Debug, $"Packet received <{id}> <{string.Join(" ,",data.Select(k => k.ToString()).ToArray())}>");
#endif

            if (!client.Active)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Didn't process packet, client not active");
#endif
                return;
            }

            using (PacketReader rdr = new PacketReader(new MemoryStream(data)))
            {
                switch (id)
                {
                    case (int)PacketId.Hello:
                        Hello(client, rdr);
                        break;
                    case (int)PacketId.Create:
                        Create(client, rdr);
                        break;
                    case (int)PacketId.Load:
                        Load(client, rdr);
                        break;
                    case (int)PacketId.Move:
                        Move(client, rdr);
                        break;
                    case (int)PacketId.InvSwap:
                        InvSwap(client, rdr);
                        break;
                    case (int)PacketId.ShootAck:
                        ShootAck(client, rdr);
                        break;
                    case (int)PacketId.AoeAck:
                        AoeAck(client, rdr);
                        break;
                    case (int)PacketId.PlayerHit:
                        PlayerHit(client, rdr);
                        break;
                    case (int)PacketId.SquareHit:
                        SquareHit(client, rdr);
                        break;
                    case (int)PacketId.PlayerShoot:
                        PlayerShoot(client, rdr);
                        break;
                    case (int)PacketId.EnemyHit:
                        EnemyHit(client, rdr);
                        break;
                    case (int)PacketId.PlayerText:
                        PlayerText(client, rdr);
                        break;
                    case (int)PacketId.EditAccountList:
                        EditAccountList(client, rdr);
                        break;
                    case (int)PacketId.UseItem:
                        UseItem(client, rdr);
                        break;
                    case (int)PacketId.GotoAck:
                        GotoAck(client, rdr);
                        break;
                    case (int)PacketId.Escape:
                        Escape(client, rdr);
                        break;
                    case (int)PacketId.InvDrop:
                        InvDrop(client, rdr);
                        break;
                }
            }
        }

        public static void InvDrop(Client client, PacketReader rdr)
        {
            byte slot = rdr.ReadByte();
            client.Player.DropItem(slot);
        }

        public static void Escape(Client client, PacketReader rdr)
        {
            client.Active = false;
            client.Player.FameStats.Escapes++;
            if (client.Player.HP <= 10)
                client.Player.FameStats.NearDeathEscapes++;
            client.Send(Reconnect(Manager.NexusId));
            Manager.AddTimedAction(2000, client.Disconnect);
        }

        public static void GotoAck(Client client, PacketReader rdr)
        {
            int time = rdr.ReadInt32(); 
            client.Player.TryGotoAck(time);
        }

        public static void UseItem(Client client, PacketReader rdr)
        {
            int time = rdr.ReadInt32();
            SlotData slot = new SlotData(rdr);
            Position usePos = new Position(rdr);
            client.Player.TryUseItem(time, slot, usePos);
        }

        public static void EditAccountList(Client client, PacketReader rdr)
        {
            int accountListId = rdr.ReadInt32();
            bool add = rdr.ReadBoolean();
            int objectId = rdr.ReadInt32();
            Entity en = client.Player.Parent.GetEntity(objectId);
            if (en != null && en is Player target) 
            {
                if (target.AccountId == client.Player.AccountId)
                    return;

                switch (accountListId)
                {
                    case 0: //Lock
                        if (add) client.Account.LockedIds.Add(target.AccountId);
                        else client.Account.LockedIds.Remove(target.AccountId);
                        client.Send(AccountList(0, client.Account.LockedIds));
                        break;
                    case 1: //Ignore
                        if (add) client.Account.IgnoredIds.Add(target.AccountId);
                        else client.Account.IgnoredIds.Remove(target.AccountId);
                        client.Send(AccountList(1, client.Account.IgnoredIds));
                        break;
                }
            }
        }

        public static void PlayerText(Client client, PacketReader rdr)
        {
            string text = rdr.ReadString(); 
            client.Player.Chat(text);
        }

        public static void EnemyHit(Client client, PacketReader rdr)
        {
            int time = rdr.ReadInt32();
            int bulletId = rdr.ReadInt32();
            int targetId = rdr.ReadInt32(); 
            client.Player.TryHitEnemy(time, bulletId, targetId);
        }

        public static void PlayerShoot(Client client, PacketReader rdr)
        {
            int time = rdr.ReadInt32();
            Position pos = new Position(rdr);
            float angle = rdr.ReadSingle();
            bool ability = rdr.ReadBoolean();
            byte numShots = rdr.PeekChar() != -1 ? rdr.ReadByte() : (byte)1; 
            client.Player.TryShoot(time, pos, angle, ability, numShots);
        }

        public static void SquareHit(Client client, PacketReader rdr)
        {
            int time = rdr.ReadInt32();
            int bulletId = rdr.ReadInt32(); 
            client.Player.TryHitSquare(time, bulletId);
        }

        public static void PlayerHit(Client client, PacketReader rdr)
        {
            int bulletId = rdr.ReadInt32(); 
            client.Player.TryHit(bulletId);
        }

        public static void ShootAck(Client client, PacketReader rdr)
        {
            int time = rdr.ReadInt32(); 
            client.Player.TryShootAck(time);
        }

        public static void AoeAck(Client client, PacketReader rdr)
        {
            int time = rdr.ReadInt32();
            Position pos = new Position(rdr); 
            client.Player.TryAckAoe(time, pos);
        }

        public static void Hello(Client client, PacketReader rdr)
        {
            string buildVersion = rdr.ReadString();
            int gameId = rdr.ReadInt32();
            string username = rdr.ReadString();
            string password = rdr.ReadString();
            byte[] mapJson = rdr.ReadBytes(rdr.ReadInt32());

            if (client.State == ProtocolState.Handshaked) //Only allow Hello to be processed once.
            {
                AccountModel acc = Database.Verify(username, password, client.IP);
                if (acc == null)
                {
                    client.Send(Failure(0, "Invalid account."));
                    Manager.AddTimedAction(1000, client.Disconnect);
                    return;
                }

                if (acc.Banned)
                {
                    client.Send(Failure(0, "Banned."));
                    Manager.AddTimedAction(1000, client.Disconnect);
                    return;
                }

                if (!acc.Ranked && gameId == Manager.EditorId)
                {
                    client.Send(Failure(0, "Not ranked."));
                    Manager.AddTimedAction(1000, client.Disconnect);
                }

                Manager.GetClient(acc.Id)?.Disconnect();

                if (Database.IsAccountInUse(acc))
                {
                    client.Send(Failure(0, "Account in use!"));
                    Manager.AddTimedAction(1000, client.Disconnect);
                    return;
                }

                client.Account = acc;
                client.Account.Connected = true;
                client.Account.Save();
                client.TargetWorldId = gameId;

                Manager.AccountIdToClientId[client.Account.Id] = client.Id;
                World world = Manager.GetWorld(gameId);

#if DEBUG
                if (client.TargetWorldId == Manager.EditorId)
                {
                    Program.Print(PrintType.Debug, "Loading editor world");
                    JSMap map = new JSMap(Encoding.UTF8.GetString(mapJson));
                    world = new World(map, Resources.Worlds["Dreamland"]);
                    client.TargetWorldId = Manager.AddWorld(world);
                }
#endif

                if (world == null)
                {
                    client.Send(Failure(0, "Invalid world!"));
                    Manager.AddTimedAction(1000, client.Disconnect);
                    return;
                }

                uint seed = (uint)MathUtils.NextInt(1, int.MaxValue - 1);
                client.Random = new wRandom(seed);
                client.Send(MapInfo(world.Width, world.Height, world.Name, world.DisplayName, seed, world.Background, world.ShowDisplays, world.AllowTeleport));
                client.State = ProtocolState.Awaiting; //Allow the processing of Load/Create.
            }
        }

        public static void Create(Client client, PacketReader rdr)
        {
            int classType = rdr.ReadInt16();
            int skinType = rdr.ReadInt16();

            if (client.State == ProtocolState.Awaiting)
            {
                CharacterModel character = Database.CreateCharacter(client.Account, classType, skinType);
                if (character == null)
                {
                    client.Send(Failure(0, "Failed to create character."));
                    client.Disconnect();
                    return;
                }

                World world = Manager.GetWorld(client.TargetWorldId);
                client.Character = character;
                client.Player = new Player(client);
                client.State = ProtocolState.Connected;
                client.Send(CreateSuccess(world.AddEntity(client.Player, world.GetRegion(Region.Spawn).ToPosition()), client.Character.Id));
            }
        }

        public static void Load(Client client, PacketReader rdr)
        {
            int charId = rdr.ReadInt32();

            if (client.State == ProtocolState.Awaiting)
            {
                CharacterModel character = Database.LoadCharacter(client.Account, charId);
                if (character.IsNull || character.Dead)
                {
                    client.Send(Failure(0, "Failed to load character."));
                    client.Disconnect();
                    return;
                }

                World world = Manager.GetWorld(client.TargetWorldId);
                client.Character = character;
                client.Player = new Player(client);
                client.State = ProtocolState.Connected;
                client.Send(CreateSuccess(world.AddEntity(client.Player, world.GetRegion(Region.Spawn).ToPosition()), client.Character.Id));
            }
        }

        public static void Move(Client client, PacketReader rdr)
        {
            int time = rdr.ReadInt32();
            Position position = new Position(rdr); 
            client.Player.TryMove(time, position);
        }

        public static void InvSwap(Client client, PacketReader rdr)
        {
            int time = rdr.ReadInt32();
            Position position = new Position(rdr);
            SlotData slot1 = new SlotData(rdr);
            SlotData slot2 = new SlotData(rdr); 
            client.Player.SwapItem(slot1, slot2);
        }

        public static int Write(Client client, byte[] buffer, int offset, byte[] packet)
        {
            MemoryStream stream = new MemoryStream(buffer, offset + 4, buffer.Length - offset - 4);
            stream.Write(packet);
            int length = (int)stream.Position;
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(length + 5)), 0, buffer, offset, 4);
            return length + 5;
        }

        public static byte[] MapInfo(int width, int height, string name, string displayName, uint seed, int background, bool showDisplays, bool allowPlayerTeleport)
        {
            using (PacketWriter wtr = new PacketWriter(new MemoryStream()))
            {
                wtr.Write((byte)PacketId.MapInfo);
                wtr.Write(width);
                wtr.Write(height);
                wtr.Write(name);
                wtr.Write(displayName);
                wtr.Write(seed);
                wtr.Write(background);
                wtr.Write(showDisplays);
                wtr.Write(allowPlayerTeleport);
                return (wtr.BaseStream as MemoryStream).ToArray();
            }
        }

        public static byte[] InvResult(int result)
        {
            using (PacketWriter wtr = new PacketWriter(new MemoryStream()))
            {
                wtr.Write((byte)PacketId.InvResult);
                wtr.Write(result);
                return (wtr.BaseStream as MemoryStream).ToArray();
            }
        }

        public static byte[] Failure(int errorId, string description)
        {
            using (PacketWriter wtr = new PacketWriter(new MemoryStream()))
            {
                wtr.Write((byte)PacketId.Failure);
                wtr.Write(errorId);
                wtr.Write(description);
                return (wtr.BaseStream as MemoryStream).ToArray();
            }
        }

        public static byte[] CreateSuccess(int objectId, int charId)
        {
            using (PacketWriter wtr = new PacketWriter(new MemoryStream()))
            {
                wtr.Write((byte)PacketId.CreateSuccess);
                wtr.Write(objectId);
                wtr.Write(charId);
                return (wtr.BaseStream as MemoryStream).ToArray();
            }
        }

        public static byte[] Update(List<TileData> tiles, List<ObjectDefinition> adds, List<ObjectDrop> drops)
        {
            using (PacketWriter wtr = new PacketWriter(new MemoryStream()))
            {
                wtr.Write((byte)PacketId.Update);
                wtr.Write((short)tiles.Count);
                foreach (TileData k in tiles)
                    k.Write(wtr);

                wtr.Write((short)adds.Count);
                foreach (ObjectDefinition k in adds)
                    k.Write(wtr);

                wtr.Write((short)drops.Count);
                foreach (ObjectDrop k in drops)
                    k.Write(wtr);

                return (wtr.BaseStream as MemoryStream).ToArray();
            }
        }

        public static byte[] NewTick(List<ObjectStatus> statuses, Dictionary<StatType, object> playerStats)
        {
            using (PacketWriter wtr = new PacketWriter(new MemoryStream()))
            {
                wtr.Write((byte)PacketId.NewTick);
                wtr.Write((short)statuses.Count);
                foreach (ObjectStatus k in statuses)
                    k.Write(wtr);
                if (playerStats.Count > 0)
                {
                    wtr.Write((byte)playerStats.Count);
                    foreach (KeyValuePair<StatType, object> k in playerStats)
                    {
                        wtr.Write((byte)k.Key);
                        if (ObjectStatus.IsStringStat(k.Key))
                            wtr.Write((string)k.Value);
                        else
                            wtr.Write((int)k.Value);
                    }
                }
                return (wtr.BaseStream as MemoryStream).ToArray();
            }
        }

        public static byte[] EnemyShoot(int bulletId, int ownerId, byte bulletType, Position startPos, float angle, short damage, byte numShots, float angleInc)
        {
            using (PacketWriter wtr = new PacketWriter(new MemoryStream()))
            {
                wtr.Write((byte)PacketId.EnemyShoot);
                wtr.Write(bulletId);
                wtr.Write(ownerId);
                wtr.Write(bulletType);
                startPos.Write(wtr);
                wtr.Write(angle);
                wtr.Write(damage);
                if (numShots > 1)
                {
                    wtr.Write(numShots);
                    wtr.Write(angleInc);
                }
                return (wtr.BaseStream as MemoryStream).ToArray();
            }
        }

        public static byte[] ShowEffect(ShowEffectIndex effect, int targetObjectId, uint color, Position pos1 = new Position(), Position pos2 = new Position())
        {
            using (PacketWriter wtr = new PacketWriter(new MemoryStream()))
            {
                wtr.Write((byte)PacketId.ShowEffect);
                wtr.Write((byte)effect);
                wtr.Write(targetObjectId);
                wtr.Write((int)color);
                pos1.Write(wtr);
                if (pos2.X != 0 || pos2.Y != 0)
                    pos2.Write(wtr);
                return (wtr.BaseStream as MemoryStream).ToArray();
            }
        }

        public static byte[] Goto(int objectId, Position pos)
        {
            using (PacketWriter wtr = new PacketWriter(new MemoryStream()))
            {
                wtr.Write((byte)PacketId.Goto);
                wtr.Write(objectId);
                pos.Write(wtr);
                return (wtr.BaseStream as MemoryStream).ToArray();
            }
        }

        public static byte[] Aoe(Position pos, float radius, int damage, ConditionEffectIndex effect, uint color)
        {
            using (PacketWriter wtr = new PacketWriter(new MemoryStream()))
            {
                wtr.Write((byte)PacketId.Aoe);
                pos.Write(wtr);
                wtr.Write(radius);
                wtr.Write((short)damage);
                wtr.Write((byte)effect);
                wtr.Write((int)color);
                return (wtr.BaseStream as MemoryStream).ToArray();
            }
        }

        public static byte[] Damage(int targetId, ConditionEffectIndex[] effects, int damage)
        {
            using (PacketWriter wtr = new PacketWriter(new MemoryStream()))
            {
                wtr.Write((byte)PacketId.Damage);
                wtr.Write(targetId);
                wtr.Write((byte)effects.Length);
                for (int i = 0; i < effects.Length; i++)
                    wtr.Write((byte)(effects[i]));
                wtr.Write((ushort)damage);
                return (wtr.BaseStream as MemoryStream).ToArray();
            }
        }

        public static byte[] Death(int accountId, int charId, string killer)
        {
            using (PacketWriter wtr = new PacketWriter(new MemoryStream()))
            {
                wtr.Write((byte)PacketId.Death);
                wtr.Write(accountId);
                wtr.Write(charId);
                wtr.Write(killer);
                return (wtr.BaseStream as MemoryStream).ToArray();
            }
        }

        public static byte[] AllyShoot(int ownerId, int containerType, float angle)
        {
            using (PacketWriter wtr = new PacketWriter(new MemoryStream()))
            {
                wtr.Write((byte)PacketId.AllyShoot);
                wtr.Write(ownerId);
                wtr.Write((short)containerType);
                wtr.Write(angle);
                return (wtr.BaseStream as MemoryStream).ToArray();
            }
        }
        
        public static byte[] PlaySound(string sound)
        {
            using (PacketWriter wtr = new PacketWriter(new MemoryStream()))
            {
                wtr.Write((byte)PacketId.PlaySound);
                wtr.Write(sound);
                return (wtr.BaseStream as MemoryStream).ToArray();
            }
        }

        public static byte[] Text(string name, int objectId, int numStars, int bubbleTime, string recipent, string text)
        {
            using (PacketWriter wtr = new PacketWriter(new MemoryStream()))
            {
                wtr.Write((byte)PacketId.Text);
                wtr.Write(name);
                wtr.Write(objectId);
                wtr.Write(numStars);
                wtr.Write((byte)bubbleTime);
                wtr.Write(recipent);
                wtr.Write(text);
                return (wtr.BaseStream as MemoryStream).ToArray();
            }
        }

        public static byte[] AccountList(int accountListId, List<int> accountIds)
        {
            using (PacketWriter wtr = new PacketWriter(new MemoryStream()))
            {
                wtr.Write((byte)PacketId.AccountList);
                wtr.Write(accountListId);
                wtr.Write((short)accountIds.Count);
                for (int i = 0; i < accountIds.Count; i++)
                    wtr.Write(accountIds[i]);
                return (wtr.BaseStream as MemoryStream).ToArray();
            }
        }

        public static byte[] ServerPlayerShoot(int bulletId, int ownerId, int containerType, Position startPos, float angle, float angleInc, List<Projectile> projs)
        {
            using (PacketWriter wtr = new PacketWriter(new MemoryStream()))
            {
                wtr.Write((byte)PacketId.ServerPlayerShoot);
                wtr.Write(bulletId);
                wtr.Write(ownerId);
                wtr.Write((short)containerType);
                startPos.Write(wtr);
                wtr.Write(angle);
                wtr.Write(angleInc);
                wtr.Write((byte)projs.Count);
                for (int i = 0; i < projs.Count; i++)
                    wtr.Write((short)projs[i].Damage);
                return (wtr.BaseStream as MemoryStream).ToArray();
            }
        }

        public static byte[] Reconnect(int gameId)
        {
            using (PacketWriter wtr = new PacketWriter(new MemoryStream()))
            {
                wtr.Write((byte)PacketId.Reconnect);
                wtr.Write(gameId);
                return (wtr.BaseStream as MemoryStream).ToArray();
            }
        }

        public static byte[] Notification(int objectId, string text, uint color)
        {
            using (PacketWriter wtr = new PacketWriter(new MemoryStream()))
            {
                wtr.Write((byte)PacketId.Notification);
                wtr.Write(objectId);
                wtr.Write(text);
                wtr.Write((int)color);
                return (wtr.BaseStream as MemoryStream).ToArray();
            }
        }

        public static byte[] PolicyFile = _policyFile();
        static byte[] _policyFile()
        {
            using (PacketWriter wtr = new PacketWriter(new MemoryStream()))
            {
                wtr.WriteNullTerminatedString(
                    @"<cross-domain-policy>" +
                    @"<allow-access-from domain=""*"" to-ports=""*"" />" +
                    @"</cross-domain-policy>");
                wtr.Write((byte)'\r');
                wtr.Write((byte)'\n');
                return (wtr.BaseStream as MemoryStream).ToArray();
            }
        }
    }
}
