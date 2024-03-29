﻿using System;
using System.Globalization;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Converters;

namespace Core;

public class BotSettings
{
    public static string Path = $"{Directory.GetCurrentDirectory()}/data/botconfig.json";
        
    private static readonly BotSettings _instanse = LoadConfigs(Path);
        
    [JsonProperty("api_version")]
    public string ApiVersion { get; set; }
        
    [JsonProperty("is_sync")]
    public bool IsSync { get; set; }

    [JsonProperty("max_req_in_thread")]
    public int MaxReqInThread { get; set; }

    [JsonProperty("saving_delay")]
    public int SavingDelay { get; set; }

    [JsonProperty("min_listening_delay")]
    public int MinListeningDelay { get; set; }

    [JsonProperty("max_listening_delay")]
    public int MaxListeningDelay { get; set; }

    [JsonProperty("bot_login")]
    public string BotLogin { get; set; }

    [JsonProperty("bot_pass")]
    public string BotPassword { get; set; }

    [JsonProperty("bot_id")]
    public int BotId { get; set; }

    [JsonProperty("connection_string")]
    public string ConnectionString { get; set; }

    [JsonProperty("admin_id")]
    public string AdminId { get; set; }

    [JsonProperty("bot_client_id")]
    public int BotClientId { get; set; }
    
    [JsonProperty("bot_client_secret")]
    public string BotClientSecret { get; set; }

    [JsonProperty("bot_api_scope")]
    public int BotApiScope { get; set; }

    [JsonProperty("messages_client_id")]
    public int MessagesClientId { get; set; }
    
    [JsonProperty("messages_client_secret")]
    public string MessagesClientSecret { get; set; }

    [JsonProperty("messages_api_scope")]
    public int MessagesApiScope { get; set; }

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