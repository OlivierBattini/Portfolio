/*
 *    (C) Copyright 2013 Olivier Battini (https://olivierbattini.fr)
 *
 *    ALL RIGHTS RESERVED
 *
 *    This file is only published for showcase purposes and use in source and
 *    binary forms, with or without modification, are not permitted in any way.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AIMLbot;
using AIMLbot.Utils;
using log4net;
using Mono.Addins;
using Nini.Config;
using System.Timers;

/*

    Some references removed

*/

namespace VirreaAIML4OS
{
    public class VirreaAIML4OS : ISharedRegionModule
    {
        /*

            Some code removed

        */
        #region AIML Chat Bot
        private string viBotChat(UUID hostID, UUID scriptID, string botName, UUID userId, string message)
        {
            Log("viBotChat called");
            if (!_BotsUsers.ContainsKey(userId))
                SetupBotAndUser(botName, userId);
            BotUser botUser = _BotsUsers[userId];
            Request chatRequest = new Request(message, botUser.User, botUser.Bot);
            Result chatResult = botUser.Bot.Chat(chatRequest);
            List<string> authorizedUsers = new List<string>();
            authorizedUsers.AddRange(_Config.GetString("CommandsAuthorizedUsers", "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries));
            if (authorizedUsers.Contains(userId.ToString()))
                return ProcessCommands(chatResult.Output);
            return chatResult.Output;
        }
        private string viBotSave(UUID hostID, UUID scriptID)
        {
            SaveUserSessions();
            return "";
        }
        private string viBotReload(UUID hostID, UUID scriptID)
        {
            _BotsUsers = new Dictionary<UUID, BotUser>();
            return "";
        }
        #endregion
        #region Utils
        private void SetupBotAndUser(string botName, UUID userId)
        {
            // Checking if a bot is instanciated for conversation with the user
            if (!_BotsUsers.ContainsKey(userId))
            {
                // Creating and initializing bot and user
                Bot newBot = new Bot();
                newBot.loadSettings(GetBotSettingsFileName(botName));
                User newUser = new User(userId.ToString(), newBot);
                // Loading AIML files for bot
                AIMLLoader loader = new AIMLLoader(newBot);
                loader.loadAIML(GetBotAimlDirectory(botName));
                // Loading existing user session (predicates) if any
                string sessionPath = GetUserSessionFileName(botName, userId);
                if (File.Exists(sessionPath))
                {
                    newUser.Predicates.loadSettings(sessionPath);
                }
                // Setting predicates if not existing
                UserAccount userAccount = _Scenes[0].UserAccountService.GetUserAccount(UUID.Zero, userId);
                //IGroupsModule groupsModule = _Scenes[0].RequestModuleInterface<IGroupsModule>();
                //groupsModule.GetGroupTitle(userAccount.PrincipalID);
                newUser.Predicates.addSetting(PREDICATE_NAME, userAccount.Name);
                newUser.Predicates.addSetting(PREDICATE_FIRSTNAME, userAccount.FirstName);
                newUser.Predicates.addSetting(PREDICATE_LASTNAME, userAccount.LastName);
                newUser.Predicates.addSetting(PREDICATE_EMAIL, userAccount.Email);
                newUser.Predicates.addSetting(PREDICATE_USERLEVEL, userAccount.UserLevel.ToString());
                newUser.Predicates.addSetting(PREDICATE_USERTITLE, userAccount.UserTitle);
                if (!newUser.Predicates.containsSettingCalled(PREDICATE_ALREADYMET)) newUser.Predicates.addSetting(PREDICATE_ALREADYMET, "false");
                _BotsUsers.Add(userId, new BotUser(botName, newBot, newUser));
            }
        }
        private string ProcessCommands(string chatOutput)
        {
            // TODO
            return chatOutput;
        }
        private void SaveUserSessions()
        {
            foreach (BotUser botUser in _BotsUsers.Values)
            {
                botUser.User.Predicates.DictionaryAsXML.Save(String.Format(@"{0}\{1}.session", GetBotSessionsDirectory(botUser.BotName), botUser.User.UserID));
                Log(String.Format("Saved user {0} {1}", botUser.User.Predicates.grabSetting("name"), botUser.User.UserID));
            }
            Log("User sessions saved");
        }
        private string GetBotSettingsFileName(string botName)
        {
            return String.Format(@"{0}\Settings.xml", GetBotConfigDirectory(botName));
        }
        private string GetUserSessionFileName(string botName, UUID userId)
        {
            return String.Format(@"{0}\{1}.session", GetBotSessionsDirectory(botName), userId.ToString());
        }
        private void Log(string message)
        {
            _Log.WarnFormat("[{0}] {1}", Name, message);
        }
        #endregion
        #region Classes
        private class BotUser
        {
            private string _BotName;
            private Bot _Bot;
            private User _User;
            public string BotName { get { return _BotName; } }
            public Bot Bot { get { return _Bot; } }
            public User User { get { return _User; } }
            public BotUser(string botName, Bot bot, User user)
            {
                _BotName = botName;
                _Bot = bot;
                _User = user;
            }
        }
        #endregion
    }
}
