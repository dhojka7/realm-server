using RotMG.Common;
using RotMG.Game;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RotMG.Networking
{
    public enum SocketEventState
    {
        Awaiting, //Can start sending/receiving
        InProgress, //Currently sending/receiving
    }

    public class ReceiveState
    {
        public int PacketLength;
        public readonly byte[] PacketBytes;
        public SocketEventState State;

        public ReceiveState()
        {
            PacketBytes = new byte[GameServer.BufferSize];
            PacketLength = GameServer.PrefixLength;
        }

        public byte[] GetPacketBody()
        {
            byte[] packetBody = new byte[PacketLength - GameServer.PrefixLength];
            Array.Copy(PacketBytes, GameServer.PrefixLength, packetBody, 0, packetBody.Length);
            return packetBody;
        }

        public int GetPacketId()
        {
            return PacketBytes[4];
        }

        public void Reset()
        {
            State = SocketEventState.Awaiting;
            PacketLength = 0;
        }
    }

    public class SendState
    {
        public int BytesWritten;
        public int PacketLength;
        public byte[] PacketBytes;
        public SocketEventState State;

        public readonly byte[] Data;

        public SendState()
        {
            Data = new byte[0x50000];
        }

        public void Reset()
        {
            State = SocketEventState.Awaiting;
            PacketLength = 0;
            BytesWritten = 0;
            PacketBytes = null;
        }
    }

    public static partial class GameServer
    {
        public const int BufferSize = 0x10000;
        public const int PrefixLength = 5;
        public const int PrefixLengthWithId = PrefixLength - 1;
        public const int AddBackMinDelay = 10000;
        public const byte MaxClientsPerIp = 4;

        private static bool _terminating;
        private static Socket _listener;
        private static ConcurrentQueue<Client> _clients;
        private static ConcurrentQueue<Client> _addBack;
        private static Dictionary<string, int> _connected;

        public static void Init()
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, Settings.Ports[1]);
            _listener = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(endpoint);

            _connected = new Dictionary<string, int>();
            _addBack = new ConcurrentQueue<Client>();
            _clients = new ConcurrentQueue<Client>();
            for (int i = 0; i < Settings.MaxClients; i++)
                _clients.Enqueue(new Client(new SendState(), new ReceiveState()));
        }

        public static void Stop()
        {
            _terminating = true;
            Thread.Sleep(200);
        }

        public static void Start()
        {
            _listener.Listen((int)(Settings.MaxClients * 1.2f));
            Program.Print(PrintType.Info, $"Started GameServer listening at <{_listener.LocalEndPoint}>");

            while (!_terminating)
            {
                try
                {
                    //Wait for a client to connect and validate the connection.
                    Socket skt = _listener.Accept();

                    List<Client> queueBack = new List<Client>();
                    while (_addBack.TryDequeue(out Client add))
                    {
                        if (add.IP != null)
                        {
                            _connected[add.IP]--;
                            if (_connected[add.IP] == 0)
                                _connected.Remove(add.IP);
                            add.IP = null;
                        }

                        if (!(Manager.TotalTimeUnsynced - add.DCTime > AddBackMinDelay))
                            queueBack.Add(add);
                        else
                            _clients.Enqueue(add);
                    }

                    foreach (Client q in queueBack)
                        _addBack.Enqueue(q);

#if DEBUG
                    if (skt == null || !skt.Connected)
                    {
                        Program.Print(PrintType.Warn, "<Socket connection aborted>");
                        continue;
                    }
#endif

#if DEBUG
                    Program.Print(PrintType.Debug, $"Client connected from <{skt.RemoteEndPoint}>");
#endif

                    Client client;
                    if (!_clients.TryDequeue(out client))
                    {
#if DEBUG
                        Program.Print(PrintType.Warn, $"No pooled client available, aborted connection from <{skt.RemoteEndPoint}>");
#endif
                        skt.Disconnect(false);
                        continue;
                    }

                    string ip = skt.RemoteEndPoint.ToString().Split(':')[0];
                    if (!_connected.ContainsKey(ip))
                        _connected[ip] = 1;
                    else
                    {
                        if (_connected[ip] == MaxClientsPerIp)
                        {
#if DEBUG
                            Program.Print(PrintType.Warn, $"Too many clients connected, disconnecting <{skt.RemoteEndPoint}>");
#endif
                            skt.Disconnect(false);
                            continue;
                        }
                        _connected[ip]++;
                    }

                    Program.PushWork(() =>
                    {
                        client.BeginHandling(skt, ip);
                    });

                    Thread.Sleep(10);
                }
#if DEBUG
                catch (Exception ex)
                {
                    Program.Print(PrintType.Error, ex.ToString());
                }
#endif
#if RELEASE
                catch
                {

                }
#endif
            }
        }

        public static void AddBack(Client client)
        {
            client.DCTime = Manager.TotalTimeUnsynced;
            _addBack.Enqueue(client);
        }
    }
}
