using RotMG.Common;
using RotMG.Game;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace RotMG.Utils
{
    public static class MathUtils
    {
        public const float ToRadians = MathF.PI / 180f;
        public const float ToDegrees = 180f / MathF.PI;

        private static Random _rnd = new Random();
        private static RandomNumberGenerator _gen = RNGCryptoServiceProvider.Create();
        public static string GenerateSalt()
        {
            var x = new byte[0x10];
            _gen.GetNonZeroBytes(x);
            return Convert.ToBase64String(x);
        }

        public static float BoundToPI(float x)
        {
            int v;
            if (x < -MathF.PI)
            {
                v = ((int)(x / -MathF.PI) + 1) / 2;
                x = x + v * 2 * MathF.PI;
            }
            else if (x > MathF.PI)
            {
                v = ((int)(x / MathF.PI) + 1) / 2;
                x = x - v * 2 * MathF.PI;
            }
            return x;
        }

        public static int Next(int length)
        {
            return _rnd.Next(length);
        }

        public static int NextInt(int min = 0, int max = 1)
        {
            return (int)NextFloat(min, max);
        }

        public static int NextIntSnap(int min = 0, int max = 1, int snap = 100)
        {
            int r = (int)NextFloat(min, max);
            r -= r % snap;
            return r;
        }

        public static float NextFloat(float min = 0, float max = 1)
        {
            return (float)(_rnd.NextDouble() * (max - min) + min);
        }

        public static bool NextBool()
        {
            return _rnd.Next(2) == 0;
        }

        public static float NextAngle()
        {
            return NextFloat(-MathF.PI, MathF.PI);
        }

        public static bool Chance(float chance)
        {
            return _rnd.NextDouble() <= chance;
        }

        public static Position Position(float x, float y)
        {
            return new Position(NextFloat(-x, x), NextFloat(-y, y));
        }

        public static int PlusMinus()
        {
            return _rnd.Next(2) == 0 ? -1 : 1;
        }

        public static float GetSpeed(this Entity entity, float spd)
        {
#if DEBUG
            if (entity == null || entity.Parent == null)
                throw new Exception("Undefined entity");
#endif
            return entity.HasConditionEffect(ConditionEffectIndex.Slowed) ? (5.55f * spd + 0.74f) / 2 : 5.55f * spd + 0.74f;
        }

        public static float Angle(this Position from, Position to)
        {
            return MathF.Atan2(to.Y - from.Y, to.X - from.X);
        }

        public static float Distance(this Position from, Position to)
        {
            float v1 = from.X - to.X, v2 = from.Y - to.Y;
            return (float)Math.Sqrt((v1 * v1) + (v2 * v2));
        }

        public static float DistanceSquared(this Position from, Position to)
        {
            float v1 = from.X - to.X, v2 = from.Y - to.Y;
            return (v1 * v1) + (v2 * v2);
        }

        public static float Distance(this Position from, Entity to)
        {
#if DEBUG
            if (to == null)
                throw new Exception("Undefined entity");
#endif
            float v1 = from.X - to.Position.X, v2 = from.Y - to.Position.Y;
            return (float)Math.Sqrt((v1 * v1) + (v2 * v2));
        }

        public static float DistanceSquared(this Position from, Entity to)
        {
#if DEBUG
            if (to == null)
                throw new Exception("Undefined entity");
#endif
            float v1 = from.X - to.Position.X, v2 = from.Y - to.Position.Y;
            return (v1 * v1) + (v2 * v2);
        }

        public static float Lerp(float value1, float value2, float amount)
        {
            return value1 + (value2 - value1) * amount;
        }
    }
}
