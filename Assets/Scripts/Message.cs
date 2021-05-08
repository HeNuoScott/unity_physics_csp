using Newtonsoft.Json.Converters;
using System.Globalization;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Data
{
    public partial class Message
    {
        [JsonProperty("Type")]
        public MessageType Type { get; set; }
        [JsonProperty("Content")]
        public object Content { get; set; }
    }

    public partial class MessageDeserializer
    {
        public static Message FromJson(string json) => JsonConvert.DeserializeObject<Message>(json, Converter.Settings);
    }

    public static class MessageSerializer
    {
        public static string ToJson(this Message self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    public enum MessageType
    {
        Ping,
        Pong,
        Client_Request_Connect,
        Server_Responses_Connect,
        Client_Request_Operation,
        Broad_Connect,
        Broad_Operation,
    }

    public struct Client_Request_Connect
    {
        public uint networkId;
    }

    public struct Client_Request_Operation
    {
        public uint networkId;
        public InputMessage inputMessage;
    }

    public struct Server_Responses_Connect
    {
        public uint networkId;
    }

    public struct Broad_Connect
    {
        public uint networkId;
        public bool isReConnect;
    }

    public struct Broad_Operation
    {
        public uint tick_number;
        public Dictionary<uint, StateMessage> StateMessage;
    }
}