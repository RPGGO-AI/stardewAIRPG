using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Characters;
using v2api_client_csharp;
using static StardewValley.Minigames.TargetGame;

namespace stardewvalley_ai_mod
{
    public class RPGGO
    {
        private bool inited = false;
        private bool initing = false;
        private string targetNPCName = "";
        private string chatInput = "";
        private Mod mod;

        private Dictionary<string, NPC> npcCache = new Dictionary<string, NPC>();

        private RPGGOClient client;
        private string gameId => config.gameId;
        private string apiKey => config.apiKey;
        private string sessionId => config.sessionId;
        private string dmId => config.dmId;

        private ModConfig config;

        private Dictionary<string, string> npcNameToId = new Dictionary<string, string>();
        private Dictionary<string, string> npcIdToName = new Dictionary<string, string>();

        private Dictionary<string, string> chineseNameToEnglishName = new Dictionary<string, string>
        {
            { "塞巴斯蒂安", "sebastian" }, { "潘妮", "penny" }, { "阿比盖尔", "abigail" }, { "山姆", "sam" }
        };

        private Regex affectionRegex = new Regex("(\\w+?)'s.+?(\\d+)%");
        private double lastEmoteTime;
        private bool lastFrameChatBoxActive;

        public RPGGO(Mod mod, ModConfig config)
        {
            this.mod = mod;
            this.config = config;
        }

        public void OnButtonReleased(ButtonReleasedEventArgs e)
        {
            if (e.Button == SButton.R)
            {
                var lowerNPCName = targetNPCName.ToLower();
                Log($"Release R npcName:{lowerNPCName} npcCache.count:{npcCache.Count}");
                if (npcCache.TryGetValue(lowerNPCName, out var npc))
                {
                    Log($"name:{GetNPCName(npc)} speed:{npc.speed} addedSpeed:{npc.addedSpeed}");
                    npc.addedSpeed = -npc.speed;
                    Game1.chatBox.activate();
                } else
                {
                    Log($"Not found in npcCache name:{lowerNPCName}");
                }
            }
        }

        public async Task Init()
        {
            if (!Context.IsWorldReady) return;
            
            if (inited) return;

            if (initing) return;

            initing = true;

            Game1.chatBox.chatBox.OnEnterPressed += ChatBox_OnEnterPressed;

            // request game data
            Log($"[RPGGO.Init] new client with apiKey:{apiKey}");
            client = new RPGGOClient(apiKey);
            var gameMetadata = await client.GetGameMetadataAsync(gameId);

            Log($"[RPGGO.Init] Game Name: {gameMetadata.Data.Name}");
            Log($"[RPGGO.Init] Intro: {gameMetadata.Data.Intro}");
            Log($"[RPGGO.Init] Chapters: {gameMetadata.Data.Chapters.Count}");

            foreach (var chr in gameMetadata.Data.Chapters[0].Characters)
            {
                Log($"[RPGGO.Init] Character id: {chr.Id} name: {chr.Name}");
                npcNameToId[chr.Name.ToLower()] = chr.Id;
                npcIdToName[chr.Id] = chr.Name;
            }

            Log("[RPGGO.Init] Get game metadata");
            Log($"[RPGGO.Init] sessionId: {sessionId}");

            Game1.chatBox.addMessage(gameMetadata.Data.Chapters[0].Name, Microsoft.Xna.Framework.Color.Cyan);
            Game1.chatBox.addMessage(gameMetadata.Data.Chapters[0].Background, Microsoft.Xna.Framework.Color.Cyan);
            Game1.chatBox.addMessage(" ", Microsoft.Xna.Framework.Color.Cyan);
            Game1.chatBox.addMessage(" ", Microsoft.Xna.Framework.Color.Cyan);
            Game1.chatBox.addMessage(" ", Microsoft.Xna.Framework.Color.Cyan);

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                config.sessionId = RandomString();
                mod.Helper.WriteConfig<ModConfig>(config);
                Log($"[RPGGO.Init] Start game sessionId: {sessionId}");
                await client.StartGameAsync(gameId, sessionId);
            }
            else
            {
                Log($"[RPGGO.Init] Resume game sessionId: {sessionId}");
                await client.ResumeSessionAsync(gameId, sessionId);
            }

