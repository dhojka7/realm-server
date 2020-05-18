using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RotMG.Game.Logic.Behaviors
{
    public class Grenade : Behavior
    {
        public readonly float Range;
        public readonly float Radius;
        public readonly int Damage;
        public readonly float? FixedAngle;
        public readonly int Cooldown;
        public readonly int CooldownOffset;
        public readonly int CooldownVariance;
        public readonly ConditionEffectDesc[] Effects;
        public readonly uint Color;

        public Grenade(
            float range = 8, 
            int damage = 100, 
            float radius = 5, 
            float? fixedAngle = null, 
            int cooldown = 0,
            int cooldownOffset = 0,
            int cooldownVariance = 0,
            ConditionEffectIndex effect = ConditionEffectIndex.Nothing, 
            int effectDuration = 0, 
            uint color = 0xFFFF0000)
        {
            Range = range;
            Damage = damage;
            Radius = radius;
            FixedAngle = fixedAngle * MathUtils.ToRadians;
            Cooldown = cooldown;
            CooldownOffset = cooldownOffset;
            CooldownVariance = cooldownVariance;
            Effects = new ConditionEffectDesc[1]
            {
                new ConditionEffectDesc(effect, effectDuration)
            };
            Color = color;
        }

        public override void Enter(Entity host)
        {
            host.StateCooldown[Id] = CooldownOffset;
        }

        public override bool Tick(Entity host)
        {
            host.StateCooldown[Id] -= Settings.MillisecondsPerTick;
            if (host.StateCooldown[Id] <= 0)
            {
                if (host.HasConditionEffect(ConditionEffectIndex.Stunned))
                    return false;

                Entity target = host.GetNearestPlayer(Range);
                if (target != null || FixedAngle != null)
                {
                    Position p;
                    if (FixedAngle != null)
                        p = new Position(
                            Range * MathF.Cos(FixedAngle.Value) + host.Position.X,
                            Range * MathF.Sin(FixedAngle.Value) + host.Position.Y);
                    else
                        p = new Position(
                            target.Position.X,
                            target.Position.Y
                            );


                    AoeAck ack = new AoeAck
                    {
                        Damage = Damage,
                        Radius = Radius,
                        Effects = Effects,
                        Position = p,
                        Hitter = host.Desc.DisplayId,
                        Time = Manager.TotalTime + 1500
                    };

                    byte[] eff = GameServer.ShowEffect(ShowEffectIndex.Throw, host.Id, Color, p);
                    byte[] aoe = GameServer.Aoe(p, Radius, Damage, Effects[0].Effect, Color);
                    Entity[] players = host.Parent.PlayerChunks.HitTest(host.Position, Player.SightRadius)
                        .Where(e => (e is Player j) && j.Entities.Contains(host)).ToArray();

                    foreach (Entity en in players)
                        (en as Player).Client.Send(eff);

                    Manager.AddTimedAction(1500, () => 
                    {
                        foreach (Entity en in players)
                            if (en.Parent != null)
                            {
                                (en as Player).AwaitAoe(ack);
                                (en as Player).Client.Send(aoe);
                            }
                    });
                }

                host.StateCooldown[Id] = Cooldown;
                if (CooldownVariance != 0)
                    host.StateCooldown[Id] += MathUtils.NextIntSnap(-CooldownVariance, CooldownVariance, Settings.MillisecondsPerTick);
                return true;
            }
            return false;
        }
    }
}
