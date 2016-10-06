using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    class Program
    {
        static void Main(string[] args)
        {
            BotHandler bot = new BotHandler();
            bot.Launch();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
