using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;

namespace RotMG.Game.Entities
{
    public partial class Player
    {
        public const int SightRadius = 15;
        private const float StartAngle = 0;
        private const float EndAngle = (float)(2 * Math.PI);
        private const float RayStepSize = .05f;
        private const float AngleStepSize = 2.30f / (SightRadius * 2);

        private static readonly IntPoint[] SurroundingPoints = new IntPoint[]
        {
            new IntPoint(1, 0),
            new IntPoint(1, 1),
            new IntPoint(0, 1),
            new IntPoint(-1, 1),
            new IntPoint(-1, 0),
            new IntPoint(-1, -1),
            new IntPoint(0, -1),
            new IntPoint(1, -1)
        };

        private static HashSet<IntPoint> SightCircle;
        public static void InitSightCircle()
        {
            SightCircle = new HashSet<IntPoint>();
            for (int x = -SightRadius; x <= SightRadius; x++)
                for (int y = -SightRadius; y <= SightRadius; y++)
                    if (x * x + y * y <= SightRadius * SightRadius)
                        SightCircle.Add(new IntPoint(x, y));
        }

        private static HashSet<IntPoint>[] SightRays;
        public static void InitSightRays()
        {
            List<HashSet<IntPoint>> sightRays = new List<HashSet<IntPoint>>();

            float currentAngle = StartAngle;
            while (currentAngle < EndAngle)
            {
                HashSet<IntPoint> ray = new HashSet<IntPoint>();
                float dist = RayStepSize;
                while (dist < (SightRadius + 25))
                {
                    IntPoint point = new IntPoint(
                        (int)(dist * Math.Cos(currentAngle)),
                        (int)(dist * Math.Sin(currentAngle)));

                    if (SightCircle.Contains(point))
                        ray.Add(point);
                    dist += RayStepSize;
                }
                sightRays.Add(ray);
                currentAngle += AngleStepSize;
            }

            SightRays = sightRays.ToArray();
        }

        public int[,] TileUpdates;
        public Dictionary<int, int> EntityUpdates;
        public HashSet<Entity> Entities;
        public HashSet<IntPoint> CalculatedSightCircle;

        public void SendNewTick()
        {
            List<ObjectStatus> statuses = new List<ObjectStatus>();
            foreach (Entity en in Entities)
                if (EntityUpdates[en.Id] != en.UpdateCount)
                {
                    statuses.Add(en.GetObjectStatus(true));
                    EntityUpdates[en.Id] = en.UpdateCount;
                }

            Client.Send(GameServer.NewTick(statuses, PrivateSVs));
            PrivateSVs.Clear();
            AwaitingMoves++;
        }

