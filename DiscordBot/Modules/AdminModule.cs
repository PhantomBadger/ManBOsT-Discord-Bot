using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Modules;
using Discord.Commands;
using System.IO;
using Newtonsoft.Json;

namespace DiscordBot
{
    class AdminModule : IModule
    {

        ModuleManager moduleManager;
        DiscordClient discordClient;

        public static Dictionary<ulong, string> SpecificMessageMap { get; set; }
        const string specificUserFileName = "DiscordBot_SpecificUsers.txt";

        void IModule.Install(ModuleManager manager)
        {
            moduleManager = manager;
            discordClient = manager.Client;

            //Load in any specific users
            SpecificMessageMap = new Dictionary<ulong, string>();
            LoadSpecificMessageMap();
            Configuration.LogMessage("[Setup] Loaded " + SpecificMessageMap.Count + " specific users");


            moduleManager.CreateCommands("admin", cmd =>
            {
                //Admin Commands

                //Add a specific suer to a file so we can reference them via commands
                cmd.CreateCommand("adduser")
                    .Alias(new string[] { "a" })
                    .Description("Adds a user to the specific users file")
                    .Parameter("User ID", ParameterType.Required)
                    .Parameter("User Message", ParameterType.Required)
                    .Do(async e =>
                    {
                    //Check that it's an admin
                        if (e.User.Id == BotHandler.Config.AdminID && !BotHandler.TestForThrottle("adminadduser", e.User.Name))
                        {
                            string userMessage = e.GetArg("User Message");
                            string userIDRaw = e.GetArg("User ID");
                            ulong userID;

                            //Check its a valid ulong
                            if (!ulong.TryParse(userIDRaw, out userID))
                            {
                                await e.Channel.SendMessage("Invalid User ID");
                                Configuration.LogMessage($"[AdminCommand] {e.User.Name} gave an incorrect User ID");
                                return;
                            }

                            //Check it doesnt already exist
                            if (SpecificMessageMap.ContainsKey(userID))
                            {
                                await e.Channel.SendMessage("ID Already Exists");
                                Configuration.LogMessage($"[AdminCommand] {e.User.Name} gave an already added User ID");
                                return;
                            }

                            SpecificMessageMap.Add(userID, userMessage);
                            UpdateSpecificMessageMap();
                            await e.Channel.SendMessage($"User ID {userID} with message: {userMessage} added to the specific user's file");
                            Configuration.LogMessage($"[AdminCommand] {e.User.Name} Added User {userID} with ID {userMessage} to the specific user's file");
                            return;
                        }
                        else
                        {
                            await e.Channel.SendMessage("Sorry, only the dev can use these commands, or the 'dev' doesnt exist in the specific users file...");
                            Configuration.LogMessage($"[AdminCommand] {e.User.Name} tried to use an admin command");
                            return;
                        }

                    });

                cmd.CreateCommand("getusers")
                    .Description("Shows a list of the current specific users and their IDs")
                    .Do(async e =>
                    {
                        //Check that it's an admin
                        if (e.User.Id == BotHandler.Config.AdminID && !BotHandler.TestForThrottle("admingetusers", e.User.Name))
                        {
                            string message = "";
                            for (int i = 0; i < SpecificMessageMap.Count; i++)
                            {
                                message += $"User: {SpecificMessageMap.ElementAt(i).Key} Message: {SpecificMessageMap.ElementAt(i).Value}\n";
                            }
                            await e.Channel.SendMessage(message);
                        }
                        else
                        {
                            await e.Channel.SendMessage("Sorry, only the dev can use these commands, or the 'dev' doesnt exist in the specific users file...");
                            Configuration.LogMessage($"[AdminCommand] {e.User.Name} tried to use an admin command");
                            return;
                        }
                    });

                //Change what game he's playing (currently only for this session)
                cmd.CreateCommand("setgame")
                    .Description("Changes the bot's current game")
                    .Parameter("game", ParameterType.Required)
                    .Do(async e =>
                    {
                        if (e.User.Id == BotHandler.Config.AdminID && !BotHandler.TestForThrottle("adminsetgame", e.User.Name))
                        {
                            string gameName = e.GetArg("game");

                            discordClient.SetGame(gameName);

                            await e.Channel.SendMessage("I am now playing " + gameName);
                            Configuration.LogMessage("[AdminCommand] " + e.User.Name + " Set the Bot's current game to " + gameName);
                            return;
                        }
                        else
                        {
                            await e.Channel.SendMessage("Sorry, only the dev can use these commands, or the 'dev' doesnt exist in the specific users file...");
                            Configuration.LogMessage("[AdminCommand] " + e.User.Name + " tried to use an admin command");
                            return;
                        }
                    });
            });

            discordClient.MessageReceived += async (s, e) =>
            {
                //Make sure it's not itself
                if (!e.Message.IsAuthor)
                {
                    //Dev Goodnight
                    if (e.Message.IsMentioningMe() &&
                        e.Message.Text.ToLower().Contains("goodnight") &&
                        e.User.Id == BotHandler.Config.AdminID &&
                        !BotHandler.TestForThrottle("goodbye", e.User.Name))
                    {
                        Configuration.LogMessage("[Event] Posting Dramatic Goodbye as the Dev Kills me");
                        await e.Channel.SendMessage("Remember me, and tell my tale...for I....was...a bull");
                    }
                }
            };
        }

        public void LoadSpecificMessageMap()
        {
            //Loads up the IDs of any important people needed for user-specific commands
            if (File.Exists(specificUserFileName))
            {
                string json = File.ReadAllText(specificUserFileName, Encoding.UTF8);
                Dictionary<ulong, string> tempDic = JsonConvert.DeserializeObject<Dictionary<ulong, string>>(json);
                if (tempDic != null)
                {
                    SpecificMessageMap = tempDic;
                }
            }
            else
            {
                File.Create(specificUserFileName).Close();
                SpecificMessageMap.Add(BotHandler.Config.AdminID, "Hello, Father");
                UpdateSpecificMessageMap();
            }
        }

        public void UpdateSpecificMessageMap()
        {
            File.WriteAllText(specificUserFileName, JsonConvert.SerializeObject(SpecificMessageMap, Formatting.Indented));
        }
    }
}
