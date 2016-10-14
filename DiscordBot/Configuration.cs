using System;
using System.Linq;
using System.IO;

namespace DiscordBot
{
    class Configuration
    {
        public char Prefix { get; set; } = '!';
        public string DiscordToken { get; set; }
        public string YouTubeToken { get; set; }
        public TimeSpan ThrottleLimit { get; set; } = new TimeSpan(0, 0, 2);
        public static int MaxLogCharLength { get; set; } = 70;
        public static string VersionNo { get; } = "1.3.2";
        public string OverwatchHeroesFile { get; set; } = "DiscordBot_OWHeroes.txt";
        public string ADVideoFile { get; set; } = "DiscordBot_AuntyDonnaVideos.txt";
        public string ADQuoteFile { get; set; } = "DiscordBot_AuntyDonnaQuotes.txt";
        public string ADActiveQuotesFile { get; set; } = "DiscordBot_AuntyDonnaActiveQuotes.txt";
        public int ADMaxVidCount { get; set; } = 100;

        public Configuration()
        {
            Prefix = '!';
            DiscordToken = "";
            YouTubeToken = "";
        }

        public void WriteConfig(string filename)
        {
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
                sw.WriteLine("// General");
                sw.WriteLine("Prefix:\t\t\t" + Prefix);
                sw.WriteLine("DiscordToken:\t\t" + DiscordToken);
                sw.WriteLine("YouTubeToken:\t\t" + YouTubeToken);
                sw.WriteLine("ThrottleLimit:\t\t" + ThrottleLimit.ToString());
                sw.WriteLine("MaxLogCharLength:\t" + MaxLogCharLength);
                sw.WriteLine();
                sw.WriteLine("// Overwatch");
                sw.WriteLine("HeroesFile:\t\t" + OverwatchHeroesFile);
                sw.WriteLine();
                sw.WriteLine("// AuntyDonna");
                sw.WriteLine("VideoFile:\t\t" + ADVideoFile);
                sw.WriteLine("QuoteFile:\t\t" + ADQuoteFile);
                sw.WriteLine("ActiveQuoteFile:\t" + ADActiveQuotesFile);
                sw.WriteLine("MaxVidCount:\t\t" + ADMaxVidCount);
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
                            case "heroesfile":
                                OverwatchHeroesFile = propVal;
                                break;
                            case "videofile":
                                ADVideoFile = propVal;
                                break;
                            case "quotefile":
                                ADQuoteFile = propVal;
                                break;
                            case "activequotefile":
                                ADActiveQuotesFile = propVal;
                                break;
                            case "maxvidcount":
                                int outVidCount;
                                if (!int.TryParse(propVal, out outVidCount))
                                {
                                    //Incorrectly formatted, so leave default
                                    break;
                                }
                                ADMaxVidCount = outVidCount;
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
