using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Historia
{
    public class Proxy
    {
        private static TOSCrypto crypto = new TOSCrypto();
        private static readonly object ConsoleWriterLock = new object();
        private static HTMLWriter writer;
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

        public Proxy(HTMLWriter _writer)
        {
            writer = _writer;
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
                    state.destination.Send(state.buffer, 0, read, SocketFlags.None);

                    byte[] copy = new byte[read];
                    Buffer.BlockCopy(state.buffer, 0, copy, 0, read);
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

        private void Process(byte[] buffer, State.Direction direction)
        {

            try
            {
                if (direction == State.Direction.ClientToServer)
                {
                    var lengthBytes = buffer.Take(sizeof(short)).ToArray();
                    buffer = buffer.Skip(sizeof(short)).ToArray();
                    var packetLength = BitConverter.ToInt16(lengthBytes, 0);

                    if (buffer.Length != packetLength)
                        Console.WriteLine(string.Format("Error. The read {0} doesn't match packetlength {1}", buffer.Length, packetLength));

                    if (buffer.Length != packetLength)
                        throw new Exception(string.Format("Error. The packet length {0} does not match the buffer length {1} !", packetLength, buffer.Length));

                    crypto.Decrypt(buffer, 0, packetLength);

                    byte[] bak = new byte[packetLength + 2];
                    Buffer.BlockCopy(lengthBytes, 0, bak, 0, 2);
                    Buffer.BlockCopy(buffer, 0, bak, 2, packetLength);

                    while (buffer.Length > 0)
                    {
                        var header = BitConverter.ToInt16(buffer, 0);
                        var opcode = Op.opcodes.Where(x => x.header == header).FirstOrDefault();
                        if (opcode == null)
                            throw new Exception(string.Format("Error. The opcode with header {0} was not found!\n\tDumping buffer: {1}\npacket: {2}", header, BitConverter.ToString(buffer), BitConverter.ToString(bak)));

                        if (opcode.size == 0)
                        {
                            var dlength = BitConverter.ToInt16(buffer, 6);
                            var chunk = buffer.Take(dlength).ToArray();
                            buffer = buffer.Skip(dlength).ToArray();

                            lock (writer)
                            {
                                writer.Append(opcode, chunk, "CLIENT => SERVER");
                            }
                        }
                        else
                        {
                            var chunk = buffer.Take(packetLength).ToArray();
                            buffer = buffer.Skip(packetLength).ToArray();

                            lock (writer)
                            {
                                writer.Append(opcode, chunk, "CLIENT => SERVER");
                            }
                        }
                    }
                }
                else
                {
                    while (buffer.Length > 0)
                    {
                        var header = BitConverter.ToInt16(buffer, 0);
                        var opcode = Op.opcodes.Where(x => x.header == header).FirstOrDefault();
                        if (opcode == null)
                            throw new Exception(string.Format("Error. The opcode with header {0} was not found!\n\tDumping buffer: {1}", header, BitConverter.ToString(buffer)));

                        if (opcode.size == 0)
                        {
                            var dlength = BitConverter.ToInt16(buffer, 6);
                            var chunk = buffer.Take(dlength).ToArray();
                            buffer = buffer.Skip(dlength).ToArray();

                            lock (writer)
                            {
                                writer.Append(opcode, chunk, "SERVER => CLIENT");
                            }
                        }
                        else
                        {
                            var chunk = buffer.Take(opcode.size).ToArray();
                            buffer = buffer.Skip(opcode.size).ToArray();

                            lock (writer)
                            {
                                writer.Append(opcode, chunk, "SERVER => CLIENT");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
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
