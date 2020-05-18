using RotMG.Game;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace RotMG.Common
{
    public struct FameStats
    {
        public int BaseFame;
        public int TotalFame;
        public List<FameBonus> Bonuses;
    }

    public struct FameBonus
    {
        public string Name;
        public int Fame;
    }

    public struct TileData
    {
        public ushort TileType;
        public short X;
        public short Y;

        public void Write(PacketWriter wtr)
        {
            wtr.Write(X);
            wtr.Write(Y);
            wtr.Write(TileType);
        }
    }

    public struct ObjectDrop
    {
        public int Id;
        public bool Explode;

        public void Write(PacketWriter wtr)
        {
            wtr.Write(Id);
            wtr.Write(Explode);
        }
    }

    public struct ObjectDefinition
    {
        public ushort ObjectType;
        public ObjectStatus ObjectStatus;

        public void Write(PacketWriter wtr)
        {
            wtr.Write(ObjectType);
            ObjectStatus.Write(wtr);
        }
    }

    public struct ObjectStatus
    {
        public int Id;
        public Position Position;
        public Dictionary<StatType, object> Stats;

        public void Write(PacketWriter wtr)
        {
            wtr.Write(Id);
            Position.Write(wtr);

            wtr.Write((byte)Stats.Count);
            foreach (KeyValuePair<StatType, object> k in Stats)
            {
                wtr.Write((byte)k.Key);
                if (IsStringStat(k.Key))
                    wtr.Write((string)k.Value);
                else 
                    wtr.Write((int)k.Value);
            }
        }

        public static bool IsStringStat(StatType stat)
        {
            switch (stat)
            {
                case StatType.Name:
                case StatType.GuildName:
                    return true;
            }
            return false;
        }
    }

    public struct IntPoint
    {
        public int X;
        public int Y;

        public IntPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static bool operator ==(IntPoint a, IntPoint b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(IntPoint a, IntPoint b) => a.X != b.X || a.Y != b.Y;

        public bool Equals(IntPoint other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj)
        {
            if (obj is IntPoint p)
            {
                return Equals(p);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (Y << 16) ^ X;
        }

        public override string ToString()
        {
            return $"X:{X}, Y:{Y}";
        }
    }

    public struct SlotData
    {
        public int ObjectId;
        public byte SlotId;

        public SlotData(PacketReader rdr)
        {
            ObjectId = rdr.ReadInt32();
            SlotId = rdr.ReadByte();
        }
    }

    public struct Position
    {
        public float X;
        public float Y;

        public Position(float x, float y)
        {
            X = x;
            Y = y;
        }

        public Position(PacketReader rdr)
        {
            X = rdr.ReadSingle();
            Y = rdr.ReadSingle();
        }

        public void Write(PacketWriter wtr)
        {
            wtr.Write(X);
            wtr.Write(Y);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + X.GetHashCode();
                hash = hash * 23 + Y.GetHashCode();
                return hash;
            }
        }

        public static Position operator -(Position value)
        {
            value.X = -value.X;
            value.Y = -value.Y;
            return value;
        }


        public static bool operator ==(Position value1, Position value2)
        {
            return value1.X == value2.X && value1.Y == value2.Y;
        }


        public static bool operator !=(Position value1, Position value2)
        {
            return value1.X != value2.X || value1.Y != value2.Y;
        }


        public static Position operator +(Position value1, Position value2)
        {
            value1.X += value2.X;
            value1.Y += value2.Y;
            return value1;
        }


        public static Position operator -(Position value1, Position value2)
        {
            value1.X -= value2.X;
            value1.Y -= value2.Y;
            return value1;
        }


        public static Position operator *(Position value1, Position value2)
        {
            value1.X *= value2.X;
            value1.Y *= value2.Y;
            return value1;
        }


        public static Position operator *(Position value, float scaleFactor)
        {
            value.X *= scaleFactor;
            value.Y *= scaleFactor;
            return value;
        }


        public static Position operator *(float scaleFactor, Position value)
        {
            value.X *= scaleFactor;
            value.Y *= scaleFactor;
            return value;
        }


        public static Position operator /(Position value1, Position value2)
        {
            value1.X /= value2.X;
            value1.Y /= value2.Y;
            return value1;
        }


        public static Position operator /(Position value1, float divider)
        {
            float factor = 1 / divider;
            value1.X *= factor;
            value1.Y *= factor;
            return value1;
        }

        public static Position Lerp(Position value1, Position value2, float amount)
        {
            return new Position(
                MathUtils.Lerp(value1.X, value2.X, amount),
                MathUtils.Lerp(value1.Y, value2.Y, amount));
        }

        public static void Lerp(ref Position value1, ref Position value2, float amount, out Position result)
        {
            result = new Position(
                MathUtils.Lerp(value1.X, value2.X, amount),
                MathUtils.Lerp(value1.Y, value2.Y, amount));
        }

        public void Normalize()
        {
            float val = 1.0f / (float)Math.Sqrt((X * X) + (Y * Y));
            X *= val;
            Y *= val;
        }

        public static Position Normalize(Position value)
        {
            float val = 1.0f / (float)Math.Sqrt((value.X * value.X) + (value.Y * value.Y));
            value.X *= val;
            value.Y *= val;
            return value;
        }

        public static void Normalize(ref Position value, out Position result)
        {
            float val = 1.0f / (float)Math.Sqrt((value.X * value.X) + (value.Y * value.Y));
            result.X = value.X * val;
            result.Y = value.Y * val;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override string ToString()
        {
            return $"X:{X}, Y:{Y}";
        }
    }
}