            inited = true;
            initing = false;
            Log("[RPGGO.Init] Inited");
        }

        private string RandomString(int length=8)
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

        private async Task ChatToNPC(string characterId, string msg, Action<string, string> MessageCallback)
        {
            var msgId = RandomString();
            await client.ChatSseAsync(characterId, gameId, msg, msgId, sessionId, MessageCallback, (_) =>
            {

            }, BeforeChapterSwitch, AfterChapterSwitch, OnGameEnding);
        }

        private void BeforeChapterSwitch(string action_msg, GameOngoingResponse currentRsp)
        {
            Game1.chatBox.addMessage("\n", Microsoft.Xna.Framework.Color.Cyan);
            Game1.chatBox.addMessage("Congratulations", Microsoft.Xna.Framework.Color.Cyan);
            Game1.chatBox.addMessage($"Current chapter <{currentRsp?.Data.Chapter.Name}> passed.", Microsoft.Xna.Framework.Color.Cyan);
            Game1.chatBox.addMessage(action_msg, Microsoft.Xna.Framework.Color.Cyan);
            Game1.chatBox.addMessage("\n", Microsoft.Xna.Framework.Color.Cyan);
        }

        private void AfterChapterSwitch(string msg, GameOngoingResponse currentRsp)
        {
            Game1.chatBox.addMessage("\n", Microsoft.Xna.Framework.Color.Cyan);
            Game1.chatBox.addMessage($"New chapter <{currentRsp?.Data.Chapter.Name}> starts.", Microsoft.Xna.Framework.Color.Cyan);
            Game1.chatBox.addMessage(currentRsp?.Data.Chapter.Background, Microsoft.Xna.Framework.Color.Cyan);
            Game1.chatBox.addMessage("\n", Microsoft.Xna.Framework.Color.Cyan);
        }

        private void OnGameEnding(string msg)
        {
            Game1.chatBox.addMessage("Game Over", Microsoft.Xna.Framework.Color.Cyan);
            Game1.chatBox.addMessage(msg, Microsoft.Xna.Framework.Color.Cyan);
        }

        private string GetChatBoxInput()
        {
            if (Game1.chatBox != null && Game1.chatBox.chatBox != null)
            {
                if (Game1.chatBox.chatBox.finalText.Count > 0)
                {
                    return Game1.chatBox.chatBox.finalText[0].message.Trim();
                }
            }
            return "";
        }

        private bool TryGetCharacterIdFromName(string name, out string id)
        {
            if (npcNameToId.TryGetValue(name.ToLower(), out id))
            {
                return true;
            }
            return false;
        }

        private bool TryGetCharacterNameFromId(string id, out string name)
        {
            if (npcIdToName.TryGetValue(id, out name)) {
                return true;
            }
            return false;
        }

        private IEnumerable<string> SplitMessage(string message)
        {
            int charCount = 0;
            int maxLength = 40;
            IEnumerable<string> splits = from w in message.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                         group w by (charCount += w.Length + 1) / maxLength into g
                                         select string.Join(" ", g);
            return splits;
        }

        private async Task<string> SingleChatToNPC(string chrId, string chatInput)
        {
            Log($"RPGGO.SingleChatToNPC npcName:{chrId} chatInput:{chatInput}");
            var source = new TaskCompletionSource<string>();
            var msgId = RandomString();
            await client.ChatSseAsync(chrId, gameId, chatInput, msgId, sessionId, (chrId, msg) =>
            {
                Log($"RPGGO.SingleChatToNPC response: {msg}");
                source.TrySetResult(msg);
            }, (_) => { });
            return await source.Task;
        }

