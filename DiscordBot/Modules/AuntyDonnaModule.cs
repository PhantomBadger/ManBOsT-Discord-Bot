using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Modules;
using Discord.Commands;
using System.IO;
using Google.Apis.YouTube.v3;
using Newtonsoft.Json;
using Google.Apis.Services;
using System.Text.RegularExpressions;

namespace DiscordBot
{
    class AuntyDonnaModule : IModule
    {
        private ModuleManager moduleManager;
        private DiscordClient discordClient;

        const string adVideoFileName = "DiscordBot_AuntyDonnaVideos.txt";
        TimeSpan maxTimeStampAge = new TimeSpan(14, 0, 0, 0); //A week before reloading
        List<string> auntyDonnaVideoCollection;
        int maxVideoCount = 100;

        const string adQuoteFileName = "DiscordBot_AuntyDonnaQuotes.txt";
        const string adActiveQuoteFileName = "DiscordBot_AuntyDonnaActiveQuotes.txt";
        public const string pendingReplyFormat = @"^ *[yn] \d* *$";
        Dictionary<ulong, string> activeQuotes;

        void IModule.Install(ModuleManager manager)
        {
            moduleManager = manager;
            discordClient = manager.Client;

            //Get the Aunty Donna Video List
            LoadAuntyDonnaVideoList();

            //Set up the active quotes list
            activeQuotes = new Dictionary<ulong, string>();
            LoadActiveQuotesFile();

            moduleManager.CreateCommands("", cmd =>
            {
                //Random Aunty Donna Quote
                cmd.CreateCommand("AuntyDonna")
                    .Alias(new string[] { "ad", "cum" })
                    .Description("Posts a random Aunty Donna quote from my limited library")
                    .Do(async e =>
                    {
                        Configuration.LogMessage("[Command] " + e.User.Name + " is requesting a quote");
                        await e.Channel.SendMessage(GetRandomAuntyDonnaQuote());
                    });

                //Random Aunty Donna Video
                cmd.CreateCommand("AuntyDonnaVid")
                    .Alias(new string[] { "adv" })
                    .Description("Posts a random Aunty Donna video")
                    .Do(async e =>
                    {
                        Configuration.LogMessage("[Command] " + e.User.Name + " is requesting a video");
                        await e.Channel.SendMessage("What do you think, of this?");
                        await e.Channel.SendMessage(GetRandomAuntyDonnaVideo());
                    });

                //Suggest AD Quote
                cmd.CreateCommand("AuntyDonnaSuggestion")
                    .Alias(new string[] { "suggest", "suggestion", "ads", "cum?" })
                    .Description("Suggest an Aunty Donna quote to be added to the list. Quote requires Approval.")
                    .Parameter("Quote", ParameterType.Required)
                    .Do(async e =>
                    {
                        Configuration.LogMessage("[Command] " + e.User.Id + " has suggested the quote " + e.GetArg("Quote"));
                        await e.Channel.SendMessage(SuggestAuntyDonnaQuote(e.GetArg("Quote"), e));
                    });

                //See all pending quotes
                cmd.CreateCommand("PendingQuotes")
                    .Alias(new string[] { "adp", "pending" })
                    .Description("Posts all Aunty Donna Quotes Pending Approval")
                    .Do(async e =>
                    {
                        Configuration.LogMessage("[Command] " + e.User.Name + " is requesting a list of pending quotes");
                        await e.Channel.SendMessage(DisplayPendingAuntyDonnaQuotes());
                    });
            });

            discordClient.MessageReceived += async (s, e) =>
            {
                //Make sure it's not itself
                if (!e.Message.IsAuthor)
                {
                    string text = e.Message.Text;

                    //Check to see if the message has a key word we're listening for

                    //Discuss
                    if (text.ToLower().Contains("discuss"))
                    {
                        Configuration.LogMessage("[Event] Someone said 'Discuss' so I posted a meme");
                        await e.Channel.SendMessage("I'll treat you like a discus you piece of shit!");
                    }

                    //Private message to me
                    if (e.Message.Channel.IsPrivate)
                    {
                        //If it is from the dev
                        if (AdminModule.specificUsers.ContainsKey("dev") && e.Message.User.Id == AdminModule.specificUsers["dev"])
                        {
                            //Check to see if the message follows the format required to approve a quote
                            if (Regex.IsMatch(e.Message.Text, AuntyDonnaModule.pendingReplyFormat))
                            {
                                //Get the name of the user and whether it was approved or not
                                string result = e.Message.Text.Trim().Substring(0, 1);
                                string userIdRaw = e.Message.Text.Trim().Substring(1).Trim();
                                ulong userId;
                                if (!ulong.TryParse(userIdRaw, out userId))
                                {
                                    Configuration.LogMessage("[ERROR] Invalid user ID " + userIdRaw);
                                    await e.Channel.SendMessage("Invalid user ID provided, seems something went wrong...");
                                    return;
                                }

                                User user;
                                foreach (var server in discordClient.Servers)
                                {
                                    if ((user = server.GetUser(userId)) != null)
                                    {
                                        //We found the user
                                        await e.Channel.SendMessage(ResolveAuntyDonnaSuggestion(user, result));
                                        return;
                                    }
                                }

                                //Can't find the user, something went awry
                                Configuration.LogMessage("[ERROR] Cannot Find User " + userId);
                                await e.Channel.SendMessage("Can't seem to find that user, seems something went wrong...");
                            }
                        }
                    }
                }
            };
        }

