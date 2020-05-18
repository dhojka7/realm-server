using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RotMG.Game.Entities
{
    public class AoeAck
    {
        public int Damage;
        public ConditionEffectDesc[] Effects;
        public Position Position;
        public string Hitter;
        public float Radius;
        public int Time;
    }

    public struct ProjectileAck
    {
        public static ProjectileAck Undefined = new ProjectileAck
        {
            Projectile = null,
            Time = -1
        };

        public Projectile Projectile;
        public int Time;

        public override bool Equals(object obj)
        {
#if DEBUG
            if (obj is null)
                throw new Exception("Undefined object");
#endif
            return (obj as ProjectileAck?).Value.Projectile.Id == Projectile.Id;
        }

        public override int GetHashCode()
        {
            return Projectile.Id;
        }
    }

    public partial class Player
    {
        private const int TimeUntilAckTimeout = 2000;
        private const int TickProjectilesDelay = 2000;
        private const float RateOfFireThreshold = 1.1f;
        private const float EnemyHitRangeAllowance = 1.7f;
        private const float EnemyHitTrackPrecision = 8;
        private const int EnemyHitHistoryBacktrack = 2;

        public Queue<List<Projectile>> AwaitingProjectiles;
        public Dictionary<int, ProjectileAck> AckedProjectiles;

        public Queue<AoeAck> AwaitingAoes; //Doesn't really belong here... But Player.Aoe.cs???

        public Dictionary<int, Projectile> ShotProjectiles;
        public int NextAEProjectileId = int.MinValue; //Goes up positively from bottom (Server sided projectiles)
        public int NextProjectileId; //Goes down negatively (Client sided projectiles)
        public int ShotTime;
        public int ShotDuration;

        public void TickProjectiles()
        {
            if (Manager.TotalTime % TickProjectilesDelay != 0)
                return;

            foreach (AoeAck aoe in AwaitingAoes)
            {
                if (Manager.TotalTime - aoe.Time > TimeUntilAckTimeout)
                {
#if DEBUG
                    Program.Print(PrintType.Error, "Aoe ack timed out");
#endif
                    Client.Disconnect();
                    return;
                }
            }

            foreach (List<Projectile> apList in AwaitingProjectiles)
            {
                foreach (Projectile ap in apList)
                {
                    if (Manager.TotalTime - ap.Time > TimeUntilAckTimeout)
                    {
#if DEBUG
                        Program.Print(PrintType.Error, "Proj ack timed out");
#endif
                        Client.Disconnect();
                        return;
                    }
                }
            }
        }

        public int GetNextDamageSeeded(int min, int max, int data)
        {
            float dmgMod = ItemDesc.GetStat(data, ItemData.Damage, ItemDesc.DamageMultiplier);
            int minDmg = min + (int)(min * dmgMod);
            int maxDmg = max + (int)(max * dmgMod);
            return (int)Client.Random.NextIntRange((uint)minDmg, (uint)maxDmg);
        }

        public int GetNextDamage(int min, int max, int data)
        {
            float dmgMod = ItemDesc.GetStat(data, ItemData.Damage, ItemDesc.DamageMultiplier);
            int minDmg = min + (int)(min * dmgMod);
            int maxDmg = max + (int)(max * dmgMod);
            return MathUtils.NextInt(minDmg, maxDmg);
        }

        public void TryHitEnemy(int time, int bulletId, int targetId)
        {
            if (!ValidTime(time))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Invalid time for enemy hit");
#endif
                Client.Disconnect();
                return;
            }

            if (ShotProjectiles.TryGetValue(bulletId, out Projectile p))
            {
                Entity target = Parent.GetEntity(targetId);
                if (target == null || !target.Desc.Enemy)
                {
#if DEBUG
                    Program.Print(PrintType.Error, "Invalid enemy target");
#endif
                    return;
                }
                int elapsed = time - p.Time;
                int steps = (int)Math.Ceiling((p.Desc.Speed / 100f) * ((elapsed * EnemyHitTrackPrecision) / 1000f));
                float timeStep = (float)elapsed / steps;

                for (int k = 0; k <= steps; k++)
                {
                    Position pos = p.PositionAt(k * timeStep);
                    if (k == steps) //Try hit enemy
                    {
                        if (target.Desc.Static)
                        {
                            if (pos.Distance(target.Position) <= EnemyHitRangeAllowance && p.CanHit(target))
                            {
                                target.HitByProjectile(p);
                                if (!p.Desc.MultiHit)
                                    ShotProjectiles.Remove(p.Id);
                                return;
                            }
                        }
                        else
                        {
                            for (int j = 0; j <= EnemyHitHistoryBacktrack; j++)
                            {
                                if (pos.Distance(target.TryGetHistory(j)) <= EnemyHitRangeAllowance && p.CanHit(target))
                                {
                                    target.HitByProjectile(p);
                                    if (!p.Desc.MultiHit)
                                        ShotProjectiles.Remove(p.Id);
                                    return;
                                }
                            }
                        }
#if DEBUG
                    Console.WriteLine(pos);
                    Console.WriteLine(target);
                    Program.Print(PrintType.Error, "Enemy hit aborted, too far away from projectile");
#endif
                    }
                    else //Check collisions to make sure player isn't shooting through walls etc
                    {
                        Tile tile = Parent.GetTileF(pos.X, pos.Y);

                        if ((tile == null || tile.Type == 255) ||
                            (tile.StaticObject != null && !tile.StaticObject.Desc.Enemy && (tile.StaticObject.Desc.EnemyOccupySquare || !p.Desc.PassesCover && tile.StaticObject.Desc.OccupySquare)))
                        {
#if DEBUG
                            Program.Print(PrintType.Error, "Shot projectile hit wall, removed");
#endif
                            ShotProjectiles.Remove(bulletId);
                            return;
                        }
                    }
                }
            }
#if DEBUG
            else
            {
                Program.Print(PrintType.Error, "Tried to hit enemy with undefined projectile");
            }
#endif
        }

        public void TryShoot(int time, Position pos, float attackAngle, bool ability, int numShots)
        {
            if (!ValidTime(time))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Invalid time for player shoot");
#endif
                Client.Disconnect();
                return;
            }

            if (AwaitingGoto.Count > 0)
            {
                Client.Random.Drop(numShots);
                return;
            }

            if (!ValidMove(time, pos))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Invalid move for player shoot");
#endif
                Client.Disconnect();
                return;
            }

            int startId = NextProjectileId;
            NextProjectileId -= numShots;

            ItemDesc desc = ability ? GetItem(1) : GetItem(0);
            if (desc == null)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Undefined item descriptor");
