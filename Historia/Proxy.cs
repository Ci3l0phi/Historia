using Historia.Lib;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Historia
{
    public class Proxy
    {
        private static TOSCrypto crypto = new TOSCrypto();
        private static readonly object ConsoleWriterLock = new object();

        private static bool ZoneProxyExists = false;

        private class State
        {
            public byte[] buffer = new byte[8192];
            public Socket source;
            public Socket destination;
            public Direction direction;

            public enum Direction
            {
                ServerToClient,
                ClientToServer
            }
        }

        public void Start(IPEndPoint local, IPEndPoint remote)
        {
            try
            {
                var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                client.Bind(local);
                client.Listen(50);

                while (true)
                {
                    var intermediate = client.Accept();
                    if (!server.Connected)
                        server.Connect(remote);

                    var clientState = new State()
                    {
                        source = intermediate,
                        destination = server,
                        direction = State.Direction.ClientToServer
                    };
                    ReceiveAsync(clientState);

                    var serverState = new State()
                    {
                        source = server,
                        destination = intermediate,
                        direction = State.Direction.ServerToClient
                    };
                    ReceiveAsync(serverState);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void Receive(State state)
        {
            try
            {
                var read = state.source.Receive(state.buffer, 0, state.buffer.Length, SocketFlags.None);
                if (read > 0)
                {
                    if (state.direction == State.Direction.ServerToClient)
                    {
                        var injected = ProcessZone(state.buffer, read);
                        if (injected != null)
                            state.buffer = injected;
                    }
                    
                    state.destination.Send(state.buffer, 0, read, SocketFlags.None);
                    
                    var copy = ByteHelper.Copy(state.buffer, read);
                    ProcessAsync(copy, state.direction);

                    Array.Clear(state.buffer, 0, state.buffer.Length);
                    ReceiveAsync(state);
                }
                else
                {
                    Console.WriteLine("[Proxy] End of transmission.");
                }
            } catch (Exception e)
            {
                state.source.Close();
                state.destination.Close();
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// I'm sure this can be simplified ;)
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="direction"></param>
        private void Process(byte[] buffer, State.Direction direction)
        {
            string directionAsStr = (direction == State.Direction.ClientToServer ? "C => S" : "S => C");

            try
            {
                if (direction == State.Direction.ClientToServer)
                {
                    var packetLength = BitConverter.ToInt16(buffer, 0);

                    if ((buffer.Length -2) != packetLength)
                    {
                        LogUnknownPacket(buffer, directionAsStr);
                        return;
                    }

                    crypto.Decrypt(buffer, sizeof(short), packetLength);

                    var header = BitConverter.ToInt16(buffer, 2);
                    var opcode = Op.opcodes.Where(x => x.header == header).FirstOrDefault();
                    if (opcode == null)
                    {
                        LogUnknownPacket(buffer, directionAsStr);
                        return;
                    }

                    var chunk = buffer.Skip(2).ToArray();
                    lock (ConsoleWriterLock)
                    {
                        Program.writer.Append(opcode, chunk, directionAsStr, packetLength);
                    }
                } else
                {
                    while (buffer.Length > 0)
                    {
                        var header = BitConverter.ToInt16(buffer, 0);
                        var opcode = Op.opcodes.Where(x => x.header == header).FirstOrDefault();
                        if (opcode == null)
                        {
                            LogUnknownPacket(buffer, directionAsStr);
                            return;
                        }

                        int chunkLength;
                        if (opcode.size == 0)
                        {
                            chunkLength = BitConverter.ToInt16(buffer, 6);
                        } else
                        {
                            chunkLength = opcode.size;
                        }

                        var chunk = buffer.Take(chunkLength).ToArray();
                        buffer = buffer.Skip(chunkLength).ToArray();
                        lock (ConsoleWriterLock)
                        {
                            Program.writer.Append(opcode, chunk, directionAsStr, chunkLength);
                        }
                    }
                }
            } catch (Exception e)
            {
                Console.WriteLine(e.Message, e.StackTrace);
            }
        }

        /// <summary>
        /// Logs unknown packets.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="direction"></param>
        public void LogUnknownPacket(byte[] buffer, string direction)
        {
            lock (ConsoleWriterLock)
            {
                var opcode = Op.opcodes.Where(x => x.name == "UNKNOWN").FirstOrDefault();
                Program.writer.Append(opcode, buffer, direction, buffer.Length);
            }
            return;
        }

        /// <summary>
        /// Starts a new proxy instance for the zone.
        /// </summary>
        public byte[] ProcessZone(byte[] buffer, int read)
        {
            try
            {
                var header = BitConverter.ToInt16(buffer, 0);
                var opcode = Op.opcodes.Where(x => x.header == header).FirstOrDefault();
                if ((opcode == null) || (opcode.name != Op.StartGamePacket.name))
                    return null;
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            try
            {
                var copy = ByteHelper.Copy(buffer, read);
                var packet = new Op.StartGamePacket(copy);

                Endpoint.RemoteZone = new IPEndPoint(new IPAddress(packet.address), BitConverter.ToInt32(packet.port, 0));
                
                if (!ZoneProxyExists)
                {
                    ZoneProxyExists = true;
                    new Proxy().StartAsync(Endpoint.LocalZone, Endpoint.RemoteZone);
                }

                packet.address = Endpoint.LocalZone.Address.GetAddressBytes();
                packet.port = BitConverter.GetBytes(Endpoint.LocalZone.Port);
                return packet.Build();
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return null;
        }

        public async void StartAsync(IPEndPoint local, IPEndPoint remote)
        {
            await Task.Run(() => this.Start(local, remote));
        }

        private async void ReceiveAsync(State state)
        {
            await Task.Run(() => this.Receive(state));
        }

        private async void ProcessAsync(byte[] buffer, State.Direction direction)
        {
            await Task.Run(() => this.Process(buffer, direction));
        }
    }
}
