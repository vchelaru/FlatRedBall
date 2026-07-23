using FRBDKUpdater;
using Npc.ViewModels;
using ToolsUtilities;

namespace Npc;

/// <summary>
/// Abstracts downloading a template zip so project creation can run without the WPF download window
/// (e.g. in tests, which instead supply a local template via <see cref="PlatformProjectInfo.LocalSourceFile"/>).
/// Production uses <see cref="WpfDownloadService"/>.
/// </summary>
public interface IDownloadService
{
    GeneralResponse Download(NewProjectViewModel viewModel, string destinationZip, string fileToDownload);
}

public class WpfDownloadService : IDownloadService
{
    public GeneralResponse Download(NewProjectViewModel viewModel, string destinationZip, string fileToDownload)
    {
        var urs = new UpdaterRuntimeSettings();
        urs.FileToDownload = fileToDownload;
        urs.FormTitle = "Downloading " + viewModel.SelectedProject.FriendlyName;

        urs.LocationToSaveFile = destinationZip;

        string whereToSaveSettings =
            FileManager.UserApplicationDataForThisApplication + "DownloadInformation." + UpdaterRuntimeSettings.RuntimeSettingsExtension;

        var window = new UpdaterWpf.Views.MainWindow(whereToSaveSettings, urs);
        window.CancelButtonVisibility = viewModel.IsCancelButtonVisible
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;

        window.Owner = viewModel.owner;
        var result = window.ShowDialog();

        if (result == null)
        {
            return GeneralResponse.UnsuccessfulWith("Download Cancelled");
        }
        else
        {
            return window.GeneralResponse ?? GeneralResponse.UnsuccessfulWith("Download Cancelled");
        }
    }
}