#endif
                Client.Random.Drop(numShots);
                return;
            }


            if (numShots != desc.NumProjectiles)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Manipulated num shots");
#endif
                Client.Random.Drop(numShots);
                return;
            }

            if (HasConditionEffect(ConditionEffectIndex.Stunned))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Stunned...");
#endif
                Client.Random.Drop(numShots);
                return;
            }

            if (ability)
            {
                if (ShootAEs.TryDequeue(out ushort aeItemType))
                {
                    if (aeItemType != desc.Type)
                    {
                        Client.Random.Drop(numShots);
                        return;
                    }

                    float arcGap = (desc.ArcGap * MathUtils.ToRadians);
                    float totalArc = arcGap * (numShots - 1);
                    float angle = attackAngle - (totalArc / 2f);
                    for (int i = 0; i < numShots; i++)
                    {
                        int damage = (int)(GetNextDamageSeeded(desc.Projectile.MinDamage, desc.Projectile.MaxDamage, ItemDatas[1]) * GetAttackMultiplier());
                        Projectile projectile = new Projectile(this, desc.Projectile, startId - i, time, angle + (arcGap * i), pos, damage);
                        ShotProjectiles.Add(projectile.Id, projectile);
                    }

                    byte[] packet = GameServer.AllyShoot(Id, desc.Type, attackAngle);
                    foreach (Entity en in Parent.PlayerChunks.HitTest(Position, SightRadius))
                        if (en is Player player && player.Client.Account.AllyShots && !player.Equals(this))
                            player.Client.Send(packet);

                    FameStats.Shots += numShots;
                }
                else
                {
#if DEBUG
                    Program.Print(PrintType.Error, "Invalid ShootAE");
#endif
                    Client.Random.Drop(numShots);
                }
            }
            else
            {
                if (time > ShotTime + ShotDuration)
                {
                    float arcGap = (desc.ArcGap * MathUtils.ToRadians);
                    float totalArc = arcGap * (numShots - 1);
                    float angle = attackAngle - (totalArc / 2f);
                    for (int i = 0; i < numShots; i++)
                    {
                        int damage = (int)(GetNextDamageSeeded(desc.Projectile.MinDamage, desc.Projectile.MaxDamage, ItemDatas[0]) * GetAttackMultiplier());
                        Projectile projectile = new Projectile(this, desc.Projectile, startId - i, time, angle + (arcGap * i), pos, damage);
                        ShotProjectiles.Add(projectile.Id, projectile);
                    }

                    byte[] packet = GameServer.AllyShoot(Id, desc.Type, attackAngle);
                    foreach (Entity en in Parent.PlayerChunks.HitTest(Position, SightRadius))
                        if (en is Player player && player.Client.Account.AllyShots && !player.Equals(this))
                            player.Client.Send(packet);

                    FameStats.Shots += numShots;
                    float rateOfFireMod = ItemDesc.GetStat(ItemDatas[0], ItemData.RateOfFire, ItemDesc.RateOfFireMultiplier);
                    float rateOfFire = desc.RateOfFire;
                    rateOfFire *= 1 + rateOfFireMod;
                    ShotDuration = (int)((1f / GetAttackFrequency() * (1f / rateOfFire)) * (1f / RateOfFireThreshold));
                    ShotTime = time;
                }

                else
                {
#if DEBUG
                    Program.Print(PrintType.Error, "Shot too early, ignored");
#endif
                    Client.Random.Drop(numShots);
                }
            }
        }

        public void AwaitAoe(AoeAck aoe)
        {
            AwaitingAoes.Enqueue(aoe);
        }

        public bool CheckProjectiles(int time)
        {
            foreach (KeyValuePair<int, Projectile> p in ShotProjectiles.ToArray())
            {
                int elapsed = time - p.Value.Time;
                if (elapsed > p.Value.Desc.LifetimeMS)
                {
#if DEBUG
                    Program.Print(PrintType.Error, "Shot projectile removed");
#endif
                    ShotProjectiles.Remove(p.Key);
                    continue;
                }
            }
            foreach (KeyValuePair<int, ProjectileAck> p in AckedProjectiles.ToArray()) 
            {
                int elapsed = time - p.Value.Time;
                if (elapsed > p.Value.Projectile.Desc.LifetimeMS)
                {
#if DEBUG
                    Program.Print(PrintType.Error, "Proj lifetime expired");
#endif
                    AckedProjectiles.Remove(p.Key);
                    continue;
                }

                Position pos = p.Value.Projectile.PositionAt(elapsed);
                float dx = Math.Abs(Position.X - pos.X);
                float dy = Math.Abs(Position.Y - pos.Y);
                if (dx <= 0.4f && dy <= 0.4f)
                {
                    if (p.Value.Projectile.CanHit(this))
                    {
                        if (HitByProjectile(p.Value.Projectile))
                        {
#if DEBUG
                            Program.Print(PrintType.Error, "Died cause of server collision");
#endif
                            return true;
                        }
                        AckedProjectiles.Remove(p.Key);
#if DEBUG
                        Program.Print(PrintType.Error, "Collided on server");
#endif
                    }
#if DEBUG
                    else
                    {
                        Program.Print(PrintType.Error, "In range but can't hit...?");
                    }
#endif
                }
            }
            return false;
        }

        public void TryHit(int bulletId)
        {
            if (AckedProjectiles.TryGetValue(bulletId, out ProjectileAck v))
            {
                if (v.Projectile.CanHit(this))
                {
                    HitByProjectile(v.Projectile);
                    AckedProjectiles.Remove(bulletId);
                }
            }
#if DEBUG
            else
            {
                Program.Print(PrintType.Error, "Tried to hit with undefined projectile");
            }
#endif
        }

        public override bool HitByProjectile(Projectile projectile)
        {
            return Damage(Resources.Type2Object[projectile.Desc.ContainerType].DisplayId,
                   projectile.Damage, 
                   projectile.Desc.Effects, 
                   projectile.Desc.ArmorPiercing);
        }

        public void AwaitProjectiles(List<Projectile> projectiles)
        {
            AwaitingProjectiles.Enqueue(projectiles);
        }

        public void TryHitSquare(int time, int bulletId)
        {
            if (!ValidTime(time))
            {
#if DEBUG
                Program.Print(PrintType.Error, "HitSquare invalid time");
#endif
                Client.Disconnect();
                return;
            }

            if (AckedProjectiles.TryGetValue(bulletId, out ProjectileAck ac))
            {
                Position pos = ac.Projectile.PositionAt(time - ac.Time);
                Tile tile = Parent.GetTileF(pos.X, pos.Y);

                if ((tile == null || tile.Type == 255 || TileUpdates[(int)pos.X, (int)pos.Y] != Parent.Tiles[(int)pos.X, (int)pos.Y].UpdateCount) ||
                    (tile.StaticObject != null && (tile.StaticObject.Desc.EnemyOccupySquare || !ac.Projectile.Desc.PassesCover && tile.StaticObject.Desc.OccupySquare)))
                    AckedProjectiles.Remove(bulletId);
#if DEBUG
                else
                {
                    Program.Print(PrintType.Error, "Manipualted SquareHit?");
                }
#endif
            }
#if DEBUG
            else
            {
                Program.Print(PrintType.Error, "Tried to hit square with undefined projectile");
            }
#endif
        }

        public void TryAckAoe(int time, Position pos)
        {
            if (!ValidTime(time))
            {
#if DEBUG
                Program.Print(PrintType.Error, "AoeAck invalid time");
#endif
                Client.Disconnect();
                return;
            }

            if (AwaitingAoes.TryDequeue(out AoeAck aoe))
            {
                if (!ValidMove(time, pos) && AwaitingGoto.Count == 0)
                {
#if DEBUG
                    Program.Print(PrintType.Error, "INVALID MOVE FOR AOEACK!");
#endif
                    Client.Disconnect();
                    return;
                }

                if (pos.Distance(aoe.Position) < aoe.Radius && !HasConditionEffect(ConditionEffectIndex.Invincible))
                {
                    Damage(aoe.Hitter, aoe.Damage, aoe.Effects, false);
                }
            }
            else
            {
#if DEBUG
                Program.Print(PrintType.Error, "AoeAck desync");
#endif
                Client.Disconnect();
            }
        }

        public void TryShootAck(int time)
        {
            if (!ValidTime(time))
            {
#if DEBUG
                Program.Print(PrintType.Error, "ShootAck invalid time");
#endif
                Client.Disconnect();
                return;
            }

            if (AwaitingProjectiles.TryDequeue(out List<Projectile> projectiles))
            {
                foreach (Projectile p in projectiles)
                {
                    if (p.Owner.Equals(this))
                    {
                        p.Time = time;
                        ShotProjectiles[p.Id] = p;
                    }
                    else
                    {
#if DEBUG
                        if (AckedProjectiles.ContainsKey(p.Id))
                        {
                            Program.Print(PrintType.Warn, "Duplicate ack key");
                        }
#endif
                        ProjectileAck ack = new ProjectileAck { Projectile = p, Time = time };
                        AckedProjectiles[p.Id] = ack;
                    }
                }
            }
            else
            {
#if DEBUG
                Program.Print(PrintType.Error, "ShootAck desync");
#endif
                Client.Disconnect();
            }
        }
    }
}
