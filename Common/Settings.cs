using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace RotMG.Common
{
    public static class Settings
    {
        public static int MaxClients;
        public static string Address;
        public static int[] Ports;
        public static string ResourceDirectory;
        public static string DatabaseDirectory;
        public static int TicksPerSecond;
        public static int MillisecondsPerTick;
        public static float SecondsPerTick;

        public static void Init()
        {
            if (File.Exists("Settings.xml"))
            {
                XElement data = XElement.Parse(File.ReadAllText("Settings.xml"));
                MaxClients = data.ParseInt("MaxClients", 256);
                Address = data.ParseString("Address", "127.0.0.1");
                Ports = data.ParseIntArray("Ports", ":");
                ResourceDirectory = data.ParseString("@res", "Common/Resources");
                DatabaseDirectory = data.ParseString("@db", "Database");
                TicksPerSecond = data.ParseInt("TicksPerSecond", 5);
                MillisecondsPerTick = 1000 / TicksPerSecond;
                SecondsPerTick = 1f / TicksPerSecond;
            }
        }
    }
}
