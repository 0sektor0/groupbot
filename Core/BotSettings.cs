using System;
using System.Globalization;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Converters;

namespace Core;

public class BotSettings
{
    public static string Path = Directory.GetCurrentDirectory()+"/data/botconfig.json";
        
    private static readonly BotSettings _instanse = LoadConfigs(Path);
        
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

    [JsonProperty("client_id")]
    public int ClientId { get; set; }
    
    [JsonProperty("client_secret")]
    public string ClientSecret { get; set; }

    [JsonProperty("messages_endpoint")]
    public string MessagesEndpoint { get; set; }

    public DateTime LastCheckTime { get; set; }

    public static void SetPath(string path) => Path = path;

    public static BotSettings GetSettings() => _instanse;

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