using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using NAudio;
using NAudio.Wave;
using TeamspeakBot;

namespace TriviaBot
{
    class Program
    {
        static Dictionary<int, string> Questions = new Dictionary<int, string>();
        static Dictionary<string, string> Answers = new Dictionary<string, string>();
        static List<string> InspiringQuotes = new List<string>();

        static Dictionary<string, int> Contestants = new Dictionary<string, int>();

        static List<string> ChatFolderDirectory = new List<string>(Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\TS3Client\\chats")); //construct on start up (now needs less scope, memory waste)
        static string ChatFolder;

        static string TeamspeakName = null;
        static int clid = 0;
        static List<string> Queue = new List<string>(); //List of items in the queue to play
        static int QuestionAmount = 20;
        static float Volume = 0.5F;
        static int targetmode = 2; //IF 2 = talk in channel, IF 3 = talk in global
        

        static string ServerLocation = "\\\\HTPC-PC\\teamspeak3-server_win64\\files\\virtualserver_1"; //THE SERVER LOCATION


        static void Main(string[] args)
        {
            Console.WriteLine("Starting up bot (v0.9.4)... ~~ added new commands");
            SendMessage(" << Starting up bot (v0.9.4) >> ~~ added new commands");

            //Do a check to see if teamspeak is running
            Process[] pname = Process.GetProcessesByName("ts3client_win64");
            if (pname.Length == 0)
            {
                Console.WriteLine("Cannot find teamspeak, make sure that it is running");
                Thread.Sleep(5000);
                Environment.Exit(0);
            }

            if (ChatFolderDirectory.Count == 0)
            {
                Console.WriteLine("No chat logs available in the directory: " + Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\TS3Client\\chats");
                Thread.Sleep(5000);
                Environment.Exit(0);
            }
            else //In future build, change to: if there is more than one folder, ask the user to pick
            {
                ChatFolder = ChatFolderDirectory[0];

            }
            Console.WriteLine("Getting your teamspeak username...");

            TcpClient client = new TcpClient();
            client.Connect("localhost", 25639);

            StreamWriter sw = new StreamWriter(client.GetStream());
            StreamReader sr = new StreamReader(client.GetStream());

            sw.WriteLine("whoami");
            sw.Flush();
            sr.ReadLine();
            sr.ReadLine();
            sr.ReadLine();
            sr.ReadLine();
            sr.ReadLine();
            sr.ReadLine();
            string Sresponse = sr.ReadLine();


            string[] ArrayResponse = Sresponse.Split(' ');
            Sresponse = ArrayResponse[0];
            clid = int.Parse(Sresponse.Substring(Sresponse.IndexOf('=') + 1));

            Console.WriteLine("Teamspeak ID: " + clid);
            Thread.Sleep(500);
            sw.WriteLine("clientupdate client_nickname=GirthBot");

            TeamspeakName = "girthbot"; //Change to allow debug - can use #IFDEBUG but CBA

            sw.Flush();
            sw.Close();
            sr.Close();
            client.Close();

            Console.WriteLine("Your teamspeak name has been set to: " + "\nWaiting for a user to enter inputs.. (5sec delay on read)");

            try
            {

                using (StreamReader srQUESTIONS = new System.IO.StreamReader("triviaQuestions.txt"))
                {
                    for (int marker = 1; ; marker++) //update to be smarter!!!
                    {
                        string Question = srQUESTIONS.ReadLine();
                        string Answer = srQUESTIONS.ReadLine();
                        if (Question == null) break;
                        if (Answer == null) break;

                        Questions.Add(marker, Question);
                        Answers.Add(Question, Answer);
                        srQUESTIONS.ReadLine(); //read next empty line

                    }
                }
                using (StreamReader quotes = new System.IO.StreamReader("inspiringQuotes.txt"))
                {
                    for (; ; )
                    {
                        string quote = quotes.ReadLine();
                        if (quote == null) break;
                        InspiringQuotes.Add(quote);
                    }
                }
            }
            catch
            {
                Console.WriteLine("One of the files required is broken (triviaQuestions.txt, inspiringQuotes.txt)");
                Thread.Sleep(1000);
                Environment.Exit(0);
            }

            for (; ; )
            {
                //detect input of !trivia <number> (if no number, default is 10)
                DetectInput();


                #region Trivia
                //We've got the amount of questions, generate random number list to ask random questions
                List<int> QuestionNumbers = new List<int>(GenerateQuestions(QuestionAmount)); //Key thing to know here is that questions list contains the number of which to ask

                SendMessage(" <<< Dr. Girth's Trivia Adventure™ is starting! >>>");
                SendMessage(" Hold on to your seats boys, its going to be a bumpy ride!");
                Thread.Sleep(2500);
                SendMessage(" ---------------------------------------------------------");
                SendMessage(" CURRENTLY IN BETA! Tell Jon if you find any bugs, make sure you spell correctly");
                SendMessage(" For questions regarding numbers, answer '10' (and not ten)");
                Thread.Sleep(2500);
                string beforeChat = null;

                //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                //         QUESTION TIME!!!!     //
                //>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
                for (int x = 0; x < QuestionAmount; x++)
                {
                    SendMessage(" ------------------------------------------------------");
                    //question from pool
                    SendMessage("Question " + (x + 1) + ": " + Questions[QuestionNumbers[x]]);

                    //create a copy of chat to compare before & after
                    using (StreamReader originalsr = new System.IO.StreamReader(ChatFolderDirectory[0] + "\\triviaRead.txt")) { beforeChat = originalsr.ReadToEnd(); }
                    Thread.Sleep(15000);
                    //read in what users have put
                    SendMessage("5 seconds left!");
                    Thread.Sleep(5000);
                    ReadAnswerTrivia(beforeChat, Answers[Questions[QuestionNumbers[x]]]); //send the previous chat & the answer

                    SendMessage("The correct answer was: " + Answers[Questions[QuestionNumbers[x]]] /*answer to question*/);
                    if (x == (QuestionAmount / 2) - 1)
                    {
                        SendMessage("The current scores are: " /*answer to question*/);
                        foreach (KeyValuePair<string, int> pair in Contestants)
                        {
                            SendMessage(pair.Key + " - " + pair.Value);
                        }
                    }
                    Thread.Sleep(5000);
                }
                SendMessage("All questions have been answered, time to add up the scores!!!!");
                Thread.Sleep(2000);

                SendMessage("The total scores are..." /*add lowest scorer*/);
                Thread.Sleep(1000);
                foreach (KeyValuePair<string, int> pair in Contestants)
                {
                    SendMessage(pair.Key + " - " + pair.Value);
                }
                Thread.Sleep(1000);
                SendMessage("And the winner is...");
                Thread.Sleep(500);

                int highestScore = 0;
                string highestScorer = "Everyone did shit";
                foreach (KeyValuePair<string, int> pair in Contestants)
                {
                    if (pair.Value > highestScore)
                    {
                        highestScore = pair.Value;
                        highestScorer = pair.Key;
                    }
                }
                SendMessage(highestScorer + " with a total of: " + highestScore); //WINNER!!

                Contestants.Clear();
                #endregion
            }
        }

        private static void SendMessage(string message)
        {
            //Example of server message: "sendtextmessage targetmode=2 msg=Hello\sChannel"
            message = message.Replace(" ", "\\s");
            message = "sendtextmessage " + "targetmode=" + targetmode + " msg=" + message;

            TcpClient client = new TcpClient();
            client.Connect("localhost", 25639);

            StreamWriter sw = new StreamWriter(client.GetStream());

            sw.WriteLine(message);
            sw.Flush();
            Console.WriteLine("Sent message in client: " + message);
            Thread.Sleep(500);
            sw.Close();
            client.Close();
        }

        private static List<int> GenerateQuestions(int QuestionAmount)
        {
            Random rand = new Random();
            List<int> result = new List<int>();
            HashSet<int> check = new HashSet<int>();
            for (Int32 i = 0; i < QuestionAmount; i++)
            {
                int curValue = rand.Next(1, (Questions.Count + 1));
                while (check.Contains(curValue))
                {
                    curValue = rand.Next(1, (Questions.Count + 1));
                }
                result.Add(curValue);
                check.Add(curValue);
            }

            return result;
        }
        private static void DetectInput()
        {
            try
            {
                if (ChatFolderDirectory.Count == 0)
                {
                    Console.WriteLine("There are no chat logs available");
                    Thread.Sleep(1000);
                    Environment.Exit(0);
                }

                string OriginalContent = null;
                string fileContent = null;

                Console.WriteLine("\r\nCreating media plug...");
                IWavePlayer waveOutDevice = new WaveOut();
                AudioFileReader audioFileReader;
                Console.WriteLine("Created media plug successfully");


                File.Delete(ChatFolderDirectory[0] + "\\triviaRead.txt");
                File.Copy(ChatFolderDirectory[0] + "\\channel.txt", ChatFolderDirectory[0] + "\\triviaRead.txt");
                Console.WriteLine("\r\n\r\nCreated clone file successfully");
                using (StreamReader originalsr = new System.IO.StreamReader(ChatFolderDirectory[0] + "\\triviaRead.txt"))
                {
                    OriginalContent = originalsr.ReadToEnd();
                }
                Console.WriteLine("Read the clone file successfully");

                for (bool triviaStart = false; ; ) //We run this loop until we detect the !Trivia message, putting the thread to sleep at the end to save performance
                {                                  //This is pretty shit/old way of doing it, it should be changed to a method call from another class!!!

                    File.Delete(ChatFolderDirectory[0] + "\\triviaRead.txt");
                    File.Copy(ChatFolderDirectory[0] + "\\channel.txt", ChatFolderDirectory[0] + "\\triviaRead.txt");
                    using (StreamReader sr = new System.IO.StreamReader(ChatFolderDirectory[0] + "\\triviaRead.txt"))
                    {
                        fileContent = sr.ReadToEnd();
                    }

                    //Check if there is new stuff
                    int index = fileContent.IndexOf(OriginalContent);
                    string cleanPath = (index < 0)
                        ? fileContent
                        : fileContent.Remove(index, OriginalContent.Length);

                    cleanPath = cleanPath.ToLower();


                    //Each line needs to perform '-' removal and [0] add to a list of users
                    //[1] is the users answer
                    List<string> ChatComment = new List<string>(cleanPath.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries));

                    for (int i = 0; i < ChatComment.Count; i++)  //This may need to be optimised in the future, it's pretty hefty on performance and it will run every 5seconds
                    {
                        var regex = new Regex("[<][^<]*[>]"); //Removing the timestamp
                        ChatComment[i] = (regex.Replace(ChatComment[i], String.Empty)).Trim();

                        if (!ChatComment[i].StartsWith(TeamspeakName))
                        {
                            //Time to get creative
                            if (ChatComment[i].Contains("!trivia"))
                            {
                                string s = ChatComment[i].Substring(ChatComment[i].IndexOf("!trivia"));
                                s = s.Substring(s.IndexOf(' ') + 1);
                                try
                                {
                                    QuestionAmount = Int32.Parse(s);
                                    if (QuestionAmount > 50 || QuestionAmount < 0)
                                    {
                                        SendMessage("----Trivia Start: Cannot start a trivia game with that many or few questions (Maximum of 50)");
                                    }
                                    else
                                    {
                                        SendMessage("----Trivia Start: Starting Trivia with " + s + " questions");
                                        triviaStart = true;
                                    }
                                }
                                catch
                                {
                                    if (s != "!trivia") SendMessage("----Trivia Start: Invalid amount of questions");
                                    else
                                    {
                                        QuestionAmount = 20;
                                        triviaStart = true;
                                    }
                                }
                            }
                            else if (ChatComment[i].Contains("!play <random")) //NEED TO IMPLEMENT
                            {
                                List<string> ChannelFolder = new List<string>(Directory.GetDirectories(ServerLocation)); //Getting a list of every channels folder
                                List<string> allFiles = new List<string>();

                                //For each channel folder, check for an .mp3, .wav or .wma - then store its location in a list, then pick one at random and add to the queue
                                foreach(string channel in ChannelFolder)
                                {
                                    if (Directory.Exists(channel + "\\GirthSounds"))
                                    {
                                        List<string> channelFiles = new List<string>(Directory.GetFiles(channel + "\\GirthSounds"));
                                        foreach (string mediaFile in channelFiles)
                                        {
                                            if (mediaFile.EndsWith(".mp3") || mediaFile.EndsWith(".wav") || mediaFile.EndsWith(".wma"))
                                            {
                                                allFiles.Add(mediaFile);
                                            }
                                        }
                                    }
                                }

                                Random r = new Random();
                                int num = r.Next(1, allFiles.Count+1);
                                Queue.Add(allFiles[num-1]);

                            }
                            else if (ChatComment[i].Contains("!play") && !ChatComment[i].Contains("!play <random")) //Pretty complicated, have to mess about with playstates & a list of what to play
                            {
                                string s = ChatComment[i].Substring(ChatComment[i].IndexOf("!play"));
                                s = s.Substring(s.IndexOf(' ') + 1);
                                if (s == "!play" || s == "") SendMessage("----Play: Invalid parameter, enter the name of the sound you want to play after '!play'");
                                else
                                {
                                    //We do a search algorithm on every folder in 'virtual_server' to find that file.
                                    if ((!s.EndsWith(".wav")) && (!s.EndsWith(".wma"))) s = s + ".mp3"; 

                                    FileSearch(s);
                                }
                            }
                            else if (ChatComment[i].Contains("!help")) //Do a timer check for the below, so it cant be spammed
                            {
                                SendMessage("You can enter the following commands: ");
                                SendMessage("    !trivia <amount of questions> - Start a game of Trivia with a specific amount of questions, if no number is entered the default is 20");
                                SendMessage("    !play <file> - Plays the sound of the file's name uploaded to the channel's browser (default is mp3, add .wav or .wma to end to play those) - Using the ALL command you can play all media files in a folder, e.g. '!play fallout4 ALL' ----- If you add <random instead, it will play a random file from the server");
                                SendMessage("    !skip - Skips the current track being played");
                                SendMessage("    !queue - Displays the current tracks in the queue");
                                SendMessage("    !speak <global OR channel> - Set if the bot should speak globally or just in the current channel");
                                SendMessage("    !move <channel number> - Moves the bot to the given channel");
                                SendMessage("    !purge - Clears all songs from the playlist");
                                SendMessage("    !volume - Changes the volume of Girth on the next song");
                                SendMessage("    !roll - Rolls a dice, add a number at the end to specify how many sides to the die (default is 6)");
                                SendMessage("    !inspireme - Throws out an inspiring quote, perfect for starting off the day");
                                SendMessage("    !firstlast - (to be implemented), a random catagory is selected and players must take turns to give a word relating to that topic, that word must start with the last letter of the last answer given ");
                                SendMessage("    !anagram <topic> - a random word is chosen, it will be jumbled up and placed in chat - the first person to figure out the original word wins - use !anagram list - to list the topics available");
                                SendMessage("    !dungeon - (to be implemented)");
                            }
                            else if (ChatComment[i].Contains("!skip"))
                            {
                                if (waveOutDevice.PlaybackState.ToString() == "Playing")
                                {
                                    waveOutDevice.Stop();
                                    waveOutDevice.Dispose();
                                    SendMessage("Skipping track..");
                                }
                            }
                            else if (ChatComment[i].Contains("!queue"))
                            {
                                if (Queue.Count > 0)
                                {
                                    for (int x = 0; x < Queue.Count; x++)
                                    {
                                        int y = x + 1;
                                        string[] ArrayFile = Queue[x].Split('\\');
                                        string fluffRemoved = ArrayFile[ArrayFile.Length - 1];
                                        SendMessage("Track No: " + y + " - " + fluffRemoved);
                                    }
                                }
                                else
                                {
                                    SendMessage("No songs in the queue");
                                }
                            }
                            else if (ChatComment[i].Contains("!speak"))
                            {
                                string s = ChatComment[i].Substring(ChatComment[i].IndexOf("!speak"));
                                s = (s.Substring(s.IndexOf(' ') + 1)).ToLower();

                                if (s == "global")
                                {
                                    targetmode = 3;
                                    SendMessage("----Speech target changed, Hello World");
                                }
                                else if (s == "channel")
                                {
                                    targetmode = 2;
                                    SendMessage("----Speech target changed, Hello World");
                                }
                                else SendMessage("----Speak: Invalid speech target");
                            }
                            else if (ChatComment[i].Contains("!move"))
                            {
                                string s = ChatComment[i].Substring(ChatComment[i].IndexOf("!move"));
                                s = (s.Substring(s.IndexOf(' ') + 1)).ToLower();

                                TcpClient client = new TcpClient();
                                client.Connect("localhost", 25639);
                                StreamWriter sw = new StreamWriter(client.GetStream());
                                //StreamReader sr = new StreamReader(client.GetStream()); //testing response

                                try
                                {
                                    int cid = Int32.Parse(s);
                                    sw.WriteLine("clientmove cid=" + cid + " clid=" + clid);
                                    sw.Flush();
                                    //string response = sr.ReadLine() + sr.ReadLine() + sr.ReadLine() + sr.ReadLine() + sr.ReadLine() + sr.ReadLine() + sr.ReadLine();
                                    Thread.Sleep(1000);
                                    SendMessage("Greetings young travellers");
                                }
                                catch { SendMessage("Invalid channel number"); }
                                sw.Close();
                                client.Close();
                            }
                            else if (ChatComment[i].Contains("!purge"))
                            {
                                try
                                {
                                    SendMessage("As if I could forget. Listen, Ryan, there's something about the playlist you should know. Oh no. It's too late. These files have all been infected. They may look fine now, but it's a matter of time before they turn into shit.");
                                    SendMessage("This entire playlist must be purged.");
                                    waveOutDevice.Stop();
                                    waveOutDevice.Dispose();
                                    Queue.Clear();
                                }
                                catch { Console.WriteLine("Possible trash occured in !purge - need to diagnose"); }
                            }
                            else if (ChatComment[i].Contains("!volume"))
                            {
                                string s = ChatComment[i].Substring(ChatComment[i].IndexOf("!volume"));
                                s = (s.Substring(s.IndexOf(' ') + 1)).ToLower();
                                try
                                {
                                    float input = float.Parse(s);
                                    if (input > 1.0 || input < 0.1) SendMessage("----VOLUME: Volume number supplied is too high or too low, ranges supported are: 0.1-1.0"); 
                                    else 
                                    {
                                        Volume = input;
                                        SendMessage("Volume changed to: " + input + "(Note that volume is not changed on the fly)");
                                    }
                                }
                                catch
                                {
                                    SendMessage("----VOLUME: Invalid volume number, ranges supported: 0.1-1.0");
                                }
                            }
                            else if (ChatComment[i].Contains("!roll") || ChatComment[i].Contains("!random"))
                            {
                                string s = null;
                                if (ChatComment[i].Contains("!roll")) s = ChatComment[i].Substring(ChatComment[i].IndexOf("!roll"));
                                else s = ChatComment[i].Substring(ChatComment[i].IndexOf("!random"));
                                s = (s.Substring(s.IndexOf(' ') + 1)).ToLower();
                                Random r = new Random();
                                int dice = 0;
                                int sides = 7;
                                if (s != "!roll")
                                {
                                    try
                                    {
                                        sides = Int32.Parse(s);
                                        sides++;
                                        if (sides > 1 && sides <10000000)
                                        {
                                            dice = r.Next(1, sides);
                                            SendMessage("----ROLL: You rolled " + dice);
                                        }
                                        else
                                        {
                                            SendMessage("----ROLL: Number is out of range, please enter a value from 2-9999999");
                                        }
                                    }
                                    catch { SendMessage("----ROLL: Invalid value"); }
                                }
                                else
                                {
                                    dice = r.Next(1, sides);
                                    if (dice == 1) SendMessage("----ROLL: You rolled a 1 ⚀");
                                    else if (dice == 2) SendMessage("----ROLL: You rolled a 2 ⚁");
                                    else if (dice == 3) SendMessage("----ROLL: You rolled a 3 ⚂");
                                    else if (dice == 4) SendMessage("----ROLL: You rolled a 4 ⚃");
                                    else if (dice == 5) SendMessage("----ROLL: You rolled a 5 ⚄");
                                    else if (dice == 6) SendMessage("----ROLL: You rolled a 6 ⚅");
                                }
                            }
                            else if (ChatComment[i].Contains("!anagram"))
                            {
                                Anagram anagram = new Anagram(ServerLocation, targetmode, ChatFolder, ChatComment[i]);
                            }

                            #region MEME_COMMANDS

                            else if (ChatComment[i].Contains("!wakemeup"))
                            { SendMessage("((cant wake up))"); }
                            else if (ChatComment[i].Contains("!haribochallenge"))
                            { FileSearch("no.mp3"); }
                            else if (ChatComment[i].Contains("!diegirth") || ChatComment[i].Contains("!killgirth"))
                            {
                                string User = ChatComment[i].Split(':')[0].Trim();
                                SendMessage("I'm sorry, " + User + ". I'm afraid I can't do that.");
                            }
                            else if (ChatComment[i].Contains("!topkek"))
                            { SendMessage("https://www.youtube.com/watch?v=_Su2cIP-onU"); }
                            else if (ChatComment[i].Contains("!meme"))
                            { SendMessage("https://www.youtube.com/watch?v=sJDU6U5hTWg"); }
                            else if (ChatComment[i].Contains("!killme"))
                            {
                                SendMessage("http://www.pharmacy2u.co.uk/paracetamol-caplets-500mg-teva-p7735.html");
                            }
                            else if (ChatComment[i].Contains("!dita"))
                            {
                                FileSearch("dita.wav");
                            }
                            #endregion
                            else if (ChatComment[i].Contains("!dungeon"))
                            {
                                DungeonQuest dungeon = new DungeonQuest();
                            }
                            else if (ChatComment[i].Contains("!inspireme"))
                            {
                                Random r = new Random();
                                int num = r.Next(1, InspiringQuotes.Count+1);
                                SendMessage(InspiringQuotes[num]);
                            }
                        }
                    }

                    try
                    {
                        if (waveOutDevice.PlaybackState.ToString() == "Stopped" && Queue.Count > 0) //The music player has stopped and the list still contains stuff to play
                        {
                            //
                            string s = Queue[0];

                            audioFileReader = new AudioFileReader(s);
                            audioFileReader.Volume = Volume;
                            waveOutDevice.Init(audioFileReader);
                            waveOutDevice.Play();

                            //C:\--------\---------\---\-\ remover
                            string[] ArrayFile = s.Split('\\');
                            string TimeLength = audioFileReader.TotalTime.ToString(); //Format this (remember ':0' is an emote in teamspeak..)
                            TimeLength = TimeLength.Split('.')[0].Trim(); //Getting rid of the miliseconds..
                            TimeLength = TimeLength.Replace(':', ';');

                            s = "Location: " + ArrayFile[ArrayFile.Length - 1] + " // Time: " + TimeLength;
                            SendMessage("----Playing file... " + s);
                            Console.WriteLine("Playing file in directory: " + s);

                            Queue.RemoveAt(0);
                        }
                    }
                    catch (Exception e)
                    {
                        SendMessage("----ERROR: There was a problem with that audio file, the exception thrown is: " + e);
                        if (Queue.Count != 0)
                        {
                            Queue.RemoveAt(0);
                        }
                    }


                    if (triviaStart == true) break;
                    OriginalContent = fileContent;
                    Thread.Sleep(5000);
                }
            }
            catch { 
                Console.WriteLine("Something went wrong in the detection method");
            Thread.Sleep(5000);
            Environment.Exit(0);
        
            }
        }

        private static void ReadAnswerTrivia(string oldChat, string Answer)
        {
            string fileContent = null;

            File.Delete(ChatFolderDirectory[0] + "\\triviaRead.txt");
            File.Copy(ChatFolderDirectory[0] + "\\channel.txt", ChatFolderDirectory[0] + "\\triviaRead.txt");
            using (StreamReader sr = new System.IO.StreamReader(ChatFolderDirectory[0] + "\\triviaRead.txt"))
            {
                fileContent = sr.ReadToEnd();
            }

            //Check if there is new stuff
            int index = fileContent.IndexOf(oldChat);
            string cleanPath = (index < 0)
                ? fileContent
                : fileContent.Remove(index, oldChat.Length);

            var regex = new Regex("[<][^<]*[>]");
            cleanPath = (regex.Replace(cleanPath, String.Empty)).Trim();

            //Each line needs to perform '-' removal and [0] add to a list of users
            //[1] is the users answer
            List<string> ChatComment = new List<string>(cleanPath.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries));
            List<string> AlreadyScored = new List<String>();

            bool FirstCorrect = true;
            for (int i = 0; i < ChatComment.Count; i++)
            {
                try
                {
                    ChatComment[i] = ChatComment[i].Trim();
                    if (ChatComment[i].Contains(':') && !ChatComment[i].StartsWith(TeamspeakName))
                    {
                        {
                            string User = ChatComment[i].Split(':')[0].Trim();
                            if (!Contestants.ContainsKey(User))
                            {
                                Contestants.Add(User, 0);
                            }

                            string UserAnswer = ChatComment[i].Split(':')[1].Trim();
                            if (UserAnswer.ToLower() == Answer.ToLower() && !AlreadyScored.Contains(User))
                            {
                                //their answer is correct!
                                int score = Contestants[User];
                                score++;
                                if (FirstCorrect == true)
                                {
                                    score++; //give two extra if they are the first one to give that answer
                                    score++;
                                    FirstCorrect = false;
                                }
                                Contestants[User] = score;
                                AlreadyScored.Add(User);

                            }
                        }
                    }
                }
                catch { /*invalid, they get no points*/}
            }

        }

