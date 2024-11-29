using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stardewvalley_ai_mod
{
    internal class StardewHelper
    {

        public static string[] GetNPCsForConfig()
        {
            var npcs = Utility.getAllVillagers();
            var stardewNPCNames = new List<string>();
            stardewNPCNames.Add("");
            foreach (var npc in npcs)
            {
                stardewNPCNames.Add(npc.Name);
            }
            stardewNPCNames.Sort();
            return stardewNPCNames.ToArray();
        }

        public static int GetFriendshipPoints(string npcName)
        {
            if (!Game1.player.friendshipData.TryGetValue(npcName, out var friendship))
            {
                return 0;
            }
            return friendship.Points;
        }
    }
}
