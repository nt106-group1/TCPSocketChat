using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SocketChat
{
    public partial class Form1 : Form
    {
        private Socket listener = null;
        private bool started = false;
        private int _port = 11000;
        private static int _buff_size = 2048;
        private byte[] _buffer = new byte[_buff_size];
        private Thread serverThread = null;
        private delegate void SafeCallDelegate(string text, Control obj);
        private List<Socket> clientSockets = new List<Socket>();
        public Form1()
        {
            InitializeComponent();
            listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (started)
                {
                    started = false;
                    button2.Text = "Listen";
                    serverThread = null;
                    listener.Close();
                }
                else
                {
                    serverThread = new Thread(() => this.listen());
                    serverThread.Start();
                   
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void listen()
        {
            listener.Bind(new IPEndPoint(IPAddress.Parse(textBox1.Text), _port));
            listener.Listen(10);
            started = true;
            UpdateTextThreadSafe("Stop", button2);
            UpdateTextThreadSafe("Start listening", richTextBox1);
            while (started)
            {
                Socket client = listener.Accept();
                clientSockets.Add(client);
                Thread clientThread = new Thread(() => this.readingClientSocket(client)); //Lambla expression
                clientThread.Start();
                UpdateTextThreadSafe("Accepted connection from " + client.RemoteEndPoint.ToString(), this.richTextBox1);
                UpdateTextThreadSafe(client.RemoteEndPoint.ToString() + " has joined the chat!", richTextBox1);                
            }
        }

        private void readingClientSocket(Socket client)
        {
            byte[] buffer = new byte[_buff_size];
            while (client.Connected)
            {
                if (client.Available > 0)
                {
                    StringBuilder sb = new StringBuilder();

                    while (client.Available > 0)
                    {
                        int bRead = client.Receive(buffer, _buff_size, SocketFlags.None);
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, bRead));
                        Array.Clear(buffer, 0, buffer.Length); // Xóa buffer sau khi nhận xong
                    }

                    string receivedStr = client.RemoteEndPoint + ": " + sb.ToString();
                    UpdateTextThreadSafe(receivedStr, richTextBox1);

                    foreach (Socket s in clientSockets)
                    {
                        s.Send(Encoding.UTF8.GetBytes(receivedStr));
                    }
                }
            }
        }

        private void UpdateTextThreadSafe(string text, Control control)
        {
            if (control.InvokeRequired)
            {
                var d = new SafeCallDelegate(UpdateTextThreadSafe);
                control.Invoke(d, new object[] { text, control});
            }
            else
            {
                if (control is RichTextBox)
                {
                    ((RichTextBox)control).AppendText("\r\n" + text);
                    ((RichTextBox)control).ScrollToCaret();
                } 
                else
                {
                    control.Text = text;

                }
            }
        }
    }
}
