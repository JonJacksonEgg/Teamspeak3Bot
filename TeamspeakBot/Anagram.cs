using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TeamspeakBot
{
    class Anagram
    {
        static string ServerLocation;
        static int targetmode;
        static string ChatFolder;
        static string OriginalContent;
        static bool HintChecker = false; //Used so that chat cannot spam !hint
        public Anagram(string serverLocation, int targetMode, string chatfolder, string ChatMessage)
        {
            ServerLocation = serverLocation;
            targetmode = targetMode;
            ChatFolder = chatfolder;

            File.Delete(ChatFolder + "\\AnagramRead.txt"); //Time to get a copy of the chat for the comparison
            File.Copy(ChatFolder + "\\channel.txt", ChatFolder + "\\AnagramRead.txt");

            using (StreamReader originalsr = new System.IO.StreamReader(ChatFolder + "\\AnagramRead.txt"))
            {
                OriginalContent = originalsr.ReadToEnd();
            }

            string categoryinfo = ChatMessage.Split(':')[1].Trim().ToLower();
            //Chat commands for Anagram are handled in the class itself to keep it seperate, following commands:
            
            //list -- lists all of the categorys 
            if (categoryinfo == "!anagram list") ShowCategorys();
          
            //<something> <number> -- does a set amount of anagrams specified by the number

            //everything else is treated as a topic, if non is given it's
            else Start(ChatMessage);

        }

        public static void ShowCategorys()
        {
            using (StreamReader anagramtxt = new System.IO.StreamReader(ServerLocation + "\\channel_1\\Girth Files\\anagram.txt"))
            {
                string categoryMessage = "-----ANAGRAM:";
                string line;
                try
                {
                    for (; ; )
                    {
                        line = anagramtxt.ReadLine().ToLower();
                        if (line.StartsWith("[")) //It's a topic
                        {
                            categoryMessage = categoryMessage + " " + line + ",";
                        }
                    }
                }
                catch { SendMessage(categoryMessage); }
            }
        }
        public static void Start(string info)
        {
            //Pick a word and give one of the suggestions
            string userinfo = info.Split(':')[0].Trim();
            string categoryinfo = info.Split(':')[1].Trim(); //Default is random is nothing is given
            categoryinfo = categoryinfo.Remove(0, 8).Trim();

            if (categoryinfo == "") categoryinfo = "Random";
            SendMessage("-----ANAGRAM: Game Started by: " + userinfo);
            Thread.Sleep(500);

            //Instead of storing this in the program, make it more dynamic so it doesnt need to be loaded into the program if an edit has been made

            using (StreamReader anagramtxt = new System.IO.StreamReader(ServerLocation + "\\channel_1\\Girth Files\\anagram.txt"))
            {
                bool CategoryFound = false;
                string line = null;
                try
                {
                    for (; ; )
                    {
                        line = anagramtxt.ReadLine().ToLower();
                        if (!line.StartsWith("/")) //Ignore if it's a comment line
                        {
                            if (line.StartsWith("[")) //It's a topic
                            {
                                //logic for figuring out all the questions:
                                line = line.TrimStart('[');
                                line = line.TrimEnd(']');
                                if (line == categoryinfo)
                                {
                                    CategoryFound = true;
                                    List<string> words = new List<string>();
                                    try
                                    {
                                        for (; ; )
                                        {
                                            line = anagramtxt.ReadLine().ToLower();
                                            if (line.StartsWith("[")) break; //This is the next category
                                            else if (line != "") words.Add(line);
                                        }
                                    }
                                    catch { }
                                    Random r = new Random();
                                    int num = r.Next(0, words.Count);
                                    //Got the word we want to use, now format it to jumble it up & get the hint
                                    string word = words[num].Split('-')[0].Trim();
                                    string hint = words[num].Split('-')[1].Trim();

                                    string wordMix = "";

                                    for (; ; ) //Sorta dumb atm, it just muddles it up and if it isnt the same as original breaks - might need to improve
                                    {
                                        wordMix = new string(word.ToCharArray().
                                                    OrderBy(s => (r.Next(2) % 2) == 0).ToArray());

                                        if (wordMix != word && !wordMix.EndsWith(" ")) break;
                                    }

                                    SendMessage("-----ANAGRAM: The word to solve is: " + wordMix + " - Category: " + categoryinfo);

                                    //Begin checking for answers
                                    Stopwatch stopwatch = new Stopwatch();
                                    stopwatch.Start();

                                    bool CorrectAnswer = false; //need to optimize
                                    while (stopwatch.Elapsed < TimeSpan.FromSeconds(60))
                                    {
                                        //
                                        Thread.Sleep(1000);
                                        if (AnagramAnswers(word, hint) == true )
                                        {
                                            SendMessage("-----ANAGRAM: Correct! - The Answer was: " + word);
                                            CorrectAnswer = true;
                                            break;
                                        }
                                        if (stopwatch.Elapsed > TimeSpan.FromSeconds(45) && stopwatch.Elapsed < TimeSpan.FromSeconds(46)) SendMessage("-----ANAGRAM: 15 seconds to go!");

                                    }
                                    stopwatch.Stop();
                                    if (CorrectAnswer == false) SendMessage("-----ANAGRAM: The word was: " + word);
                                    break;
                                }
                            }
                            //It's a word, it should be ignored
                        }
                    }
                }
                catch { 
                    /*Reached the end of the file*/
                    if (CategoryFound == false) SendMessage("-----ANAGRAM: Category could not be found");
                }
            }

        }

        public static bool AnagramAnswers(string Answer, string Hint) //Checks chat for the answer and returns true or false if it has been found
        {
            try
            {
                string fileContent = null;



                File.Delete(ChatFolder + "\\AnagramRead.txt");
                File.Copy(ChatFolder + "\\channel.txt", ChatFolder + "\\AnagramRead.txt");

                using (StreamReader sr = new System.IO.StreamReader(ChatFolder + "\\AnagramRead.txt"))
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
                    if (ChatComment[i].Contains(Answer)) return true;
                    else if (ChatComment[i].Contains("!hint") && HintChecker == false)
                    {
                        if (Hint == "") SendMessage("-----ANAGRAM: No hint provided for this word!");
                        else SendMessage("-----ANAGRAM HINT: " + Hint);
                        HintChecker = true; //Used so that chat cannot spam !hint
                    }
                }

                return false;
            }
            catch
            {
                Console.WriteLine("Something went wrong in the detection method for: " + "ANAGRAM");
                SendMessage("Jon's messed up the code in the reading chat part of the program :( Anagram game ending");
                Environment.Exit(0);
                return false; //Pretty funny this is a compilation requirement as its never reached
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
    }
}
