namespace System.Runtime.CompilerServices;

/// <summary>
/// .NET's attribute that has the compiler pass, as a string, the source text of the argument
/// given for another parameter. .NET Framework 4.8 does not define it, and the compiler binds
/// it by its full name, so this internal copy serves InstallerClean.Core and every assembly
/// that sees Core's internals.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class CallerArgumentExpressionAttribute : Attribute
{
    public CallerArgumentExpressionAttribute(string parameterName)
    {
        ParameterName = parameterName;
    }

    public string ParameterName { get; }
}
