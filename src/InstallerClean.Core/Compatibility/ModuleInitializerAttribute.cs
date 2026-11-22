namespace System.Runtime.CompilerServices;

/// <summary>
/// .NET's attribute that has the C# compiler call the static method it marks from the
/// assembly's module initializer, which the runtime runs before any other code in the assembly.
/// .NET Framework 4.8 does not define it, and the compiler finds it by its full name, so this
/// internal copy serves InstallerClean.Core and every assembly that sees Core's internals: each
/// exe's own initializer and the tests' carry this one type.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal sealed class ModuleInitializerAttribute : Attribute
{
}
