using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace v2api_client_csharp
{
    public class RPGGOClient
    {
        private readonly HttpClient _httpClient;

        public RPGGOClient(string apiKey)
        {
            _httpClient = new HttpClient();
            //_httpClient.DefaultRequestHeaders.Add("accept", "application/json");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("Authorization", apiKey);
            //_httpClient.DefaultRequestHeaders..Add("Content-Type", "application/json");
        }

        public async Task<GameMetadataResponse> GetGameMetadataAsync(string gameId)
        {
            var requestBody = new
            {
                game_id = gameId
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            Console.WriteLine($"GetGameMetadataAsync start request gameId: {gameId}");
            var response = await _httpClient.PostAsync("https://api.rpggo.ai/v2/open/game/gamemetadata", requestContent);
            response.EnsureSuccessStatusCode();

            Console.WriteLine($"GetGameMetadataAsync get resp gameId: {gameId}");
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"GetGameMetadataAsync get resp content gameId: {gameId}");
            var gameMetadataResponse = JsonConvert.DeserializeObject<GameMetadataResponse>(responseContent);

            return gameMetadataResponse;
        }

        // Start a game
        public async Task<StartGameResponse> StartGameAsync(string gameId, string sessionId)
        {
            var requestBody = new
            {
                game_id = gameId,
                session_id = sessionId
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.rpggo.ai/v2/open/game/startgame", requestContent);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var startGameResponse = JsonConvert.DeserializeObject<StartGameResponse>(responseContent);

            return startGameResponse;
        }

        // Resume a game session
        public async Task<StartGameResponse> ResumeSessionAsync(string gameId, string sessionId)
        {
            var requestBody = new
            {
                game_id = gameId,
                session_id = sessionId
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.rpggo.ai/v2/open/game/resumesession", requestContent);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var resumeSessionResponse = JsonConvert.DeserializeObject<StartGameResponse>(responseContent);

            return resumeSessionResponse;
        }
 

        // Method to monitor SSE stream
        public async Task ChatSseAsync(
            string characterId, 
            string gameId, 
            string message, 
            string messageId, 
            string sessionId, 
            Action<string, string> onChatMessageReceived, 
            Action<string> onImageMessageReceived)
        {
            var requestBody = new
            {
                character_id = characterId,
                game_id = gameId,
                message = message,
                message_id = messageId,
                session_id = sessionId
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.rpggo.ai/v2/open/game/chatsse", requestContent);
            response.EnsureSuccessStatusCode();

            // Get the stream to read SSE messages
            var stream = await response.Content.ReadAsStreamAsync();
            using (var reader = new StreamReader(stream))
            {
                string line;
                string completeMessage = string.Empty;

                // Continuously read the stream
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    //Console.WriteLine("message is " + line);
                    // SSE events are separated by empty lines
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        if (!string.IsNullOrWhiteSpace(completeMessage))
                        {
                            // Trigger callback when the message is complete
                            var sseMsg = JsonConvert.DeserializeObject<SSEResponse>(completeMessage);
                            if (sseMsg == null)
                            {
                                throw new Exception("json deserialize issue:" + completeMessage);
                            }

                            if (sseMsg.Data.Result.CharacterType  == "common_npc")
                            {
                                onChatMessageReceived(sseMsg.Data.Result.CharacterId, sseMsg.Data.Result.Text);
                            } 
                            else if (sseMsg.Data.Result.CharacterType == "async_npc")
                            {
                                if (sseMsg.Data.Result.Text != "") {
                                    onChatMessageReceived(sseMsg.Data.Result.CharacterId, sseMsg.Data.Result.Text);
                                }
                            }
                            else if (sseMsg.Data.Result.CharacterType == "picture_produce_dm")
                            {
                                onImageMessageReceived(sseMsg.Data.Result.Image);
                            }

                            completeMessage = string.Empty;
                        }
                    }
                    else if (line.StartsWith("data:"))
                    {
                        // Extract data after the 'data:' field
                        completeMessage += line.Substring(5).Trim();
                    }
                }
            }
        }

    }

}
