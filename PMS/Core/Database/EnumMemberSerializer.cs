using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace PMS.Core.Database;

public class EnumMemberSerializer<T> : SerializerBase<T> where T : struct, Enum
{
    public override T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var value = context.Reader.ReadString();

        var fields = typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public);

        foreach (var field in fields)
        {
            var enumMemberAttr = field.GetCustomAttribute<EnumMemberAttribute>();
            if (enumMemberAttr?.Value == value)
            {
                return (T)field.GetValue(null)!;
            }
        }

        var availableValues = fields.Select(f => CustomAttributeExtensions.GetCustomAttribute<EnumMemberAttribute>((MemberInfo)f)?.Value ?? f.Name);
        throw new FormatException($"Unable to deserialize '{value}' to enum {typeof(T).Name}. Available values: {string.Join(", ", availableValues)}");
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value)
    {
        var field = typeof(T).GetField(value.ToString());
        var enumMemberAttr = field?.GetCustomAttribute<EnumMemberAttribute>();
        var stringValue = enumMemberAttr?.Value ?? value.ToString();
        context.Writer.WriteString(stringValue);
    }
}