using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenericModConfigMenu;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using v2api_client_csharp;

namespace stardewvalley_ai_mod
{
    public class ModEntry : Mod
    {
        GameWorld world;
        private RPGGOAPIGameConfig config;
        private SessionConfig session;

        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonReleased += Input_ButtonReleased;
            helper.Events.Display.MenuChanged += Display_MenuChanged;
            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            helper.Events.GameLoop.DayEnding += GameLoop_DayEnding;
            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
            helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
        }

        private void Input_ButtonReleased(object? sender, StardewModdingAPI.Events.ButtonReleasedEventArgs e)
        {
            if (world != null)
            {
                world.OnButtonReleased(e);
            }
        }

        private void GameLoop_UpdateTicked(object? sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
        {
            if (world != null)
            {
                world.Update();
            }
        }

        private void GameLoop_SaveLoaded(object? sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
        {
            world = new GameWorld(this, config, session);
        }

        private void Display_MenuChanged(object? sender, StardewModdingAPI.Events.MenuChangedEventArgs e)
        {
            Monitor.Log($"Test.Display_MenuChanged NewMenu: {e.NewMenu}");

        }

        private void GameLoop_GameLaunched(object? sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)


        {
            Monitor.Log($"Test.GameLoop_GameLaunched");
            this.config = ReadRPGGOConfig();
            this.session = ReadSession();
        }

        private T ReadConfign<T>(string fileName)
        {
            var configPath = Path.Join(this.Helper.DirectoryPath, fileName);
            Monitor.Log($"ReadConfig path: {configPath}");
            try
            {
                var jsonContent = File.ReadAllText(configPath);
                var ret = JsonConvert.DeserializeObject<T>(jsonContent);
                return ret;
            }
            catch (Exception e)
            {
                Monitor.Log("Failed to read config: {configPath}");
                Monitor.Log(e.ToString());
            }
            return default;
        }

        private void SaveConfig<T>(T configObj, string configName)
        {
            var configPath = Path.Join(this.Helper.DirectoryPath, configName);
            Monitor.Log($"SaveConfig path: {configPath}");
            try
            {
                var jsonContent = JsonConvert.SerializeObject(configObj);
                File.WriteAllText(configPath, jsonContent);
            }
            catch (Exception e)
            {
                Monitor.Log("Failed to save config");
                Monitor.Log(e.ToString());
            }
        }

        public SessionConfig ReadSession()
        {
            var ret = ReadConfign<SessionConfig>("session.json");
            if (ret != null)
            {
                Monitor.Log($"ReadSession get sessionId from file: {ret.sessionId}");
                return ret;
            }
            
            return new SessionConfig();
        }

        public void SaveSession(SessionConfig session)
        {
            SaveConfig<SessionConfig>(session, "session.json");
        }

        public RPGGOAPIGameConfig ReadRPGGOConfig()
        {
            var ret = ReadConfign<RPGGOAPIGameConfig>("rpggo_config.json");
            if (ret != null)
            {
                Monitor.Log($"ReadRPGGOConfig get game id from file: {ret.gameId}");
                return ret;
            }

            Monitor.Log($"You need rpggo_config.json to start the game. Download it from rpggo.ai", LogLevel.Error);
            return new RPGGOAPIGameConfig();
        }

        public void SaveRPGGOConfig(RPGGOAPIGameConfig rpggoConfig)
        {
            SaveConfig<RPGGOAPIGameConfig>(rpggoConfig, "rpggo_config.json");
        }

        public void ResetConfigMenu()
        {
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
            {
                return;
            }

            configMenu.Register(
                mod: this.ModManifest,
                reset: () => {
                    this.config = new RPGGOAPIGameConfig();
                    this.session = new SessionConfig();
                },
                save: () =>
                {
                    SaveRPGGOConfig(this.config);
                    SaveSession(this.session);
                }
            );

            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => "Game Settings:"
            );

            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "Game ID",
                getValue: () => this.config.gameId,
                setValue: value => this.config.gameId = value
            );
            
            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "API Key",
                getValue: () => this.config.apiKey,
                setValue: value => this.config.apiKey = value
            );

            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => "Characters:"
            );

            foreach ( var character in this.config.gameCharacters)
            {
                configMenu.AddTextOption(
                    mod: this.ModManifest,
                    name: () => character,
                    getValue: () => this.config.GetBindedStardewChar(character),
                    setValue: value => this.config.SetBindedCharacter(character, value),
                    allowedValues: this.config.stardewCharacters
                );
            }
        }

        private void GameLoop_DayEnding(object? sender, StardewModdingAPI.Events.DayEndingEventArgs e)
        {
            Monitor.Log($"Test.GameLoop_DayEnding Days:{Game1.Date.TotalDays} Name:{Game1.player.Name}");
        }

        private void GameLoop_DayStarted(object? sender, StardewModdingAPI.Events.DayStartedEventArgs e)
        {
            Monitor.Log($"Test.GameLoop_DayStarted Days:{Game1.Date.TotalDays} Name:{Game1.player.Name}");
        }
    }
}
