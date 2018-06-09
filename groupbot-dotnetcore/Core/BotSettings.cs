using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Converters;



namespace groupbot.Core
{

    public class BotSettings
    {
        [JsonProperty("is_sync")]
        public static bool IsSync = false;

        [JsonProperty("max_req_in_thread")]
        public static int MaxReqInThread = 4;

        [JsonProperty("saving_delay")]
        public static int SavingDelay = 14400;

        [JsonProperty("listening_delay")]
        public static int ListeningDelay = 800;

        [JsonProperty("vk_requests_period")]
        public static int VkRequestsPeriod { get; set; }

        [JsonProperty("bot_login")]
        public static string BotLogin { get; set; }

        [JsonProperty("bot_pass")]
        public static string BotPass { get; set; }

        [JsonProperty("bot_id")]
        public static int BotId { get; set; }

        [JsonProperty("connection_string")]
        public static string ConnectionString { get; set; }

        public static DateTime LastCheckTime { get; set; }

        private static BotSettings FromJson(string json) => JsonConvert.DeserializeObject<BotSettings>(json, groupbot.Core.Converter.Settings);

        public static void LoadConfigs(string path)
        {
            if(!File.Exists(path))
                throw new FileNotFoundException($"{path} not found");

            using(StreamReader reader = new StreamReader(path))
                FromJson(reader.ReadToEnd());

            LastCheckTime = DateTime.UtcNow;
        }
    }

    public static class Serialize
    {
        public static string ToJson(this BotSettings self) => JsonConvert.SerializeObject(self, groupbot.Core.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}

