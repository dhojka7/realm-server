using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace RotMG.Networking
{
    public class PacketWriter : BinaryWriter
    {
        public PacketWriter(Stream input) : base(input, Encoding.UTF8) { }

        public override void Write(short value)
        {
            base.Write(IPAddress.NetworkToHostOrder(value));
        }

        public override void Write(ushort value)
        {
            base.Write((ushort)IPAddress.HostToNetworkOrder((short)value));
        }

        public override void Write(int value)
        {
            base.Write(IPAddress.NetworkToHostOrder(value));
        }

        public override void Write(uint value)
        {
            base.Write((uint)IPAddress.NetworkToHostOrder((int)value));
        }

        public override void Write(float value)
        {
            byte[] b = BitConverter.GetBytes(value);
            Array.Reverse(b);
            base.Write(b);
        }

        public override void Write(string value)
        {
            byte[] data = Encoding.UTF8.GetBytes(value);
            Write((short)data.Length);
            base.Write(data);
        }

        public void WriteUTF32(string value)
        {
            Write(value.Length);
            Write(Encoding.UTF8.GetBytes(value));
        }

        public void WriteNullTerminatedString(string str)
        {
            Write(Encoding.UTF8.GetBytes(str));
            Write((byte)0);
        }

        public static void BlockCopyInt32(byte[] data, int int32)
        {
            byte[] lengthBytes = BitConverter.GetBytes(IPAddress.NetworkToHostOrder(int32));
            data[0] = lengthBytes[0];
            data[1] = lengthBytes[1];
            data[2] = lengthBytes[2];
            data[3] = lengthBytes[3];
        }
    }
}
