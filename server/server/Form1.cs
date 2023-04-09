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



namespace server
{
    public partial class Form1 : Form
    {

        struct MyStruct
        {
            public double score;
            public string name;




            public MyStruct(double my_score, string my_name)
            {
                score = my_score;
                name = my_name;
            }

        }



        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        List<Socket> clientSockets = new List<Socket>();

        bool terminating = false;
        bool listening = false;


        //ADDED
        int number_of_clients = 0;
        List<string> connected_clients = new List<string>();
        List<string> wholeText = new List<string>();
        bool disconnect_all = false;
        List<Socket> contenders = new List<Socket>();
        List<string> contender_names= new List<string>();
        List<double> scores = new List<double>();
        List<bool> waiting= new List<bool>();
        bool next_question = false; 
        bool quizRunning = false;
        Thread quizThread;
        Thread quizOverThread;
        Thread quizStatusCheckThread;
        List<int> round_answers= new List<int>();
        Thread disconnectMessagesThread;

        string later_disconnect_message = "";


        Thread testThread;


        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();
        }

        public void send_message_to_all(string my_message)
        {
            string message_to_send = my_message;
            Byte[] buffer = Encoding.Default.GetBytes(message_to_send);
            foreach (Socket client in clientSockets)
            {
                try
                {
                    client.Send(buffer);
                }
                catch
                {
                    logs.AppendText("There is a problem! Check the connection...\n");
                    terminating = true;
                    textBox_message.Enabled = false;
                    button_send.Enabled = false;
                    textBox_port.Enabled = true;
                    button_listen.Enabled = true;
                    serverSocket.Close();
                }

            }
        }

        /*private void disconnectMessage()
        {
            
            List<string> missing_names = new List<string>();
            List<string> previous_contender_names_list = new List<string>();
            List<string> previous_connected_list = new List<string>();



            while (listening)
            {
                missing_names.Clear();
                if (connected_clients.Count != previous_connected_list.Count)
                {
                    foreach (string item in previous_connected_list)
                    {
                        if (!connected_clients.Contains(item))
                        {
                            missing_names.Add(item);
                        }

                    }


                    foreach (string item in missing_names)  //If someone has left
                    {
                        if (previous_contender_names_list.Contains(item))   //In quiz
                        {
                            send_message_to_all("\"" + item + "\" has disconnected and is removed from the quiz.");
                        }

                        else  //Not in quiz
                        {
                            send_message_to_all("\"" + item + "\" has disconnected.");
                        }
                    }


                    previous_connected_list.Clear();

                    if (connected_clients.Count != 0)
                    {
                        foreach (string item in connected_clients)
                        {
                            previous_connected_list.Add(item);
                        }
                    }

                    previous_contender_names_list.Clear();

                    if (contender_names.Count != 0)
                    {
                        foreach (string item in contender_names)
                        {
                            previous_contender_names_list.Add(item);
                        }
                    }
                }
            }
        }*/

        private bool check_if_waiting(List<bool> my_list)
        {
            int temp_len = my_list.Count;
            
            foreach(bool item in my_list)
            {
                if (item == false)
                {
                    return true;
                
                }
            }

            return false;

            
        }

        //DEBUG
        private void test1()
        {
            while (true)
            {
                

                string d15 = "Scores: ";
                foreach (double score in scores)
                {
                    d15+= score + " ";
                }
                
                
                string d1 = "clientSockets.Count = " + clientSockets.Count + "\n";
                string d2 = "bool terminating = " + terminating + "\n";
                string d3 = "bool listening = " + listening + "\n";
                string d4 = "int number_of_clients = " + number_of_clients + "\n";
                string d5 = "connected_clients.Count = " + connected_clients.Count + "\n";
                string d6 = "wholeText.Count = " + wholeText.Count + "\n";
                string d7 = "bool disconnect_all = " + disconnect_all + "\n";
                string d8 = "contenders.Count = " + contenders.Count + "\n";
                string d9 = "contender_names.Count = " + contender_names.Count + "\n";
                string d10 = "scores.Count = " + scores.Count + "\n";
                string d11 = "waiting.Count = " + waiting.Count + "\n";
                string d12 = "bool next_question = " + next_question + "\n";
                string d13 = "bool quizRunning = " + quizRunning + "\n";
                string d14 = "round_answers.Count = " + round_answers.Count + "\n";
                richTextBox1.Text = d1 + d2 + d3 + d4 + d5 + d6 + d7 + d8 + d9 + d10 + d11 + d12 + d13 + d14 + d15;
            }
        }




