using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImgProc.Converters;

class TimeSpanConverter : JsonConverter<TimeSpan>
{
  public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    if (reader.TokenType == JsonTokenType.Number)
    {
      return TimeSpan.FromMilliseconds(reader.GetInt32());
    }

    throw new JsonException();
  }

  public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
  {
    writer.WriteNumberValue((int)value.TotalMilliseconds);
  }
}

class UnixDateTimeConverter : JsonConverter<DateTime>
{
  public override DateTime Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
  {
    if (reader.TokenType != JsonTokenType.Number)
    {
      throw new JsonException(
          String.Format("Unexpected token parsing date. Expected Integer, got {0}.",
          reader.TokenType));
    }

    var ticks = (long)reader.GetInt32();

    var date = new DateTime(1970, 1, 1);
    date = date.AddSeconds(ticks);

    return date;
  }
  public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
  {
    long ticks;
    var epoc = new DateTime(1970, 1, 1);
    var delta = ((DateTime)value) - epoc;
    if (delta.TotalSeconds < 0)
    {
      writer.WriteNumberValue(0);
      return;
    }
    ticks = (long)delta.TotalSeconds;

    writer.WriteNumberValue(ticks);
  }
}

class UnixDateTimeNullableConverter : JsonConverter<DateTime?>
{
  public override DateTime? Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
  {
    if (reader.TokenType != JsonTokenType.Number)
    {
      throw new JsonException(
          String.Format("Unexpected token parsing date. Expected Integer, got {0}.",
          reader.TokenType));
    }

    if (reader.ValueTextEquals("null"))
    {
      return null;
    }

    var ticks = (long)reader.GetInt32();

    var date = new DateTime(1970, 1, 1);
    date = date.AddSeconds(ticks);

    return date;
  }
  public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
  {
    if (!value.HasValue)
    {
      writer.WriteNullValue();
      return;
    }
    long ticks;
    var epoc = new DateTime(1970, 1, 1);
    var delta = (value ?? epoc) - epoc;
    if (delta.TotalSeconds < 0)
    {
      writer.WriteNumberValue(0);
      return;
    }
    ticks = (long)delta.TotalSeconds;

    writer.WriteNumberValue(ticks);
  }
}
