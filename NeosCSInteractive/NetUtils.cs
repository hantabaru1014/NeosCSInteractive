using System;
using System.Net;
using System.Net.Sockets;

namespace NeosCSInteractive
{
    public class NetUtils
    {
        private static readonly IPEndPoint DefaultLoopbackEndpoint = new IPEndPoint(IPAddress.Loopback, 0);

        public static int GetAvailablePort()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(DefaultLoopbackEndpoint);
                return ((IPEndPoint)socket.LocalEndPoint).Port;
            }
        }

        public static string CreatePassword(int length)
        {
            return System.Web.Security.Membership.GeneratePassword(length, 0);
        }
    }
}
