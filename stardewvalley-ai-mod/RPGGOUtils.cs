using StardewValley;
using StardewValley.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using v2api_client_csharp;
using StardewModdingAPI;
using System.Net.NetworkInformation;
namespace stardewvalley_ai_mod
{
    internal class RPGGOUtils
    {
        private static RPGGOClient? _client = null;
        private static RPGGOAPIGameConfig? _config = null;
        private static IMonitor? _monitor = null;


        private static Dictionary<string, string> rpggoNpcNameToId = new Dictionary<string, string>();
        private static Dictionary<string, string> rpggoNpcIdToName = new Dictionary<string, string>();

        public static void Init(RPGGOAPIGameConfig config, IMonitor monitor, string? testEndpoint = null)
        {
            if (_client == null)
            {
                if (testEndpoint == null)
                    _client = new RPGGOClient(config.apiKey);
                else
                    _client = new RPGGOClient(config.apiKey, testEndpoint);
                _config = config;
                _monitor = monitor;
            }
        }

        public static RPGGOClient GetClient()
        {
            if (_client == null)
            {
                throw new Exception("Need to init rpggo client first");
            }
            return _client;
        }

        public static async Task<Dictionary<string, string>> GetAllGameCharacters(string gameId)
        {
            var allChars = new Dictionary<string, string>();
            var gameMetadata = await RPGGOUtils.GetClient().GetGameMetadataAsync(gameId);

            foreach (var chap in gameMetadata.Data.Chapters)
            {
                foreach (var chr in chap.Characters)
                {
                    allChars[chr.Name] = chr.Id;
                }
            }
            return allChars;
        }
        private static void BeforeChapterSwitch(string action_msg, GameOngoingResponse? currentRsp)
        {
            Game1.chatBox.addMessage("", StrFormater.getWhiteSpaceColor());
            Game1.chatBox.addMessage(StrFormater.getFormatSecction("Congrats!"), StrFormater.getSystemColor());
            Game1.chatBox.addMessage(action_msg, StrFormater.getTextColor());
            Game1.chatBox.addMessage("", StrFormater.getWhiteSpaceColor());
        }

        private static void AfterChapterSwitch(string msg, GameOngoingResponse? currentRsp)
        {
            Game1.chatBox.addMessage("\n", StrFormater.getWhiteSpaceColor());
            Game1.chatBox.addMessage(StrFormater.getFormatSecction("Switch to New chapter"), StrFormater.getSystemColor());
            Game1.chatBox.addMessage($"Chapter < {currentRsp?.Data.Chapter.Name} > starts.", StrFormater.getTitleColor());
            Game1.chatBox.addMessage("Intro: " + currentRsp?.Data.Chapter.Background, StrFormater.getTextColor());
            Game1.chatBox.addMessage("", StrFormater.getWhiteSpaceColor());

            RefreshCharactersInChapter(currentRsp?.Data.Chapter);
        }

        private static void OnGameEnding(string msg)
        {
            Game1.chatBox.addMessage(StrFormater.getFormatSecction("Game Over!"), StrFormater.getSystemColor());
            Game1.chatBox.addMessage(msg, StrFormater.getTitleColor());
            Game1.chatBox.addMessage("", StrFormater.getWhiteSpaceColor());
        }

        private static void onGameErrorReceived(string msg)
        {
            Game1.chatBox.addMessage("server side error: " + msg, StrFormater.getSystemErrorColor());
            Game1.chatBox.addMessage("", StrFormater.getWhiteSpaceColor());
        }

        private static void onGameOutOfCoinsReceived(string msg)
        {
            Game1.chatBox.addMessage("Out of balance: " + msg, StrFormater.getOutOfBalanceColor());
            Game1.chatBox.addMessage("", StrFormater.getWhiteSpaceColor());
        }

        public static void RefreshCharactersInChapter(Chapter? chapter)
        {
            if (chapter == null)
                return;
            
            rpggoNpcNameToId.Clear();
            rpggoNpcIdToName.Clear();
            foreach (var chr in chapter.Characters)
            {
                _monitor?.Log($"[RPGGO.Init] Character id: {chr.Id} name: {chr.Name}");
                rpggoNpcNameToId[chr.Name] = chr.Id;
                rpggoNpcIdToName[chr.Id] = chr.Name;
            }
        }

        public static bool TryGetRPGGOCharacterNameById(string id, out string name)
        {
            if (rpggoNpcIdToName.TryGetValue(id, out name))
            {
                return true;
            }
            return false;
        }

        public static bool TryGetRPGGOCharacterIdByName(string name, out string id)
        {
            if (rpggoNpcNameToId.TryGetValue(name, out id))
            {
                return true;
            }
            return false;
        }

        public static bool IsRPGGOCharacterNameExist(string name)
        {
            return rpggoNpcNameToId.ContainsKey(name);
        }

        public static string RandomString(int length = 8)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[length];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            var finalString = new String(stringChars);
            return finalString;
        }

        public static async Task<string> SingleChatToNPC(string gameId, string sessionId, string chrId, string chatInput)
        {
            _monitor?.Log($"RPGGO.SingleChatToNPC npcName:{chrId} chatInput:{chatInput}");
            var source = new TaskCompletionSource<string>();
            var msgId = RandomString();
            await RPGGOUtils.GetClient().ChatSseAsync(chrId, gameId, chatInput, msgId, sessionId, (chrId, msg) =>
            {
                _monitor?.Log($"RPGGO.SingleChatToNPC response: {msg}");
                source.TrySetResult(msg);
            }, (_) => { }, BeforeChapterSwitch, AfterChapterSwitch, OnGameEnding, onGameErrorReceived, onGameOutOfCoinsReceived);
            return await source.Task;
        }
    }
}
