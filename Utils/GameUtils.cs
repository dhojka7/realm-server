using RotMG.Common;
using RotMG.Game;
using RotMG.Game.Entities;
using RotMG.Networking;
using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Utils
{
    public static class GameUtils
    {
        public static int GetDefenseDamage(this Entity entity, int damage, int defense, bool pierces)
        {
#if DEBUG
            if (entity == null || entity.Parent == null)
                throw new Exception("Undefined entity");
#endif
            if (pierces)
                defense = 0;

            if (entity.HasConditionEffect(ConditionEffectIndex.Armored))
                defense *= 2;

            int min = damage * 3 / 20;
            int d = Math.Max(min, damage - defense);
            return d;
        }

        public static Entity GetNearestEntity(this Entity entity, float radius)
        {
#if DEBUG
            if (entity == null || entity.Parent == null || radius <= 0)
                throw new Exception();
#endif

            Entity nearest = null;
            float dist = float.MaxValue;
            foreach (Entity en in entity.Parent.EntityChunks.HitTest(entity.Position, radius))
            {
                float d;
                if ((d = entity.Position.Distance(en.Position)) < dist)
                {
                    nearest = en;
                    dist = d;
                }
            }
            return nearest;
        }

        public static Entity GetNearestEntity(this Entity entity, float radius, float angle, float cone)
        {
#if DEBUG
            if (entity == null || entity.Parent == null || radius <= 0)
                throw new Exception();
#endif

            Entity nearest = null;
            float dist = float.MaxValue;
            foreach (Entity en in entity.Parent.EntityChunks.HitTest(entity.Position, radius))
            {
                if (Math.Abs(angle - MathF.Atan2(en.Position.Y - entity.Position.Y, en.Position.X - entity.Position.X)) > cone)
                    continue;

                float d;
                if ((d = entity.Position.Distance(en.Position)) < dist)
                {
                    nearest = en;
                    dist = d;
                }
            }
            return nearest;
        }

        public static Entity GetNearestEnemy(this Entity entity, float radius)
        {
#if DEBUG
            if (entity == null || entity.Parent == null || radius <= 0)
                throw new Exception();
#endif

            Entity nearest = null;
            float dist = float.MaxValue;
            foreach (Entity en in entity.Parent.EntityChunks.HitTest(entity.Position, radius))
            {
                if (!(en is Enemy))
                    continue;

                if (en.HasConditionEffect(ConditionEffectIndex.Invincible) ||
                    en.HasConditionEffect(ConditionEffectIndex.Stasis))
                    continue;

                float d;
                if ((d = entity.Position.Distance(en.Position)) < dist)
                {
                    nearest = en;
                    dist = d;
                }
            }
            return nearest;
        }

        public static Entity GetNearestEnemy(this Entity entity, float radius, float angle, float cone, Position target)
        {
#if DEBUG
            if (entity == null || entity.Parent == null || radius <= 0)
                throw new Exception();
#endif

            Entity nearest = null;
            float dist = float.MaxValue;
            foreach (Entity en in entity.Parent.EntityChunks.HitTest(entity.Position, radius))
            {
                if (!(en is Enemy))
                    continue;

                if (en.HasConditionEffect(ConditionEffectIndex.Invincible) ||
                    en.HasConditionEffect(ConditionEffectIndex.Stasis))
                    continue;

                if (Math.Abs(angle - MathF.Atan2(en.Position.Y - entity.Position.Y, en.Position.X - entity.Position.X)) > cone)
                    continue;

                float d;
                if ((d = target.Distance(en.Position)) < dist)
                {
                    nearest = en;
                    dist = d;
                }
            }
            return nearest;
        }

        public static Entity GetNearestEnemy(this Entity entity, float radius, float angle, float cone, Position target, HashSet<Entity> exclude)
        {
#if DEBUG
            if (entity == null || entity.Parent == null || radius <= 0)
                throw new Exception();
#endif

            Entity nearest = null;
            float dist = float.MaxValue;
            foreach (Entity en in entity.Parent.EntityChunks.HitTest(entity.Position, radius))
            {
                if (!(en is Enemy))
                    continue;

                if (en.HasConditionEffect(ConditionEffectIndex.Invincible) ||
                    en.HasConditionEffect(ConditionEffectIndex.Stasis))
                    continue;

                if (Math.Abs(angle - MathF.Atan2(en.Position.Y - entity.Position.Y, en.Position.X - entity.Position.X)) > cone)
                    continue;

                if (exclude.Contains(en))
                    continue;

                float d;
                if ((d = target.Distance(en.Position)) < dist)
                {
                    nearest = en;
                    dist = d;
                }
            }
            return nearest;
        }

        public static Entity GetNearestEnemy(this Entity entity, float radius, HashSet<Entity> exclude)
        {
#if DEBUG
            if (entity == null || entity.Parent == null || radius <= 0)
                throw new Exception();
#endif

            Entity nearest = null;
            float dist = float.MaxValue;
            foreach (Entity en in entity.Parent.EntityChunks.HitTest(entity.Position, radius))
            {
                if (!(en is Enemy))
                    continue;

                if (en.HasConditionEffect(ConditionEffectIndex.Invincible) ||
                    en.HasConditionEffect(ConditionEffectIndex.Stasis))
                    continue;

                if (exclude.Contains(en))
                    continue;

                float d;
                if ((d = entity.Position.Distance(en.Position)) < dist)
                {
                    nearest = en;
                    dist = d;
                }
            }
            return nearest;
        }

        public static Entity GetNearestPlayer(this Entity entity, float radius)
        {
#if DEBUG
            if (entity == null || entity.Parent == null || radius <= 0)
                throw new Exception();
#endif
            Entity nearest = null;
            float dist = float.MaxValue;
            foreach (Entity en in entity.Parent.PlayerChunks.HitTest(entity.Position, radius))
            {
                if (en.HasConditionEffect(ConditionEffectIndex.Invisible))
                    continue;

                float d;
                if ((d = entity.Position.Distance(en.Position)) < dist)
                {
                    nearest = en;
                    dist = d;
                }
            }
            return nearest;
        }
    }
}
