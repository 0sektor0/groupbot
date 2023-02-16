using System;
using System.Globalization;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Converters;

namespace GroupBot.BotCore;


public class BotSettings
{
    public static string path = Directory.GetCurrentDirectory()+"/data/botconfig.json";
    
    private static readonly BotSettings instanse = LoadConfigs(path);
    
    [JsonProperty("api_version")]
    public string ApiVersion { get; set; }
    
    [JsonProperty("is_sync")]
    public bool IsSync { get; set; }

    [JsonProperty("max_req_in_thread")]
    public int MaxReqInThread { get; set; }

    [JsonProperty("saving_delay")]
    public int SavingDelay { get; set; }

    [JsonProperty("listening_delay")]
    public int ListeningDelay { get; set; }

    [JsonProperty("vk_requests_period")]
    public int VkRequestsPeriod { get; set; }

    [JsonProperty("bot_login")]
    public string BotLogin { get; set; }

    [JsonProperty("bot_pass")]
    public string BotPass { get; set; }

    [JsonProperty("bot_id")]
    public int BotId { get; set; }

    [JsonProperty("connection_string")]
    public string ConnectionString { get; set; }

    [JsonProperty("admin_id")]
    public string AdminId { get; set; }

    [JsonProperty("is_messages_enabled")]
    public bool IsMessagesEnabled { get; set; }

    [JsonProperty("should_deploy_on_start")]
    public bool ShouldDeployOnStart { get; set; }

    public DateTime LastCheckTime { get; set; }

    
    public static void SetPath(string path) => BotSettings.path = path;

    public static BotSettings GetSettings() => instanse;

    private static BotSettings FromJson(string json) => JsonConvert.DeserializeObject<BotSettings>(json, Converter.Settings);

    private static BotSettings LoadConfigs(string path)
    {
        BotSettings s;

        if(!File.Exists(path))
            throw new FileNotFoundException($"{path} not found");

        using(StreamReader reader = new StreamReader(path))
            s = FromJson(reader.ReadToEnd());

        s.LastCheckTime = DateTime.UtcNow;
        return s;
    }
}


internal static class Serialize
{
    public static string ToJson(this BotSettings self) => JsonConvert.SerializeObject(self, Converter.Settings);
}


internal static class Converter
{
    public static readonly JsonSerializerSettings Settings = new()
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        Converters = {
            new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
        },
    };
}