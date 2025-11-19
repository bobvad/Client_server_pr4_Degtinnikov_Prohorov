using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;
using Newtonsoft.Json;

namespace Client
{
    public class Program
    {

        public static IPAddress IpAdress;
        public static int Port;
        public static int Id = -1;
        
        public static bool CheckCommand(string message)
        {
            bool BCommand = false;
            string[] DataMessage = message.Split(new string[1] { " " }, StringSplitOptions.None);

            if (DataMessage.Length > 0 )
            {
               string Command = DataMessage[0];
                if (Command == "connect")
                {
                    if(DataMessage.Length != 3)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Использование: connect [login] [password]\nПример: connect User1 P@ssw0rd");
                        BCommand = false;
                    }
                    else
                     BCommand = true;
                }
                else if(Command == "cd")
                    BCommand = true;

                else if(Command == "get")
                {
                    if (DataMessage.Length == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Использование: get [NameFile]\nПример: get Test.txt");
                        BCommand = false;
                    }
                    else
                        BCommand= true;
                }
                else if(Command == "set")
                {
                    if(DataMessage.Length == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Red; 
                        Console.WriteLine("Использование: set [NameFile]\nПример: set Test.txt");
                        BCommand = false;
                    }
                    else
                        BCommand = true;
                }
            }

            return BCommand;
        }
   
    }
}