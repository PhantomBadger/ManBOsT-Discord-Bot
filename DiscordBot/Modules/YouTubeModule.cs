using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Modules;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace DiscordBot
{
    class YouTubeModule : IModule
    {
        ModuleManager moduleManager;
        DiscordClient discordClient;

        void IModule.Install(ModuleManager manager)
        {
            moduleManager = manager;
            discordClient = manager.Client;

            moduleManager.CreateCommands("", cgb =>
            {
                //Search YouTube
                discordClient.GetService<CommandService>().CreateCommand("YouTubeSearch")
                    .Alias(new string[] { "yt", "youtube", "video" })
                    .Description("Posts the first YouTube video found with the provided Query")
                    .Parameter("Query", ParameterType.Required)
                    .Do(e =>
                    {
                        if (!BotHandler.TestForThrottle("youtubesearch", e.User.Name))
                        {
                            Configuration.LogMessage("[Command] " + e.User.Name + " is searching YouTube for: " + e.GetArg("Query"));
                            SearchYoutube(e.GetArg("Query"), e).Wait();
                        }
                    });
            });
        }
        private async Task SearchYoutube(string query, CommandEventArgs e)
        {
            //Set up the YT Service
            YouTubeService ytService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = BotHandler.config.YouTubeToken,
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