        private static void FileSearch(string file)
        {
            //Main Directory to Search FROM:

            List<string> ChannelFolder = new List<string>(Directory.GetDirectories(ServerLocation)); //Getting a list of every channels folder

            if (file.Contains(" all")) // if it contains the all command..
            {
                //get the first part of the command (i.e. fallout ALL.mp3) make it just become 'fallout'
                string[] ArrayFile = file.Split(' ');
                file = ArrayFile[0];

                foreach (string channel in ChannelFolder)
                {
                    if (Directory.Exists(channel + "//GirthSounds")) //Check if it contains the girth sound folder
                    {
                        if (Directory.Exists(channel + "//GirthSounds//" + file))
                        {
                            List<string> fileFolderFiles = new List<string>(Directory.GetFiles(channel + "//GirthSounds//" + file)); //Getting a list of every file in the folder
                            if (fileFolderFiles.Count > 0)
                            {
                                foreach (string mediafile in fileFolderFiles)
                                {
                                    if (mediafile.EndsWith(".mp3") || mediafile.EndsWith(".wav") || mediafile.EndsWith(".wma"))
                                    {
                                        Queue.Add(mediafile);
                                    }
                                }
                            }
                            else
                            {
                                SendMessage("----Play: The directory given contains no files");
                                return;
                            }
                            return;
                        }
                    }
                }
            }
            else
            { 
            foreach (string channel in ChannelFolder)
            {
                if (Directory.Exists(channel + "//GirthSounds")) //Check if it contains the girth sound folder
                {
                    if (File.Exists(channel + "//GirthSounds//" + file))
                    {
                        Queue.Add(channel + "//GirthSounds//" + file);
                        return;
                    }
                }
            }

            //if no previous return condition is not met, it does not exist
            SendMessage("----Play: No file or Folder with that name could be found");
            return;
            }
        }

    }
}
