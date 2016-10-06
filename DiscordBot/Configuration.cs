using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace DiscordBot
{
    class Configuration
    {
        public char Prefix { get; set; }
        public string DiscordToken { get; set; }
        public string YouTubeToken { get; set; }

        public const int maxLogLength = 70;
        public const string versionNo = "1.0";

        public Configuration()
        {
            Prefix = '!';
            DiscordToken = "";
            YouTubeToken = "";
        }

        public void WriteConfig(string filename)
        {
            if (!File.Exists(filename))
            {
                File.Create(filename).Close();
            }

            File.WriteAllText(filename, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static Configuration LoadConfig(string filename)
        {
            string json = File.ReadAllText(filename);
            return JsonConvert.DeserializeObject<Configuration>(json);
        }

        public void UserSetup()
        {
            Configuration.LogMessage("[Config] New Config File Setup!");
            Configuration.LogMessage("[Config] Please Enter the Discord Token: ");
            DiscordToken = Console.ReadLine();
            Configuration.LogMessage("[Config] Please Enter the YouTube Token: ");
            YouTubeToken = Console.ReadLine();
            return;
        }

        public static void LogMessage(string message)
        {
            if (message.Length > maxLogLength)
            {
                Console.WriteLine(DateTime.Now.ToString() + " " + message.Substring(0, maxLogLength) + "...");
            }
            else
            {
                Console.WriteLine(DateTime.Now.ToString() + " " + message);
            }
        }
    }
}
