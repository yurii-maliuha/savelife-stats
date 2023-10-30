using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace SaveLife.Stats.Domain.Extensions
{
    public static class JsonSerializerExtensions
    {
        private static readonly JsonSerializerOptions _defaultSerializerSettings = InitDefaultSerrings();

        private static JsonSerializerOptions InitDefaultSerrings()
        {
            var settings = new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false

            };
            settings.Converters.Add(new JsonStringEnumConverter());
            return settings;
        }

        public static JsonSerializerOptions DefaultOptions => _defaultSerializerSettings;

        public static T Deserialize<T>(this string json)
        {
            return JsonSerializer.Deserialize<T>(json, _defaultSerializerSettings);
        }

        public static string Serialize<TModel>(this TModel model)
        {
            return JsonSerializer.Serialize(model, _defaultSerializerSettings);
        }
    }
}
