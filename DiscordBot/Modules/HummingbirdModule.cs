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
                        await e.Channel.SendMessage(SearchForAnime(e.GetArg("Query"), e));
                    }
                });
            });
        }

        private string SearchForAnime(string query, CommandEventArgs e)
        {
            try
            {
                //Output Log Message
                Configuration.LogMessage("[Command] " + e.User.Name + " searched Hummingbird for '" + query + "'");

                //Construct our web request url
                string requestUrl = @"http://hummingbird.me/api/v1/search/anime/?query=";
                requestUrl += query.Trim().Replace(" ", "+");

                //Create the web request
                HttpWebRequest request = WebRequest.CreateHttp(requestUrl);

                //Get the response and dispose of it when we're done via 'using'
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    //Check to make sure it went through ok
                    //If it isnt, our status code wont be ok
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        //Throw an exception, it'll be caught by the catch down below
                        throw new Exception(String.Format("Server error (HTTP {0}: {1}).",
                                                          response.StatusCode,
                                                          response.StatusDescription));
                    }

                    Anime[] responseObj;

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
                            responseObj = ser.Deserialize<Anime[]>(jsonReader);
                        }
                    }

                    if (responseObj.Length <= 0)
                    {
                        Configuration.LogMessage("[Command] Couldn't find any anime with that query");
                        return "Cannot find any anime by that name";
                    }

                    Anime foundAnime = responseObj[0];

                    string message =
                        "**MAL:** " + foundAnime.mal_id + "\n" +
                        "**Title:** " + foundAnime.title + "\n" +
                        "**Status:** " + foundAnime.status + "\n" +
                        "**Episode Count:** " + foundAnime.episode_count + "\n" +
                        "**Community Rating:** " + foundAnime.community_rating + "\n" +
                        "**URL:** " + foundAnime.url + "\n" +
                        "**Synopsis:** " + foundAnime.synopsis + "\n" +
                        "**Genres:** ";

                    for(int i = 0; i < foundAnime.genres.Count(); i++)
                    {
                        message += "`" + foundAnime.genres[i].name + "`";
                        if (i != foundAnime.genres.Count() -1)
                        {
                            message += ", ";
                        }
                    }
                    return message;
                }
            }
            catch(Exception ex)
            {
                Configuration.LogMessage("[Command] Anime Search Failed, Exception: " + ex.Message);
                return ex.Message;
            }
        }
    }
}
