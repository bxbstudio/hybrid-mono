using System;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class BakeAfterAttribute : Attribute
{
   public Type TargetType;

    public BakeAfterAttribute(Type targetType)
    {
        TargetType = targetType;
    }
}