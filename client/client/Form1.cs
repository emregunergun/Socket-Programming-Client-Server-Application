using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace client
{
    public partial class Form1 : Form
    {

        bool terminating = false;
        bool connected = false;
        Socket clientSocket;

        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();
        }

        private void button_connect_Click(object sender, EventArgs e)
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string IP = textBox_ip.Text;

            int portNum;
            if(Int32.TryParse(textBox_port.Text, out portNum))
            {

                if (textBox_username.Text != "" && textBox_username.Text.All(char.IsLetterOrDigit))//Checks if username is alphanumeric
                {
                    try
                    {
                        clientSocket.Connect(IP, portNum);
                        button_connect.Enabled = false;
                        //textBox_message.Enabled = true;
                        //button_send.Enabled = true;
                        connected = true;
                        logs.AppendText("Trying to connect to the server...\n");

                        //Added
                        button_disconnect.Enabled = true;
                        textBox_ip.Enabled = false;
                        textBox_port.Enabled = false;
                        textBox_username.Enabled = false;

                        Thread receiveThread = new Thread(Receive);
                        receiveThread.Start();


                        //Send the username
                        string message = textBox_username.Text;

                        if (message != "" && message.Length <= 64)
                        {
                            Byte[] buffer = Encoding.Default.GetBytes(message);
                            clientSocket.Send(buffer);
                        }


                    }
                    catch
                    {
                        logs.AppendText("Could not connect to the server!\n");
                    }
                }

                else
                {
                    logs.AppendText("Username can't be empty and must be alphanumeric!\n");
                }
            }
            else
            {
                logs.AppendText("Check the port!\n");
            }

        }

        private void Receive()
        {
            while(connected)
            {
                try
                {
                    Byte[] buffer = new Byte[256];
                    clientSocket.Receive(buffer);

                    string incomingMessage = Encoding.Default.GetString(buffer);
                    incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));

                    logs.AppendText(incomingMessage + "\n");

                    if (incomingMessage[incomingMessage.Length - 1] == '?')
                    {
                        button_send.Enabled = true;
                        textBox_message.Enabled = true;
                    }

                    else if (incomingMessage[0] == '"')
                    {

                    }
                    
                    else
                    {
                        button_send.Enabled = false;
                        textBox_message.Enabled = false;
                    }
                }
                catch
                {
                    if (!terminating)
                    {
                        logs.AppendText("Connection ceased.\n");
                        button_connect.Enabled = true;
                        textBox_message.Enabled = false;
                        button_send.Enabled = false;

                        //ADDED
                        button_disconnect.Enabled = false;
                        textBox_ip.Enabled = true;
                        textBox_port.Enabled = true;
                        textBox_username.Enabled = true;
                    }

                    clientSocket.Close();
                    connected = false;
                }

            }
        }

        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            connected = false;
            terminating = true;
            Environment.Exit(0);
        }

        private void button_send_Click(object sender, EventArgs e)
        {
            string message = textBox_message.Text;
            textBox_message.Clear();

            if (int.TryParse(message, out _))
            {
                if (message.Length <= 64)
                {
                    if (message != "")
                    {
                        Byte[] buffer = Encoding.Default.GetBytes(message);
                        clientSocket.Send(buffer);

                        button_send.Enabled = false;
                        textBox_message.Enabled = false;

                        logs.AppendText("Your answer is sent as \"" + message + "\"\n");
                    }

                    else
                    {
                        logs.AppendText("Answer can't be empty!\n");
                    }
                }
                else
                {
                    logs.AppendText("Answer is too long!\n");
                }
            }
            else
            {
                logs.AppendText("Answer can only contain numbers!\n");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button_disconnect_Click(object sender, EventArgs e)
        {
            logs.AppendText("Disconnecting from server...");
            
            clientSocket.Close();
        }

        private void button_clearLog_Click(object sender, EventArgs e)
        {
            logs.Clear();
        }

        private void textBox_message_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
