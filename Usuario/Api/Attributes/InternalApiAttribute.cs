namespace Api.Attributes;

[AttributeUsage(
    AttributeTargets.Method | AttributeTargets.Class
)]
public sealed class InternalApiAttribute : Attribute
{
}