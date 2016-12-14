using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Modules;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;

namespace DiscordBot
{
    class OverwatchModule : IModule
    {
        #region LootBox Profile Structs
        struct LootBox_ProfileResponse
        {
            public LootBox_ProfileInfo data;
        }

        struct LootBox_ProfileInfo
        {
            public string username;
            public int level;
            public LootBox_GameModes games;
            public LootBox_Playtime playtime;
            public string avatar;
            public LootBox_CompetitiveRank competitive;
            public string levelframe;
            public string star;
        }

        struct LootBox_GameModes
        {
            public LootBox_QuickplayMatchInfo quick;
            public LootBox_CompetitiveMatchInfo competitive;
        }

        struct LootBox_QuickplayMatchInfo
        {
            public string wins;
        }

        struct LootBox_CompetitiveMatchInfo
        {
            public string wins;
            public int lost;
            public string played;
        }

        struct LootBox_CompetitiveRank
        {
            public string rank;
            public string rank_img;
        }

        struct LootBox_Playtime
        {
            public string quick;
            public string competitive;
        }
        #endregion

        #region LootBox Hero Structs

        private string requestedHeroName = "Tracer";

        struct LootBox_HeroResponse
        {
            //[JsonProperty($"{requestedHeroName}")]
            public string hero;
        }

        #endregion

        enum HeroRoles { Offense, Defense, Tank, Support, None };

        struct OverwatchHero
        {
            public string name;
            public HeroRoles role;
        }

        List<OverwatchHero> owHeroes;

        DiscordClient discordClient;
        ModuleManager moduleManager;

        void IModule.Install(ModuleManager manager)
        {
            discordClient = manager.Client;
            moduleManager = manager;

            moduleManager.CreateCommands("", cgb =>
            {
                //Get the info for an Overwatch Profile
                cgb.CreateCommand("profileinfo")
                .Alias(new string[] { "getprofile", "profile" })
                .Description("Gets the given player's Overwatch profile information. Provided Battle.Net ID should be in the `name#id` format, eg. `Test#1234`")
                .Parameter("BattleID", ParameterType.Required)
                .Do(e =>
                {
                    if (!BotHandler.TestForThrottle("overwatchprofile", e.User.Name))
                    {
                        Configuration.LogMessage("[Command] " + e.User.Name + " is getting " + e.GetArg("BattleID") + "'s profile info");

                        Stopwatch sw = new Stopwatch();
                        sw.Start();

                        //Store a placeholder message to edit later
                        Message message = e.Channel.SendMessage("Requesting info from LootBox Servers, Please Wait...").Result;

                        //Get name
                        string name = e.GetArg("BattleID");

                        //Format name to match desired format
                        name = name.Replace('#', '-');

                        //Create request url
                        string requestUrl = @"https://api.lootbox.eu/pc/eu/" + name + "/profile";

                        LootBox_ProfileResponse response = BotHandler.PerformRESTCall<LootBox_ProfileResponse>(requestUrl);

                        message.Edit(FormatProfileResponse(response, sw.Elapsed));
                        sw.Stop();
                    }
                });

                /*
                cgb.CreateCommand("heroinfo")
                .Alias(new string[] { "gethero" })
                .Description("Gets the given player's stats with the specified hero for the specified game mode (either `competitiveplay` or `quickplay`, will default to competitive). Provided Battle.Net ID should be in `name#id` format, eg. `Test#1234`")
                .Parameter("BattleID", ParameterType.Required)
                .Parameter("HeroName", ParameterType.Required)
                .Parameter("GameMode", ParameterType.Optional)
                .Do(e =>
                {
                    if (!BotHandler.TestForThrottle("heroinfo", e.User.Name))
                    {
                        Configuration.LogMessage($"[Command] {e.User.Name} is getting info on {e.GetArg("BattleID")}'s {e.GetArg("GameMode")} {e.GetArg("HeroName")}");

                        //Start our API timer
                        Stopwatch sw = new Stopwatch();
                        sw.Start();

                        //Store a placeholder message to edit later
                        Message message = e.Channel.SendMessage("Requesting info from LootBox Servers, Please Wait...").Result;

                        //Get Args
                        string battleID = e.GetArg("BattleID").Replace('#', '-');
                        string gamemode = e.GetArg("GameMode");
                        if (string.IsNullOrWhiteSpace(gamemode))
                        {
                            gamemode = "competitiveplay";
                        }
                        string hero = e.GetArg("HeroName");

                        //Make sure the hero is in Title case
                        hero = hero[0].ToString().ToUpper() + hero.Substring(1).ToLower();

                        //Create request URL
                        string requestUrl = $"https://api.lootbox.eu/pc/eu/{battleID}/{gamemode}/hero/{hero}";

                        //TODO:
                        //Make response structs - http://stackoverflow.com/questions/13517792/deserializing-json-with-dynamic-keys
                        //Format response struct
                        //Edit message
                        Console.WriteLine("a");
                        dynamic responseObj = BotHandler.PerformRESTCall<dynamic>(requestUrl);
                        Console.WriteLine("b");

                        sw.Stop();
                    }
                });*/

                //Get Random Hero
                cgb.CreateCommand("randomhero")
                .Alias(new string[] { "rh", "hero" })
                .Description("Gives a random hero for the user to play, Optional parameter of `Offense`, `Defense`, `Tank`, and `Support`")
                .Parameter("Role", ParameterType.Optional)
                .Do(async e =>
                {
                    if (!BotHandler.TestForThrottle("randomhero", e.User.Name))
                    {
                        LoadOverwatchHeroes(BotHandler.Config.OverwatchHeroesFile);

                        HeroRoles roleFilter = HeroRoles.None;

                        //Get the Role Filter
                        if (!string.IsNullOrWhiteSpace(e.GetArg("Role")))
                        {
                            try
                            {
                                roleFilter = (HeroRoles)Enum.Parse(typeof(HeroRoles), e.GetArg("Role"), true);
                            }
                            catch
                            {
                                await e.Channel.SendMessage("I dont recognise the role " + e.GetArg("Role") + ", giving you a random hero of any role instread.");
                                roleFilter = HeroRoles.None;
                            }
                        }

                        if (roleFilter == HeroRoles.None)
                        {
                            Configuration.LogMessage("[Command] " + e.User.Name + " is requesting a random Overwatch Hero");
                            //Pick a random hero from our list
                            Random rand = new Random();
                            int randNum = rand.Next(0, owHeroes.Count);

                            string message = "You should play " + owHeroes[randNum].name;
                            await e.Channel.SendMessage(message);
                        }
                        else
                        {
                            Configuration.LogMessage("[Command] " + e.User.Name + " is requesting a random " + e.GetArg("Role") + " Overwatch Hero");
                            //Filter out that role classes into another List
                            List<OverwatchHero> roleDict = new List<OverwatchHero>();

                            roleDict = owHeroes.Where(x => x.role == roleFilter).ToList<OverwatchHero>();

                            if (roleDict.Count <= 0)
                            {
                                await e.Channel.SendMessage("Couldn't find any heroes");
                                return;
                            }

                            Random rand = new Random();
                            int randNum = rand.Next(0, roleDict.Count);

                            string message = "You should play the " + roleFilter.ToString().ToLower() + " hero " + roleDict[randNum].name;

                            await e.Channel.SendMessage(message);
                        }
                    }
                });                
            });
        }