        private string GetRandomAuntyDonnaQuote()
        {
            List<string> quotes = new List<string>();

            using (StreamReader sr = new StreamReader(adQuoteFileName))
            {
                while (sr.Peek() >= 0)
                {
                    quotes.Add(sr.ReadLine());
                }
            }

            if (quotes.Count <= 0)
            {
                return "No Quotes Available. Suggest some or contact the dev";
            }

            Random rnd = new Random();
            int i = rnd.Next(quotes.Count);

            string quote;
            while (String.IsNullOrWhiteSpace((quote = quotes[i])))
            {
                i = rnd.Next(quotes.Count);
            }

            Configuration.LogMessage("[Command] Posted Random Quote: " + quote);
            return quote;
        }

        private string GetRandomAuntyDonnaVideo()
        {
            Random rnd = new Random();
            int i = rnd.Next(auntyDonnaVideoCollection.Count);

            Configuration.LogMessage("[Command] Posted Random Video: " + auntyDonnaVideoCollection[i]);
            return auntyDonnaVideoCollection[i];
        }

        private string SuggestAuntyDonnaQuote(string quote, CommandEventArgs e)
        {
            //Check if this user already has a quote for consideration
            if (activeQuotes.ContainsKey(e.User.Id))
            {
                return "You may only have one quote available for consideration at a time. You fuck.";
            }
            //Send a message to the Dev ID
            if (AdminModule.specificUsers.ContainsKey("dev"))
            {
                e.Channel.GetUser(AdminModule.specificUsers["dev"]).SendMessage(e.User + " (" + e.User.Id + ") has suggested the quote:\n" + quote +
                    "\nPlease reply with either 'y' or 'n' followed by the User Id who suggested the quote, eg 'y 124225617133568001'");
                activeQuotes.Add(e.User.Id, quote);
                UpdateActiveQuotesFile();

                return "Your Quote has been sent to an Admin for approval. You fuck.";
            }
            else
            {
                return "There is no 'dev' in the specific users file, please add one";
            }
        }

        private string ResolveAuntyDonnaSuggestion(User user, string response)
        {
            //Check if the quote was allowed or not, if so, add to list and remove from active
            //Return reply to dev & message user
            if (response.Trim().ToLower() == "y")
            {
                //Quote approved
                string quote = activeQuotes[user.Id];

                //Write to file
                using (StreamWriter sw = new StreamWriter(adQuoteFileName, true))
                {
                    sw.WriteLine(quote);
                }

                //Remove from active quotes
                activeQuotes.Remove(user.Id);
                UpdateActiveQuotesFile();

                //Send user a confirmed message
                user.SendMessage("Your quote suggestion:\n\"" + quote + "\"\nHas been approved!");
                Configuration.LogMessage("[Command] " + user.Id + "'s quote \"" + quote + "\" has been approved");

                return "Quote Approved!";
            }
            else if (response.Trim().ToLower() == "n")
            {
                //Quote rejected
                string quote = activeQuotes[user.Id];
                activeQuotes.Remove(user.Id);
                UpdateActiveQuotesFile();

                //Send user a confirmed message
                user.SendMessage("Your quote suggestion:\n\"" + quote + "\"\nHas been denied!");
                Configuration.LogMessage("[Command] " + user.Id + "'s quote \"" + quote + "\" has been denied");

                return "Quote Denied. What a fuckwit.";
            }
            else
            {
                return "That is not a valid character. Please enter y or n";
            }
        }

