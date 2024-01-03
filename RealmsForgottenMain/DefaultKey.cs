using System;

namespace RealmsForgotten;

[AttributeUsage(AttributeTargets.Property)]
public class DefaultKey : Attribute
{
    public object DefaultValue { get; }

    public DefaultKey(object defaultValue)
    {
        DefaultValue = defaultValue;
    }
}