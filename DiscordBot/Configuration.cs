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
        public TimeSpan ThrottleLimit { get; set; }
        public static int MaxLogCharLength { get; set; } = 70;
        public static double VersionNo { get; set; } = 1.1;

        public Configuration()
        {
            Prefix = '!';
            DiscordToken = "";
            YouTubeToken = "";
            ThrottleLimit = new TimeSpan(0, 0, 2);
        }

        public void WriteConfig(string filename)
        {
            /*
            if (!File.Exists(filename))
            {
                File.Create(filename).Close();
            }
            
            File.WriteAllText(filename, JsonConvert.SerializeObject(this, Formatting.Indented));
            */
            using (StreamWriter sw = new StreamWriter(filename))
            {
                //Write the header
                sw.WriteLine("///////////////////////////////////////////////////////");
                sw.WriteLine("// Discord Bot ManBOsT Version " + VersionNo);
                sw.WriteLine("// Written by Josh Leland - www.phantombadger.com");
                sw.WriteLine("// Lines with // are commented out and not parsed");
                sw.WriteLine("// All properties should be a key value pair seperated by :");
                sw.WriteLine("// Any incorrectly formatted properties will be ignored");
                sw.WriteLine("///////////////////////////////////////////////////////");
                sw.WriteLine();
                sw.WriteLine("Prefix:\t\t\t" + Prefix);
                sw.WriteLine("DiscordToken:\t\t" + DiscordToken);
                sw.WriteLine("YouTubeToken:\t\t" + YouTubeToken);
                sw.WriteLine("ThrottleLimit:\t\t" + ThrottleLimit.ToString());
                sw.WriteLine("MaxLogCharLength:\t" + MaxLogCharLength);
            }
        }

        public void LoadConfig(string filename)
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    //Check if it's a comment or whitespace
                    if (line.StartsWith("//") || string.IsNullOrWhiteSpace(line))
                    {
                        //Go to next line
                        continue;
                    }
                    else
                    {
                        string[] splitLine = line.Split(':');
                        if (splitLine.Count() != 2)
                        {
                            //If there isnt just a key and value, then it's not correctly formatted so skip
                            continue;
                        }

                        string propName = splitLine[0].Trim();
                        string propVal = splitLine[1].Trim();

                        //Assign the value to our config
                        switch(propName.ToLower())
                        {
                            case "prefix":
                                Prefix = propVal[0];
                                break;
                            case "discordtoken":
                                DiscordToken = propVal;
                                break;
                            case "youtubetoken":
                                YouTubeToken = propVal;
                                break;
                            case "throttlelimit":
                                TimeSpan outThrottleLimit;
                                if (!TimeSpan.TryParse(propVal, out outThrottleLimit))
                                {
                                    //Incorrectly formatted, so let's just leave it default
                                    break;
                                }
                                ThrottleLimit = outThrottleLimit;
                                break;
                            case "maxlogcharlength":
                                int outLogCharLength;
                                if (!int.TryParse(propVal, out outLogCharLength))
                                {
                                    //Incorrectly formatted, so lets just leave it default
                                    break;
                                }
                                MaxLogCharLength = outLogCharLength;
                                break;

                        }
                    }
                }

            }
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
            if (message.Length > MaxLogCharLength)
            {
                Console.WriteLine(DateTime.Now.ToString() + " " + message.Substring(0, MaxLogCharLength) + "...");
            }
            else
            {
                Console.WriteLine(DateTime.Now.ToString() + " " + message);
            }
        }
    }
}
