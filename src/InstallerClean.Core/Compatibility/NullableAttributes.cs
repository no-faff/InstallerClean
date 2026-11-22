namespace System.Diagnostics.CodeAnalysis;

// .NET's nullable-analysis attributes that .NET Framework 4.8 does not define. The compiler
// reads them by their full names, so these internal copies serve InstallerClean.Core and every
// assembly that sees Core's internals. Each carries the AttributeUsage .NET gives it.

/// <summary>An output that is not null when the method returns, even where its type allows null.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
internal sealed class NotNullAttribute : Attribute
{
}

/// <summary>An out parameter that may be null when the method returns <see cref="ReturnValue"/>.</summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class MaybeNullWhenAttribute : Attribute
{
    public MaybeNullWhenAttribute(bool returnValue)
    {
        ReturnValue = returnValue;
    }

    public bool ReturnValue { get; }
}

/// <summary>An out parameter that is not null when the method returns <see cref="ReturnValue"/>.</summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class NotNullWhenAttribute : Attribute
{
    public NotNullWhenAttribute(bool returnValue)
    {
        ReturnValue = returnValue;
    }

    public bool ReturnValue { get; }
}

/// <summary>
/// The fields or properties named in <see cref="Members"/> are not null once the method or
/// property returns. CommunityToolkit.Mvvm's generator puts it on the setter it writes for each
/// [ObservableProperty] field, where it can see the type.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
internal sealed class MemberNotNullAttribute : Attribute
{
    public MemberNotNullAttribute(string member)
    {
        Members = [member];
    }

    public MemberNotNullAttribute(params string[] members)
    {
        Members = members;
    }

    public string[] Members { get; }
}

/// <summary>An output that is not null when the parameter named <see cref="ParameterName"/> is not null.</summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
internal sealed class NotNullIfNotNullAttribute : Attribute
{
    public NotNullIfNotNullAttribute(string parameterName)
    {
        ParameterName = parameterName;
    }

    public string ParameterName { get; }
}
