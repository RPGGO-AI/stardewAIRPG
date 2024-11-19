using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Figgle;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Characters;
using StardewValley.TerrainFeatures;
using v2api_client_csharp;
using static StardewValley.Minigames.TargetGame;

namespace stardewvalley_ai_mod
{
    public class RPGGOWorld
    {
        private bool inited = false;
        private bool initing = false;
        private string targetNPCName = "";
        private string chatInput = "";
        private ModEntry mod;

        private Dictionary<string, NPC> npcCache = new Dictionary<string, NPC>();

        private RPGGOClient client;
        private string gameId => config.gameId;
        private string apiKey => config.apiKey;
        private string sessionId => session.sessionId;

        private RPGGOAPIGameConfig config;
        private SessionConfig session;

        private Dictionary<string, string> rpggoNpcNameToId = new Dictionary<string, string>();
        private Dictionary<string, string> rpggoNpcIdToName = new Dictionary<string, string>();


        private Regex affectionRegex = new Regex("(\\w+?)'s.+?(\\d+)%");
        private double lastEmoteTime;
        private bool lastFrameChatBoxActive;

        public RPGGOWorld(ModEntry mod, RPGGOAPIGameConfig config, SessionConfig session)
        {
            this.mod = mod;
            this.config = config;
            this.session = session;
        }

