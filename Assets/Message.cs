using Newtonsoft.Json.Converters;
using System.Globalization;
using Newtonsoft.Json;

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
}