# ManBOsT-Discord-Bot
A Discord Bot using Discord.Net, based off of the Australian Comedy Group "Aunty Donna". Some strong language inside.

# How to Build
I built this using Visual Studio, and can only guarantee that it will work with that
Not going to lie, it had a load of dependencies, but you should only need to manually get the following off of NuGet

- Discord.Net
- Newtonsoft.Json
- Google.Apis.YouTube.v3

Once the dependencies are all sorted, build the project as normal, on the first run it will create the config file and ask for your Discord and YouTube API tokens.
Once it has ran for the first time and created some of the files it needs, you may want to go into the "DiscordBot_SpecificUsers.txt" file and change the value of the 'dev' key/value pair to your Discord ID, this allows you to use admin commands. 

The SpecificUsers file allows you to reference specific users for certain commands. As this bot was made for personal use, there is reference to some of my friends (liam and emma) in the source code, this references the key/value pairs in the above file, and shouldnt do anything unless you add your own Liam/Emma.
