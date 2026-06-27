using Newtonsoft.Json;

namespace RF_AIDialog
{
    internal sealed class BattleShoutResponse
    {
        [JsonProperty("response")]
        public string? Response { get; set; }

        [JsonProperty("movement")]
        public string? Movement { get; set; }

        [JsonProperty("formation")]
        public string? Formation { get; set; }

        [JsonProperty("arrangement")]
        public string? Arrangement { get; set; }

        [JsonProperty("firing")]
        public string? Firing { get; set; }

        [JsonProperty("confidence")]
        public double Confidence { get; set; } = 0.0;

        [JsonProperty("ally_replies")]
        public string[]? AllyReplies { get; set; }

        [JsonProperty("enemy_replies")]
        public string[]? EnemyReplies { get; set; }
    }
}
