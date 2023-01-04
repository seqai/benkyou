using System.Reflection;

namespace Benkyou.Infrastructure;

public static class EnumHelpers
{
    public static TEnum FromAlias<TEnum>(string alias, bool withDefaultName = true, bool withFallback = false, TEnum defaultValue = default!, IEqualityComparer<string>? comparer = null) where TEnum : Enum
    {
        comparer ??= StringComparer.OrdinalIgnoreCase;
        
        var enumType = typeof(TEnum);
        var enumValues = Enum.GetValues(enumType);
        
        foreach (var enumValue in enumValues)
        {
            if (withDefaultName && comparer.Equals(enumValue.ToString()!, alias))
            {
                return (TEnum) enumValue;
            }
            
            var enumMemberInfo = enumType.GetMember(enumValue.ToString()!);
            var enumAliasAttribute = enumMemberInfo[0].GetCustomAttribute<EnumStringAliasAttribute>();
            if (enumAliasAttribute != null && enumAliasAttribute.Aliases.Contains(alias, comparer))
            {
                return (TEnum) enumValue;
            }
        }

        if (withFallback)
        {
            return defaultValue;
        }
        
        throw new ArgumentException($"Alias {alias} not found for enum {typeof(TEnum).Name}");
    }

    public static IReadOnlyCollection<string> GetAliases<TEnum>(this TEnum enumValue, bool withDefaultName = true) where TEnum : Enum
    {
        var enumType = typeof(TEnum);
        var enumMemberInfo = enumType.GetMember(enumValue.ToString()!);
        var enumAliasAttribute = enumMemberInfo[0].GetCustomAttribute<EnumStringAliasAttribute>();

        if (enumAliasAttribute != null)
        {
            return withDefaultName ? 
                enumAliasAttribute.Aliases.Append(enumValue.ToString()).ToList() : 
                enumAliasAttribute.Aliases;
        }
        
        return withDefaultName ? new[] { enumValue.ToString() } : Array.Empty<string>();
    }
}