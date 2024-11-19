using Microsoft.Xna.Framework.Media;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static StardewValley.Minigames.MineCart;

namespace stardewvalley_ai_mod
{
    public class ModConfig
    {
    }
    public class RPGGOAPIGameConfig
    {
        public string gameId = "";
        public string dmId = "";
        public string apiKey = "";
        public string[] stardewCharacters = { };
        public string[] gameCharacters = {};

        // RPGGO Chars -> Stardew Valley Chars
        public Dictionary<string, string> characterBinding = new Dictionary<string, string>();

        // Stardew Valley Chars -> RPGGO Chars
        public Dictionary<string, string> reverseCharacterBinding = new Dictionary<string, string>();

        public string GetBindedRPGGOChar(string stardewCharName)
        {
            if (reverseCharacterBinding.ContainsKey(stardewCharName) == false) return "";

            return reverseCharacterBinding[stardewCharName];

        }

        public string GetBindedStardewChar(string rpggoCharName)
        {
            if (characterBinding.ContainsKey(rpggoCharName) == false) return "";

            return characterBinding[rpggoCharName];

        }

        public void SetBindedCharacter(string rpggoCharName, string stardewCharName)
        {
            if (string.IsNullOrEmpty(rpggoCharName) || string.IsNullOrEmpty(stardewCharName)) {
                return;
            }
            characterBinding[rpggoCharName] = stardewCharName;
            reverseCharacterBinding[stardewCharName] = rpggoCharName;
        }
    }

    public class SessionConfig
    {
        public string sessionId = "";

    }
}

