using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientForm
{
    public partial class ClientForm : Form
    {
        private Socket clientSocket = null;
        private static int _buff_size = 2048;
        private delegate void SafeCallDelegate(string text, Control obj);
        private Thread recvThread = null;
        public ClientForm()
        {
            InitializeComponent();
            clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                IPAddress serverIp = IPAddress.Parse(textBox1.Text);
                int serverPort = int.Parse(textBox2.Text);
                IPEndPoint serverEp = new IPEndPoint(serverIp, serverPort);
                clientSocket.Connect(serverEp);
                richTextBox1.Text += "Connected to " + serverEp.ToString();
                this.Text = "Connected to " + serverEp.ToString();
                recvThread = new Thread(() => this.readingClientSocket());
                recvThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                clientSocket.Send(Encoding.UTF8.GetBytes(richTextBox2.Text));
                richTextBox2.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void readingClientSocket()
        {
            byte[] buffer = new byte[_buff_size];
            while (clientSocket != null && clientSocket.Connected)
            {
                if (clientSocket.Available > 0)
                {
                    StringBuilder sb = new StringBuilder();

                    while (clientSocket.Available > 0)
                    {
                        int bRead = clientSocket.Receive(buffer, _buff_size, SocketFlags.None);
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, bRead));
                        Array.Clear(buffer, 0, buffer.Length); // Xóa buffer sau khi nhận xong
                    }

                    string receivedStr = "Message forwarded by server from " + sb.ToString();
                    UpdateTextThreadSafe(receivedStr, richTextBox1);
                }
            }
        }


        private void UpdateTextThreadSafe(string text, Control control)
        {
            if (control.InvokeRequired)
            {
                var d = new SafeCallDelegate(UpdateTextThreadSafe);
                control.Invoke(d, new object[] { text, control });
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

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                byte[] fileData = File.ReadAllBytes(filePath);
                string fileName = Path.GetFileName(filePath);
                string header = $"FILE|{fileName}|{fileData.Length}";

                try
                {
                    // Gửi header trước
                    clientSocket.Send(Encoding.UTF8.GetBytes(header));

                    // Gửi dữ liệu file
                    clientSocket.Send(fileData);

                    // Nếu là file văn bản, lấy nội dung
                    string fileExtension = Path.GetExtension(filePath).ToLower();
                    string fileContent = "";
                    if (fileExtension == ".txt" || fileExtension == ".csv" || fileExtension == ".log")
                    {
                        try
                        {
                            fileContent = File.ReadAllText(filePath);
                        }
                        catch (Exception ex)
                        {
                            fileContent = "Error reading file content: " + ex.Message;
                        }
                    }
                    else
                    {
                        fileContent = "File is not a text file, content cannot be displayed.";
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error sending file: " + ex.Message);
                }
            }
        }
    }
}


