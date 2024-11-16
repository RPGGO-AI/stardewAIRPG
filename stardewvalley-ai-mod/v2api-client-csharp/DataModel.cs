using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace v2api_client_csharp
{

    public class RPGGOResponse
    {
        public int Code { get; set; }
        public string Msg { get; set; }
    }

    public class GameMetadataResponse: RPGGOResponse
    {
        [JsonProperty("data")]
        public FullGameData Data { get; set; }
    }

    public class GameOngoingResponse: RPGGOResponse
    {
        [JsonProperty("data")]
        public OngoingGameData Data { get; set; }
    }

    public class GameData
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("game_id")]
        public string GameId { get; set; }

        [JsonProperty("background")]
        public string Background { get; set; }

        [JsonProperty("intro")]
        public string Intro { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("genre")]
        public string Genre { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class OngoingGameData : GameData
    {
        [JsonProperty("chapter")]
        public Chapter Chapter { get; set; }
    }

    public class FullGameData : GameData
    {
        [JsonProperty("chapters")]
        public List<Chapter> Chapters { get; set; }
    }


    public class Chapter
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("chapter_id")]
        public string ChapterId { get; set; }

        [JsonProperty("background")]
        public string Background { get; set; }

        [JsonProperty("intro")]
        public string Intro { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("background_audio")]
        public string BackgroundAudio { get; set; }

        [JsonProperty("ending_audio")]
        public string EndingAudio { get; set; }

        [JsonProperty("characters")]
        public List<Character> Characters { get; set; }
    }

    public class Character
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("avatar")]
        public string Avatar { get; set; }

        [JsonProperty("phases")]
        public List<string> Phases { get; set; }
    }


    public class Action
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class Result
    {
        [JsonProperty("character_id")]
        public string CharacterId { get; set; }

        [JsonProperty("character_type")]
        public string CharacterType { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("image_type")]
        public int ImageType { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("text_ext")]
        public string TextExt { get; set; }

        [JsonProperty("message_id")]
        public string MessageId { get; set; }

        [JsonProperty("log_id")]
        public string LogId { get; set; }

        [JsonProperty("action_list")]
        public List<Action> ActionList { get; set; }
    }

    public class ChapterTip
    {
        [JsonProperty("chapter_id")]
        public string ChapterId { get; set; }

        [JsonProperty("closing_statement")]
        public string ClosingStatement { get; set; }
    }

    public class Goal
    {
        [JsonProperty("chapter_tip")]
        public ChapterTip ChapterTip { get; set; }

        [JsonProperty("goals_status")]
        public List<object> GoalsStatus { get; set; }
    }

    public class ChatCount
    {
        public Dictionary<string, int> ChatMap { get; set; }
    }

    public class GameMeta
    {
        [JsonProperty("player_lang")]
        public string PlayerLang { get; set; }

        [JsonProperty("chat_count")]
        public ChatCount ChatCount { get; set; }

        [JsonProperty("max_prompt_tokens")]
        public int MaxPromptTokens { get; set; }
    }

    public class GameStatus
    {
        [JsonProperty("game_id")]
        public string GameId { get; set; }

        [JsonProperty("goal_id")]
        public string GoalId { get; set; }

        [JsonProperty("goal")]
        public Goal Goal { get; set; }

        [JsonProperty("chapter_id")]
        public string ChapterId { get; set; }

        [JsonProperty("action")]
        public int Action { get; set; }

        [JsonProperty("action_message")]
        public string ActionMessage { get; set; }

        [JsonProperty("game_meta")]
        public GameMeta GameMeta { get; set; }

        [JsonProperty("chapters_map")]
        public List<string> ChaptersMap { get; set; }

        [JsonProperty("displayed_status")]
        public Dictionary<string, object> DisplayedStatus { get; set; }
    }

    public class SSEData
    {
        public Result Result { get; set; }

        [JsonProperty("game_status")]
        public GameStatus GameStatus { get; set; }
    }

    public class SSEResponse
    {
        public int Ret { get; set; }
        public string Message { get; set; }
        public SSEData Data { get; set; }
    }

}