        private int GetFriendshipPoints(string npcName)
        {
            if (!Game1.player.friendshipData.TryGetValue(npcName, out var friendship))
            {
                return 0;
            }
            return friendship.Points;
        }

        private async Task DoChat(string npcName, string chatInput)
        {
            if (npcCache.TryGetValue(npcName.ToLower(), out var npc))
            {
                if (TryGetCharacterIdFromName(npcName, out var chrId))
                {
                    Log($"RPGGO.DoChat npcName:{npcName} chatInput:{chatInput} chrId:{chrId}");

                    Game1.chatBox.addMessage($"{npc.displayName} is thinking", Microsoft.Xna.Framework.Color.Green);
                    Game1.chatBox.addMessage($" ", Microsoft.Xna.Framework.Color.White);

                    // npc.facePlayer(Game1.player);
                    npc.faceTowardFarmerForPeriod(100 * 1000, 5, false, Game1.player);
                    npc.hasJustStartedFacingPlayer = true;

                    // await ChatToNPC(chrId, chatInput, OnTalkMessageCallback);
                    var response = await SingleChatToNPC(chrId, chatInput);

                    ShowNPCMessage(npc, response);
                    var affection = await QueryNPCAffection(npcName);

                    var old = GetFriendshipPoints(npcName);
                    var amount = affection - old;
                    Log($"RPGGO.DoChat npcName:{npcName} changed friendship point:{amount} new:{affection}");
                    Game1.player.changeFriendship(amount, npc);
                }
                else
                {
                    LogError($"RPGGO.DoChat CharacterId not found for npc: {npcName}");
                }
            }
            else
            {
                LogError($"RPGGO.DoChat Error not found npc: {npcName}");
            }
        }

        private async Task ShowNPCMessage(NPC npc, string responseMessage)
        {
            Game1.chatBox.addMessage($"{npc.displayName}:{responseMessage}", Microsoft.Xna.Framework.Color.Yellow);
            Game1.chatBox.addMessage(" ", Microsoft.Xna.Framework.Color.White);

            int bubbleDuration = 3000;
            foreach (string s in SplitMessage(responseMessage))
            {
                Log($"RPGGO.DoChat split: {s}");
                npc.showTextAboveHead(s, null, 2, bubbleDuration);
                await Task.Delay(bubbleDuration);
            }
        }

        private async Task<int> QueryNPCAffection(string npcName)
        {
            var source = new TaskCompletionSource<int>();
            var msgId = RandomString();
            await client.ChatSseAsync(dmId, gameId, $"Give me {npcName}'s Affection", msgId, sessionId, (chrId, msg) =>
            {
                Log($"RPGGO.QueryNPCAffection response: {msg}");
                var matches = affectionRegex.Matches(msg);
                foreach (Match match in matches)
                {
                    var groups = match.Groups;
                    var matchName = groups[1].Value;
                    var matchAffection = groups[2].Value;
                    if (matchName == npcName)
                    {
                        if (int.TryParse(matchAffection, out var intAffection))
                        {
                            source.TrySetResult(intAffection);
                            return;
                        }
                    }
                }
                source.TrySetResult(0);
            }, (_) => { });
            return await source.Task;
        }

