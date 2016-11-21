using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TeamspeakBot
{
    class DungeonQuest
    {
        static int targetmode = 2; //(the channel it uses to send messages to)

        //Need to design the structure, -- first task is to create a character and get relevent info about that character

        static List<Character> PlayerCharacters = new List<Character>(); //Contains each players character and all the relevent info regarding them

        static List<string> NameContainer = new List<string>();

        public void Begin()
        {
            SendMessage("Still testing");
            //Read in the text files data


            //Foreach Player, ask them a question and create a CHARACTER object from that question

            //Setting the characters info logic here (so we dont need to do big dictionary creation on every object for each player)


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

    class Character
    {
        //CHARACTER: whats important?
        //NAME, TITLE, AGE, RACE, CLASS, HOMETOWN, STATS (STRENGTH, INTELLECT, CHARISMA, AGILITY)
        string Name;
        string title;
        int age;
        string race;
        string profession;
        string hometown;
        bool friendly; //This is determined by race, essentially if they are something like an orc they will be treated poorly by others
        List<int> stats = new List<int>();
        public Character(string questionAnswer)
        {

        }

    }


}
