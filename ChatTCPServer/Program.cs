using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChatTCPServer
{
    class Program
    {
        static readonly object _lock = new object();
        static readonly Dictionary<int, TcpClient> list_clients = new Dictionary<int, TcpClient>();
        static readonly List<Client> clients = new List<Client>();

        static void Main(string[] args)
        {
            int count = 1;

            TcpListener ServerSocket = new TcpListener(IPAddress.Any, 5000);
            ServerSocket.Start();

            while (true)
            {
                TcpClient client = ServerSocket.AcceptTcpClient();
                lock (_lock) list_clients.Add(count, client);
                Console.WriteLine("Someone connected!!");

                Thread t = new Thread(handle_clients);
                t.Start(count);
                count++;
            }
        }

        public static void handle_clients(object o)
        {
            int id = (int)o;
            TcpClient client;

            lock (_lock) client = list_clients[id];

            while (true)
            {
                string data = string.Empty;
                string response = string.Empty;

                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int byte_count = stream.Read(buffer, 0, buffer.Length);
                data = Encoding.ASCII.GetString(buffer, 0, byte_count);
                if (data.Split(':')[0] == "connect")
                {
                    clients.Add(new Client
                    {
                        Username = data.Split(':')[1],
                        TcpClient = client
                    });
                    foreach (var item in clients)
                    {
                        response += item.Username + ",";
                    }
                    broadcast(response, client);
                    response = $"User : {data.Split(':')[1]} is connected";
                }
                if (data.Split(':')[0] == "message")
                {
                    var clientUser = clients.Where(p => p.Username.ToLower() == (data.Split(':')[1]).Split('?')[0].ToLower()).Select(a => a.TcpClient).FirstOrDefault();
                    if (clientUser != null)
                    {
                        var message = data.Split(':')[1].Split('?')[1];
                        broadcast($"From { FindUser(client).Username}:" + message, clientUser);
                    }

                }

                if (byte_count == 0)
                {
                    break;
                }

                Console.WriteLine(response);
            }

            lock (_lock) list_clients.Remove(id);
            client.Client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        public static void broadcast(string data, TcpClient client = null)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(data + Environment.NewLine);


            if (client == null)
            {
                lock (_lock)
                {
                    foreach (TcpClient c in list_clients.Values)
                    {
                        NetworkStream stream = c.GetStream();

                        stream.Write(buffer, 0, buffer.Length);
                    }
                }
            }
            else
            {
                lock (_lock)
                {
                    NetworkStream stream = client.GetStream();

                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }

        public static TcpClient FindUser(string username)
        {
            return clients.Where(p => p.Username.ToLower() == username.ToLower()).Select(a => a.TcpClient).FirstOrDefault();
        }

        public static Client FindUser(TcpClient username)
        {
            return clients.Where(p => p.TcpClient == username).FirstOrDefault();
        }
    }
}