        private void OnTalkMessageCallback(string chrId, string msg)
        {
            Log($"RPGGO OnTalkMessageCallback chrId:{chrId} msg:{msg}");
            if (TryGetCharacterNameFromId(chrId, out var chrName))
            {
                Log($"RPGGO.DoChat npcName:{chrName} chrId:{chrId} responseText:{msg}");
                if (npcCache.TryGetValue(chrName.ToLower(), out var npc))
                {
                    ShowNPCMessage(npc, msg);
                } else if (chrName == "Affection Mornitor")
                {
                    var match = affectionRegex.Match(msg);
                    if (match.Success) {
                        var groups = match.Groups;
                        var npcName = groups[1].Value.ToLower();
                        var affection = int.Parse(groups[2].Value);
                        Log($"RPGGO OnTalkMessageCallback Change affection for npc: {npcName}, affection: {affection}");
                        if (npcCache.TryGetValue(npcName, out var affectedNPC))
                        {
                            Log($"RPGGO OnTalkMessageCallback Do change friendship for npc: {npcName}, affection: {affection}");

                            Game1.player.changeFriendship(affection, affectedNPC);
                        } else
                        {
                            LogError($"RPGGO OnTalkMessageCallback cannot find npc: {npcName} to change friendship");
                        }
                    } else {
                        LogError($"RPGGO OnTalkMessageCallback Failed to parse affection message");
                    }
                    //var npcName = "";
                    //Game1.player.getFriendshipHeartLevelForNPC(npcName);
                }
            }
        }

        private void ChatBox_OnEnterPressed(StardewValley.Menus.TextBox sender) {
            Log($"RPGGO.ChatBox_OnEnterPressed npcTarget:{targetNPCName} input:{chatInput}");

            // recover speed
            if (npcCache.TryGetValue(targetNPCName.ToLower(), out var npc))
            {
                npc.addedSpeed = 0;
            }

            if (string.IsNullOrWhiteSpace(targetNPCName))
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(chatInput))
            {
                return;
            }
            DoChat(targetNPCName, chatInput);
        }

        public void Update()
        {
            Init();

            UpdateChatBoxClose();
            UpdateNPCs();
            SelectTarget();
            UpdateChatInput();
        }

        private void UpdateChatBoxClose()
        {
            if (Game1.chatBox != null)
            {
                if (!Game1.chatBox.isActive() && lastFrameChatBoxActive)
                {
                    Log($"ChatBox closed. Release npc {targetNPCName.ToLower()}");
                    // 释放NPC移动
                    if (npcCache.TryGetValue(targetNPCName.ToLower(), out var npc))
                    {
                        npc.addedSpeed = 0;
                    }
                }
                lastFrameChatBoxActive = Game1.chatBox.isActive();
            }
        }

        private void UpdateChatInput()
        {
            chatInput = GetChatBoxInput();
        }

        private void UpdateNPCs()
        {
            npcCache.Clear();
            foreach (var loc in Game1.locations)
            {
                foreach (var npc in loc.characters)
                {
                    npcCache[GetNPCName(npc).ToLower()] = npc;
                    // npc.startGlowing(Microsoft.Xna.Framework.Color.Cyan, true, 0.01f);

                    if (npcNameToId.ContainsKey(GetNPCName(npc).ToLower()))
                    {
                        npc.doEmote(2);
                    }
                }
            }
        }

        private void SelectTarget()
        {
            var playerPos = Game1.player.Tile;

            var minDistance = 10f;

            foreach (var (_, npc) in npcCache) {
                var distance = (playerPos - npc.Tile).Length();
                if (distance < minDistance)
                {
                    minDistance = distance;
                    targetNPCName = GetNPCName(npc);
                }
            }

            // Glow
            foreach (var (_, npc) in npcCache)
            {
                if (GetNPCName(npc) != targetNPCName && npc.isGlowing)
                {
                    npc.stopGlowing();
                }
                else if (npcNameToId.TryGetValue(GetNPCName(npc), out var _) && GetNPCName(npc) == targetNPCName && !npc.isGlowing)
                {
                    npc.startGlowing(Microsoft.Xna.Framework.Color.Purple, border: false, 0.01f);
                }
            }
        }

        private string GetNPCName(NPC npc)
        {
            return npc.Name;
        }

        private void Log(string msg)
        {
            mod.Monitor.Log(msg);
        }

        private void LogError(string msg)
        {
            mod.Monitor.Log(msg, LogLevel.Error);
        }
    }
}
