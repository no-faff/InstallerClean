// The reader behind check-native-imports.ps1, compiled there by Add-Type. It opens one
// merged exe and reads what decides which Windows libraries that exe can load: the
// libraries its P/Invokes name, the library search the assembly and each declaration set,
// the assemblies it references, and every call its methods make, in the order each
// method's IL makes them.
//
// A METHOD IS KEYED BY TYPE, NAME AND FULL SIGNATURE, so SystemLibraries::SearchSystem32For
// taking an Assembly and the overload taking three delegates are different members. A call is
// recorded from call, callvirt, newobj, jmp, ldftn and ldvirtftn to the member each names.
// A virtual or interface call is not followed to the overrides it can reach. A method whose
// IL cannot be read stops the run rather than leaving the reader short of it.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace NativeImports
{
    public sealed class Sig : ISignatureTypeProvider<string, object>
    {
        readonly Assembly _assembly;
        public Sig(Assembly assembly) { _assembly = assembly; }
        public string GetPrimitiveType(PrimitiveTypeCode t) => t.ToString();
        public string GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte k) => _assembly.TypeName(h);
        public string GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte k) => _assembly.TypeName(h);
        public string GetTypeFromSpecification(MetadataReader r, object c, TypeSpecificationHandle h, byte k) => r.GetTypeSpecification(h).DecodeSignature(this, c);
        public string GetSZArrayType(string e) => e + "[]";
        public string GetArrayType(string e, ArrayShape s) => e + "[" + new string(',', s.Rank - 1) + "]";
        public string GetByReferenceType(string e) => e + "&";
        public string GetPointerType(string e) => e + "*";
        public string GetPinnedType(string e) => e;
        public string GetGenericInstantiation(string g, ImmutableArray<string> a) => g + "<" + string.Join(",", a) + ">";
        public string GetGenericMethodParameter(object c, int i) => "!!" + i;
        public string GetGenericTypeParameter(object c, int i) => "!" + i;
        public string GetModifiedType(string m, string u, bool r) => u;
        public string GetFunctionPointerType(MethodSignature<string> s) => "fnptr(" + string.Join(",", s.ParameterTypes) + ")" + s.ReturnType;
    }

    public sealed class Import
    {
        public string Key;
        public string Type;
        public string Library;
        public string EntryPoint;
        // The value of the DefaultDllImportSearchPaths attribute on this declaration, or null
        // where it carries none, so the assembly's own governs.
        public int? SearchPaths;
    }

    public sealed class Call
    {
        public string Caller;
        public string Callee;
        // The int32 an ldc.i4 pushed in the instruction just before the call, or null where
        // the instruction before it pushed anything else. For a call whose last argument is a
        // constant, as the flags of LoadLibraryEx are, that is the argument.
        public int? LastConstant;
    }

    // One assembly's metadata, its P/Invokes and every call its methods make.
    public sealed class Assembly
    {
        public string Name;
        public string Sha256;
        public string Path;
        public Version Version;
        public int? SearchPaths;
        public List<Import> Imports = new();
        public List<Call> Calls = new();
        public HashSet<string> Methods = new();
        public List<string> AssemblyReferences = new();

        PEReader _pe;
        MetadataReader _md;

        public static Assembly Open(byte[] bytes, string path)
        {
            var a = new Assembly { Path = path, Sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() };
            a._pe = new PEReader(new MemoryStream(bytes, false));
            if (!a._pe.HasMetadata) throw new InvalidDataException($"{path} carries no .NET metadata.");
            a._md = a._pe.GetMetadataReader();
            if (!a._md.IsAssembly) throw new InvalidDataException($"{path} is not an assembly.");
            var definition = a._md.GetAssemblyDefinition();
            a.Name = a._md.GetString(definition.Name);
            a.Version = definition.Version;
            foreach (var h in a._md.AssemblyReferences)
                a.AssemblyReferences.Add(a._md.GetString(a._md.GetAssemblyReference(h).Name));
            foreach (var h in a._md.GetAssemblyDefinition().GetCustomAttributes())
                if (a.AttributeName(a._md.GetCustomAttribute(h)) == "DefaultDllImportSearchPathsAttribute")
                    a.SearchPaths = a.ReadSearchPaths(a._md.GetCustomAttribute(h));
            return a;
        }

        string AttributeName(CustomAttribute attribute)
        {
            var type = attribute.Constructor.Kind switch
            {
                HandleKind.MemberReference => _md.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent,
                HandleKind.MethodDefinition => (EntityHandle)_md.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType(),
                _ => default,
            };
            return type.Kind switch
            {
                HandleKind.TypeReference => _md.GetString(_md.GetTypeReference((TypeReferenceHandle)type).Name),
                HandleKind.TypeDefinition => _md.GetString(_md.GetTypeDefinition((TypeDefinitionHandle)type).Name),
                _ => null,
            };
        }

        // DefaultDllImportSearchPathsAttribute's one constructor argument, a DllImportSearchPath.
        int ReadSearchPaths(CustomAttribute attribute)
        {
            var blob = _md.GetBlobReader(attribute.Value);
            if (blob.ReadUInt16() != 1) throw new InvalidDataException("a custom attribute blob without its prolog");
            return blob.ReadInt32();
        }

        string RawTypeName(TypeDefinitionHandle h)
        {
            var t = _md.GetTypeDefinition(h);
            var name = _md.GetString(t.Name);
            if (t.IsNested) return RawTypeName(t.GetDeclaringType()) + "+" + name;
            var ns = _md.GetString(t.Namespace);
            return ns.Length > 0 ? ns + "." + name : name;
        }

        public string TypeName(TypeDefinitionHandle h) => RawTypeName(h);

        public string TypeName(TypeReferenceHandle h)
        {
            var t = _md.GetTypeReference(h);
            var name = _md.GetString(t.Name);
            if (t.ResolutionScope.Kind == HandleKind.TypeReference)
                return TypeName((TypeReferenceHandle)t.ResolutionScope) + "+" + name;
            var ns = _md.GetString(t.Namespace);
            return ns.Length > 0 ? ns + "." + name : name;
        }

        string TypeName(EntityHandle h) => h.Kind switch
        {
            HandleKind.TypeDefinition => TypeName((TypeDefinitionHandle)h),
            HandleKind.TypeReference => TypeName((TypeReferenceHandle)h),
            HandleKind.TypeSpecification => SpecificationName((TypeSpecificationHandle)h),
            _ => throw new BadImageFormatException($"a member's parent is a {h.Kind}"),
        };

        // A generic instantiation is named by its generic type; any other specification (an
        // array, a pointer) is decoded.
        string SpecificationName(TypeSpecificationHandle h)
        {
            var spec = _md.GetTypeSpecification(h);
            var blob = _md.GetBlobReader(spec.Signature);
            if (blob.ReadSignatureTypeCode() == SignatureTypeCode.GenericTypeInstance)
            {
                blob.ReadCompressedInteger();
                return TypeName(blob.ReadTypeHandle());
            }
            return spec.DecodeSignature(new Sig(this), null);
        }

        static string Parameters(MethodSignature<string> s) => "(" + string.Join(",", s.ParameterTypes) + ")";

        public string MethodKey(MethodDefinitionHandle h)
        {
            var m = _md.GetMethodDefinition(h);
            return TypeName(m.GetDeclaringType()) + "::" + _md.GetString(m.Name) + Parameters(m.DecodeSignature(new Sig(this), null));
        }

        // The method a call token names, or null where it names a field or something else.
        string MethodKey(EntityHandle h)
        {
            switch (h.Kind)
            {
                case HandleKind.MethodDefinition:
                    return MethodKey((MethodDefinitionHandle)h);
                case HandleKind.MemberReference:
                {
                    var m = _md.GetMemberReference((MemberReferenceHandle)h);
                    if (m.Parent.Kind == HandleKind.MethodDefinition) return MethodKey((MethodDefinitionHandle)m.Parent);
                    if (m.GetKind() != MemberReferenceKind.Method) return null;
                    return TypeName(m.Parent) + "::" + _md.GetString(m.Name) + Parameters(m.DecodeMethodSignature(new Sig(this), null));
                }
                case HandleKind.MethodSpecification:
                    return MethodKey(_md.GetMethodSpecification((MethodSpecificationHandle)h).Method);
                default:
                    return null;
            }
        }

        // Reads every method: its import where it has one, and every call its IL makes, in IL
        // order. Any method that cannot be read throws, naming it.
        public void ReadMethods()
        {
            foreach (var h in _md.MethodDefinitions)
            {
                var m = _md.GetMethodDefinition(h);
                string key;
                try { key = MethodKey(h); }
                catch (Exception e) { throw new InvalidDataException($"{Name}: method row {MetadataTokens.GetRowNumber(h)} cannot be keyed: {e.Message}", e); }
                Methods.Add(key);
                var import = m.GetImport();
                if (!import.Module.IsNil)
                {
                    int? own = null;
                    foreach (var a in m.GetCustomAttributes())
                        if (AttributeName(_md.GetCustomAttribute(a)) == "DefaultDllImportSearchPathsAttribute")
                            own = ReadSearchPaths(_md.GetCustomAttribute(a));
                    Imports.Add(new Import {
                        Key = key, Type = RawTypeName(m.GetDeclaringType()), SearchPaths = own,
                        Library = _md.GetString(_md.GetModuleReference(import.Module).Name), EntryPoint = _md.GetString(import.Name),
                    });
                    continue;
                }
                if (m.RelativeVirtualAddress == 0) continue;
                try
                {
                    var il = _pe.GetMethodBody(m.RelativeVirtualAddress).GetILBytes();
                    foreach (var (token, constant) in IL.Calls(il))
                    {
                        var callee = MethodKey(MetadataTokens.EntityHandle(token));
                        if (callee != null) Calls.Add(new Call { Caller = key, Callee = callee, LastConstant = constant });
                    }
                }
                catch (Exception e) when (e is not InvalidDataException)
                {
                    throw new InvalidDataException($"{Name}: {key} cannot be read: {e.Message}", e);
                }
            }
        }

        // The calls a method makes, in the order its IL makes them.
        public List<Call> CallsOf(string caller) => Calls.Where(c => c.Caller == caller).ToList();

        // One chain of calls from $from to $to, each member named, or null where there is none.
        public List<string> Reach(string from, string to)
        {
            var parent = new Dictionary<string, string> { [from] = null };
            var queue = new Queue<string>();
            queue.Enqueue(from);
            var byCaller = Calls.ToLookup(c => c.Caller, c => c.Callee);
            while (queue.Count > 0)
            {
                var at = queue.Dequeue();
                if (at == to)
                {
                    var chain = new List<string>();
                    for (var k = at; k != null; k = parent[k]) chain.Insert(0, k);
                    return chain;
                }
                foreach (var next in byCaller[at])
                {
                    if (parent.ContainsKey(next)) continue;
                    parent[next] = at;
                    queue.Enqueue(next);
                }
            }
            return null;
        }
    }

    public static class IL
    {
        static readonly Dictionary<ushort, int> OperandSize = BuildSizes();

        static Dictionary<ushort, int> BuildSizes()
        {
            var sizes = new Dictionary<ushort, int>();
            foreach (ILOpCode op in Enum.GetValues(typeof(ILOpCode)))
                sizes[(ushort)op] = op switch
                {
                    ILOpCode.Switch => -1,
                    ILOpCode.Ldc_i8 or ILOpCode.Ldc_r8 => 8,
                    ILOpCode.Ldarg or ILOpCode.Ldarga or ILOpCode.Starg or ILOpCode.Ldloc or ILOpCode.Ldloca or ILOpCode.Stloc => 2,
                    _ when op.IsBranch() => op.GetBranchOperandSize(),
                    ILOpCode.Ldarg_s or ILOpCode.Ldarga_s or ILOpCode.Starg_s or ILOpCode.Ldloc_s or ILOpCode.Ldloca_s or ILOpCode.Stloc_s
                        or ILOpCode.Ldc_i4_s or ILOpCode.Unaligned => 1,
                    ILOpCode.Ldc_i4 or ILOpCode.Ldc_r4 or ILOpCode.Jmp or ILOpCode.Call or ILOpCode.Calli or ILOpCode.Callvirt
                        or ILOpCode.Cpobj or ILOpCode.Ldobj or ILOpCode.Ldstr or ILOpCode.Newobj or ILOpCode.Castclass or ILOpCode.Isinst
                        or ILOpCode.Unbox or ILOpCode.Ldfld or ILOpCode.Ldflda or ILOpCode.Stfld or ILOpCode.Ldsfld or ILOpCode.Ldsflda
                        or ILOpCode.Stsfld or ILOpCode.Stobj or ILOpCode.Box or ILOpCode.Newarr or ILOpCode.Ldelema or ILOpCode.Ldelem
                        or ILOpCode.Stelem or ILOpCode.Unbox_any or ILOpCode.Refanyval or ILOpCode.Mkrefany or ILOpCode.Ldtoken
                        or ILOpCode.Ldftn or ILOpCode.Ldvirtftn or ILOpCode.Initobj or ILOpCode.Constrained or ILOpCode.Sizeof => 4,
                    _ => 0,
                };
            return sizes;
        }

        // Every instruction whose operand is a metadata token, with its opcode and the token: a
        // type, a method, a field, a signature or a string. An opcode the table does not know
        // throws, rather than walking on misaligned.
        public static IEnumerable<(ILOpCode Op, int Token)> Tokens(byte[] il)
        {
            int i = 0;
            while (i < il.Length)
            {
                ushort code = il[i++];
                if (code == 0xFE) code = (ushort)(0xFE00 | il[i++]);
                if (!OperandSize.TryGetValue(code, out var size)) throw new InvalidDataException($"opcode {code:X} at IL offset {i}");
                if (size == -1)
                {
                    int targets = BitConverter.ToInt32(il, i);
                    i += 4 + 4 * targets;
                    continue;
                }
                var op = (ILOpCode)code;
                if (size == 4 && !op.IsBranch() && op is not (ILOpCode.Ldc_i4 or ILOpCode.Ldc_r4 or ILOpCode.Ldstr))
                    yield return (op, BitConverter.ToInt32(il, i));
                i += size;
            }
        }

        // Every instruction that names a method, with its token and the constant an ldc.i4
        // pushed just before it. An opcode the table does not know throws, rather than walking
        // on misaligned. A calli names no method and is not listed.
        public static IEnumerable<(int Token, int? Constant)> Calls(byte[] il)
        {
            int i = 0;
            int? previous = null;
            while (i < il.Length)
            {
                ushort code = il[i++];
                if (code == 0xFE) code = (ushort)(0xFE00 | il[i++]);
                if (!OperandSize.TryGetValue(code, out var size)) throw new InvalidDataException($"opcode {code:X} at IL offset {i}");
                if (size == -1)
                {
                    int targets = BitConverter.ToInt32(il, i);
                    i += 4 + 4 * targets;
                    previous = null;
                    continue;
                }
                var op = (ILOpCode)code;
                if (op is ILOpCode.Call or ILOpCode.Callvirt or ILOpCode.Newobj or ILOpCode.Jmp or ILOpCode.Ldftn or ILOpCode.Ldvirtftn)
                    yield return (BitConverter.ToInt32(il, i), previous);
                previous = op switch
                {
                    ILOpCode.Ldc_i4 => BitConverter.ToInt32(il, i),
                    ILOpCode.Ldc_i4_s => (sbyte)il[i],
                    ILOpCode.Ldc_i4_0 => 0, ILOpCode.Ldc_i4_1 => 1, ILOpCode.Ldc_i4_2 => 2, ILOpCode.Ldc_i4_3 => 3,
                    ILOpCode.Ldc_i4_4 => 4, ILOpCode.Ldc_i4_5 => 5, ILOpCode.Ldc_i4_6 => 6, ILOpCode.Ldc_i4_7 => 7,
                    ILOpCode.Ldc_i4_8 => 8, ILOpCode.Ldc_i4_m1 => -1,
                    _ => null,
                };
                i += size;
            }
        }
    }
}
