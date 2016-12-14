using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Modules;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Net;

namespace DiscordBot
{
    class BotHandler
    {
        private DiscordClient discordClient;
        public static Configuration Config { get; set; }

        const string configFileName = "DiscordBot_Config.txt";
        public static Dictionary<string, DateTime> CommandThrottle { get; set; }

        private DateTime launchTime;


        public void Launch()
        {
            //Load up the Config File
            Config = new Configuration();
            if (!File.Exists(configFileName))
            {
                //Make a new Config File
                Config.UserSetup();
            }
            else
            {
                //Load up the existing one
                Config.LoadConfig(configFileName);
                Configuration.LogMessage("[Setup] Loaded Config File!");
                
            }
            Config.WriteConfig(configFileName);

            //Create the client
            discordClient = new DiscordClient();

            //Setup the client's command stuff
            discordClient.UsingCommands(x =>
            {
                x.PrefixChar = Config.Prefix;
                x.HelpMode = HelpMode.Public;
            });

            //Display any log messages that arrive in the console window
            discordClient.Log.Message += (s, e) =>
            {
                Configuration.LogMessage("[" + e.Severity + "] " + e.Source + ": " + e.Message);
            };
            
            //Make sure everythings set up
            if (discordClient == null)
            {
                Configuration.LogMessage("[ERROR] Discord Client doesnt Exist!\n[ERROR] Here's an Aunty Donna Quote to help you in this time of need.\n[ERROR]\tYou never help your mother with the chicken tonight.\n[ERROR]\n[ERROR]\t...Chicken tonight..");
                return;
            }

            //Add Existing Modules
            discordClient.AddService<ModuleService>();
            discordClient.AddModule<AdminModule>();
            discordClient.AddModule<AuntyDonnaModule>();
            discordClient.AddModule<HummingbirdModule>();
            discordClient.AddModule<YouTubeModule>();
            discordClient.AddModule<OverwatchModule>();

            //Set up the command throttle
            CommandThrottle = new Dictionary<string, DateTime>();

            //Record the launch time
            launchTime = DateTime.Now;

            //Set up the Commands
            CommandSetup();

            //Set up the Events
            EventSetup();
        }

        private void CommandSetup()
        {
            //Display Version Number
            discordClient.GetService<CommandService>().CreateCommand("versionno")
                .Alias(new string[] { "version", "versionnumber" })
                .Description("Posts the current version number of the bot")
                .Do(async e =>
                {
                    if (!TestForThrottle("versionno", e.User.Name))
                    {
                        Configuration.LogMessage("[Command] " + e.User.Name + " is checking the version number");
                        await e.Channel.SendMessage("The current version number of ManBOsT is: " + Configuration.VersionNo);
                    }
                });

            //Display Uptime
            discordClient.GetService<CommandService>().CreateCommand("uptime")
                .Alias(new string[] { "up" })
                .Description("Posts how long the bot has been up")
                .Do(async e =>
                {
                    if (!TestForThrottle("uptime", e.User.Name))
                    {
                        TimeSpan uptime = DateTime.Now - launchTime;
                        Configuration.LogMessage("[Command] " + e.User.Name + " is checking the uptime");
                        await e.Channel.SendMessage("I have been up for " + uptime.ToString("d'd 'h'h 'm'm 's's'") + ", and was launched on " + launchTime.ToString());
                    }
                });

            //User Info
            discordClient.GetService<CommandService>().CreateCommand("userinfo")
                .Alias(new string[] { "user" })
                .Description("Get the info of the user mentioned, or the one who made the request if none was specified")
                .Parameter("UserTag", ParameterType.Optional)
                .Do(async e =>
                {
                    if (!TestForThrottle("userinfo", e.User.Name))
                    {
                        if (string.IsNullOrWhiteSpace(e.GetArg("UserTag")))
                        {
                            //Themselves
                            Configuration.LogMessage("[Command] " + e.User.Name + " is getting information about themselves");
                            await e.Channel.SendMessage(GetUserInfo(e.User));
                        }
                        else
                        {
                            //Other User
                            Configuration.LogMessage("[Command] " + e.User.Name + " is getting information about " + e.GetArg("UserTag"));
                            User target = e.Message.MentionedUsers.FirstOrDefault();

                            await e.Channel.SendMessage(GetUserInfo(target));
                        }
                    }
                });

            //Channel Info
            discordClient.GetService<CommandService>().CreateCommand("channelinfo")
                .Alias(new string[] { "channel" })
                .Description("Get the info of the channel mentioned, or the current one if none was specified")
                .Parameter("ChannelName", ParameterType.Optional)
                .Do(async e =>
                {
                    if(!TestForThrottle("channelinfo", e.User.Name))
                    {
                        if (string.IsNullOrWhiteSpace(e.GetArg("ChannelName")))
                        {
                            //This channel
                            Configuration.LogMessage("[Command] " + e.User.Name + " is getting information about " + e.Channel.Name);
                            await e.Channel.SendMessage(GetChannelInfo(e.Channel));
                        }
                        else
                        {
                            //Other channel
                            Configuration.LogMessage("[Command] " + e.User.Name + " is getting information about " + e.GetArg("ChannelName"));
                            Channel target = e.Message.MentionedChannels.FirstOrDefault();

                            await e.Channel.SendMessage(GetChannelInfo(target));
                        }
                    }
                });
        }

