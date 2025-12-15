using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Protor.Converters;

internal class DerivedTypeConverter : JsonConverter
{
    public const string DiscriminatorPropertyName = "type";

    Type? baseType;
    Dictionary<string, Type> derivedTypes;

    public DerivedTypeConverter(Type baseType)
    {
        derivedTypes = baseType.GetCustomAttributes<DerivedTypeAttribute>().Select(a => new KeyValuePair<string, Type>(a.Discriminator, a.Type)).ToDictionary();
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == baseType;
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var obj = JObject.Load(reader);
        Type derived = derivedTypes[(string)obj[DiscriminatorPropertyName]!];
        return serializer.Deserialize(obj.CreateReader(), derived);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

class DerivedContractResolver : DefaultContractResolver
{
    protected override JsonConverter? ResolveContractConverter(Type objectType)
    {
        if (objectType.GetCustomAttributes<DerivedTypeAttribute>(false).Any())
        {
            return new DerivedTypeConverter(objectType);
        }

        return base.ResolveContractConverter(objectType);
    }
}
