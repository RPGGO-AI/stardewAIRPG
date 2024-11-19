using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stardewvalley_ai_mod
{
    internal class StrFormater
    {
        public static Microsoft.Xna.Framework.Color getLogoColor()
        {
            return Microsoft.Xna.Framework.Color.Gold;
        }

        public static Microsoft.Xna.Framework.Color getTitleColor()
        {
            return Microsoft.Xna.Framework.Color.Salmon;
        }

        public static Microsoft.Xna.Framework.Color getTextColor()
        {
            return Microsoft.Xna.Framework.Color.MistyRose;
        }

        public static Microsoft.Xna.Framework.Color getNPCColor()
        {
            return Microsoft.Xna.Framework.Color.LemonChiffon;
        }

        public static Microsoft.Xna.Framework.Color getNPCChatColor()
        {
            return Microsoft.Xna.Framework.Color.Khaki;
        }

        public static Microsoft.Xna.Framework.Color getWhiteSpaceColor()
        {
            return Microsoft.Xna.Framework.Color.White;
        }
        public static Microsoft.Xna.Framework.Color getSystemColor()
        {
            return Microsoft.Xna.Framework.Color.White;
        }

        public static string getFormatSecction(string words)
        {
            StringBuilder s = new StringBuilder();
            s.AppendLine("****************************************************");
            var spacelen = (70 - words.Length) / 2;
            var space = new String(' ', spacelen > 0 ? spacelen : 1);
            var str = space + words;
            s.AppendLine(str);
            s.AppendLine("****************************************************");
            return s.ToString();
        }
    }
}
