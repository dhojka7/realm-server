using RotMG.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.Entities
{
    public class ConnectionBuilder
    {
        public static int[] Dot = BuildConnections(0x02020202);
        public static int[] UShortLine = BuildConnections(0x01020202);
        public static int[] L = BuildConnections(0x01010202);
        public static int[] Line = BuildConnections(0x01020102);
        public static int[] T = BuildConnections(0x01010201);
        public static int[] Cross = BuildConnections(0x01010101);

        public static int[] BuildConnections(uint bits)
        {
            int[] connections = new int[4];
            for (int k = 0; k < 4; k++)
            {
                connections[k] = (int)bits;
                bits = (bits >> 8) | (bits << 24);
            }
            return connections;
        }
    }

    public class ConnectedObject : StaticObject
    {
        public ConnectedObject(ushort type) : base(type)
        {

        }

        public override ObjectDefinition GetObjectDefinition()
        {
            TrySetSV(StatType.Connect, FindConnection());
            return base.GetObjectDefinition();
        }

        public int FindConnection()
        {
            int mx = (int)Position.X;
            int my = (int)Position.Y;
            bool[,] nearby = new bool[3, 3];

            for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                    nearby[x + 1, y + 1] = (Parent.GetTile(mx + x, my + y)?.StaticObject?.Type ?? -1) == Type;

            if (nearby[1, 0] && nearby[1, 2] && nearby[0, 1] && nearby[2, 1])
                return ConnectionBuilder.Cross[0];

            if (nearby[0, 1] && nearby[1, 1] && nearby[2, 1] && nearby[1, 0])
                return ConnectionBuilder.T[0];

            if (nearby[1, 0] && nearby[1, 1] && nearby[1, 2] && nearby[2, 1])
                return ConnectionBuilder.T[1];

            if (nearby[0, 1] && nearby[1, 1] && nearby[2, 1] && nearby[1, 2])
                return ConnectionBuilder.T[2];

            if (nearby[1, 0] && nearby[1, 1] && nearby[1, 2] && nearby[0, 1])
                return ConnectionBuilder.T[3];

            if (nearby[1, 0] && nearby[1, 1] && nearby[1, 2])
                return ConnectionBuilder.Line[0];

            if (nearby[0, 1] && nearby[1, 1] && nearby[2, 1])
                return ConnectionBuilder.Line[1];

            if (nearby[1, 0] && nearby[2, 1])
                return ConnectionBuilder.L[0];

            if (nearby[2, 1] && nearby[1, 2])
                return ConnectionBuilder.L[1];

            if (nearby[1, 2] && nearby[0, 1])
                return ConnectionBuilder.L[2];

            if (nearby[0, 1] && nearby[1, 0])
                return ConnectionBuilder.L[3];

            if (nearby[1, 0])
                return ConnectionBuilder.UShortLine[0];

            if (nearby[2, 1])
                return ConnectionBuilder.UShortLine[1];

            if (nearby[1, 2])
                return ConnectionBuilder.UShortLine[2];

            if (nearby[0, 1])
                return ConnectionBuilder.UShortLine[3];

            return ConnectionBuilder.Dot[0];
        }
    }
}