        private void EventSetup()
        {
            //Message Listening
            discordClient.MessageReceived += async (s, e) =>
            {
                //Make sure it's not itself
                if (!e.Message.IsAuthor)
                {
                    string text = e.Message.Text.ToLower();

                    //Check to see if the message has a key word we're listening for

                    //Hello
                    if (e.Message.IsMentioningMe() && 
                            (text.Contains("hello") ||
                             text.Contains("hi") ||
                             text.Contains("hey") ||
                             text.Contains("greetings traveller") ||
                             text.Contains("greeting") ||
                             text.Contains("hiya")))
                    {
                        if (!TestForThrottle("hello", e.User.Name))
                        {

                            Configuration.LogMessage("[Event] Saying Hello to " + e.User.Name);
                            await e.Channel.SendMessage("Hello, Little Girl " + e.User.Mention);
                        }
                    }
                }
            };

            //Listen for user update
            discordClient.UserUpdated += async (s, e) =>
            {
                if ((e.Before.VoiceChannel == null && e.After.VoiceChannel != null))
                {
                    //If the user has a specific message mapped print that
                    if (AdminModule.SpecificMessageMap.ContainsKey(e.After.Id))
                    {
                        //Print their message
                        Configuration.LogMessage($"[Event] Saying Specific Message for {e.After.Name}");
                        await e.After.Server.DefaultChannel.SendMessage($"{AdminModule.SpecificMessageMap[e.After.Id].Trim()} {e.After.Mention}");
                    }
                }
            };

            //Start our Listener Code
            discordClient.ExecuteAndWait(async () =>
            {
                //Keep trying to connect
                while (true)
                {
                    try
                    {
                        //Connect to the Discord server using our email and password
                        await discordClient.Connect(Config.DiscordToken, TokenType.Bot);

                        //Set the starting game
                        discordClient.SetGame("Petz 5");
                        Configuration.LogMessage("[Setup] Setting Game to " + discordClient.CurrentGame.Name);

                        Configuration.LogMessage("[Info] Ready!");

                        break;
                    }
                    catch (Exception ex)
                    {
                        discordClient.Log.Error("Login Failed", ex);
                        await Task.Delay(discordClient.Config.FailedReconnectDelay);
                    }
                }
            });
        }

        private string GetUserInfo(User user)
        {
            if (user != null)
            {
                //Format the user's information
                string message = "**Name:** " + user.Name + "\n" +
                     "**Server Nickname:** " + user.Nickname + "\n" +
                     "**Discord ID:** " + user.Id + "\n" +
                     "**Avatar:** " + user.AvatarUrl + "\n" +
                     "**Roles:** ";
                foreach (var role in user.Roles)
                {
                    message += "`" + role.Name + "` ";
                }

                return message;
            }
            else
            {
                return "Could not find a user by that name, make sure to mention the user requested with '@'";
            }
        }

        private string GetChannelInfo(Channel channel)
        {
            if (channel != null)
            {
                string message = "**Name:** " + channel.Name + "\n" +
                    "**Discord ID:** " + channel.Id + "\n" +
                    "**Topic:** " + channel.Topic + "\n" +
                    "**Position in Server:** " + channel.Position + "\n";
                if (channel.Messages.FirstOrDefault() != null)
                {
                    message += "**Latest Message:** `" + channel.Messages.FirstOrDefault() + "`";
                }

                return message;
            }
            else
            {
                return "Could not find a channel by that name, make sure to mention the channel requested with '#'";
            }
        }

        public static bool TestForThrottle(string commandName, string user)
        {
            if (CommandThrottle.ContainsKey(commandName))
            {
                //Throttle the command if it's less than our throttle limit
                if (DateTime.Now - CommandThrottle[commandName] > Config.ThrottleLimit)
                {
                    CommandThrottle[commandName] = DateTime.Now;
                    return false;
                }
                else
                {
                    Configuration.LogMessage("[Info] Throttling Command " + commandName + " from user " + user + " due to excessive calls");
                    return true;
                }
            }
            else
            {
                if (CommandThrottle == null)
                {
                    CommandThrottle = new Dictionary<string, DateTime>();
                }

                //Add the command to our dictionary
                CommandThrottle.Add(commandName, DateTime.Now);
                return false;
            }
        }

        public static T PerformRESTCall<T>(string requestUrl)
        {
            try
            {
                //create web request
                HttpWebRequest request = WebRequest.CreateHttp(requestUrl);

                //Get response
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        //Throw an exception, it'll be caught by the catch down below
                        throw new Exception(String.Format("Server error (HTTP {0}: {1}).",
                                                          response.StatusCode,
                                                          response.StatusDescription));
                    }

                    T responseObj;

                    //Attach to the response stream with a stream reader
                    using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                    {
                        //Use the Newtonsoft JSON Convert to deserialise it out of JSON
                        using (JsonTextReader jsonReader = new JsonTextReader(sr))
                        {
                            JsonSerializer ser = new JsonSerializer
                            {
                                NullValueHandling = NullValueHandling.Ignore,
                                MissingMemberHandling = MissingMemberHandling.Ignore
                            };
                            responseObj = ser.Deserialize<T>(jsonReader);
                        }
                    }

                    return responseObj;
                }
            }
            catch (Exception ex)
            {
                Configuration.LogMessage("[Error] REST API Exception: " + ex.Message);
                return default(T);
            }
        }
    }
}
