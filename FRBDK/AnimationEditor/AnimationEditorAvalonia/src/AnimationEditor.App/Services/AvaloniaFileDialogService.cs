using AnimationEditor.Core.IO;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;

namespace AnimationEditor.App.Services;

internal sealed class AvaloniaFileDialogService : IFileDialogService
{
    private readonly Window _owner;

    public AvaloniaFileDialogService(Window owner) => _owner = owner;

    public async Task<string?> PickSaveFileAsync(string title, string defaultExtension, string fileTypeDescription)
    {
        var file = await _owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            DefaultExtension = defaultExtension,
            FileTypeChoices = new[]
            {
                new FilePickerFileType(fileTypeDescription)
                {
                    Patterns = new[] { $"*.{defaultExtension}" }
                }
            }
        });
        return file?.Path.LocalPath;
    }

    public async Task<string?> PickOpenFileAsync(string title, string defaultExtension, string fileTypeDescription)
    {
        var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(fileTypeDescription)
                {
                    Patterns = new[] { $"*.{defaultExtension}" }
                }
            }
        });
        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }
}