        private string DisplayPendingAuntyDonnaQuotes()
        {
            if (activeQuotes.Count <= 0)
            {
                return "There are no pending quotes!";
            }

            string message = "The currently active quotes are:\n";
            for (int i = 0; i < activeQuotes.Count; i++)
            {
                message += activeQuotes.ElementAt(i).Key + " : \"" + activeQuotes.ElementAt(i).Value + "\"\n";
            }
            return message;
        }

        private void LoadActiveQuotesFile()
        {
            if (File.Exists(adActiveQuoteFileName))
            {
                string json = File.ReadAllText(adActiveQuoteFileName, Encoding.UTF8);
                Dictionary<ulong, string> tempDic = JsonConvert.DeserializeObject<Dictionary<ulong, string>>(json);
                if (tempDic != null)
                {
                    activeQuotes = tempDic;
                }
            }
        }

        private void UpdateActiveQuotesFile()
        {
            File.WriteAllText(adActiveQuoteFileName, JsonConvert.SerializeObject(activeQuotes, Formatting.Indented));
        }

        private void LoadAuntyDonnaVideoList()
        {
            //Check if the file exists
            if (!File.Exists(adVideoFileName))
            {
                //Load up the videos and make the file
                GetAllAuntyDonnaVideos().Wait();
            }

            using (StreamReader sr = new StreamReader(adVideoFileName))
            {
                //First get the date, if it is from too long ago, we update the list
                DateTime lastWriteTime;
                if (!DateTime.TryParse(sr.ReadLine(), out lastWriteTime))
                {
                    Configuration.LogMessage("[ERROR] Time Stamp in Aunty Donna Video File isn't valid! Reloading files");

                    //Re-load in the urls
                    GetAllAuntyDonnaVideos().Wait();
                }

                //Check how long ago the timestamp is
                TimeSpan timeStampAge = DateTime.Now - lastWriteTime;
                if (timeStampAge > maxTimeStampAge)
                {
                    Configuration.LogMessage("[Setup] Video Time Stamp from too long ago, reloading files");
                    //We need to reload in the videos, it's been too long brother!
                    GetAllAuntyDonnaVideos().Wait();
                }

                //just grab the files and add them to our list
                auntyDonnaVideoCollection = new List<string>();

                while (sr.Peek() >= 0)
                {
                    auntyDonnaVideoCollection.Add(sr.ReadLine());
                }
            }

            Configuration.LogMessage("[Setup] " + auntyDonnaVideoCollection.Count + " Aunty Donna Videos loaded into the list");
        }

        private async Task GetAllAuntyDonnaVideos()
        {
            Configuration.LogMessage("[Setup] Getting around " + maxVideoCount + " total Aunty Donna Videos at the Ready!");
            //Set up the YT Service
            YouTubeService ytService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = BotHandler.config.YouTubeToken,
                ApplicationName = this.GetType().ToString()
            });

            //Search only for the Aunty Donna Channel
            SearchResource.ListRequest searchListRequest = ytService.Search.List("snippet");
            searchListRequest.ChannelId = "UC_mneEC0wc29EGGmIsN_xLA";

            List<string> localCollection = new List<string>();

            bool repeating = false;

            while (localCollection.Count <= maxVideoCount && !repeating)
            {
                repeating = true;

                //Execute Search
                var searchListResponse = await searchListRequest.ExecuteAsync();

                //Check theres still videos to add
                if (searchListResponse.Items.Count <= 0)
                {
                    break;
                }

                //Add each result to the list, then display
                foreach (var searchResult in searchListResponse.Items)
                {
                    if (searchResult.Id.Kind == "youtube#video")
                    {
                        string url = "https://www.youtube.com/watch?v=" + searchResult.Id.VideoId;
                        if (!localCollection.Contains(url))
                        {
                            //We consider it repeating once an entire page has given us existing videos
                            repeating = false;
                            localCollection.Add(url);
                        }
                        //LogMessage("https://www.youtube.com/watch?v=" + sr.Id.VideoId);
                    }
                }

                searchListRequest.PageToken = searchListResponse.NextPageToken;
            }

            Configuration.LogMessage("[Setup] " + localCollection.Count + " Aunty Donna Videos collected");

            //Write to file
            using (StreamWriter sw = new StreamWriter(adVideoFileName, false))
            {

                //Add the current date so we can check how long ago the file was updated
                sw.WriteLine(DateTime.Now);

                foreach (string url in localCollection)
                {
                    sw.WriteLine(url);
                }

                Configuration.LogMessage("[Setup] " + localCollection.Count + " Aunty Donna Videos written to file");
            }
        }
    }
}
