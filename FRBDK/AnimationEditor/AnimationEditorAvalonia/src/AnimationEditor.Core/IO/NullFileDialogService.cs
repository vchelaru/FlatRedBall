using System.Threading.Tasks;

namespace AnimationEditor.Core.IO;

/// <summary>
/// No-op <see cref="IFileDialogService"/> used as the default until the Avalonia
/// app layer wires in a real <c>StorageProvider</c>-backed implementation.
/// Always returns <c>null</c> (simulates the user cancelling every dialog).
/// </summary>
public sealed class NullFileDialogService : IFileDialogService
{
    public static readonly NullFileDialogService Instance = new();

    public Task<string?> PickSaveFileAsync(string title, string defaultExtension, string fileTypeDescription)
        => Task.FromResult<string?>(null);

    public Task<string?> PickOpenFileAsync(string title, string defaultExtension, string fileTypeDescription)
        => Task.FromResult<string?>(null);
}
