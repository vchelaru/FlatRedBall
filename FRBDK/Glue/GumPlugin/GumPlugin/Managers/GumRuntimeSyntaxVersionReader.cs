using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;

namespace GumPlugin.Managers;

/// <summary>
/// Reads the <c>Gum.DataTypes.GumSyntaxVersionAttribute</c> stamped on a referenced Gum runtime
/// assembly (e.g. GumCore.DesktopGlNet6.dll), so Glue codegen can tell whether the engine a user's
/// project actually references supports a given Gum runtime feature - as opposed to trusting the
/// gumx project version alone, which only says what the *project* wants, not what the *referenced
/// binary* actually contains (issue #1967: a v3 gumx generating FilledStrokedRectangle-backed
/// Rectangle code against a stale, non-source-linked GumCore.*.dll that predates that type).
/// </summary>
/// <remarks>
/// Deliberately reads raw ECMA-335 metadata via <see cref="PEReader"/> rather than loading the
/// assembly (Assembly.LoadFrom/MetadataLoadContext) - a real GumCore.*.dll pulls in MonoGame/XNA
/// native dependencies Glue has no business loading just to read one assembly-level attribute.
/// Mirrors Gum.ProjectServices.CodeGeneration.SyntaxVersionDetectionService.ReadVersionFromAssembly
/// in the sibling Gum repo (that service's own equivalent, used by Gum's own tooling) - kept as a
/// separate small copy here rather than a shared reference, since Glue does not otherwise depend on
/// Gum.ProjectServices.
/// </remarks>
public static class GumRuntimeSyntaxVersionReader
{
    /// <summary>
    /// Resolves a "Reference" assembly name (e.g. "GumCore.DesktopGlNet6") to a HintPath in the
    /// given project's raw .csproj XML, or null if not found as a plain DLL reference (a
    /// ProjectReference/PackageReference isn't handled here - see <see cref="TryFindPackageReferenceDll"/>
    /// and the source-linked exemption callers apply via IsFrbSourceLinked()).
    /// </summary>
    internal static string TryFindHintPath(string csprojContents, string csprojDirectory, string referenceName)
    {
        string pattern =
            $@"<Reference\s+Include=""{Regex.Escape(referenceName)}""\s*>\s*<HintPath>([^<]+)</HintPath>";
        Match match = Regex.Match(csprojContents, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success)
        {
            return null;
        }

        string hintPath = match.Groups[1].Value.Trim();
        try
        {
            return Path.GetFullPath(Path.Combine(csprojDirectory, hintPath));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Resolves a NuGet-restored "FlatRedBall.GumCore.*"-style PackageReference to its lib DLL in
    /// the global packages cache, mirroring SyntaxVersionDetectionService.FindDllInNuGetCache.
    /// </summary>
    internal static string TryFindPackageReferenceDll(string csprojContents, string packageName, string nuGetCacheRoot)
    {
        string elementPattern =
            $@"<PackageReference\b(?=[^>]*\bInclude=""{Regex.Escape(packageName)}"")[^>]*?(?:/>|>(?<body>.*?)</PackageReference>)";
        Match elementMatch = Regex.Match(csprojContents, elementPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!elementMatch.Success)
        {
            return null;
        }

        string version = null;
        Match versionAttribute = Regex.Match(elementMatch.Value, @"\bVersion=""([^""]*)""", RegexOptions.IgnoreCase);
        if (versionAttribute.Success)
        {
            version = versionAttribute.Groups[1].Value;
        }
        else if (elementMatch.Groups["body"].Success)
        {
            Match versionElement = Regex.Match(elementMatch.Groups["body"].Value, "<Version>([^<]*)</Version>", RegexOptions.IgnoreCase);
            if (versionElement.Success)
            {
                version = versionElement.Groups[1].Value;
            }
        }

        if (string.IsNullOrEmpty(version))
        {
            return null;
        }

        string packageDir = Path.Combine(nuGetCacheRoot, packageName.ToLowerInvariant(), version, "lib");
        if (!Directory.Exists(packageDir))
        {
            return null;
        }

        string[] preferredTfms = { "net8.0", "net7.0", "net6.0", "netstandard2.1", "netstandard2.0" };
        foreach (string tfm in preferredTfms)
        {
            string tfmDir = Path.Combine(packageDir, tfm);
            if (Directory.Exists(tfmDir))
            {
                string dll = Directory.EnumerateFiles(tfmDir, "*.dll", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (dll != null)
                {
                    return dll;
                }
            }
        }

        try
        {
            return Directory.EnumerateFiles(packageDir, "*.dll", SearchOption.AllDirectories).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Reads the <c>GumSyntaxVersion</c> assembly attribute out of the DLL at <paramref name="dllPath"/>.
    /// Returns null if the file doesn't exist, can't be read as a PE/metadata assembly, or doesn't
    /// declare the attribute at all - per the attribute's own doc, an absent attribute means
    /// pre-unification (version 0) conventions, so callers should treat null the same as "too old".
    /// </summary>
    public static int? ReadVersion(string dllPath)
    {
        if (string.IsNullOrEmpty(dllPath) || !File.Exists(dllPath))
        {
            return null;
        }

        try
        {
            using FileStream stream = File.OpenRead(dllPath);
            using var peReader = new PEReader(stream);
            MetadataReader reader = peReader.GetMetadataReader();
            AssemblyDefinition assemblyDefinition = reader.GetAssemblyDefinition();

            foreach (CustomAttributeHandle attributeHandle in assemblyDefinition.GetCustomAttributes())
            {
                CustomAttribute customAttribute = reader.GetCustomAttribute(attributeHandle);
                if (GetAttributeTypeName(reader, customAttribute) != "GumSyntaxVersionAttribute")
                {
                    continue;
                }

                CustomAttributeValue<string> decoded = customAttribute.DecodeValue(NullCustomAttributeTypeProvider.Instance);
                foreach (CustomAttributeNamedArgument<string> namedArgument in decoded.NamedArguments)
                {
                    if (namedArgument.Name == "Version" && namedArgument.Value is int version)
                    {
                        return version;
                    }
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string GetAttributeTypeName(MetadataReader reader, CustomAttribute customAttribute)
    {
        StringHandle nameHandle;
        switch (customAttribute.Constructor.Kind)
        {
            case HandleKind.MemberReference:
                MemberReference memberReference = reader.GetMemberReference((MemberReferenceHandle)customAttribute.Constructor);
                nameHandle = memberReference.Parent.Kind switch
                {
                    HandleKind.TypeReference => reader.GetTypeReference((TypeReferenceHandle)memberReference.Parent).Name,
                    HandleKind.TypeDefinition => reader.GetTypeDefinition((TypeDefinitionHandle)memberReference.Parent).Name,
                    _ => default
                };
                break;
            case HandleKind.MethodDefinition:
                MethodDefinition methodDefinition = reader.GetMethodDefinition((MethodDefinitionHandle)customAttribute.Constructor);
                nameHandle = reader.GetTypeDefinition(methodDefinition.GetDeclaringType()).Name;
                break;
            default:
                return null;
        }

        return nameHandle.IsNil ? null : reader.GetString(nameHandle);
    }

    private sealed class NullCustomAttributeTypeProvider : ICustomAttributeTypeProvider<string>
    {
        public static readonly NullCustomAttributeTypeProvider Instance = new();

        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();
        public string GetSystemType() => "Type";
        public string GetSZArrayType(string elementType) => elementType + "[]";
        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => "";
        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => "";
        public string GetTypeFromSerializedName(string name) => name;
        public PrimitiveTypeCode GetUnderlyingEnumType(string type) => PrimitiveTypeCode.Int32;
        public bool IsSystemType(string type) => false;
    }
}
