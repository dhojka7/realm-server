using RotMG.Common;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game
{
    public class Projectile
    {
        public readonly Entity Owner;
        public readonly ProjectileDesc Desc;
        public readonly int Id;
        public readonly float Angle;
        public readonly Position StartPosition;
        public readonly int Damage;
        public readonly HashSet<int> Hit;

        public int Time;

        public Projectile(Entity owner, ProjectileDesc desc, int id, int time, float angle, Position startPos, int damage)
        {
            Owner = owner;
            Desc = desc;
            Id = id;
            Time = time;
            Angle = MathUtils.BoundToPI(angle);
            StartPosition = startPos;
            Damage = damage;
            Hit = new HashSet<int>();
        }

        public bool CanHit(Entity en)
        {
            if (en.HasConditionEffect(ConditionEffectIndex.Invincible) || en.HasConditionEffect(ConditionEffectIndex.Stasis))
                return false;

            if (!Hit.Contains(en.Id))
            {
                Hit.Add(en.Id);
                return true;
            }
            return false;
        }

        public Position PositionAt(float elapsed)
        {
            Position p = new Position(StartPosition.X, StartPosition.Y);
            float speed = Desc.Speed;
            if (Desc.Accelerate) speed *= elapsed / Desc.LifetimeMS;
            if (Desc.Decelerate) speed *= 2 - elapsed / Desc.LifetimeMS;
            float dist = elapsed * (speed / 10000f);
            float phase = Id % 2 == 0 ? 0 : MathF.PI;
            if (Desc.Wavy)
            {
                float periodFactor = 6 * MathF.PI;
                float amplitudeFactor = MathF.PI / 64.0f;
                float theta = Angle + amplitudeFactor * MathF.Sin(phase + periodFactor * elapsed / 1000.0f);
                p.X = p.X + dist * MathF.Cos(theta);
                p.Y = p.Y + dist * MathF.Sin(theta);
            }
            else if (Desc.Parametric)
            {
                float t = elapsed / Desc.LifetimeMS * 2 * MathF.PI;
                float x = MathF.Sin(t) * (Id % 2 == 1 ? 1 : -1);
                float y = MathF.Sin(2 * t) * (Id % 4 < 2 ? 1 : -1);
                float sin = MathF.Sin(Angle);
                float cos = MathF.Cos(Angle);
                p.X = p.X + (((x * cos) - (y * sin)) * Desc.Magnitude);
                p.Y = p.Y + (((x * sin) + (y * cos)) * Desc.Magnitude);
            }
            else
            {
                if (Desc.Boomerang)
                {
                    float halfway = Desc.LifetimeMS * (Desc.Speed / 10000) / 2;
                    if (dist > halfway)
                    {
                        dist = halfway - (dist - halfway);
                    }
                }
                p.X = p.X + dist * MathF.Cos(Angle);
                p.Y = p.Y + dist * MathF.Sin(Angle);
                if (Desc.Amplitude != 0)
                {
                    float deflection = Desc.Amplitude * MathF.Sin(phase + elapsed / Desc.LifetimeMS * Desc.Frequency * 2 * MathF.PI);
                    p.X = p.X + deflection * MathF.Cos(Angle + MathF.PI / 2);
                    p.Y = p.Y + deflection * MathF.Sin(Angle + MathF.PI / 2);
                }
            }

            return p;
        }
    }
}
