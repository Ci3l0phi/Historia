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
        private readonly Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public void Init(IPEndPoint local, IPEndPoint destination)
        {
            _socket.Bind(local);
            _socket.Listen(50);

            while (true)
            {
                var source = _socket.Accept();
                var destProxy = new Proxy();
                var state = new State(source, destProxy._socket);
                destProxy.Connect(destination, source);
                source.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, Process, state);
            }
        }

        private void Connect(EndPoint remoteEndpoint, Socket destination)
        {
            var state = new State(_socket, destination);
            _socket.Connect(remoteEndpoint);
            _socket.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, Process, state);
        }

        private static void Process(IAsyncResult result)
        {
            var state = (State)result.AsyncState;
            try
            {
                var bytesRead = state.Source.EndReceive(result);
                if (bytesRead > 0)
                {
                    state.Destination.Send(state.Buffer, bytesRead, SocketFlags.None);
                    state.Source.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, Process, state);
                }
            }
            catch (Exception e)
            {
                state.Destination.Close();
                state.Source.Close();
                Console.WriteLine(e.Message);
            }
        }

        private class State
        {
            public Socket Source { get; private set; }
            public Socket Destination { get; private set; }
            public byte[] Buffer { get; private set; }

            public State(Socket source, Socket destination)
            {
                Source = source;
                Destination = destination;
                Buffer = new byte[8192];
            }
        }
    }
}