        private void button_listen_Click(object sender, EventArgs e)
        {
            int serverPort;


            //DEBUG
            testThread = new Thread(() => test1());
            testThread.Start();

            //disconnectMessagesThread = new Thread(() => disconnectMessage());
            //disconnectMessagesThread.Start();




            if (Int32.TryParse(textBox_port.Text, out serverPort))
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);
                serverSocket.Bind(endPoint);
                serverSocket.Listen(3);

                listening = true;
                button_listen.Enabled = false;
                textBox_message.Enabled = true;
                button_send.Enabled = true;

                Thread acceptThread = new Thread(Accept);
                acceptThread.Start();

                logs.AppendText("Started listening on port: " + serverPort + "\n");

                //ADDED
                textBox_port.Enabled= false;


            }
            else
            {
                logs.AppendText("Please check port number \n");
            }
        }

        private void Accept()
        {
            while(listening)
            {
                try
                {
                    Socket newClient = serverSocket.Accept();
                    clientSockets.Add(newClient);

                    //ADDED Check the list if usename exists
                    //----------------
                    Byte[] buffer = new Byte[64];
                    newClient.Receive(buffer);

                    string incomingMessage = Encoding.Default.GetString(buffer);
                    incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));

                    string current_username = incomingMessage;




                    

                    //ADDED
                    number_of_clients++;
                    button_disconnect.Enabled = true;

                    Thread receiveThread = new Thread(() => Receive(newClient)); // updated
                    receiveThread.Start();

                    if (!connected_clients.Contains(current_username))
                    {
                        logs.AppendText("\"" + current_username + "\" is connected.\n");
                        connected_clients.Add(current_username);

                        
                        foreach (Socket client in clientSockets)
                        {
                            if (client != newClient)
                            {
                                string message_to_send = "\"" + current_username + "\" is connected.";
                                Byte[] buffer63 = Encoding.Default.GetBytes(message_to_send);

                                try
                                {
                                    client.Send(buffer63);
                                }
                                catch
                                {
                                    logs.AppendText("There is a problem! Check the connection...\n");
                                    terminating = true;
                                    textBox_message.Enabled = false;
                                    button_send.Enabled = false;
                                    textBox_port.Enabled = true;
                                    button_listen.Enabled = true;
                                    serverSocket.Close();
                                }
                            }

                            else
                            {
                                string message_to_send = "Connected to server as \"" + current_username + "\".";
                                Byte[] buffer63 = Encoding.Default.GetBytes(message_to_send);

                                try
                                {
                                    client.Send(buffer63);
                                }
                                catch
                                {
                                    logs.AppendText("There is a problem! Check the connection...\n");
                                    terminating = true;
                                    textBox_message.Enabled = false;
                                    button_send.Enabled = false;
                                    textBox_port.Enabled = true;
                                    button_listen.Enabled = true;
                                    serverSocket.Close();
                                }
                            }
                        }
                    }
                    //------------

                    else
                    {
                        logs.AppendText("A user tried to connect as \"" + current_username + "\", but there already is a user with that name.\n");

                        string message_to_send = ("\"" + current_username + "\" is already taken. Try again with another username.");
                        Byte[] buffer7 = Encoding.Default.GetBytes(message_to_send);

                        try
                        {
                            newClient.Send(buffer7);
                        }
                        catch
                        {
                            logs.AppendText("There is a problem! Check the connection...\n");
                            terminating = true;
                            textBox_message.Enabled = false;
                            button_send.Enabled = false;
                            textBox_port.Enabled = true;
                            button_listen.Enabled = true;
                            serverSocket.Close();
                        }



                        newClient.Close();
                    }
                }
                catch
                {
                    if (terminating)
                    {
                        listening = false;

                        //ADDED
                        button_disconnect.Enabled = false;
                    }
                    else
                    {
                        logs.AppendText("The socket stopped working.\n");
                    }

                }
            }
        }

        private void Receive(Socket thisClient) // updated
        {
            bool connected = true;

            while (connected && !terminating && !disconnect_all)
            {




                later_disconnect_message = "";
                
                try
                {
                    Byte[] buffer = new Byte[64];
                    thisClient.Receive(buffer);

                    

                    string incomingMessage = Encoding.Default.GetString(buffer);
                    incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));
                    
                    
                    
                    
                    
                    
                    logs.AppendText(connected_clients[clientSockets.IndexOf(thisClient)] + ": " + incomingMessage + "\n");

                    int answer = Int32.Parse(incomingMessage);

                    waiting[contenders.IndexOf(thisClient)] = true;
                    round_answers[contenders.IndexOf(thisClient)] = answer;

                    if (waiting.Count != 0)
                    {
                        bool check_false = false;

                        next_question = !check_if_waiting(waiting);
                    }
                }
                catch
                {
                    
                    
                    
                    
                    
                    
                    
                    if(!terminating)
                    {
                        
                        
                        
                        //logs.AppendText("A client has disconnected\n");
                        //ADDED
                        if(connected_clients.Count == clientSockets.Count && connected_clients.Count != 0)  //Legit connected and disconnecting
                        {


                            if (contenders.Contains(thisClient))//During quiz
                            {
                                
                                logs.AppendText("\"" + connected_clients[clientSockets.IndexOf(thisClient)] + "\" has disconnected and is removed from the quiz.\n");
                                later_disconnect_message = "\"" + connected_clients[clientSockets.IndexOf(thisClient)] + "\" has disconnected and is removed from the quiz.";

                                connected_clients.Remove(connected_clients[clientSockets.IndexOf(thisClient)]);

                                scores.RemoveAt(contenders.IndexOf(thisClient));
                                waiting.RemoveAt(contenders.IndexOf(thisClient));
                                round_answers.RemoveAt(contenders.IndexOf(thisClient));
                                contender_names.RemoveAt(contenders.IndexOf(thisClient));

                                contenders.Remove(thisClient);

                                
                            }

                            else
                            {
                                logs.AppendText("\"" + connected_clients[clientSockets.IndexOf(thisClient)] + "\" has disconnected.\n");

                                later_disconnect_message = "\"" + connected_clients[clientSockets.IndexOf(thisClient)] + "\" has disconnected.";

                                connected_clients.Remove(connected_clients[clientSockets.IndexOf(thisClient)]);
                            }

                            if (waiting.Count != 0)
                            {
                                next_question = !check_if_waiting(waiting);
                            }
                        }
                        else    //trying to connect with already existing username
                        {

                        }


                        //ADDED
                        number_of_clients--;
                        if (number_of_clients == 0)
                        {
                            button_disconnect.Enabled = false;
                            disconnect_all = false;
                        }

                        thisClient.Close();
                        clientSockets.Remove(thisClient);
                        connected = false;

                        send_message_to_all(later_disconnect_message);
                    }

                    


                    
                }
            }


        }

        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(quizRunning)
            {
                string message9 = "\nServer aborted the quiz!\n";
                Byte[] bufferA = Encoding.Default.GetBytes(message9);
                foreach (Socket client in clientSockets)
                {
                    try
                    {
                        client.Send(bufferA);
                    }
                    catch
                    {
                        logs.AppendText("There is a problem! Check the connection...\n");
                        terminating = true;
                        textBox_message.Enabled = false;
                        button_send.Enabled = false;
                        textBox_port.Enabled = true;
                        button_listen.Enabled = true;
                        serverSocket.Close();
                    }

                }
            }



            string message10 = "Server shutting down!\n";
            Byte[] buffer = Encoding.Default.GetBytes(message10);
            foreach (Socket client in clientSockets)
            {
                try
                {
                    client.Send(buffer);
                }
                catch
                {
                    logs.AppendText("There is a problem! Check the connection...\n");
                    terminating = true;
                    textBox_message.Enabled = false;
                    button_send.Enabled = false;
                    textBox_port.Enabled = true;
                    button_listen.Enabled = true;
                    serverSocket.Close();
                }

            }


            listening = false;
            terminating = true;
            Environment.Exit(0);
        }

        private void button_send_Click(object sender, EventArgs e)
        {

            //Read File
            wholeText.Clear();

            string lineOfText;
            string textFilePath = "questions.txt"; //Text file for questions
            try
            {
                var filestream = new System.IO.FileStream(textFilePath,
                                  System.IO.FileMode.Open,
                                  System.IO.FileAccess.Read,
                                  System.IO.FileShare.ReadWrite);
                var file = new System.IO.StreamReader(filestream, System.Text.Encoding.UTF8, true, 128);

                while ((lineOfText = file.ReadLine()) != null)
                {
                    wholeText.Add(lineOfText);
                }
            }
            catch
            {
                logs.AppendText("No \"questions.txt\" file exists in the same folder!\n");
            }

            if(wholeText.Count != 0)
            {
                if (clientSockets.Count > 1)    //START THE QUIZ
                {
                    if (textBox_message.Text != "" && int.TryParse(textBox_message.Text, out _))
                    {
                        int number_of_turns = 0;

                        Int32.TryParse(textBox_message.Text, out number_of_turns);

                        //START QUIZ
                        string message5 = "\nStarting the quiz for " + number_of_turns + " rounds, with " + clientSockets.Count + " players...\n";
                        Byte[] buffer = Encoding.Default.GetBytes(message5);
                        foreach (Socket client in clientSockets)
                        {
                            try
                            {
                                client.Send(buffer);
                            }
                            catch
                            {
                                logs.AppendText("There is a problem! Check the connection...\n");
                                terminating = true;
                                textBox_message.Enabled = false;
                                button_send.Enabled = false;
                                textBox_port.Enabled = true;
                                button_listen.Enabled = true;
                                serverSocket.Close();
                            }

                        }

                        quizThread = new Thread(() => Quiz(number_of_turns)); // STARTING THE QUIZ
                        quizThread.Start();




                        //Quiz is over


                    }
                    else
                    {
                        logs.AppendText("ENTER AN INTEGER!\n");
                    }
                }
                else
                
                {
                    logs.AppendText("More than 1 user needs to be connected!\n");
                }
            }
            else
            {
                logs.AppendText("Couldn't read the file \"questions.txt\"\n");
            }
            

            
            
        }

        private void Quiz(int number_of_questions)
        {
            logs.AppendText("\nStarting the quiz for " + number_of_questions + " rounds, with " + clientSockets.Count + " players...\n");

            textBox_message.Enabled = false;
            button_send.Enabled = false;

            if (wholeText.Count != 0)
            {
                textBox_message.Enabled = false;



                foreach (Socket currentSocket in clientSockets)
                {
                    contenders.Add(currentSocket);
                }
               


                foreach (Socket temp1 in contenders)
                {
                    scores.Add(0);
                    contender_names.Add(connected_clients[contenders.IndexOf(temp1)]);
                }

                //int number_of_questions = Int32.Parse(textBox_message.Text);
                quizRunning = true;
                quizOverThread = new Thread(() => QuizOver()); // Thread for quiz over
                quizOverThread.Start();
                quizStatusCheckThread = new Thread(() => quizStatusCheck()); // Thread for quiz over
                quizStatusCheckThread.Start();


                for (int i = 0; i < number_of_questions; i++)   //Each round
                {
                    int current_question_index = (i * 2) % wholeText.Count;
                    int current_answer_index = (i * 2 + 1) % wholeText.Count;

                    next_question = false;
                    waiting.Clear();
                    round_answers.Clear();
                    foreach (Socket temp3 in contenders)
                    {
                        waiting.Add(false);
                        round_answers.Add(0);
                    }

                    logs.AppendText("\nAsking question " + (i+1) + ": " + wholeText[current_question_index] + "\n");

                    {//if (message != "" && message.Length <= 64)           //Send the question to ALL
                        
                        
                        
                        foreach (Socket client in clientSockets)
                        {

                            if (contenders.Contains(client))
                            {
                                string message = "\nRound " + (i + 1) + ": " + wholeText[current_question_index];
                                Byte[] buffer = Encoding.Default.GetBytes(message);
                                try
                                {
                                    client.Send(buffer);
                                }
                                catch
                                {
                                    logs.AppendText("There is a problem! Check the connection...\n");
                                    terminating = true;
                                    textBox_message.Enabled = false;
                                    button_send.Enabled = false;
                                    textBox_port.Enabled = true;
                                    button_listen.Enabled = true;
                                    serverSocket.Close();
                                }
                            }

                            else
                            {
                                string message = "\nRound " + (i + 1) + ": " + wholeText[current_question_index] + " (SPECTATOR)\n";
                                Byte[] buffer = Encoding.Default.GetBytes(message);
                                try
                                {
                                    client.Send(buffer);
                                }
                                catch
                                {
                                    logs.AppendText("There is a problem! Check the connection...\n");
                                    terminating = true;
                                    textBox_message.Enabled = false;
                                    button_send.Enabled = false;
                                    textBox_port.Enabled = true;
                                    button_listen.Enabled = true;
                                    serverSocket.Close();
                                }
                            }
                        }

                        //Question sent to all
                        while (!next_question)
                        {
                            //int p = 0;
                        }

                        //Received all answers, check if they are true
                        int correct_answer = Int32.Parse(wholeText[current_answer_index]);
                        
                        List<int> round_differences = new List<int>();
                        


                        foreach (int current_answer in round_answers)
                        {
                            int difference = Math.Abs(current_answer - correct_answer);

                            round_differences.Add(difference);
                        }

                        int minumum_difference = round_differences.Min();

                        int true_answer_count = 0;



                        //Give out points
                        foreach (int current_difference in round_differences)
                        {
                            if (minumum_difference == current_difference)
                            {
                                true_answer_count++;
                            }
                        }

                        List<string> name_of_winners = new List<string>();

                        logs.AppendText("The correct answer was: " + correct_answer + "\n");
                        send_message_to_all("The correct answer was: " + correct_answer);

                        if (true_answer_count == 1) //1 winner
                        {
                            scores[round_differences.IndexOf(minumum_difference)] += 1;
                            logs.AppendText("Winner of the round is: \"" + contender_names[round_differences.IndexOf(minumum_difference)] + "\" with a difference of \"" + minumum_difference + "\"\n\n");

                            send_message_to_all("Winner of the round is: \"" + contender_names[round_differences.IndexOf(minumum_difference)] + "\" with a difference of \"" + minumum_difference + "\"\n");
                        }

                       

                        else    //Multiple winners
                        {
                            for(int i3 = 0; i3< round_differences.Count; i3++)
                            {
                                if (round_differences[i3] == minumum_difference)
                                {
                                    scores[i3] += 0.5;

                                    name_of_winners.Add(contender_names[i3]);

                                }
                            }

                            logs.AppendText("There are multiple winners this round!\n");
                            logs.AppendText("The winners are: ");

                            string multi_winner_message = "There are multiple winners this round!\nThe winners are: ";

                            foreach (string name in name_of_winners)
                            {
                                if (name == name_of_winners[name_of_winners.Count - 1])
                                {
                                    logs.AppendText("\"" + name + "\" ");

                                    multi_winner_message += "\"" + name + "\" ";
                                }

                                else
                                {
                                    logs.AppendText("\"" + name + "\" and ");

                                    multi_winner_message += "\"" + name + "\" and ";
                                }
                                
                            }

                            logs.AppendText("with a difference of \"" + minumum_difference + "\"\n\n");

                            multi_winner_message += "with a difference of \"" + minumum_difference + "\"\n";

                            send_message_to_all(multi_winner_message);
                        }

                        double top_score = scores.Max();



                        //Scores updated
                        List<MyStruct> sorted_scores = new List<MyStruct>();
                        

                        for (int i9 = 0; i9 < contender_names.Count; i9++) 
                        {
                            MyStruct temp_struct = new MyStruct(scores[i9], contender_names[i9]);
                            sorted_scores.Add(temp_struct);
                        }

                        //Sort the scoreboard

                        sorted_scores.Sort((s1, s2) => s1.score.CompareTo(s2.score));
                        sorted_scores.Reverse();


                        if (i == number_of_questions - 1)//LAST ROUND
                        {
                            logs.AppendText("The last round is over.\nScores:\n");
                            send_message_to_all("The last round is over.\nScores:");   
                        }
                        else
                        {
                            logs.AppendText("Round " + (i + 1) + " is over.\nScores:\n");
                            send_message_to_all("Round " + (i + 1) + " is over.\nScores:");
                        }

                        //Print scoreboard
                        foreach (MyStruct item in sorted_scores)   
                        {
                            logs.AppendText(item.name + ": " + item.score + "\n");
                            send_message_to_all(item.name + ": " + item.score);
                        }

                        if (i == number_of_questions - 1)//LAST ROUND   //Print winners of the quiz
                        {
                            List<string> winner_names = new List<string>();

                            if (sorted_scores[0].score != sorted_scores[1].score)   //Only one winner
                            {
                                logs.AppendText("\nTHE WINNER IS \"" + sorted_scores[0].name + "\" WITH \"" + sorted_scores[0].score + "\" POINTS!\n");

                                send_message_to_all("\nTHE WINNER IS \"" + sorted_scores[0].name + "\" WITH \"" + sorted_scores[0].score + "\" POINTS!");
                            }

                            else    //Multiple winners
                            {
                                string multiple_winner_message = "\nTHERE ARE MULTIPLE WINNERS!\nTHE WINNERS ARE: ";


                                logs.AppendText("\nTHERE ARE MULTIPLE WINNERS!\n");
                                logs.AppendText("THE WINNERS ARE: ");

                                foreach (MyStruct item in sorted_scores)
                                {
                                    if (item.score == sorted_scores[0].score)
                                    {
                                        winner_names.Add(item.name);
                                    }
                                }

                                foreach (string item in winner_names)
                                {
                                    if(item == winner_names[winner_names.Count-1])  //Last one
                                    {
                                        logs.AppendText("\"" + item + "\" ");

                                        multiple_winner_message += "\"" + item + "\" ";
                                    }
                                    else
                                    {
                                        logs.AppendText("\"" + item + "\" and ");

                                        multiple_winner_message += "\"" + item + "\" and ";
                                    }
                                }

                                logs.AppendText("WITH \"" + sorted_scores[0].score + "\" POINTS!\n");

                                multiple_winner_message += "WITH \"" + sorted_scores[0].score + "\" POINTS!";
                                send_message_to_all(multiple_winner_message);

                            }
                        }


                        //Round is over


                    }
                }
            }

            //END OF QUIZ


            quizRunning = false;
        }

        private void QuizOver()
        {
            while (quizRunning)
            {

            }

            textBox_message.Enabled = true;
            button_send.Enabled = true;

            contenders.Clear();
            contender_names.Clear();
            waiting.Clear();
            scores.Clear();
            round_answers.Clear();
            next_question = false;

            logs.AppendText("QUIZ IS OVER!\n\n");

           
            string message9 = "QUIZ IS OVER!\n\n";
            Byte[] bufferA = Encoding.Default.GetBytes(message9);
            foreach (Socket client in clientSockets)
            {
                try
                {
                    client.Send(bufferA);
                }
                catch
                {
                    logs.AppendText("There is a problem! Check the connection...\n");
                    terminating = true;
                    textBox_message.Enabled = false;
                    button_send.Enabled = false;
                    textBox_port.Enabled = true;
                    button_listen.Enabled = true;
                    serverSocket.Close();
                }

            }
        }

        private void quizStatusCheck()
        {
            while (quizRunning)
            {
                if (disconnect_all)
                {
                    quizRunning = false;
                }
                
                else if (contenders.Count == 0 && !disconnect_all)
                {
                    logs.AppendText("No players remaining.\n");
                    string message9 = "No players remain in quiz.\n";
                    Byte[] bufferA = Encoding.Default.GetBytes(message9);
                    foreach (Socket client in clientSockets)
                    {
                        try
                        {
                            client.Send(bufferA);
                        }
                        catch
                        {
                            logs.AppendText("There is a problem! Check the connection...\n");
                            terminating = true;
                            textBox_message.Enabled = false;
                            button_send.Enabled = false;
                            textBox_port.Enabled = true;
                            button_listen.Enabled = true;
                            serverSocket.Close();
                        }

                    }
                    quizRunning = false;
                }
                
                else if (contenders.Count == 1)
                {
                    logs.AppendText("Only 1 player remaining.\n");
                    logs.AppendText("\"" + contender_names[0] + "\" wins!!!\n");

                    string message9 = "Only 1 player remaining.\n\"" + contender_names[0] + "\" wins!!!\n";
                    Byte[] bufferA = Encoding.Default.GetBytes(message9);
                    foreach (Socket client in clientSockets)
                    {
                        try
                        {
                            client.Send(bufferA);
                        }
                        catch
                        {
                            logs.AppendText("There is a problem! Check the connection...\n");
                            terminating = true;
                            textBox_message.Enabled = false;
                            button_send.Enabled = false;
                            textBox_port.Enabled = true;
                            button_listen.Enabled = true;
                            serverSocket.Close();
                        }

                    }

                    quizRunning = false;
                }
            }
        }
        private void button_clearLog_Click(object sender, EventArgs e)
        {
            logs.Clear();
        }

        private void button_disconnect_Click(object sender, EventArgs e)
        {

            string message9 = "\nServer disconnected all clients!\n";
            if(quizRunning)
            {
                message9 += "QUIZ IS OVER!!!\n";
            }
            
            
            Byte[] bufferA = Encoding.Default.GetBytes(message9);
            foreach (Socket client in clientSockets)
            {
                try
                {
                    client.Send(bufferA);
                }
                catch
                {
                    logs.AppendText("There is a problem! Check the connection...\n");
                    terminating = true;
                    textBox_message.Enabled = false;
                    button_send.Enabled = false;
                    textBox_port.Enabled = true;
                    button_listen.Enabled = true;
                    serverSocket.Close();
                }

            }

            if (quizRunning == true)
            {
                quizThread.Abort();
            }
            
            disconnect_all = true;

            while (clientSockets.Count != 0) //Disconnect all when pressed
                try
                {
                    foreach (Socket thisClient in clientSockets)
                    {
                        thisClient.Close();
                        clientSockets.Remove(thisClient);
                    }
                }
                catch
                {
                    logs.AppendText("");
                    connected_clients.Clear();
                }

            logs.AppendText("All clients disconnected!\n");

        }
    }
}
