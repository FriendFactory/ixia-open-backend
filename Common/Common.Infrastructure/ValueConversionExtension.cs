using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;

namespace Common.Infrastructure;

public static class ValueConversionExtensions
{
    public static readonly JsonSerializerSettings Settings =
        new() {NullValueHandling = NullValueHandling.Ignore, ReferenceLoopHandling = ReferenceLoopHandling.Ignore};

    public static PropertyBuilder<T> HasJsonConversion<T>(
        this PropertyBuilder<T> propertyBuilder,
        JsonSerializerSettings jsonSerializerSettings = null,
        string columnType = "json",
        bool isRequired = false
    )
        where T : class
    {
        jsonSerializerSettings ??= Settings;

        var converter = new ValueConverter<T, string>(
            v => v == null ? null : JsonConvert.SerializeObject(v, jsonSerializerSettings),
            v => string.IsNullOrWhiteSpace(v) ? null : JsonConvert.DeserializeObject<T>(v, jsonSerializerSettings)
        );

        var comparer = new ValueComparer<T>(
            (l, r) => JsonConvert.SerializeObject(l, jsonSerializerSettings) == JsonConvert.SerializeObject(r, jsonSerializerSettings),
            v => v == null ? 0 : JsonConvert.SerializeObject(v, jsonSerializerSettings).GetHashCode(),
            v => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v, jsonSerializerSettings), jsonSerializerSettings)
        );

        propertyBuilder.HasConversion(converter);
        propertyBuilder.Metadata.SetValueConverter(converter);
        propertyBuilder.Metadata.SetValueComparer(comparer);
        propertyBuilder.HasColumnType(columnType);
        propertyBuilder.IsRequired(isRequired);

        return propertyBuilder;
    }
}