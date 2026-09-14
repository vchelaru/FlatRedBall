using System;

namespace FlatRedBall.Glue.Parsing
{
    /// <summary>
    /// Seam over Glue.csproj's <c>TypeManager.GetTypeFromString(string)</c> convenience overload -
    /// the one that reads <c>TypeManager</c>'s own loaded-assembly/plugin-type caches instead of
    /// taking them as parameters (see <see cref="TypeResolution.GetTypeFromString"/>, the pure
    /// algorithm those caches feed). GlueCommon code that only has a type string, not the caches
    /// themselves, calls through here. Same pattern as <c>IObjectFinderCore</c>/<c>IAvailableAssetTypesCore</c> -
    /// <c>TypeManager</c> implements this and wires itself into <see cref="TypeResolutionCore.Self"/>
    /// from its own static constructor. See #2276.
    /// </summary>
    public interface ITypeResolutionCore
    {
        Type GetTypeFromString(string typeString);
    }

    public static class TypeResolutionCore
    {
        public static ITypeResolutionCore Self { get; set; }
    }
}