        private string FormatProfileResponse(LootBox_ProfileResponse response, TimeSpan timeTaken)
        {
            string message = "**Battle.Net ID:** " + response.data.username + "\n" +
                             "**Level:** " + response.data.level + "\n" +
                             "**Avatar:** " + response.data.avatar + "\n" +
                             "**Current Competitive Season:**\n" +
                             "__Playtime:__ " + response.data.playtime.competitive + "\n" +
                             "__Competitive Rank:__ " + response.data.competitive.rank + "\n" +
                             "__Wins:__ " + response.data.games.competitive.wins + "\n" +
                             "__Losses:__" + response.data.games.competitive.lost + "\n" +
                             "**Quickplay:**\n" +
                             "__Playtime:__ " + response.data.playtime.quick + "\n" +
                             "__Wins:__ " + response.data.games.quick.wins + "\n\n" +
                             "*This API Call took: " + timeTaken.TotalSeconds + " seconds.*";

            return message;

        }

        private void LoadOverwatchHeroes(string filename)
        {
            if (File.Exists(filename))
            {
                string rawJson = "";
                rawJson = File.ReadAllText(filename);

                try
                {
                    owHeroes = JsonConvert.DeserializeObject<List<OverwatchHero>>(rawJson);
                }
                catch (Exception ex)
                {
                    Configuration.LogMessage("[Error] Overwatch Heroes file not structured correctly " + ex.Message);
                    owHeroes = new List<OverwatchHero>();
                }
            }
            else
            {
                Configuration.LogMessage("[Error] No Overwatch Heroes file found");
                owHeroes = new List<OverwatchHero>();
            }
        }
    }
}
