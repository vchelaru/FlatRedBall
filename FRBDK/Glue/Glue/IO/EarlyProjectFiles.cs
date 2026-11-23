using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.IO
{
    /// <summary>
    /// The project <see cref="ProjectLoader.StartEarlyReads"/> is reading ahead: its .csproj, and the names of its
    /// Global Content files once the early .gluj read finishes (empty if that read failed).
    /// </summary>
    public sealed record EarlyProjectFiles(string CsprojFullPath, Task<IReadOnlyList<string>> GlobalFileNames);
}
