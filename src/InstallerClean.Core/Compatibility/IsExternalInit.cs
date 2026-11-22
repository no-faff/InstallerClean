namespace System.Runtime.CompilerServices;

/// <summary>
/// The type the C# compiler names in the signature of every init accessor, which includes
/// every positional record property. .NET Framework 4.8 does not define it, and the compiler
/// binds it by its full name, so this internal copy serves InstallerClean.Core and every
/// assembly that sees Core's internals.
/// </summary>
internal static class IsExternalInit
{
}
