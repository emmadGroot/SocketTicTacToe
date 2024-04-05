using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace TicTacToeServer
{
    internal class Program
    {
        static List<Room> rooms = new List<Room>();
        static List<Socket> clients = new List<Socket>();

        

        static Socket serverSocket;

        static readonly IPAddress ipAddress = IPAddress.Any;
        static readonly int port = 20001;

        static async Task Main(string[] args)
        {
            #region Initialize magic square
            Room.magic[0, 0] = 8;
            Room.magic[0, 1] = 1;
            Room.magic[0, 2] = 6;
            Room.magic[1, 0] = 3;
            Room.magic[1, 1] = 5;
            Room.magic[1, 2] = 7;
            Room.magic[2, 0] = 4;
            Room.magic[2, 1] = 9;
            Room.magic[2, 2] = 2;
            #endregion

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(ipAddress, port));
            serverSocket.Listen(5);

            Console.WriteLine("> Server started.");

            while (true)
            {
                Socket clientSocket = serverSocket.Accept();
                Console.WriteLine($"> Client connected. ({clientSocket.RemoteEndPoint})");

                clients.Add(clientSocket);

                Thread t = new(() =>
                {
                    string roomName = WaitForRoomName(clientSocket);
                    List<Room> roomsWithName = rooms.Where(x => x.roomName == roomName).ToList();
                    if (roomsWithName.Count == 0)
                    {
                        rooms.Add(new(roomName, clientSocket));
                    }
                    else if (roomsWithName[0].players.Count != 2)
                    {
                        roomsWithName[0].AddPlayer(clientSocket);
                    }
                    else
                    {
                        byte[] error = Encoding.UTF8.GetBytes("ERROR");
                        clientSocket.Send(error, 0, error.Length, SocketFlags.None);
                        clientSocket.Close();
                    }
                });
                t.Start();
            }
        }

        private static string WaitForRoomName(Socket clientSocket)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = 0;
            try
            {
                bytesRead = clientSocket.Receive(buffer);
            }
            catch (SocketException) // Client disconnected
            {
                return "";
            }
            if (bytesRead == 0) // Client disconnected
            {
                return "";
            }
            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }
    }
}
