using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Historia
{
    public class ProxyBak
    {
        private readonly Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static TOSCrypto crypto = new TOSCrypto();
        private static HTMLWriter _writer;
        private static readonly object ConsoleWriterLock = new object();

        public void Init(IPEndPoint local, IPEndPoint destination, HTMLWriter writer)
        {
            _writer = writer;
            _socket.Bind(local);
            _socket.Listen(50);

            while (true)
            {
                var source = _socket.Accept();
                var destProxy = new ProxyBak();
                var state = new State(source, destProxy._socket, Direction.ClientToServer);
                destProxy.Connect(destination, source);
                source.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, OnReceive, state);
            }
        }

        private void Connect(EndPoint remoteEndpoint, Socket destination)
        {
            var state = new State(_socket, destination, Direction.ServerToClient);
            _socket.Connect(remoteEndpoint);
            _socket.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, OnReceive, state);
        }

        private void OnReceive(IAsyncResult result)
        {
            var state = (State)result.AsyncState;
            try
            {
                var bytesRead = state.Source.EndReceive(result);
                var processed = 0;

                if (bytesRead > 0)
                {
                    state.Destination.Send(state.Buffer, bytesRead, SocketFlags.None);
                    state.Source.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, OnReceive, state);

                    //new Task(() => { Process(state.Buffer, bytesRead); }).Start();
                    Process(state.Buffer, bytesRead, state.Direction);
                }
                else
                {
                    Console.WriteLine("Transmission End.");
                }
            }
            catch (Exception e)
            {
                state.Destination.Close();
                state.Source.Close();
                Console.WriteLine(e.Message);
            }
        }

        private void Process(byte[] packet, int read, Direction direction)
        {
            /*
            lock(ConsoleWriterLock)
            {
                

                if (direction == Direction.ClientToServer)
                {
                    //dump packets for reading
                    byte[] buffer = new byte[read];
                    Buffer.BlockCopy(packet, 0, buffer, 0, read);

                    
                    try
                    {
                        crypto.Decrypt(buffer, 2, read - 2);
                        Console.WriteLine("read {0}", read);
                        Console.WriteLine("dump {0}", BitConverter.ToString(buffer));
                    } catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    
                }

                
            } */

            


            /*
            
            byte[] buffer = new byte[read];
            Buffer.BlockCopy(packet, 0, buffer, 0, read);

            try
            {

                if (direction == Direction.ClientToServer)
                {
                    var lengthBytes = buffer.Take(sizeof(short)).ToArray();
                    buffer = buffer.Skip(sizeof(short)).ToArray();
                    var packetLength = BitConverter.ToInt16(lengthBytes, 0);

                    if (read != packetLength)
                        Console.WriteLine(string.Format("Error. The read {0} doesn't match packetlength {1}", read, packetLength));

                    if (buffer.Length != packetLength)
                        throw new Exception(string.Format("Error. The packet length {0} does not match the buffer length {1} !", packetLength, buffer.Length));

                    crypto.Decrypt(buffer, 0, packetLength);

                    byte[] bak = new byte[packetLength+2];
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

                            lock (_writer)
                            {
                                _writer.Append(opcode, chunk, direction);
                            }
                        }
                        else
                        {
                            var chunk = buffer.Take(opcode.size).ToArray();
                            buffer = buffer.Skip(opcode.size).ToArray();

                            lock (_writer)
                            {
                                _writer.Append(opcode, chunk, direction);
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

                            lock (_writer)
                            {
                                _writer.Append(opcode, chunk, direction);
                            }
                        }
                        else
                        {
                            var chunk = buffer.Take(opcode.size).ToArray();
                            buffer = buffer.Skip(opcode.size).ToArray();

                            lock (_writer)
                            {
                                _writer.Append(opcode, chunk, direction);
                            }
                        }
                    }
                }
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }*/
            


            



            //byte[] copy;
            //var packetLength = BitConverter.ToInt16(buffer, 0);
            //if (read == packetLength + 2)
            //{
            //    copy = new byte[packetLength];
            //    Buffer.BlockCopy(buffer, 2, copy, 0, packetLength);
            //    crypto.Decrypt(copy, 0, packetLength);
            //} else
            //{
            //    copy = new byte[read];
            //    Buffer.BlockCopy(buffer, 0, copy, 0, read);
            //}

            //// opcode
            //try
            //{
            //    var header = BitConverter.ToInt16(copy, 0);
            //    var opcode = Op.opcodes.Where(x => x.header == header).FirstOrDefault();
            //    if (opcode == null)
            //    {
            //        StringBuilder hex = new StringBuilder();
            //        foreach (var b in copy)
            //        {
            //            hex.AppendFormat("{0:x2}", b);
            //            hex.Append(" ");
            //        }

            //        lock (ConsoleWriterLock)
            //        {
            //            Console.WriteLine("[Proxy] Unknown Packet received:");
            //            Console.WriteLine("{0,10}: {1,10}", "header", header);
            //            Console.WriteLine("{0,10}: {1,10}", "length", packetLength);
            //            Console.WriteLine("{0,10}: {1,10}", "raw", hex);
            //        }
            //    }

            //    //if (opcode.size == 0)
            //    //{
            //    //    if (copy.Length < 6)
            //    //        throw new Exception("Error. Buffer too small.");

            //    //    packetLength = BitConverter.ToInt16(copy, 6);
            //    //    var chunk = copy.Take(packetLength).ToArray();
            //    //    copy = copy.Skip(packetLength).ToArray();
            //    //}

                    
            //    lock (_writer)
            //    {
            //        _writer.Append(opcode, copy, direction);
            //    }
            //} catch (Exception e)
            //{
            //    Console.WriteLine(e.Message);
            //}
        }

        //public void Parse(byte[] buffer)
        //{
        //    var packetLength = BitConverter.ToInt16(buffer, 0);
        //}

        private class State
        {
            public Socket Source { get; private set; }
            public Socket Destination { get; private set; }
            public Direction Direction { get; private set; }
            public byte[] Buffer { get; private set; }

            public State(Socket source, Socket destination, Direction direction)
            {
                Source = source;
                Destination = destination;
                Direction = direction;
                Buffer = new byte[8192];
            }
        }

        public enum Direction
        {
            ServerToClient,
            ClientToServer
        }
    }
}
