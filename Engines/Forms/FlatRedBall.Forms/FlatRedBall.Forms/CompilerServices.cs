// 2025-05-24 Justin: this is required to compile Forms for netstandard because
// this property only existed after .NET Core 5. See:
// https://stackoverflow.com/questions/64749385/predefined-type-system-runtime-compilerservices-isexternalinit-is-not-defined
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