        public void OnButtonReleased(ButtonReleasedEventArgs e)
        {
            if (e.Button == SButton.R)
            {
                Log($"Release R npcName:{targetNPCName} npcCache.count:{npcCache.Count}");
                if (npcCache.TryGetValue(targetNPCName, out var npc))
                {
                    Log($"name:{GetNPCName(npc)} speed:{npc.speed} addedSpeed:{npc.addedSpeed}");
                    npc.addedSpeed = -npc.speed;
                    Game1.chatBox.activate();
                } else
                {
                    Log($"Not found in npcCache name:{targetNPCName}");
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

            Log($"[RPGGO.Init] new client with apiKey:{apiKey}");
            RPGGOUtils.Init(this.config, this.mod.Monitor);

            // update config
            this.config.stardewCharacters = StardewHelper.GetNPCsForConfig();
            var gameCharactersDict = await RPGGOUtils.GetAllGameCharacters(this.config.gameId);
            this.config.gameCharacters = gameCharactersDict.Keys.ToArray();
            this.mod.SaveRPGGOConfig(this.config);

            // reset Menu
            this.mod.ResetConfigMenu();

            // request game data
            var gameMetadata = await RPGGOUtils.GetClient().GetGameMetadataAsync(gameId);

            Log($"[RPGGO.Init] Game Name: {gameMetadata.Data.Name}");
            Log($"[RPGGO.Init] Intro: {gameMetadata.Data.Intro}");
            Log($"[RPGGO.Init] Chapters: {gameMetadata.Data.Chapters.Count}");

            // get all characters info
            foreach (var chr in gameMetadata.Data.Chapters[0].Characters)
            {
                Log($"[RPGGO.Init] Character id: {chr.Id} name: {chr.Name}");
                rpggoNpcNameToId[chr.Name] = chr.Id;
                rpggoNpcIdToName[chr.Id] = chr.Name;
            }

            Log("[RPGGO.Init] Get game metadata");
            Log($"[RPGGO.Init] sessionId: {sessionId}");

            Game1.chatBox.addMessage(FiggleFonts.Slant.Render("RPGGO"), StrFormater.getLogoColor());
            Game1.chatBox.addMessage(StrFormater.getFormatSecction(gameMetadata.Data.Name), StrFormater.getSystemColor());

            Game1.chatBox.addMessage($"Chapter < {gameMetadata.Data.Chapters[0].Name} > starts.", StrFormater.getTitleColor());
            Game1.chatBox.addMessage("Intro: " + gameMetadata.Data.Chapters[0].Background, StrFormater.getTextColor());
            Game1.chatBox.addMessage(" ", StrFormater.getWhiteSpaceColor());
            Game1.chatBox.addMessage(" ", StrFormater.getWhiteSpaceColor());

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                session.sessionId = RPGGOUtils.RandomString();
                mod.SaveSession(session);
                Log($"[RPGGO.Init] Start game sessionId: {sessionId}");
                await RPGGOUtils.GetClient().StartGameAsync(gameId, sessionId);
            }
            else
            {
                Log($"[RPGGO.Init] Resume game sessionId: {sessionId}");
                await RPGGOUtils.GetClient().ResumeSessionAsync(gameId, sessionId);
            }

            inited = true;
            initing = false;
            Log("[RPGGO.Init] Inited");
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
            if (rpggoNpcNameToId.TryGetValue(name, out id))
            {
                return true;
            }
            return false;
        }

        private bool TryGetCharacterNameFromId(string id, out string name)
        {
            if (rpggoNpcIdToName.TryGetValue(id, out name)) {
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


        private async Task DoChat(string npcName, string chatInput)
        {
            if (npcCache.TryGetValue(npcName, out var npc))
            {
                var rpggoCharName = this.config.GetBindedRPGGOChar(npcName);
                if (TryGetCharacterIdFromName(rpggoCharName, out var chrId))
                {
                    Log($"RPGGO.DoChat npcName:{npcName} chatInput:{chatInput} chrId:{chrId}");

                    Game1.chatBox.addMessage($"{npc.displayName}[|{rpggoCharName}]is thinking...", StrFormater.getNPCColor());
                    Game1.chatBox.addMessage($" ", StrFormater.getWhiteSpaceColor());

                    // npc.facePlayer(Game1.player);
                    npc.faceTowardFarmerForPeriod(100 * 1000, 5, false, Game1.player);
                    npc.hasJustStartedFacingPlayer = true;

                    // await ChatToNPC(chrId, chatInput, OnTalkMessageCallback);
                    var response = await RPGGOUtils.SingleChatToNPC(gameId, sessionId, chrId, chatInput);

                    ShowNPCMessage(npc, response);
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
            var rpggoCharName = this.config.GetBindedRPGGOChar(npc.displayName);
            Game1.chatBox.addMessage($"{npc.displayName}[|{rpggoCharName}]:{responseMessage}", StrFormater.getNPCChatColor());
            Game1.chatBox.addMessage(" ", StrFormater.getWhiteSpaceColor());

            int bubbleDuration = 3000;
            foreach (string s in SplitMessage(responseMessage))
            {
                Log($"RPGGO.DoChat split: {s}");
                npc.showTextAboveHead(s, null, 2, bubbleDuration);
                await Task.Delay(bubbleDuration);
            }
        }

        private void OnTalkMessageCallback(string chrId, string msg)
        {
            Log($"RPGGO OnTalkMessageCallback chrId:{chrId} msg:{msg}");
            if (TryGetCharacterNameFromId(chrId, out var chrName))
            {
                Log($"RPGGO.DoChat npcName:{chrName} chrId:{chrId} responseText:{msg}");
                if (npcCache.TryGetValue(chrName, out var npc))
                {
                    ShowNPCMessage(npc, msg);
                } else if (chrName == "Affection Mornitor")
                {
                    var match = affectionRegex.Match(msg);
                    if (match.Success) {
                        var groups = match.Groups;
                        var npcName = groups[1].Value;
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
            if (npcCache.TryGetValue(targetNPCName, out var npc))
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
                    Log($"ChatBox closed. Release npc {targetNPCName}");
                    // 释放NPC移动
                    if (npcCache.TryGetValue(targetNPCName, out var npc))
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
                    npcCache[GetNPCName(npc)] = npc;
                    // npc.startGlowing(Microsoft.Xna.Framework.Color.Cyan, true, 0.01f);

                    var rpggoCharName = this.config.GetBindedRPGGOChar(GetNPCName(npc));
                    if (rpggoNpcNameToId.ContainsKey(rpggoCharName))
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
                var rpggoCharName = this.config.GetBindedRPGGOChar(GetNPCName(npc));
                if (GetNPCName(npc) != targetNPCName && npc.isGlowing)
                {
                    npc.stopGlowing();
                }
                else if (rpggoNpcNameToId.TryGetValue(rpggoCharName, out var _) && GetNPCName(npc) == targetNPCName && !npc.isGlowing)
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