        public void SendUpdate()
        {
            bool nUpdate = ShouldCalculateSightCircle();
            HashSet<IntPoint> sight = Parent.BlockSight == 0 ? SightCircle :
                    nUpdate ? CalculateSightCircle() : CalculatedSightCircle;

            List<TileData> tiles = new List<TileData>();
            List<ObjectDefinition> adds = new List<ObjectDefinition>();
            List<ObjectDrop> drops = new List<ObjectDrop>();
            HashSet<int> droppedIds = new HashSet<int>();

            if (nUpdate)
            {
                //Get tiles
                foreach (IntPoint p in sight)
                {
                    int x = p.X + (int)Position.X;
                    int y = p.Y + (int)Position.Y;
                    Tile tile = Parent.GetTile(x, y);

                    if (tile == null || TileUpdates[x, y] == tile.UpdateCount)
                        continue;

                    tiles.Add(new TileData
                    {
                        TileType = tile.Type,
                        X = (short)x,
                        Y = (short)y
                    });

                    TileUpdates[x, y] = tile.UpdateCount;
                }

                //Add statics
                foreach (IntPoint p in SightCircle)
                {
                    int x = p.X + (int)Position.X;
                    int y = p.Y + (int)Position.Y;

                    Tile tile = Parent.GetTile(x, y);
                    if (tile == null || tile.StaticObject == null)
                        continue;

                    if (TileUpdates[x, y] == tile.UpdateCount)
                    {
                        if (Entities.Add(tile.StaticObject))
                        {
                            adds.Add(tile.StaticObject.GetObjectDefinition());
                            EntityUpdates.Add(tile.StaticObject.Id, tile.StaticObject.UpdateCount);
                        }
                    }
                }
            }

            foreach (Entity en in Parent.PlayerChunks.HitTest(Position, SightRadius))
            {
                if (Entities.Add(en))
                {
                    adds.Add(en.GetObjectDefinition());
                    EntityUpdates.Add(en.Id, en.UpdateCount);
                }
            }

            //Add players
            foreach (Player player in Parent.Players.Values)
            {
                if (Entities.Add(player))
                {
                    adds.Add(player.GetObjectDefinition());
                    EntityUpdates.Add(player.Id, player.UpdateCount);
                }
            }

            //Add entities
            foreach (Entity en in Parent.EntityChunks.HitTest(Position, SightRadius))
            {
                IntPoint point = new IntPoint
                {
                    X = (int)en.Position.X - (int)Position.X,
                    Y = (int)en.Position.Y - (int)Position.Y
                };

                if (en is Container)
                    if ((en as Container).OwnerId != -1 && (en as Container).OwnerId != Id)
                        continue;

                if (sight.Contains(point) && Entities.Add(en))
                {
                    adds.Add(en.GetObjectDefinition());
                    EntityUpdates.Add(en.Id, en.UpdateCount);
                }
            }

            //Remove entities and statics (as they end up in the same Entities dictionary
            foreach (Entity en in Entities)
            {
                IntPoint point = new IntPoint
                {
                    X = (int)en.Position.X - (int)Position.X,
                    Y = (int)en.Position.Y - (int)Position.Y
                };

                if (en.Desc.Static)
                {
                    if (en.Parent == null || !SightCircle.Contains(point))
                    {
                        drops.Add(en.GetObjectDrop());
                        droppedIds.Add(en.Id);
                        EntityUpdates.Remove(en.Id);
                    }
                }
                else
                {
                    if (en.Parent == null || (!sight.Contains(point) && !en.Desc.Player))
                    {
                        drops.Add(en.GetObjectDrop());
                        droppedIds.Add(en.Id);
                        EntityUpdates.Remove(en.Id);
                    }
                }
            }

            Entities.RemoveWhere(k => droppedIds.Contains(k.Id));

            if (tiles.Count > 0 || adds.Count > 0 || drops.Count > 0)
            {
                Client.Send(GameServer.Update(tiles, adds, drops));
                FameStats.TilesUncovered += tiles.Count;
            }
        }

        IntPoint _p; int _w;
        private bool ShouldCalculateSightCircle()
        {
            IntPoint pos = Position.ToIntPoint();
            if (_p != pos || Parent.UpdateCount != _w)
            {
                _p = pos;
                _w = Parent.UpdateCount;
                return true;
            }
            return false;
        }

        private HashSet<IntPoint> CalculateSightCircle()
        {
            CalculatedSightCircle.Clear();

            if (Parent.BlockSight == 1) //Line casting
            {
                foreach (HashSet<IntPoint> ray in SightRays)
                    foreach (IntPoint p in ray)
                    {
                        if (Parent.BlocksSight(p.X + (int)Position.X, p.Y + (int)Position.Y))
                            break;

                        CalculatedSightCircle.Add(p);
                        foreach (IntPoint s in SurroundingPoints)
                        {
                            IntPoint sp = new IntPoint(p.X + s.X, p.Y + s.Y);
                            if (SightCircle.Contains(sp))
                                CalculatedSightCircle.Add(sp);
                        }
                    }
            }

            if (Parent.BlockSight == 2) //Path
            {
                Stack<IntPoint> scan = new Stack<IntPoint>();
                HashSet<int> scanned = new HashSet<int>();

                scan.Push(Position.ToIntPoint());
                while (scan.Count != 0)
                {
                    IntPoint current = scan.Pop();
                    if (CalculatedSightCircle.Add(new IntPoint(current.X - (int)Position.X, current.Y - (int)Position.Y)))
                        foreach (IntPoint s in SurroundingPoints)
                        {
                            IntPoint p = new IntPoint(current.X + s.X, current.Y + s.Y);
                            IntPoint c = new IntPoint(p.X - (int)Position.X, p.Y - (int)Position.Y);
                            if (scanned.Contains(c.GetHashCode()) || !SightCircle.Contains(c))
                                continue;

                            scanned.Add(c.GetHashCode());
                            if (Parent.BlocksSight(p.X, p.Y))
                            {
                                CalculatedSightCircle.Add(c);
                                continue;
                            }

                            scan.Push(p);
                        }
                }
            }

            return CalculatedSightCircle;
        }
    }
}
