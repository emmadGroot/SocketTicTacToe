using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TicTacToeServer
{
    public class Room
    {
        public string roomName;

        public List<Socket> players = [];

        public char[,] grid = new char[3, 3];
        static public readonly int[,] magic = new int[3, 3];

        public bool gameActive = false;
        public int last = 1;
        public int turncounter = 0;

        public Room(string roomName, Socket clientSocket)
        {
            this.roomName = roomName;
            players.Add(clientSocket);
            byte[] succes = Encoding.UTF8.GetBytes("SUCCES");
            clientSocket.Send(succes, 0, succes.Length, SocketFlags.None);
        }

        public void AddPlayer(Socket clientSocket)
        {
            if (players.Count == 1)
            {
                players.Add(clientSocket);
                byte[] succes = Encoding.UTF8.GetBytes("SUCCES");
                clientSocket.Send(succes, 0, succes.Length, SocketFlags.None);
                foreach (Socket player in players)
                {
                    Thread t = new(() =>
                    {
                        ReceiveThread(player);
                    });
                    t.Start();
                }
                Broadcast("START");
                gameActive = true;
            }
            else
            {
                return;
            }
        }

        private void ReceiveThread(Socket clientSocket)
        {
            while (true)
            {
                if (gameActive)
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = 0;
                    try
                    {
                        bytesRead = clientSocket.Receive(buffer);
                    }
                    catch (SocketException) // Client disconnected
                    {
                        break;
                    }
                    if (bytesRead == 0) // Client disconnected
                    {
                        break;
                    }

                    string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    string[] split = dataReceived.Split('-');

                    // parse data from dataReceived
                    int row = int.Parse(split[0]);
                    int col = int.Parse(split[1]);

                    int player = players.IndexOf(clientSocket);

                    Console.WriteLine($"$ [ROOM: {roomName}] Received message: {dataReceived} from player {player}");

                    // don't let a player do consecutive moves
                    if (last != player)
                    {
                        last = player;

                        // determine player
                        char c;
                        if (player == 0) c = 'X';
                        else c = 'O';
                        turncounter++;
                        grid[row, col] = c;

                        Broadcast($"{c} on {row}-{col}");

                        // check for win or tie
                        if (CheckForWin(c, row, col))
                        {
                            Broadcast($"VICTORY {c}");
                            Console.WriteLine($"! [ROOM: {roomName}] Ending game due to victory");
                            EndGame();
                        }
                        else if (turncounter == 9)
                        {
                            Broadcast("DRAW");
                            Console.WriteLine($"! [ROOM: {roomName}] Ending game due to a draw");
                            EndGame();
                        }


                    }
                }
            }

            if (gameActive && clientSocket.Connected)
            {
                Console.WriteLine($"! [ROOM: {roomName}] Ending game due to disconnection of {clientSocket.RemoteEndPoint}");
                EndGame();

                Console.WriteLine($"> [ROOM: {roomName}] Client disconnected. ({clientSocket.RemoteEndPoint})");
            }
        }

        void Broadcast(string message)
        {
            byte[] dataToSend = Encoding.UTF8.GetBytes(message);
            int total = 0;
            foreach (Socket clientSocket in players)
            {
                if (clientSocket.Connected)
                {
                    clientSocket.Send(dataToSend, 0, dataToSend.Length, SocketFlags.None);
                    total++;
                }
            }
            Console.WriteLine($"$ [ROOM: {roomName}] Sent message \"{message}\" to {total} clients");
        }

        // Determine if there's a winner using a magic square (more info @ https://mathworld.wolfram.com/MagicSquare.html)
        public bool CheckForWin(char player, int row, int column)
        {
            Console.WriteLine($"? [ROOM: {roomName}] Checking for winner");

            // there can't be a winner until turn 5
            if (turncounter <= 4) return false;

            Dictionary<string, int> totals = new Dictionary<string, int>()
            {
                { "row", 0 },
                { "col", 0 },
                { "diag", 0 },
                { "diagreverse", 0 }
            };

            for (int i = 0; i < 3; i++)
            {
                // row
                if (grid[row, i] == player)
                    totals["row"] += magic[row, i];
                // column
                if (grid[i, column] == player)
                    totals["col"] += magic[i, column];

                // diagonal top left to bottom right
                if (column == row && grid[i, i] == player)
                    totals["diag"] += magic[i, i];

                // diagonal top right to bottom left
                if (row + column == 2)
                {
                    int reverse = 2 - i;
                    if (grid[i, reverse] == player)
                        totals["diagreverse"] += magic[i, reverse];
                }

            }

            if (totals.ContainsValue(15))
                return true;

            return false;
        }

        private void EndGame()
        {
            gameActive = false;
            grid = new char[3, 3];
            turncounter = 0;
            last = 1;
            try
            {
                // Disconnect everyone
                foreach (Socket socket in players)
                {
                    if (socket.Connected)
                    {
                        socket.Blocking = false;
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Disconnect(true);
                        Console.WriteLine($"> [ROOM: {roomName}] Client disconnected. ({socket.RemoteEndPoint})");

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Did not disconnect properly: \n {ex.Message}");
            }

            players.Clear();
        }
    }
}
