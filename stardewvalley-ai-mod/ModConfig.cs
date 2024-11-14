using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace stardewvalley_ai_mod
{
    public class ModConfig
    {
        public string gameId = "G2AHD9ENT";
        public string dmId = "CCT67P3C0";

        public string ApiKey { get; set; }

        private static ModConfig? _onlyOne = null;

    public static ModConfig CreateModConfig() {
            if (_onlyOne != null)
            {
                return _onlyOne;
            }

            Console.WriteLine(System.IO.Directory.GetCurrentDirectory());

            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string config_path = Path.Combine(assemblyFolder, "modconfig.json");
            Console.WriteLine($"Read Mod config file @{config_path}");
            if (!File.Exists(config_path)) {
                throw new Exception($"{config_path} not exist");
            }

            // read JSON directly from a file
            string jsonString = File.ReadAllText(config_path);
            ModConfig ob = JsonSerializer.Deserialize<ModConfig>(jsonString)!;
            
            string last6ApiKey = ob.ApiKey.Substring(ob.ApiKey.Length - 6);
            Console.WriteLine($"Read the config file successfully. last 6 digits of API Key is {last6ApiKey}");

            _onlyOne = ob;
            return ob;            
        }
    }
}
