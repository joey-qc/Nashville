using System.Text.Json;
using System.Text.Json.Serialization;

namespace Momentum.API.Converters;

/// <summary>
/// Ensures all DateTime values written to JSON responses carry the Z (UTC) suffix.
/// EF Core returns DateTime values from SQL Server as DateTimeKind.Unspecified;
/// this converter normalises them to Utc so the Blazor WASM deserializer can
/// automatically convert them to the browser's local time.
/// </summary>
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc);

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToUniversalTime());
}
