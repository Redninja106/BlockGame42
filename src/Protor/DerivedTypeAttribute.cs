namespace Protor;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DerivedTypeAttribute(Type type, string? discriminator = null) : Attribute
{
    public Type Type { get; } = type;
    public string Discriminator { get; } = discriminator ?? type.Name;
}