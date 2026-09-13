using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json.Linq;

namespace YoutubeExplode.Utils;

/// <summary>
/// Read-only view over a parsed JSON value, backed by Newtonsoft.Json.
/// Mirrors the subset of <c>System.Text.Json.JsonElement</c> (plus the
/// <c>JsonExtensions.Reading</c> helpers) that this library relies on, so that the
/// assembly only needs the JSON runtime already present in the host application.
/// </summary>
internal readonly struct JsonElement(JToken? token)
{
    public JToken? Token { get; } = token;

    private JValue? AsValue(JTokenType type) =>
        Token is JValue value && value.Type == type ? value : null;

    public JsonElement this[int index] =>
        Token is JArray array
            ? new JsonElement(array[index])
            : throw new InvalidOperationException(
                $"JSON value of type '{Token?.Type.ToString() ?? "Undefined"}' is not an array."
            );

    public JsonElement? GetPropertyOrNull(string propertyName)
    {
        if (Token is not JObject obj)
            return null;

        var value = obj[propertyName];
        if (value is null || value.Type is JTokenType.Null or JTokenType.Undefined)
            return null;

        return new JsonElement(value);
    }

    public string? GetStringOrNull() => AsValue(JTokenType.String)?.Value as string;

    public long? GetInt64OrNull() =>
        AsValue(JTokenType.Integer)?.Value switch
        {
            long value => value,
            int value => value,
            ulong value when value <= long.MaxValue => (long)value,
            BigInteger value when value >= long.MinValue && value <= long.MaxValue => (long)value,
            _ => null,
        };

    public int? GetInt32OrNull() =>
        GetInt64OrNull() is { } value && value >= int.MinValue && value <= int.MaxValue
            ? (int)value
            : null;

    public bool? GetBooleanOrNull() => AsValue(JTokenType.Boolean)?.Value as bool?;

    public DateTimeOffset GetDateTimeOffset() =>
        GetStringOrNull() is { } value
            ? DateTimeOffset.Parse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind
            )
            : throw new InvalidOperationException(
                $"JSON value of type '{Token?.Type.ToString() ?? "Undefined"}' is not a date."
            );

    public IEnumerable<JsonElement>? EnumerateArrayOrNull() =>
        Token is JArray array ? array.Select(item => new JsonElement(item)) : null;

    public IEnumerable<JsonElement> EnumerateArrayOrEmpty() => EnumerateArrayOrNull() ?? [];

    public IEnumerable<JsonProperty>? EnumerateObjectOrNull() =>
        Token is JObject obj
            ? obj.Properties().Select(p => new JsonProperty(p.Name, new JsonElement(p.Value)))
            : null;

    public IEnumerable<JsonProperty> EnumerateObjectOrEmpty() => EnumerateObjectOrNull() ?? [];
}

internal readonly struct JsonProperty(string name, JsonElement value)
{
    public string Name { get; } = name;

    public JsonElement Value { get; } = value;
}
