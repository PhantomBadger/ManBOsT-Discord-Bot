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
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace DiscordBot
{
    class BotHandler
    {
        DiscordClient discordClient;
        public static Configuration config;
        const string configFileName = "DiscordBot_Config.txt";

        public void Launch()
        {
            //Load up the Config File
            if (!File.Exists(configFileName))
            {
                //Make a new Config File
                config = new Configuration();
                config.UserSetup();
                config.WriteConfig(configFileName);
            }
            else
            {
                //Load up the existing one
                config = Configuration.LoadConfig(configFileName);
                Configuration.LogMessage("[Setup] Loaded Config File!");
            }

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

            //Set up the Commands
            CommandSetup();

            //Set up the Events
            EventSetup();
        }

        private void CommandSetup()
        {
            //Search YouTube
            discordClient.GetService<CommandService>().CreateCommand("YouTubeSearch")
                .Alias(new string[] { "yt", "youtube", "video" })
                .Description("Posts the first YouTube video found with the provided Query")
                .Parameter("Query", ParameterType.Required)
                .Do(e =>
                {
                    Configuration.LogMessage("[Command] " + e.User.Name + " is searching YouTube for: " + e.GetArg("Query"));
                    SearchYoutube(e.GetArg("Query"), e).Wait();
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
                             text.Contains("greeting")))
                    {
                        Configuration.LogMessage("[Event] Saying Hello to " + e.User.Name);
                        await e.Channel.SendMessage("Hello, Little Girl " + e.User.Mention);
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
                        Configuration.LogMessage("[Event] Saying Hi to Emma");
                        await e.After.Server.DefaultChannel.SendMessage("Hello, Slut! " + e.After.Mention);
                    }

                    //If Liam connects
                    if (AdminModule.specificUsers.ContainsKey("liam") && e.After.Id == AdminModule.specificUsers["liam"])
                    {
                        Configuration.LogMessage("[Event] Reminding Liam of UnrealScript");
                        await e.After.Server.DefaultChannel.SendMessage("How's that UnrealScript going " + e.Before.Mention + "?");
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
                        discordClient.SetGame("Catz 5");
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

        private async Task SearchYoutube(string query, CommandEventArgs e)
        {
            //Set up the YT Service
            YouTubeService ytService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = config.YouTubeToken,
                ApplicationName = this.GetType().ToString()
            });

            //Search using the query
            SearchResource.ListRequest searchListRequest = ytService.Search.List("snippet");
            searchListRequest.Q = query;

            //Get the search results
            var searchListResponse = await searchListRequest.ExecuteAsync();

            //Post the first one
            foreach (var searchResult in searchListResponse.Items)
            {
                if (searchResult.Id.Kind == "youtube#video")
                {
                    string url = "https://www.youtube.com/watch?v=" + searchResult.Id.VideoId;

                    Configuration.LogMessage("[Command] Posting YouTube Video " + url);

                    await e.Channel.SendMessage(url);
                    return;
                }
            }

            //We havent found any videos
            await e.Channel.SendMessage("Can't find any video with the query \"" + query + "\""); 
        }
    }
}
