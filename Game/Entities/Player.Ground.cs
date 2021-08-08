using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace RotMG.Game.Entities
{
    public partial class Player
    {
        private const float MoveSpeedThreshold = 1.1f;
        private const int SpeedHistoryCount = 10; //in world ticks (10 = 1 sec history), the lower the count, the stricter the detection

        public int MoveTime;
        public int AwaitingMoves;
        public Queue<int> AwaitingGoto;
        public List<float> SpeedHistory;
        public int TickId;
        public float PushX;
        public float PushY;

        public void PushSpeedToHistory(float speed)
        {
            SpeedHistory.Add(speed);
            if (SpeedHistory.Count > SpeedHistoryCount)
                SpeedHistory.RemoveAt(0); //Remove oldest entry
        }

        public float GetHighestSpeedHistory()
        {
            float ret = 0f;
            for (int i = 0; i < SpeedHistoryCount; i++)
            {
                if (SpeedHistory[i] > ret)
                    ret = SpeedHistory[i];
            }
            return ret;
        }

        public bool ValidMove(int time, Position pos, float speed)
        {
            int diff = time - MoveTime;
            float movementSpeed = Math.Max(speed, GetHighestSpeedHistory());
            float distanceTraveled = (movementSpeed * diff) * MoveSpeedThreshold;
            Position pushedServer = new Position(Position.X - (diff * PushX), Position.Y - (diff * PushY));
            if (pos.Distance(pushedServer) > distanceTraveled && pos.Distance(Position) > distanceTraveled)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Move stuffs... DIST/SPD = " + pos.Distance(pushedServer) + " : " + distanceTraveled);
#endif
                return false;
            }
            return true;
        }

        public void TryMove(int time, Position pos)
        {
            if (!ValidTime(time))
            {
                Client.Disconnect();
                return;
            }

            if (AwaitingGoto.Count > 0)
            {
                foreach (int gt in AwaitingGoto)
                {
                    if (gt + TimeUntilAckTimeout < time)
                    {
                        Program.Print(PrintType.Error, "Goto ack timed out");
                        Client.Disconnect();
                        return;
                    }
                }
#if DEBUG
                Program.Print(PrintType.Error, "Waiting for goto ack...");
#endif
                return;
            }
            
            if (TileFullOccupied(pos.X, pos.Y))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Tile occupied");
#endif
                Client.Disconnect();
                return;
            }

            float serverMultiplier = GetMoveMultiplier(Position);
            float clientMultiplier = GetMoveMultiplier(pos);
            float multiplier = MathF.Max(serverMultiplier, clientMultiplier);
            float movementSpeed = GetMovementSpeed(multiplier);

            if (!ValidMove(time, pos, movementSpeed))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Invalid move");
#endif
                Client.Disconnect();
                return;
            }



            AwaitingMoves--;
            if (AwaitingMoves < 0)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Too many move packets");
#endif
                Client.Disconnect();
                return;
            }

            Tile tile = Parent.Tiles[(int)pos.X, (int)pos.Y];
            TileDesc desc = Resources.Type2Tile[tile.Type];
            if (desc.Damage > 0 && !HasConditionEffect(ConditionEffectIndex.Invincible))
            {
                if (!(tile.StaticObject?.Desc.ProtectFromGroundDamage ?? false) && Damage(desc.Id, desc.Damage, new ConditionEffectDesc[0], true))
                    return;
            }

            Parent.MoveEntity(this, pos);
            if (CheckProjectiles(time))
                return;

            if (desc.Push)
            {
                PushX = desc.DX;
                PushY = desc.DY;
            }
            else
            {
                PushX = 0;
                PushY = 0;
            }

            MoveTime = time;

            PushSpeedToHistory(movementSpeed); //Add a new entry
        }

        public void TryGotoAck(int time)
        {
            if (!ValidTime(time))
            {
#if DEBUG
                Program.Print(PrintType.Error, "GotoAck invalid time");
#endif
                Client.Disconnect();
                return;
            }

            if (!AwaitingGoto.TryDequeue(out int t))
            {
#if DEBUG
                Program.Print(PrintType.Error, "No GotoAck to ack");
#endif
                Client.Disconnect();
                return;
            }
        }

        public bool Teleport(int time, Position pos)
        {
            if (!RegionUnblocked(pos.X, pos.Y))
                return false;

            Tile tile = Parent.GetTileF((int)pos.X, (int)pos.Y);
            if (tile == null || TileUpdates[(int)pos.X, (int)pos.Y] != tile.UpdateCount)
                return false;

            Parent.MoveEntity(this, pos);
            AwaitingGoto.Enqueue(time);

            byte[] eff = GameServer.ShowEffect(ShowEffectIndex.Teleport, Id, 0xFFFFFFFF, pos);
            byte[] go = GameServer.Goto(Id, pos);

            foreach (Player player in Parent.Players.Values)
            {
                if (player.Client.Account.Effects)
                    player.Client.Send(eff);
                player.Client.Send(go);
            }
            return true;
        }
    }
}
