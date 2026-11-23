using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Rise.Data.Navigation
{
    /// <summary>
    /// Polymorphic <see cref="Newtonsoft.Json"/> converter for <see cref="NavigationItemBase"/>
    /// preserving the numeric <c>$type</c> discriminator.
    /// </summary>
    internal sealed class NavigationItemJsonConverter : JsonConverter<NavigationItemBase>
    {
        private static readonly JsonSerializer PlainSerializer = JsonSerializer.CreateDefault();

        public override NavigationItemBase ReadJson(
            JsonReader reader,
            Type objectType,
            NavigationItemBase existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var obj = JObject.Load(reader);
            var itemType = ParseItemType(obj["$type"] ?? obj["ItemType"]);

            return itemType switch
            {
                NavigationItemType.Separator => obj.ToObject<NavigationItemSeparator>(PlainSerializer),
                NavigationItemType.Header => obj.ToObject<NavigationItemHeader>(PlainSerializer),
                NavigationItemType.Destination => obj.ToObject<NavigationItemDestination>(PlainSerializer),
                _ => throw new JsonSerializationException($"Unknown navigation item discriminator: {obj["$type"]}")
            };
        }

        public override void WriteJson(JsonWriter writer, NavigationItemBase value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var obj = JObject.FromObject(value, PlainSerializer);
            obj.AddFirst(new JProperty("$type", (int)value.ItemType));
            obj.WriteTo(writer);
        }

        private static NavigationItemType ParseItemType(JToken token)
        {
            if (token == null)
                return NavigationItemType.Destination;

            if (token.Type == JTokenType.Integer && Enum.IsDefined(typeof(NavigationItemType), token.Value<int>()))
                return (NavigationItemType)token.Value<int>();

            if (token.Type == JTokenType.String && Enum.TryParse<NavigationItemType>(token.Value<string>(), true, out var parsed))
                return parsed;

            return NavigationItemType.Destination;
        }
    }
}
