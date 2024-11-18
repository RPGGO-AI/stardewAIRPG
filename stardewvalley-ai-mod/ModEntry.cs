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

namespace stardewvalley_ai_mod
{
    public class ModEntry : Mod
    {
        RPGGO world;
        private ModConfig config;
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
            world = new RPGGO(this, config, session);
        }

        private void Display_MenuChanged(object? sender, StardewModdingAPI.Events.MenuChangedEventArgs e)
        {
            Monitor.Log($"Test.Display_MenuChanged NewMenu: {e.NewMenu}");

        }

        private void GameLoop_GameLaunched(object? sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            Monitor.Log($"Test.GameLoop_GameLaunched");
            this.config = this.Helper.ReadConfig<ModConfig>();
            this.session = ReadSession();
            ConfigMenu();
        }

        public SessionConfig ReadSession()
        {
            var sessionPath = Path.Join(this.Helper.DirectoryPath, "session.json");
            Monitor.Log($"ReadSession path: {sessionPath}");
            try
            {
                var jsonContent = File.ReadAllText(sessionPath);
                var ret = JsonConvert.DeserializeObject<SessionConfig>(jsonContent);
                Monitor.Log($"ReadSession get sessionId from file: {ret.sessionId}");
                return ret;
            } catch (Exception e)
            {
                Monitor.Log("Failed to read session");
                Monitor.Log(e.ToString());
            }
            return new SessionConfig();
        }

        public void SaveSession(SessionConfig session)
        {
            var sessionPath = Path.Join(this.Helper.DirectoryPath, "session.json");
            Monitor.Log($"SaveSession path: {sessionPath}");
            try
            {
                var jsonContent = JsonConvert.SerializeObject(session);
                File.WriteAllText(sessionPath, jsonContent);
            } catch (Exception e)
            {
                Monitor.Log("Failed to save session");
                Monitor.Log(e.ToString());
            }
        }

        private void ConfigMenu()
        {
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
            {
                return;
            }

            configMenu.Register(
                mod: this.ModManifest,
                reset: () => {
                    this.config = new ModConfig();
                    this.session = new SessionConfig();
                },
                save: () =>
                {
                    this.Helper.WriteConfig(this.config);
                    SaveSession(this.session);
                }
            );

            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "Game ID",
                getValue: () => this.config.gameId,
                setValue: value => this.config.gameId = value
            );
            
            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "DM ID",
                getValue: () => this.config.dmId,
                setValue: value => this.config.dmId = value
            );
            
            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "API Key",
                getValue: () => this.config.apiKey,
                setValue: value => this.config.apiKey = value
            );

            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "Session ID",
                getValue: () => this.session.sessionId,
                setValue: value => this.session.sessionId = value
            );
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
