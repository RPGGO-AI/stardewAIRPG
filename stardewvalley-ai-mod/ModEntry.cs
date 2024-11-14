using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenericModConfigMenu;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace stardewvalley_ai_mod
{
    internal class ModEntry : Mod
    {
        RPGGO world;
        private ModConfig config;

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
            world = new RPGGO(this, config);
        }

        private void Display_MenuChanged(object? sender, StardewModdingAPI.Events.MenuChangedEventArgs e)
        {
            Monitor.Log($"Test.Display_MenuChanged NewMenu: {e.NewMenu}");

            if (!(e.NewMenu is DialogueBox))
            {
                return;
            }
            NPC npc = Game1.currentSpeaker;

            if (npc == null) {
                return;    
            }

            string npcName = npc.Name;
            Monitor.Log($"Test.Display_MenuChanged DialogueBox npcName: {npcName}, eventUp: {Game1.eventUp}");

            Dialogue dialogue = npc.CurrentDialogue.Peek();
            Monitor.Log($"Test.Display_MenuChanged dialogue: {dialogue}");
            if (dialogue != null)
            {
                Monitor.Log($"Test.Display_MenuChanged dialogue: {dialogue.dialogues.Count}, index: {dialogue.currentDialogueIndex}");
                Monitor.Log("Test.Display_MenuChanged add line");
                dialogue.dialogues.Add(new DialogueLine("Test Dialog"));
            }
        }

        private void GameLoop_GameLaunched(object? sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            Monitor.Log($"Test.GameLoop_GameLaunched");

            //this.config = ModConfig.CreateModConfig();
            this.config = this.Helper.ReadConfig<ModConfig>();
            ConfigMenu();
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
                reset: () => this.config = ModConfig.CreateModConfig(),
                save: () => this.Helper.WriteConfig(this.config)
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
                getValue: () => this.config.ApiKey,
                setValue: value => this.config.ApiKey = value
            );

            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "Session ID",
                getValue: () => this.config.sessionId,
                setValue: value => this.config.sessionId = value
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
