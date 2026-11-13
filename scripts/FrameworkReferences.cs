// The reader behind check-framework-references.ps1, compiled there by Add-Type with
// NativeImports.cs, whose IL table it reads instructions with. It answers three questions
// about a merged exe:
//
//  - Which of the .NET Framework types and members the exe references a set of reference
//    assemblies does not define (Missing).
//  - Which of them the exe can reach on the way to its .NET Framework 4.8 check (Walk), which
//    on a PC below 4.8 has to run to its message.
//  - Which module initializers an assembly carries (ModuleInitializers).
//
// A REFERENCE RESOLVES AS THE RUNTIME RESOLVES IT: in the assembly its scope names, and from
// there through that assembly's type forwarders, so a type the exe takes from netstandard is
// missing from a set that has no netstandard, whichever other assembly defines a type of that
// name. A method matches on its name, generic arity, instance flag, return type and parameter
// types, required modifiers included, searched up the base types of the type that declares
// it. A reference set stands for the .NET Framework of that version: what its reference
// assemblies and facades define is what that version's assemblies define.
//
// THE WALK FOLLOWS WHAT THE RUNTIME COMPILES AND LOADS BEFORE THE CHECK HAS ANSWERED. The
// runtime compiles a method the first time it is called, and compiling it resolves every
// type, method and field its IL, its locals, its signature and its catch clauses name. A
// method followed in full is compiled and so is every method of the exe's own it calls,
// creates a delegate to or can reach through a virtual call. The entry point is compiled
// but only the calls it makes before the check are followed, since the rest run after it;
// a method it, or one compiled into it, calls and the JIT can inline is compiled with it.
// Naming a member of one of the exe's own types runs that type's static constructor, which
// is followed in full. Loading one of the exe's own types loads its base type, its
// interfaces, the value types its fields hold and the members its overrides name. Every
// virtual method of a type the walk sees created is followed, since .NET Framework's own
// code can call it, and so is the method a virtual or interface call names. A custom
// attribute is not loaded by any of this: the attributes on what the walk reads are listed
// apart (Attributes).
//
// EACH MISSING MEMBER IS LISTED ONCE, with the chain to the first method found naming it, and
// the list covers every method the walk compiles, so an empty list means nothing on the path
// is missing. A member reached by more than one route still prints one chain, so beside it
// the walk names every method of the app's own through which it is reached (AppRoutesTo):
// cutting the chain printed leaves the others.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace FrameworkReferences
{
    // Names a type in a signature by its full name and no scope, nested types after a '/', so
    // the same type reads the same from the exe and from a reference assembly. Each type
    // handle decoded is also kept, with whether the signature marks it a value type.
    public sealed class Shape : ISignatureTypeProvider<string, object>
    {
        readonly MetadataReader _md;
        public readonly List<(EntityHandle Handle, bool ValueType)> Seen = new();
        public Shape(MetadataReader md) { _md = md; }

        public string GetPrimitiveType(PrimitiveTypeCode t) => t.ToString();
        public string GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte k)
        {
            Seen.Add((h, k == (byte)SignatureTypeKind.ValueType));
            return Meta.Name(_md, h);
        }
        public string GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte k)
        {
            Seen.Add((h, k == (byte)SignatureTypeKind.ValueType));
            return Meta.Name(_md, h);
        }
        public string GetTypeFromSpecification(MetadataReader r, object c, TypeSpecificationHandle h, byte k) =>
            r.GetTypeSpecification(h).DecodeSignature(this, c);
        public string GetSZArrayType(string e) => e + "[]";
        public string GetArrayType(string e, ArrayShape s) => e + "[" + new string(',', s.Rank - 1) + "]";
        public string GetByReferenceType(string e) => e + "&";
        public string GetPointerType(string e) => e + "*";
        public string GetPinnedType(string e) => e;
        public string GetGenericInstantiation(string g, ImmutableArray<string> a) => g + "<" + string.Join(",", a) + ">";
        public string GetGenericMethodParameter(object c, int i) => "!!" + i;
        public string GetGenericTypeParameter(object c, int i) => "!" + i;
        public string GetModifiedType(string m, string u, bool required) => required ? u + " modreq(" + m + ")" : u;
        public string GetFunctionPointerType(MethodSignature<string> s) => "fnptr(" + string.Join(",", s.ParameterTypes) + ")" + s.ReturnType;
    }

    public static class Meta
    {
        public static string Name(MetadataReader md, TypeDefinitionHandle h)
        {
            var t = md.GetTypeDefinition(h);
            var name = md.GetString(t.Name);
            if (t.IsNested) return Name(md, t.GetDeclaringType()) + "/" + name;
            var ns = md.GetString(t.Namespace);
            return ns.Length > 0 ? ns + "." + name : name;
        }

        public static string Name(MetadataReader md, TypeReferenceHandle h)
        {
            var t = md.GetTypeReference(h);
            var name = md.GetString(t.Name);
            if (t.ResolutionScope.Kind == HandleKind.TypeReference) return Name(md, (TypeReferenceHandle)t.ResolutionScope) + "/" + name;
            var ns = md.GetString(t.Namespace);
            return ns.Length > 0 ? ns + "." + name : name;
        }

        public static string Name(MetadataReader md, ExportedTypeHandle h)
        {
            var t = md.GetExportedType(h);
            var name = md.GetString(t.Name);
            if (t.Implementation.Kind == HandleKind.ExportedType) return Name(md, (ExportedTypeHandle)t.Implementation) + "/" + name;
            var ns = md.GetString(t.Namespace);
            return ns.Length > 0 ? ns + "." + name : name;
        }

        // The assembly a type reference names, or null where it names this module.
        public static string Scope(MetadataReader md, TypeReferenceHandle h)
        {
            var scope = md.GetTypeReference(h).ResolutionScope;
            while (scope.Kind == HandleKind.TypeReference) scope = md.GetTypeReference((TypeReferenceHandle)scope).ResolutionScope;
            return scope.Kind == HandleKind.AssemblyReference
                ? md.GetString(md.GetAssemblyReference((AssemblyReferenceHandle)scope).Name)
                : null;
        }

        // A method signature as one string: generic arity, instance or static, return type and
        // parameters.
        public static string Signature(MethodSignature<string> s) =>
            "`" + s.GenericParameterCount + (s.Header.IsInstance ? " instance " : " static ") + s.ReturnType + "(" + string.Join(",", s.ParameterTypes) + ")";

        public static string AttributeType(MetadataReader md, CustomAttribute a)
        {
            var parent = a.Constructor.Kind switch
            {
                HandleKind.MemberReference => md.GetMemberReference((MemberReferenceHandle)a.Constructor).Parent,
                HandleKind.MethodDefinition => (EntityHandle)md.GetMethodDefinition((MethodDefinitionHandle)a.Constructor).GetDeclaringType(),
                _ => default,
            };
            return parent.Kind switch
            {
                HandleKind.TypeReference => Name(md, (TypeReferenceHandle)parent),
                HandleKind.TypeDefinition => Name(md, (TypeDefinitionHandle)parent),
                _ => null,
            };
        }

        public static PEReader Open(string path)
        {
            var pe = new PEReader(new MemoryStream(File.ReadAllBytes(path), false));
            if (!pe.HasMetadata) throw new InvalidDataException($"{path} carries no .NET metadata.");
            return pe;
        }

        // Every module initializer the assembly at the path carries: its module's static
        // constructor, and each method marked [ModuleInitializer], which the C# compiler calls
        // from that constructor.
        public static List<string> ModuleInitializers(string path)
        {
            using var pe = Open(path);
            var md = pe.GetMetadataReader();
            var found = new List<string>();
            foreach (var th in md.TypeDefinitions)
            {
                var t = md.GetTypeDefinition(th);
                foreach (var mh in t.GetMethods())
                {
                    var m = md.GetMethodDefinition(mh);
                    var name = Name(md, th) + "::" + md.GetString(m.Name);
                    if (Name(md, th) == "<Module>" && md.GetString(m.Name) == ".cctor") found.Add(name);
                    foreach (var ah in m.GetCustomAttributes())
                        if (AttributeType(md, md.GetCustomAttribute(ah)) == "System.Runtime.CompilerServices.ModuleInitializerAttribute")
                            found.Add("[ModuleInitializer] " + name);
                }
            }
            return found;
        }
    }

    // One reference assembly: the types it defines and the types it forwards, by full name.
    public sealed class ReferenceAssembly
    {
        public string Name;
        public MetadataReader Md;
        public readonly Dictionary<string, TypeDefinitionHandle> Types = new(StringComparer.Ordinal);
        public readonly Dictionary<string, string> Forwards = new(StringComparer.Ordinal);
        // The reader the metadata reads from, held as long as the metadata is.
        public PEReader Pe;

        public static ReferenceAssembly Open(string path)
        {
            // A reference set carries a few native libraries beside its assemblies.
            var pe = new PEReader(new MemoryStream(File.ReadAllBytes(path), false));
            if (!pe.HasMetadata) return null;
            var md = pe.GetMetadataReader();
            if (!md.IsAssembly) return null;
            var a = new ReferenceAssembly { Pe = pe, Md = md, Name = md.GetString(md.GetAssemblyDefinition().Name) };
            foreach (var h in md.TypeDefinitions) a.Types[Meta.Name(md, h)] = h;
            foreach (var h in md.ExportedTypes)
            {
                var e = md.GetExportedType(h);
                var impl = e.Implementation;
                while (impl.Kind == HandleKind.ExportedType) impl = md.GetExportedType((ExportedTypeHandle)impl).Implementation;
                if (impl.Kind == HandleKind.AssemblyReference)
                    a.Forwards[Meta.Name(md, h)] = md.GetString(md.GetAssemblyReference((AssemblyReferenceHandle)impl).Name);
            }
            return a;
        }
    }

    // The reference assemblies of one .NET Framework version, by assembly name.
    public sealed class ReferenceSet
    {
        public string Label;
        public readonly Dictionary<string, ReferenceAssembly> Assemblies = new(StringComparer.OrdinalIgnoreCase);
        public int Files;

        // Reads every assembly in the folder and in its Facades folder. A folder that yields no
        // mscorlib is not a reference set, and stops the run.
        public static ReferenceSet Open(string label, string folder)
        {
            var set = new ReferenceSet { Label = label };
            var files = Directory.GetFiles(folder, "*.dll").ToList();
            var facades = Path.Combine(folder, "Facades");
            if (Directory.Exists(facades)) files.AddRange(Directory.GetFiles(facades, "*.dll"));
            foreach (var f in files)
            {
                var a = ReferenceAssembly.Open(f);
                if (a is null) continue;
                set.Assemblies[a.Name] = a;
                set.Files++;
            }
            if (!set.Assemblies.ContainsKey("mscorlib")) throw new InvalidDataException($"{folder} holds no mscorlib, so it is not a .NET Framework reference set.");
            return set;
        }

        // The definition of the type the assembly named defines or forwards, or null with why.
        public (ReferenceAssembly Assembly, TypeDefinitionHandle Type)? Resolve(string assembly, string type, out string why)
        {
            why = null;
            for (int hops = 0; hops < 8; hops++)
            {
                if (!Assemblies.TryGetValue(assembly, out var a)) { why = $"{Label} has no assembly {assembly}"; return null; }
                if (a.Types.TryGetValue(type, out var h)) return (a, h);
                if (!a.Forwards.TryGetValue(type, out var next)) { why = $"{Label}'s {assembly} neither defines nor forwards it"; return null; }
                assembly = next;
            }
            why = $"{Label}'s forwarders for it go round in a loop";
            return null;
        }

        // The type a reference assembly's own handle names: one of its definitions, or one it
        // references in another assembly of the set.
        (ReferenceAssembly, TypeDefinitionHandle)? Resolve(ReferenceAssembly from, EntityHandle h)
        {
            switch (h.Kind)
            {
                case HandleKind.TypeDefinition:
                    return (from, (TypeDefinitionHandle)h);
                case HandleKind.TypeReference:
                {
                    var r = (TypeReferenceHandle)h;
                    var scope = Meta.Scope(from.Md, r);
                    var name = Meta.Name(from.Md, r);
                    if (scope is null) return from.Types.TryGetValue(name, out var own) ? (from, own) : null;
                    return Resolve(scope, name, out _);
                }
                case HandleKind.TypeSpecification:
                {
                    var blob = from.Md.GetBlobReader(from.Md.GetTypeSpecification((TypeSpecificationHandle)h).Signature);
                    if (blob.ReadSignatureTypeCode() != SignatureTypeCode.GenericTypeInstance) return null;
                    blob.ReadCompressedInteger();
                    return Resolve(from, blob.ReadTypeHandle());
                }
                default:
                    return null;
            }
        }

        // Whether the type, or a type it derives from, defines the method with this name and
        // signature, or the field with this name and type. An interface is searched together
        // with the interfaces it extends.
        public bool Defines(ReferenceAssembly a, TypeDefinitionHandle t, string name, string signature, bool field)
        {
            var seen = new HashSet<(string, TypeDefinitionHandle)>();
            var queue = new Queue<(ReferenceAssembly, TypeDefinitionHandle)>();
            queue.Enqueue((a, t));
            while (queue.Count > 0)
            {
                var (at, h) = queue.Dequeue();
                if (!seen.Add((at.Name, h))) continue;
                var md = at.Md;
                var td = md.GetTypeDefinition(h);
                var shape = new Shape(md);
                if (field)
                {
                    foreach (var fh in td.GetFields())
                    {
                        var f = md.GetFieldDefinition(fh);
                        if (md.GetString(f.Name) == name && f.DecodeSignature(shape, null) == signature) return true;
                    }
                }
                else
                {
                    foreach (var mh in td.GetMethods())
                    {
                        var m = md.GetMethodDefinition(mh);
                        if (md.GetString(m.Name) == name && Meta.Signature(m.DecodeSignature(shape, null)) == signature) return true;
                    }
                }
                if (!td.BaseType.IsNil && Resolve(at, td.BaseType) is { } b) queue.Enqueue(b);
                if ((td.Attributes & TypeAttributes.Interface) != 0)
                    foreach (var ih in td.GetInterfaceImplementations())
                        if (Resolve(at, md.GetInterfaceImplementation(ih).Interface) is { } i) queue.Enqueue(i);
            }
            return false;
        }
    }

    // One thing the exe needs that a reference set does not have, and the method that names it.
    public sealed class Miss
    {
        public string Set;
        public string What;
        public string Why;
        public string From;
        public override string ToString() => What + "  (" + Why + ")";
    }

    public sealed class Exe
    {
        public string Path;
        public string Name;
        public PEReader Pe;
        public MetadataReader Md;

        public static Exe Open(string path)
        {
            var pe = Meta.Open(path);
            var md = pe.GetMetadataReader();
            if (!md.IsAssembly) throw new InvalidDataException($"{path} is not an assembly.");
            return new Exe { Path = path, Pe = pe, Md = md, Name = md.GetString(md.GetAssemblyDefinition().Name) };
        }

        public string MethodName(MethodDefinitionHandle h)
        {
            var m = Md.GetMethodDefinition(h);
            var s = m.DecodeSignature(new Shape(Md), null);
            return Meta.Name(Md, m.GetDeclaringType()) + "::" + Md.GetString(m.Name) + "(" + string.Join(",", s.ParameterTypes) + ")";
        }

        public MethodDefinitionHandle EntryPoint
        {
            get
            {
                var token = Pe.PEHeaders.CorHeader.EntryPointTokenOrRelativeVirtualAddress;
                var h = MetadataTokens.EntityHandle(token);
                if (h.Kind != HandleKind.MethodDefinition) throw new InvalidDataException($"{Path} has no managed entry point.");
                return (MethodDefinitionHandle)h;
            }
        }

        public MethodDefinitionHandle? FindMethod(string typeName, string methodName)
        {
            foreach (var th in Md.TypeDefinitions)
            {
                if (Meta.Name(Md, th) != typeName) continue;
                foreach (var mh in Md.GetTypeDefinition(th).GetMethods())
                    if (Md.GetString(Md.GetMethodDefinition(mh).Name) == methodName) return mh;
            }
            return null;
        }

        // Where a type of .NET Framework is: its assembly and full name, or null where the handle
        // names one of the exe's own.
        public (string Assembly, string Type)? Framework(TypeReferenceHandle h) =>
            Meta.Scope(Md, h) is { } scope ? (scope, Meta.Name(Md, h)) : null;

        // The framework type a member's parent names, the generic instantiation's type where it is
        // one; null where it is the exe's own, an array or anything else the runtime makes itself.
        public (string Assembly, string Type)? FrameworkParent(EntityHandle parent)
        {
            switch (parent.Kind)
            {
                case HandleKind.TypeReference:
                    return Framework((TypeReferenceHandle)parent);
                case HandleKind.TypeSpecification:
                {
                    var blob = Md.GetBlobReader(Md.GetTypeSpecification((TypeSpecificationHandle)parent).Signature);
                    if (blob.ReadSignatureTypeCode() != SignatureTypeCode.GenericTypeInstance) return null;
                    blob.ReadCompressedInteger();
                    var g = blob.ReadTypeHandle();
                    return g.Kind == HandleKind.TypeReference ? Framework((TypeReferenceHandle)g) : null;
                }
                default:
                    return null;
            }
        }

        // The exe's own definition a member reference names on one of its own types, or nil.
        public EntityHandle OwnDefinition(MemberReferenceHandle h)
        {
            var r = Md.GetMemberReference(h);
            TypeDefinitionHandle owner;
            switch (r.Parent.Kind)
            {
                case HandleKind.TypeDefinition: owner = (TypeDefinitionHandle)r.Parent; break;
                case HandleKind.MethodDefinition: return r.Parent;
                case HandleKind.TypeSpecification:
                {
                    var blob = Md.GetBlobReader(Md.GetTypeSpecification((TypeSpecificationHandle)r.Parent).Signature);
                    if (blob.ReadSignatureTypeCode() != SignatureTypeCode.GenericTypeInstance) return default;
                    blob.ReadCompressedInteger();
                    var g = blob.ReadTypeHandle();
                    if (g.Kind != HandleKind.TypeDefinition) return default;
                    owner = (TypeDefinitionHandle)g;
                    break;
                }
                default: return default;
            }
            var name = Md.GetString(r.Name);
            var td = Md.GetTypeDefinition(owner);
            if (r.GetKind() == MemberReferenceKind.Field)
            {
                foreach (var fh in td.GetFields()) if (Md.GetString(Md.GetFieldDefinition(fh).Name) == name) return fh;
                throw new InvalidDataException($"{Name}: {Meta.Name(Md, owner)} has no field {name}, which a reference names.");
            }
            var signature = Meta.Signature(r.DecodeMethodSignature(new Shape(Md), null));
            foreach (var mh in td.GetMethods())
            {
                var m = Md.GetMethodDefinition(mh);
                if (Md.GetString(m.Name) == name && Meta.Signature(m.DecodeSignature(new Shape(Md), null)) == signature) return mh;
            }
            throw new InvalidDataException($"{Name}: {Meta.Name(Md, owner)} has no method {name}{signature}, which a reference names.");
        }

        // Why the set lacks the framework type, or null where it has it.
        public string Check(ReferenceSet set, TypeReferenceHandle h)
        {
            if (Framework(h) is not { } f) return null;
            return set.Resolve(f.Assembly, f.Type, out var why) is null ? why : null;
        }

        // Why the set lacks the framework member, or null where it has it or the member is not
        // one of .NET Framework's.
        public string Check(ReferenceSet set, MemberReferenceHandle h, out string what)
        {
            var r = Md.GetMemberReference(h);
            what = null;
            if (FrameworkParent(r.Parent) is not { } f) return null;
            var shape = new Shape(Md);
            var name = Md.GetString(r.Name);
            bool field = r.GetKind() == MemberReferenceKind.Field;
            var signature = field ? r.DecodeFieldSignature(shape, null) : Meta.Signature(r.DecodeMethodSignature(shape, null));
            what = $"{f.Type}::{name} {signature}  [{f.Assembly}]";
            if (set.Resolve(f.Assembly, f.Type, out var why) is not { } t) return "its type: " + why;
            return set.Defines(t.Assembly, t.Type, name, signature, field) ? null : $"{set.Label}'s {f.Type} has no such {(field ? "field" : "method")}";
        }

        // Every framework type and member the exe references anywhere that the set lacks.
        public List<Miss> Missing(ReferenceSet set)
        {
            var misses = new List<Miss>();
            foreach (var h in Md.TypeReferences)
                if (Check(set, h) is { } why)
                    misses.Add(new Miss { Set = set.Label, What = $"{Meta.Name(Md, h)}  [{Meta.Scope(Md, h)}]", Why = why });
            foreach (var h in Md.MemberReferences)
                if (Check(set, h, out var what) is { } why)
                    misses.Add(new Miss { Set = set.Label, What = what, Why = why });
            return misses;
        }

        public int FrameworkTypeReferences => Md.TypeReferences.Count(h => Framework(h) is not null);
        public int FrameworkMemberReferences => Md.MemberReferences.Count(h => FrameworkParent(Md.GetMemberReference(h).Parent) is not null);
    }

    // The walk from an exe's start to its .NET Framework 4.8 check: every method compiled, every
    // type loaded and every framework type and member named on the way, each with the method
    // that brought it in.
    public sealed class Walk
    {
        readonly Exe _exe;
        readonly MetadataReader _md;
        readonly ReferenceSet[] _sets;
        readonly string _checkCall;
        // The method whose calls are followed up to the check: the entry point, or the static
        // constructor of the entry point's type where that names the check.
        MethodDefinitionHandle _checkFrom;

        // How a method is compiled: in full, it runs and so can everything it reaches; as the
        // entry point, or the static constructor the check is called from, it runs up to the
        // check; inlined, it is compiled into a method that
        // names it, so the types and members it names are resolved, and it runs later or never.
        public const int Inlined = 0, Entry = 1, Full = 2;

        // Each method compiled, with how, and the one that brought it in.
        public readonly Dictionary<MethodDefinitionHandle, int> Compiled = new();
        readonly Dictionary<MethodDefinitionHandle, string> _via = new();
        // Every method that compiles each method, by name, with the most it compiled it as: a
        // method compiled inlined has its names resolved and its calls not followed.
        readonly Dictionary<string, Dictionary<string, int>> _callers = new();
        readonly Queue<MethodDefinitionHandle> _queue = new();
        readonly HashSet<TypeDefinitionHandle> _loaded = new();
        readonly HashSet<TypeDefinitionHandle> _initialized = new();
        readonly HashSet<TypeDefinitionHandle> _created = new();
        readonly HashSet<string> _virtualCalls = new();
        readonly HashSet<(string, EntityHandle)> _checked = new();
        public readonly List<Miss> Misses = new();
        // The custom attributes on the types loaded and methods compiled whose type a set
        // lacks: listed for the record, as the runtime does not load them.
        public readonly List<Miss> Attributes = new();
        public int TypesNamed, MembersNamed;

        Walk(Exe exe, ReferenceSet[] sets, string checkCall)
        {
            // PowerShell passes $null for a string as an empty string.
            _exe = exe; _md = exe.Md; _sets = sets; _checkCall = string.IsNullOrEmpty(checkCall) ? null : checkCall;
        }

        // Walks the exe from its module initializer and entry point. Where checkCall names a
        // method (Type::Name), the calls up to and including it are followed in full and the
        // rest only compiled with them: the calls of the entry point type's static constructor
        // where that names the check, which then runs before the entry point does, so none of
        // the entry point's calls is followed; otherwise the entry point's, after that static
        // constructor has been followed in full. Where checkCall is null or empty, every static
        // constructor is followed in full and none of the entry point's calls.
        public static Walk Run(Exe exe, ReferenceSet[] sets, string checkCall)
        {
            var w = new Walk(exe, sets, checkCall);
            foreach (var th in w._md.TypeDefinitions)
                if (Meta.Name(w._md, th) == "<Module>")
                    foreach (var mh in w._md.GetTypeDefinition(th).GetMethods())
                        if (w._md.GetString(w._md.GetMethodDefinition(mh).Name) == ".cctor") w.Follow(mh, "the module initializer");
            var entry = exe.EntryPoint;
            var entryType = w._md.GetMethodDefinition(entry).GetDeclaringType();
            w._checkFrom = entry;
            if (w._checkCall != null && w.StaticConstructorNamingCheck(entryType) is { } cctor)
            {
                w._checkFrom = cctor;
                w._initialized.Add(entryType);
                w.Load(entryType, "the entry point's type");
                w.Compile(cctor, Entry, via: "the entry point's type");
            }
            else w.Initialize(entryType, "the entry point's type");
            w.Compile(entry, Entry, via: "the entry point");
            w.Drain();
            return w;
        }

        // The type's static constructor, where its body names the check.
        MethodDefinitionHandle? StaticConstructorNamingCheck(TypeDefinitionHandle t)
        {
            foreach (var mh in _md.GetTypeDefinition(t).GetMethods())
            {
                var m = _md.GetMethodDefinition(mh);
                if (_md.GetString(m.Name) != ".cctor" || m.RelativeVirtualAddress == 0) continue;
                foreach (var (_, token) in NativeImports.IL.Tokens(_exe.Pe.GetMethodBody(m.RelativeVirtualAddress).GetILBytes()))
                    if (Names(MetadataTokens.EntityHandle(token)) == _checkCall) return mh;
            }
            return null;
        }

        public bool IsFollowed(MethodDefinitionHandle h) => Compiled.TryGetValue(h, out var mode) && mode == Full;

        public List<string> ChainTo(string from)
        {
            var chain = new List<string>();
            var at = from;
            for (int guard = 0; at != null && guard < 200; guard++)
            {
                chain.Insert(0, at);
                var h = FindByName(at);
                at = h is { } m && _via.TryGetValue(m, out var v) ? v : null;
            }
            return chain;
        }

        // The methods whose names start with prefix (the app's own: "InstallerClean.") through
        // which the walk reaches the method named from: the method itself where it is one, and
        // otherwise the first such method on each route back from it. A route that reaches the
        // start without one ends at the start's own name ("the entry point" and the like). The
        // method naming the member is reached from every method it is compiled into; a method
        // further back runs what it calls only where it is not merely inlined, so only those
        // routes are followed. A method reached through a virtual call is reached from the
        // method making the call, so an override can read as reached from a method other than
        // the one on whose route its type was created.
        public List<string> AppRoutesTo(string from, string prefix)
        {
            var found = new SortedSet<string>(StringComparer.Ordinal);
            var seen = new HashSet<string>();
            var queue = new Queue<string>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var at = queue.Dequeue();
                if (!seen.Add(at)) continue;
                if (at.StartsWith(prefix, StringComparison.Ordinal) || !_callers.TryGetValue(at, out var callers))
                {
                    found.Add(at);
                    continue;
                }
                foreach (var c in callers)
                    if (at == from || c.Value != Inlined) queue.Enqueue(c.Key);
            }
            return found.ToList();
        }

        readonly Dictionary<string, MethodDefinitionHandle> _byName = new();
        MethodDefinitionHandle? FindByName(string name) => _byName.TryGetValue(name, out var h) ? h : null;

        string NameOf(MethodDefinitionHandle h)
        {
            var n = _exe.MethodName(h);
            _byName[n] = h;
            return n;
        }

        void Follow(MethodDefinitionHandle h, string via) => Compile(h, Full, via: via);

        void Compile(MethodDefinitionHandle h, int mode, string via)
        {
            var name = NameOf(h);
            if (!_callers.TryGetValue(name, out var callers)) _callers[name] = callers = new Dictionary<string, int>();
            callers[via] = callers.TryGetValue(via, out var had) ? Math.Max(had, mode) : mode;
            if (Compiled.TryGetValue(h, out var was) && was >= mode) return;
            Compiled[h] = mode;
            if (!_via.ContainsKey(h)) _via[h] = via;
            _queue.Enqueue(h);
        }

        void Drain()
        {
            while (_queue.Count > 0)
            {
                var h = _queue.Dequeue();
                Read(h, Compiled[h]);
            }
        }

        void Read(MethodDefinitionHandle h, int mode)
        {
            var m = _md.GetMethodDefinition(h);
            var name = NameOf(h);
            Load(m.GetDeclaringType(), name);
            NeedAttributes(m.GetCustomAttributes(), name);
            foreach (var ph in m.GetParameters()) NeedAttributes(_md.GetParameter(ph).GetCustomAttributes(), name);
            NeedShape(sh => m.DecodeSignature(sh, null), name);
            if (m.RelativeVirtualAddress == 0) return;
            var body = _exe.Pe.GetMethodBody(m.RelativeVirtualAddress);
            if (!body.LocalSignature.IsNil)
                NeedShape(s => _md.GetStandaloneSignature(body.LocalSignature).DecodeLocalSignature(s, null), name);
            foreach (var region in body.ExceptionRegions)
                if (!region.CatchType.IsNil) NeedType(region.CatchType, name);
            // The method the check is called from follows its calls up to the check; every
            // other method followed in full follows all of them.
            bool follow = mode == Full;
            bool upToCheck = mode == Entry && _checkCall != null && h == _checkFrom;
            byte[] il;
            try { il = body.GetILBytes(); }
            catch (Exception e) { throw new InvalidDataException($"{_exe.Name}: {name} cannot be read: {e.Message}", e); }
            foreach (var (op, token) in NativeImports.IL.Tokens(il))
            {
                var handle = MetadataTokens.EntityHandle(token);
                switch (handle.Kind)
                {
                    case HandleKind.TypeReference:
                    case HandleKind.TypeDefinition:
                    case HandleKind.TypeSpecification:
                        NeedType(handle, name);
                        break;
                    case HandleKind.StandaloneSignature:
                        NeedShape(s => Meta.Signature(_md.GetStandaloneSignature((StandaloneSignatureHandle)handle).DecodeMethodSignature(s, null)), name);
                        break;
                    case HandleKind.MethodDefinition:
                    case HandleKind.MemberReference:
                    case HandleKind.MethodSpecification:
                    case HandleKind.FieldDefinition:
                    {
                        bool isCheck = upToCheck && Names(handle) == _checkCall;
                        NeedMember(handle, op, follow || upToCheck, name);
                        if (isCheck) upToCheck = false;
                        break;
                    }
                }
            }
        }

        // The Type::Name of the method a token names, or null.
        string Names(EntityHandle h)
        {
            if (h.Kind == HandleKind.MethodSpecification) h = _md.GetMethodSpecification((MethodSpecificationHandle)h).Method;
            if (h.Kind == HandleKind.MemberReference && _exe.OwnDefinition((MemberReferenceHandle)h) is { IsNil: false } d) h = d;
            if (h.Kind != HandleKind.MethodDefinition) return null;
            var m = _md.GetMethodDefinition((MethodDefinitionHandle)h);
            return Meta.Name(_md, m.GetDeclaringType()) + "::" + _md.GetString(m.Name);
        }

        void NeedMember(EntityHandle h, ILOpCode op, bool follow, string from)
        {
            if (h.Kind == HandleKind.MethodSpecification)
            {
                var spec = _md.GetMethodSpecification((MethodSpecificationHandle)h);
                NeedShape(s => string.Join(",", spec.DecodeSignature(s, null)), from);
                h = spec.Method;
            }
            if (h.Kind == HandleKind.MemberReference)
            {
                var r = (MemberReferenceHandle)h;
                var own = _exe.OwnDefinition(r);
                var reference = _md.GetMemberReference(r);
                NeedType(reference.Parent, from);
                if (own.IsNil)
                {
                    MembersNamed++;
                    foreach (var set in _sets)
                        if (_checked.Add((set.Label, h)) && _exe.Check(set, r, out var what) is { } why)
                            Misses.Add(new Miss { Set = set.Label, What = what, Why = why, From = from });
                    if (reference.GetKind() == MemberReferenceKind.Method)
                    {
                        var sig = Signature(sh => reference.DecodeMethodSignature(sh, null), from);
                        if (follow && op is ILOpCode.Callvirt or ILOpCode.Ldvirtftn) VirtualCall(_md.GetString(reference.Name), sig, from);
                    }
                    else NeedShape(s => reference.DecodeFieldSignature(s, null), from);
                    return;
                }
                h = own;
            }
            if (h.Kind == HandleKind.FieldDefinition)
            {
                var f = _md.GetFieldDefinition((FieldDefinitionHandle)h);
                Initialize(f.GetDeclaringType(), from);
                NeedShape(s => f.DecodeSignature(s, null), from);
                return;
            }
            var mh = (MethodDefinitionHandle)h;
            var m = _md.GetMethodDefinition(mh);
            Initialize(m.GetDeclaringType(), from);
            var signature = Signature(sh => m.DecodeSignature(sh, null), from);
            if (!follow)
            {
                // The JIT can compile a callee into the method calling it, unless the callee is
                // virtual or marked NoInlining.
                bool virtualCall = op is ILOpCode.Callvirt && (m.Attributes & MethodAttributes.Virtual) != 0;
                if (op is ILOpCode.Call or ILOpCode.Callvirt or ILOpCode.Newobj && !virtualCall
                    && (m.ImplAttributes & MethodImplAttributes.NoInlining) == 0)
                    Compile(mh, Inlined, from);
                return;
            }
            if (op == ILOpCode.Newobj) Created(m.GetDeclaringType(), from);
            if (op is ILOpCode.Call or ILOpCode.Callvirt or ILOpCode.Newobj or ILOpCode.Jmp or ILOpCode.Ldftn or ILOpCode.Ldvirtftn)
                Follow(mh, from);
            if (op is ILOpCode.Callvirt or ILOpCode.Ldvirtftn && (m.Attributes & MethodAttributes.Virtual) != 0)
                VirtualCall(_md.GetString(m.Name), signature, from);
        }

        // A virtual or interface call reaches the method of that name and parameters in every
        // type of the exe's own created on the walk, now or later.
        void VirtualCall(string name, MethodSignature<string> signature, string from)
        {
            var key = name + "(" + string.Join(",", signature.ParameterTypes) + ")";
            if (!_virtualCalls.Add(key)) return;
            foreach (var t in _created.ToList()) Overrides(t, key, from);
        }

        // A type created can be handed to .NET Framework, whose own code then calls its
        // overrides, so every virtual method of it and of its own base types is followed.
        void Created(TypeDefinitionHandle t, string from)
        {
            if (!_created.Add(t)) return;
            for (var at = t; !at.IsNil;)
            {
                var td = _md.GetTypeDefinition(at);
                foreach (var mh in td.GetMethods())
                    if ((_md.GetMethodDefinition(mh).Attributes & MethodAttributes.Virtual) != 0) Follow(mh, from);
                at = td.BaseType.Kind == HandleKind.TypeDefinition ? (TypeDefinitionHandle)td.BaseType : default;
            }
            foreach (var key in _virtualCalls.ToList()) Overrides(t, key, from);
        }

        void Overrides(TypeDefinitionHandle t, string key, string from)
        {
            for (var at = t; !at.IsNil;)
            {
                var td = _md.GetTypeDefinition(at);
                foreach (var mh in td.GetMethods())
                {
                    var m = _md.GetMethodDefinition(mh);
                    if ((m.Attributes & MethodAttributes.Virtual) == 0) continue;
                    var s = m.DecodeSignature(new Shape(_md), null);
                    var name = _md.GetString(m.Name);
                    // An explicit implementation is named Interface.Method; the call names Method.
                    var plain = name.Contains('.') && !name.StartsWith(".") ? name.Substring(name.LastIndexOf('.') + 1) : name;
                    if (plain + "(" + string.Join(",", s.ParameterTypes) + ")" == key) Follow(mh, from);
                }
                at = td.BaseType.Kind == HandleKind.TypeDefinition ? (TypeDefinitionHandle)td.BaseType : default;
            }
        }

        // Naming a member of one of the exe's own types runs that type's static constructor.
        void Initialize(TypeDefinitionHandle t, string from)
        {
            if (!_initialized.Add(t)) return;
            Load(t, from);
            foreach (var mh in _md.GetTypeDefinition(t).GetMethods())
                if (_md.GetString(_md.GetMethodDefinition(mh).Name) == ".cctor") Follow(mh, from);
        }

        // Loading one of the exe's own types loads its base type, its interfaces, the value types
        // its fields hold and every member its explicit overrides name.
        void Load(TypeDefinitionHandle t, string from)
        {
            if (!_loaded.Add(t)) return;
            var td = _md.GetTypeDefinition(t);
            NeedAttributes(td.GetCustomAttributes(), from);
            if (td.IsNested) Load(td.GetDeclaringType(), from);
            if (!td.BaseType.IsNil) NeedType(td.BaseType, from);
            foreach (var ih in td.GetInterfaceImplementations()) NeedType(_md.GetInterfaceImplementation(ih).Interface, from);
            foreach (var fh in td.GetFields())
                if (HoldsValueType(_md.GetFieldDefinition(fh)))
                    NeedShape(sh => _md.GetFieldDefinition(fh).DecodeSignature(sh, null), from);
            foreach (var ih in td.GetMethodImplementations())
            {
                var impl = _md.GetMethodImplementation(ih);
                if (impl.MethodDeclaration.Kind == HandleKind.MemberReference)
                    NeedMember(impl.MethodDeclaration, ILOpCode.Nop, false, from);
            }
        }

        void NeedAttributes(CustomAttributeHandleCollection attributes, string from)
        {
            foreach (var ah in attributes)
            {
                var ctor = _md.GetCustomAttribute(ah).Constructor;
                if (ctor.Kind != HandleKind.MemberReference) continue;
                if (_md.GetMemberReference((MemberReferenceHandle)ctor).Parent is not { Kind: HandleKind.TypeReference } parent) continue;
                var r = (TypeReferenceHandle)parent;
                foreach (var set in _sets)
                    if (_checked.Add((set.Label + " attribute", parent)) && _exe.Check(set, r) is { } why)
                        Attributes.Add(new Miss { Set = set.Label, What = $"{Meta.Name(_md, r)}  [{Meta.Scope(_md, r)}]", Why = why, From = from });
            }
        }

        // Whether a field holds a value type, whose layout loading the type needs; a field
        // holding a reference, an array, a pointer or a primitive needs nothing loaded for it.
        // Read from the signature's bytes: VALUETYPE (0x11), or GENERICINST (0x15) followed by
        // VALUETYPE, after any custom modifiers (0x1F, 0x20), each with its type token.
        bool HoldsValueType(FieldDefinition f)
        {
            var blob = _md.GetBlobReader(f.Signature);
            blob.ReadSignatureHeader();
            while (true)
            {
                var b = blob.ReadByte();
                if (b is 0x1F or 0x20) { blob.ReadCompressedInteger(); continue; }
                if (b == 0x15) return blob.ReadByte() == 0x11;
                return b == 0x11;
            }
        }

        void NeedType(EntityHandle h, string from)
        {
            switch (h.Kind)
            {
                case HandleKind.TypeDefinition:
                    Load((TypeDefinitionHandle)h, from);
                    break;
                case HandleKind.TypeReference:
                {
                    var r = (TypeReferenceHandle)h;
                    if (_exe.Framework(r) is null) break;
                    TypesNamed++;
                    foreach (var set in _sets)
                        if (_checked.Add((set.Label, h)) && _exe.Check(set, r) is { } why)
                            Misses.Add(new Miss { Set = set.Label, What = $"{Meta.Name(_md, r)}  [{Meta.Scope(_md, r)}]", Why = why, From = from });
                    break;
                }
                case HandleKind.TypeSpecification:
                    NeedShape(s => _md.GetTypeSpecification((TypeSpecificationHandle)h).DecodeSignature(s, null), from);
                    break;
            }
        }

        // Decodes a method signature and needs every type it names.
        MethodSignature<string> Signature(Func<Shape, MethodSignature<string>> decode, string from)
        {
            var shape = new Shape(_md);
            var signature = decode(shape);
            foreach (var (handle, _) in shape.Seen) NeedType(handle, from);
            return signature;
        }

        void NeedShape(Func<Shape, object> decode, string from)
        {
            var shape = new Shape(_md);
            decode(shape);
            foreach (var (handle, _) in shape.Seen) NeedType(handle, from);
        }
    }
}
