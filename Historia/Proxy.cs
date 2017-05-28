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
        private byte[] storageBuffer = new byte[0];

        private class State
        {
            public byte[] buffer = new byte[1032192];
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
                byte[] storage = new byte[0];
                while (true)
                {
                    var read = state.source.Receive(state.buffer, 0, state.buffer.Length, SocketFlags.None);
                    var copy = new byte[storage.Length + read];
                    Buffer.BlockCopy(storage, 0, copy, 0, storage.Length);
                    Buffer.BlockCopy(state.buffer, 0, copy, storage.Length, read);
                    storage = copy;

                    // Special case for zone transfer
                    if (state.direction == State.Direction.ServerToClient)
                    {
                        var injected = ProcessZone(state.buffer, read);
                        if (injected != null)
                            state.buffer = injected;
                    }

                    state.destination.Send(state.buffer, 0, read, SocketFlags.None);
                    Array.Clear(state.buffer, 0, state.buffer.Length);

                    var hasPacket = true;
                    while (hasPacket)
                    {
                        // Verify that enough bytes exist for BitConverter.
                        if (storage.Length <= sizeof(short))
                        {
                            hasPacket = false;
                            continue;
                        }

                        int packetLength = 0;
                        if (state.direction == State.Direction.ServerToClient)
                        {
                            var header = BitConverter.ToInt16(storage, 0);
                            var opcode = Op.opcodes.Where(x => x.header == header).FirstOrDefault();
                            if (opcode == null)
                            {
                                //check for compressed
                                if ((header & 0xF000) == 0x8000)
                                {
                                    int zipLength = (int)storage[0];
                                    var compressed = new byte[zipLength];
                                    Buffer.BlockCopy(storage, 2, compressed, 0, zipLength);
                                    compressed = ByteHelper.Decompress(compressed);

                                    storage = storage.Skip(zipLength + 3).ToArray();
                                    ByteHelper.Prepend(compressed, storage);
                                    continue;
                                }
                                else
                                {
                                    LogUnknownPacket(storage, "S => C");
                                    storage = new byte[0];
                                    continue;
                                }
                            }

                            // The packet length starts at the 6th byte if the opcode doesn't have a fixed size.
                            packetLength = (opcode.size != 0 ? opcode.size : BitConverter.ToInt16(storage, 6));
                        }
                        else
                        {
                            packetLength = BitConverter.ToInt16(storage, 0) + sizeof(short);
                        }

                        if (packetLength <= storage.Length)
                        {
                            var packet = ByteHelper.Copy(storage, packetLength);
                            storage = storage.Skip(packetLength).ToArray();
                            ProcessAsync(packet, state.direction);
                        }
                        else
                        {
                            hasPacket = false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                state.source.Close();
                state.destination.Close();
                Console.WriteLine(e.Message, e.Source, e.StackTrace);
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
                    //while (buffer.Length > 0)
                    //{
                    var packetLength = BitConverter.ToInt16(buffer, 0);
                    crypto.Decrypt(buffer, sizeof(short), packetLength);

                    var header = BitConverter.ToInt16(buffer, 2);
                    var opcode = Op.opcodes.Where(x => x.header == header).FirstOrDefault();
                    if (opcode == null)
                    {
                        LogUnknownPacket(buffer, directionAsStr);
                        return;
                    }

                    var chunk = buffer.Skip(2).ToArray();
                    //buffer = buffer.Skip(packetLength + 2).ToArray();
                    lock (ConsoleWriterLock)
                    {
                        Program.writer.Append(opcode, chunk, directionAsStr, packetLength);
                    }
                    //}
                }
                else
                {
                    //while (buffer.Length > 0)
                    // {
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
                    }
                    else
                    {
                        chunkLength = opcode.size;
                    }

                    var chunk = buffer.Take(chunkLength).ToArray();
                    //buffer = buffer.Skip(chunkLength).ToArray();
                    lock (ConsoleWriterLock)
                    {
                        Program.writer.Append(opcode, chunk, directionAsStr, chunkLength);
                    }
                    // }
                }
            }
            catch (Exception e)
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
            }
            catch (Exception e)
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
            }
            catch (Exception e)
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
