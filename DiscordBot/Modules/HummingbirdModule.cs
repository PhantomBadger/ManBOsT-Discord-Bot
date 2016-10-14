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
using System.Web;
using System.Net.Http;
using System.Net;
using System.Diagnostics;

namespace DiscordBot
{
    class HummingbirdModule : IModule
    {
        struct Genre
        {
            public string name;
        }
        struct Anime
        {
            public long id;
            public long mal_id;
            public string slug;
            public string status;
            public string url;
            public string title;
            public string alternate_title;
            public int episode_count;
            public int episode_length;
            public string cover_image;
            public string synopsis;
            public string show_type;
            public string started_airing;
            public string finished_airing;
            public double community_rating;
            public string age_rating;
            public Genre[] genres;
        }

        DiscordClient discordClient;
        ModuleManager moduleManager;

        void IModule.Install(ModuleManager manager)
        {
            discordClient = manager.Client;
            moduleManager = manager;

            moduleManager.CreateCommands("", cmd =>
            {
                //Search Anime
                cmd.CreateCommand("SearchAnime")
                .Alias(new string[] { "getanime", "anime" })
                .Parameter("Query", ParameterType.Required)
                .Do(async e =>
                {
                    if (!BotHandler.TestForThrottle("animesearch", e.User.Name))
                    {
                        Configuration.LogMessage("[Command] " + e.User.Name + " is searching for Anime using the query: " + e.GetArg("Query"));
                        
                        //Use a stopwatch for some diagnostics
                        Stopwatch sw = new Stopwatch();
                        sw.Start();

                        //Create the Request URL
                        string requestUrl = @"http://hummingbird.me/api/v1/search/anime/?query=";
                        requestUrl += e.GetArg("Query").Trim().Replace(" ", "+");

                        //Get the array response
                        Anime[] response = BotHandler.PerformRESTCall<Anime[]>(requestUrl);

                        //Make sure we actually /got/ a response
                        if (response == null || response.Length <= 0)
                        {
                            Configuration.LogMessage("[Command] Couldn't find any anime with that query");
                            await e.Channel.SendMessage("Cannot find any anime by that name");
                            return;
                        }

                        Anime foundAnime = response[0];

                        await e.Channel.SendMessage(FormatAnime(foundAnime, sw.Elapsed));

                        sw.Stop();
                    }
                });
            });
        }

        private string FormatAnime(Anime response, TimeSpan timeTaken)
        {
            string message = "**MAL:** " + response.mal_id + "\n" +
                             "**Title:** " + response.title + "\n" +
                             "**Status:** " + response.status + "\n" +
                             "**Episode Count:** " + response.episode_count + "\n" +
                             "**Community Rating:** " + response.community_rating + "\n" +
                             "**URL:** " + response.url + "\n" +
                             "**Synopsis:** " + response.synopsis + "\n" +
                             "**Genres:** ";

            for (int i = 0; i < response.genres.Count(); i++)
            {
                message += "`" + response.genres[i].name + "`";
                if (i != response.genres.Count() - 1)
                {
                    message += ", ";
                }
            }

            message += "\n\n*This API Call took: " + timeTaken.TotalSeconds + " seconds.*";
            return message;
        }
    }
}
