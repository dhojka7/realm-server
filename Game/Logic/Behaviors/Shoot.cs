using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.Logic.Behaviors
{
    public class Shoot : Behavior
    {
        public const int PredictNumTicks = 4;

        public readonly float Range;
        public readonly byte Count;
        public readonly float ShootAngle;
        public readonly float? FixedAngle;
        public readonly float? RotateAngle;
        public readonly float AngleOffset;
        public readonly float? DefaultAngle;
        public readonly float Predictive;
        public readonly int Index;
        public readonly int CooldownOffset;
        public readonly int CooldownVariance;
        public readonly int Cooldown;

        public Shoot(
            float range = 5, 
            byte count = 1, 
            float? shootAngle = null, 
            int index = 0, 
            float? fixedAngle = null, 
            float? rotateAngke = null, 
            float angleOffset = 0, 
            float? defaultAngle = null,
            float predictive = 0,
            int cooldownOffset = 0,
            int cooldownVariance = 0,
            int cooldown = 0)
        {
            Range = range;
            Count = count;
            ShootAngle = count == 1 ? 0 : (shootAngle ?? 360f / count) * MathUtils.ToRadians;
            Index = index;
            FixedAngle = fixedAngle * MathUtils.ToRadians;
            RotateAngle = rotateAngke * MathUtils.ToRadians;
            AngleOffset = angleOffset * MathUtils.ToRadians;
            DefaultAngle = defaultAngle * MathUtils.ToRadians;
            Predictive = predictive;
            CooldownOffset = cooldownOffset;
            CooldownVariance = cooldownVariance;
            Cooldown = cooldown;
        }

        public override void Enter(Entity host)
        {
            host.StateCooldown.Add(Id, CooldownOffset);
            if (RotateAngle != null) 
                host.StateObject.Add(Id, (int)0);
        }

        public override bool Tick(Entity host)
        {
            host.StateCooldown[Id] -= Settings.MillisecondsPerTick;
            if (host.StateCooldown[Id] <= 0)
            {
                if (host.HasConditionEffect(ConditionEffectIndex.Stunned))
                    return false;

                byte count = Count;
                if (host.HasConditionEffect(ConditionEffectIndex.Dazed))
                    count = (byte)Math.Ceiling(count / 2f);

                Entity target = host.GetNearestPlayer(Range);
                if (target != null || DefaultAngle != null || FixedAngle != null)
                {
                    ProjectileDesc desc = host.Desc.Projectiles[Index];
                    float angle = 0;

                    if (FixedAngle != null)
                    {
                        angle = (float)FixedAngle;
                    }
                    else if (target != null)
                    {
                        if (Predictive != 0 && Predictive > MathUtils.NextFloat())
                        {
                            Position history = target.TryGetHistory(1);
                            float targetX = target.Position.X + PredictNumTicks * (target.Position.X - history.X);
                            float targetY = target.Position.Y + PredictNumTicks * (target.Position.Y - history.Y);
                            angle = (float)Math.Atan2(targetY - host.Position.Y, targetY - host.Position.Y);
                        }
                        else
                            angle = (float)Math.Atan2(target.Position.Y - host.Position.Y, target.Position.X - host.Position.X);
                    }
                    else if (DefaultAngle != null)
                        angle = (float)DefaultAngle;

                    angle += AngleOffset;

                    if (RotateAngle != null)
                    {
                        int rotateCount = (int)host.StateObject[Id];
                        angle += (float)RotateAngle * rotateCount;
                        rotateCount++;
                        host.StateObject[Id] = rotateCount;
                    }

                    int damage = desc.Damage;
                    float startAngle = angle - ShootAngle * (count - 1) / 2;
                    int startId = host.Parent.NextProjectileId;
                    host.Parent.NextProjectileId += count;

                    List<Projectile> projectiles = new List<Projectile>();
                    for (byte k = 0; k < count; k++)
                        projectiles.Add(new Projectile(host, desc, startId + k, Manager.TotalTime, startAngle + (ShootAngle * k), host.Position, damage));

                    byte[] packet = GameServer.EnemyShoot(startId, host.Id, desc.BulletType, host.Position, startAngle, (short)damage, count, ShootAngle);
                    
                    foreach (Entity en in host.Parent.PlayerChunks.HitTest(host.Position, Player.SightRadius))
                    {
                        if (en is Player player)
                        {
                            if (player.Entities.Contains(host))
                            {
                                player.AwaitProjectiles(projectiles);
                                player.Client.Send(packet);
                            }
                        }
                    }
                }

                host.StateCooldown[Id] = Cooldown;
                if (CooldownVariance != 0)
                    host.StateCooldown[Id] += MathUtils.NextIntSnap(-CooldownVariance, CooldownVariance, Settings.MillisecondsPerTick);
                return true;
            }
            return false;
        }

        public override void Exit(Entity host)
        {
            host.StateCooldown.Remove(Id);
            if (RotateAngle != null)
                host.StateObject.Remove(Id);
        }
    }
}
