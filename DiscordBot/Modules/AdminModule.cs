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
        public static Dictionary<string, ulong> specificUsers;
        const string specificUserFileName = "DiscordBot_SpecificUsers.txt";

        ModuleManager moduleManager;
        DiscordClient discordClient;

        void IModule.Install(ModuleManager manager)
        {
            moduleManager = manager;
            discordClient = manager.Client;

            //Load in any specific users
            specificUsers = new Dictionary<string, ulong>();
            LoadImportantPeople();
            Configuration.LogMessage("[Setup] Loaded " + specificUsers.Count + " specific users");

            moduleManager.CreateCommands("admin", cmd =>
            {
                //Admin Commands

                //Add a specific suer to a file so we can reference them via commands
                cmd.CreateCommand("adduser")
                    .Alias(new string[] { "a" })
                    .Description("Adds a user to the specific users file")
                    .Parameter("User Name", ParameterType.Required)
                    .Parameter("User ID", ParameterType.Required)
                    .Do(async e =>
                    {
                    //Check that it's an admin
                    if (specificUsers.ContainsKey("dev") && e.User.Id == AdminModule.specificUsers["dev"] && !BotHandler.TestForThrottle("adminadduser", e.User.Name))
                        {
                            string userName = e.GetArg("User Name");
                            string userIDRaw = e.GetArg("User ID");
                            ulong userID;

                            //Check its a valid ulong
                            if (!ulong.TryParse(userIDRaw, out userID))
                            {
                                await e.Channel.SendMessage("Invalid User ID");
                                Configuration.LogMessage("[AdminCommand] " + e.User.Name + " gave an incorrect User ID");
                                return;
                            }

                            //Check it doesnt already exist
                            if (AdminModule.specificUsers.ContainsValue(userID) || AdminModule.specificUsers.ContainsKey(userName))
                            {
                                await e.Channel.SendMessage("User/ID Already Exists");
                                Configuration.LogMessage("[AdminCommand] " + e.User.Name + " gave an already added User ID");
                                return;
                            }

                            AdminModule.specificUsers.Add(userName, userID);
                            UpdateImportantPeopleFile();
                            await e.Channel.SendMessage("User " + userName + " with ID " + userID + " added to the specific user's file");
                            Configuration.LogMessage("[AdminCommand] " + e.User.Name + " Added User " + userName + " with ID " + userID + " to the specific user's file");
                            return;
                        }
                        else
                        {
                            await e.Channel.SendMessage("Sorry, only the dev can use these commands, or the 'dev' doesnt exist in the specific users file...");
                            Configuration.LogMessage("[AdminCommand] " + e.User.Name + " tried to use an admin command");
                            return;
                        }

                    });

                cmd.CreateCommand("getusers")
                    .Description("Shows a list of the current specific users and their IDs")
                    .Do(async e =>
                    {
                        //Check that it's an admin
                        if (specificUsers.ContainsKey("dev") && e.User.Id == AdminModule.specificUsers["dev"] && !BotHandler.TestForThrottle("admingetusers", e.User.Name))
                        {
                            string message = "";
                            for (int i = 0; i < specificUsers.Count; i++)
                            {
                                message += "Name: " + specificUsers.ElementAt(i).Key + " ID: " + specificUsers.ElementAt(i).Value + "\n";
                            }
                            await e.Channel.SendMessage(message);
                        }
                        else
                        {
                            await e.Channel.SendMessage("Sorry, only the dev can use these commands, or the 'dev' doesnt exist in the specific users file...");
                            Configuration.LogMessage("[AdminCommand] " + e.User.Name + " tried to use an admin command");
                            return;
                        }
                    });

                //Change what game he's playing (currently only for this session)
                cmd.CreateCommand("setgame")
                    .Description("Changes the bot's current game")
                    .Parameter("game", ParameterType.Required)
                    .Do(async e =>
                    {
                        if (specificUsers.ContainsKey("dev") && e.User.Id == AdminModule.specificUsers["dev"] && !BotHandler.TestForThrottle("adminsetgame", e.User.Name))
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
                        specificUsers.ContainsKey("dev") &&
                        e.User.Id == AdminModule.specificUsers["dev"] &&
                        !BotHandler.TestForThrottle("goodbye", e.User.Name))
                    {
                        Configuration.LogMessage("[Event] Posting Dramatic Goodbye as the Dev Kills me");
                        await e.Channel.SendMessage("Remember me, and tell my tale...for I....was...a bull");
                    }
                }
            };
        }

        private void LoadImportantPeople()
        {
            //Loads up the IDs of any important people needed for user-specific commands
            if (File.Exists(specificUserFileName))
            {
                string json = File.ReadAllText(specificUserFileName, Encoding.UTF8);
                Dictionary<string, ulong> tempDic = JsonConvert.DeserializeObject<Dictionary<string, ulong>>(json);
                if (tempDic != null)
                {
                    specificUsers = tempDic;
                }
            }
            else
            {
                File.Create(specificUserFileName).Close();
                if (!specificUsers.ContainsKey("dev"))
                {
                    specificUsers.Add("dev", 0);
                }
                UpdateImportantPeopleFile();
            }
        }

        private void UpdateImportantPeopleFile()
        {
            File.WriteAllText(specificUserFileName, JsonConvert.SerializeObject(specificUsers, Formatting.Indented));
        }
    }
}
