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

        /// <summary>
        /// I'm sure this can be simplified ;)
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="direction"></param>
        private void Process(byte[] buffer, State.Direction direction)
        {
            try
            {
                if (direction == State.Direction.ClientToServer)
                {
                    var packetLength = BitConverter.ToInt16(buffer, 0);

                    if ((buffer.Length -2) != packetLength)
                    {
                        lock (ConsoleWriterLock)
                        {
                            Console.WriteLine("Buffer length {0} does not match the packet length! {1}", buffer.Length, packetLength);
                            Console.WriteLine("dump: {0}", BitConverter.ToString(buffer));
                        }
                        lock (ConsoleWriterLock)
                        {
                            var _opc = Op.opcodes.Where(x => x.name == "UNKNOWN").FirstOrDefault();
                            writer.Append(_opc, buffer, "C => S", buffer.Length);
                        }
                        return;
                    }

                    crypto.Decrypt(buffer, sizeof(short), packetLength);

                    var header = BitConverter.ToInt16(buffer, 2);
                    var opcode = Op.opcodes.Where(x => x.header == header).FirstOrDefault();
                    if (opcode == null)
                    {
                        lock (ConsoleWriterLock)
                        {
                            opcode = Op.opcodes.Where(x => x.name == "UNKNOWN").FirstOrDefault();
                            writer.Append(opcode, buffer, "C => S", buffer.Length);
                        }
                        return;
                    }

                    var chunk = buffer.Skip(2).ToArray();
                    lock (ConsoleWriterLock)
                    {
                        writer.Append(opcode, chunk, "C => S", packetLength);
                    }
                } else
                {
                    while (buffer.Length > 0)
                    {
                        var header = BitConverter.ToInt16(buffer, 0);
                        var opcode = Op.opcodes.Where(x => x.header == header).FirstOrDefault();
                        if (opcode == null)
                        {
                            lock (ConsoleWriterLock)
                            {
                                opcode = Op.opcodes.Where(x => x.name == "UNKNOWN").FirstOrDefault();
                                writer.Append(opcode, buffer, "S => C", buffer.Length);
                            }
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
                            writer.Append(opcode, chunk, "S => C", chunkLength);
                        }
                    }
                }
            } catch (Exception e)
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
