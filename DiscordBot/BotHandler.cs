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

namespace DiscordBot
{
    class BotHandler
    {
        DiscordClient discordClient;
        public static Configuration config;

        const string configFileName = "DiscordBot_Config.txt";
        public static Dictionary<string, DateTime> commandThrottle;

        private DateTime launchTime;


        public void Launch()
        {
            //Load up the Config File
            config = new Configuration();
            if (!File.Exists(configFileName))
            {
                //Make a new Config File
                config.UserSetup();
            }
            else
            {
                //Load up the existing one
                config.LoadConfig(configFileName);
                Configuration.LogMessage("[Setup] Loaded Config File!");
                
            }
            config.WriteConfig(configFileName);

            //Create the client
            discordClient = new DiscordClient();

            //Setup the client's command stuff
            discordClient.UsingCommands(x =>
            {
                x.PrefixChar = config.Prefix;
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

            //Set up the command throttle
            commandThrottle = new Dictionary<string, DateTime>();

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
                        if (string.IsNullOrWhiteSpace(e.Args[0]))
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
                        if (string.IsNullOrWhiteSpace(e.Args[0]))
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
                    //If Emma connects
                    if (AdminModule.specificUsers.ContainsKey("emma") && e.After.Id == AdminModule.specificUsers["emma"])
                    {
                        if (!TestForThrottle("helloemma", e.After.Name))
                        {
                            Configuration.LogMessage("[Event] Saying Hi to Emma");
                            await e.After.Server.DefaultChannel.SendMessage("Hello, Slut! " + e.After.Mention);
                        }
                    }

                    //If Liam connects
                    if (AdminModule.specificUsers.ContainsKey("liam") && e.After.Id == AdminModule.specificUsers["liam"])
                    {
                        if (!TestForThrottle("helloliam", e.After.Name))
                        {
                            Configuration.LogMessage("[Event] Reminding Liam of UnrealScript");
                            await e.After.Server.DefaultChannel.SendMessage("How's that UnrealScript going " + e.Before.Mention + "?");
                        }
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
                        await discordClient.Connect(config.DiscordToken, TokenType.Bot);

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
            if (commandThrottle.ContainsKey(commandName))
            {
                //Throttle the command if it's less than our throttle limit
                if (DateTime.Now - commandThrottle[commandName] > config.ThrottleLimit)
                {
                    commandThrottle[commandName] = DateTime.Now;
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
                if (commandThrottle == null)
                {
                    commandThrottle = new Dictionary<string, DateTime>();
                }

                //Add the command to our dictionary
                commandThrottle.Add(commandName, DateTime.Now);
                return false;
            }
        }
    }
}
