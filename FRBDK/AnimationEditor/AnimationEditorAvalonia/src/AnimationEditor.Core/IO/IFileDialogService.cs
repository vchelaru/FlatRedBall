using System.Threading.Tasks;

namespace AnimationEditor.Core.IO;

/// <summary>
/// Abstracts native file-picker dialogs so that commands depending on them
/// (e.g. Save As) can be unit-tested by injecting a stub implementation.
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Show a save-file dialog. Returns the chosen path, or <c>null</c> if cancelled.
    /// </summary>
    Task<string?> PickSaveFileAsync(string title, string defaultExtension, string fileTypeDescription);

    /// <summary>
    /// Show an open-file dialog. Returns the chosen path, or <c>null</c> if cancelled.
    /// </summary>
    Task<string?> PickOpenFileAsync(string title, string defaultExtension, string fileTypeDescription);
}
