using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IFi.Utilities.JsonConverters
{
    public class DateTimeOffsetConverter_ISO8601 : JsonConverter<DateTimeOffset>
    {
        public static JsonSerializerOptions DefaultJsonSerializerOptions { get; }
        static DateTimeOffsetConverter_ISO8601()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new DateTimeOffsetConverter_ISO8601());
            DefaultJsonSerializerOptions = options;
        }
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (!reader.TryGetDateTimeOffset(out DateTimeOffset value))
            {
                value = DateTimeOffset.ParseExact(reader.GetString(), "yyyy-MM-dd'T'HH:mm:sszzz", null);
            }

            return value;
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd'T'HH:mm:sszzz"));
        }
    }
}
