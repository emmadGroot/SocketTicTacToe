using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TicTacToeClient
{
    public partial class Form1 : Form
    {
        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        bool cancel = false;

        bool has2Players = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void ReceiveThread(object obj)
        {
            while (true)
            {
                if (cancel)
                {
                    return;
                }
                else if (clientSocket.Connected)
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = 0;
                    try
                    {
                        bytesRead = clientSocket.Receive(buffer);
                    }
                    catch (SocketException) // disconnected
                    {
                        Reset(true);
                        break;
                    }
                    if (bytesRead == 0) // disconnected
                    {
                        Reset(true);
                        break;
                    }
                    string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (dataReceived == "START")
                    {
                        has2Players = true;
                    }
                    else if (dataReceived.StartsWith("VICTORY"))
                    {
                        DialogResult result = MessageBox.Show($"{dataReceived.Substring(8, 1)} wins!", "Game over");
                        if (result == DialogResult.OK)
                        {
                            Reset();
                        }
                    }
                    else if (dataReceived == "DRAW")
                    {
                        DialogResult result = MessageBox.Show("It's a draw!", "Game over");
                        if (result == DialogResult.OK)
                        {
                            Reset();
                        }
                    }
                    else
                    {
                        Button btn = (Button)this.Controls.Find($"btnR{dataReceived.Substring(5, 1)}C{dataReceived.Substring(7, 1)}", true)[0];
                        this.Invoke((MethodInvoker)delegate
                        {
                            btn.Text = dataReceived.Substring(0, 1);
                            btn.Click -= btnSend_Click;
                        });
                    }
                }
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (tbRoom.Text == "")
                {
                    MessageBox.Show("Enter room name");
                    return;
                }

                // get local ip
                string ipAddress = string.Empty;
                int port = 20001;
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipAddress = ip.ToString();
                    }
                }
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSocket.Connect(new IPEndPoint(IPAddress.Parse(ipAddress), port));

                byte[] messageBytes = Encoding.UTF8.GetBytes(tbRoom.Text);
                clientSocket.Send(messageBytes, 0, messageBytes.Length, SocketFlags.None);

                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                try
                {
                    bytesRead = clientSocket.Receive(buffer);
                }
                catch (SocketException) // disconnected
                {
                    Reset(true);
                    return;
                }
                if (bytesRead == 0) // disconnected
                {
                    Reset(true);
                    return;
                }
                string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (dataReceived != "SUCCES")
                {
                    MessageBox.Show("Room name already taken");
                    return;
                }

                Thread t = new(ReceiveThread);
                t.Start();

                btnConnect.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (!clientSocket.Connected)
                MessageBox.Show("Please connect to the server first");
            else if (!has2Players)
                MessageBox.Show("Not enough players are connected");
            else
            {
                Button btn = (Button)sender;
                string move = $"{btn.Name.Substring(4, 1)}-{btn.Name.Substring(6, 1)}";
                byte[] buffer = Encoding.UTF8.GetBytes(move);
                clientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
            }
        }

        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (clientSocket.Connected)
                Disconnect(false);
        }

        private void Disconnect(bool notify)
        {
            cancel = true;

            has2Players = false;

            try
            {
                clientSocket.Shutdown(SocketShutdown.Both);
            }
            finally
            {
                clientSocket.Close();
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }

            this.Invoke((MethodInvoker)delegate
            {
                btnConnect.Enabled = true;
            });

            if (notify)
            {
                MessageBox.Show("Connection terminated");
            }

        }

        private void Reset(bool notify = false)
        {
            foreach (Button btn in this.Controls.OfType<Button>())
            {
                if (btn.Name == "btnConnect")
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        btn.Enabled = true;
                    });
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        btn.Click -= btnSend_Click;
                        btn.Click += btnSend_Click;
                        btn.Text = "";
                    });
                }
            }
            has2Players = false;
            Disconnect(notify);
        }
    }
}
