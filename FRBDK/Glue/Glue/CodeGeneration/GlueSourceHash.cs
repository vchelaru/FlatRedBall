using System;
using System.Security.Cryptography;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using Newtonsoft.Json;

namespace FlatRedBall.Glue.CodeGeneration;

/// <summary>
/// A hash of an element's saved content, compiled into its generated code so a running game can report
/// which version of each element it was built from. The hash sits in the same generated file as the code
/// it describes, so the compiler always sees the two together, whoever runs the build and whenever.
/// </summary>
public static class GlueSourceHash
{
    /// <summary>
    /// Prefix of the <see cref="System.Reflection.AssemblyMetadataAttribute"/> key each element's hash is
    /// compiled under. The rest of the key is the element's Glue name.
    /// </summary>
    public const string AttributeKeyPrefix = "GlueSourceHash:";

    static readonly JsonSerializerSettings settings = new()
    {
        // Same as the per-element .glsj/.glej save, so the hash tracks what the element file holds.
        DefaultValueHandling = DefaultValueHandling.Ignore,
    };

    public static string Compute(GlueElement element)
    {
        var serialized = JsonConvert.SerializeObject(element, settings);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(serialized));
        return Convert.ToHexString(hash, 0, 8);
    }
}
