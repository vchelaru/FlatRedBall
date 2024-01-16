using FlatRedBall.Glue.Plugins;
using FlatRedBall.IO;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.ManagePlugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using System.Diagnostics;
using System.IO;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using L = Localization;
using MessageBox = System.Windows.Forms.MessageBox;
using GlueFormsCore.Extensions;

namespace FlatRedBall.Glue.Controls;

/// <summary>
/// Interaction logic for CreatePluginWindow.xaml
/// This screen is used on the top bar of the engine -> plugins -> create plugin,
/// to allow users to load their custom plugins.
/// </summary>
public partial class CreatePluginWindow
{
    private readonly SaveFileDialog _sfdPlugin;
    private readonly FolderBrowserDialog _fbdPath;

    public CreatePluginWindow()
    {
        InitializeComponent();

        _sfdPlugin = new SaveFileDialog();
        _sfdPlugin.Title = L.Texts.PluginFiles;
        _sfdPlugin.Filter = $"{L.Texts.PluginFiles}|*.plug";

        _fbdPath = new FolderBrowserDialog();

        FillInstalledPluginComboBox();

        UpdateToSelectedSource();

        Loaded += HandleLoaded;
    }

    private void HandleLoaded(object sender, RoutedEventArgs e)
    {
        this.MoveToCursor();
    }

    #region Methods

    private void FillInstalledPluginComboBox()
    {
        var plugins = PluginManagerBase.AllPluginContainers
            .Where(pluginContainer => pluginContainer.Plugin is not EmbeddedPlugin)
            .ToList();

        plugins.Sort((first, second) => String.Compare(first.Name, second.Name, StringComparison.OrdinalIgnoreCase));

        foreach (var plugin in plugins)
        {
            this.FromInstalledPluginComboBox.Items.Add(plugin);
        }
    }

    /// <summary>
    /// Invoked when the button is pressed that allows the user to pick the path the plugin is located in.
    /// </summary>
    private void btnPath_Click(object sender, EventArgs e)
    {
        if (_fbdPath.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            FromFolderTextBox.Text = _fbdPath.SelectedPath;
        }
    }

    /// <summary>
    /// User cancels form
    /// </summary>
    private void Cancel(object sender, EventArgs e)
    {
        this.DialogResult = false;
        this.Close();
    }

    /// <summary>
    /// User submits form
    /// </summary>
    private void Submit(object sender, EventArgs e)
    {
        //Validate plugin folder
        if (!Directory.Exists(PluginFolder))
        {
            MessageBox.Show(L.Texts.PluginNeedsValidFolder);
            return;
        }

        //Prompt for save path
        if (_sfdPlugin.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        Debug.Assert(this.AllFilesRadioButton.IsChecked != null);

        var exportPluginLogic = new ExportPluginLogic();
        exportPluginLogic.CreatePluginFromDirectory(
            sourceDirectory: PluginFolder, destinationFileName: _sfdPlugin.FileName,
            includeAllFiles: this.AllFilesRadioButton.IsChecked.Value);


        var startInfo = new ProcessStartInfo
        {
            FileName = FileManager.GetDirectory(_sfdPlugin.FileName),
            UseShellExecute = true
        };

        Process.Start(startInfo);
        this.DialogResult = true;
        this.Close();
    }

    /// <summary>
    /// This causes the ''FromFolder'' functions to disable/enable and vice versa for the FromInstall combobox.
    /// </summary>
    private void UpdateToSelectedSource()
    {
        Debug.Assert(FromFolderRadioButton.IsChecked != null, "FromFolderRadioButton.IsChecked != null");
        var fromFolder = FromFolderRadioButton.IsChecked.Value ? Visibility.Visible : Visibility.Hidden;
        var notFromFolder = FromFolderRadioButton.IsChecked.Value ? Visibility.Hidden : Visibility.Visible;

        FromFolderPanel.Visibility = fromFolder;
        FromFolderTextBox.Visibility = fromFolder;

        FromInstalledPluginComboBox.Visibility = notFromFolder;
    }

    /// <summary>
    /// Invoked when the PluginSourceGroup radio group changes value
    /// This causes the ''FromFolder'' functions to disable/enable and vice versa for the FromInstall combobox.
    /// </summary>
    private void PluginSourceGroupChanged(object sender, EventArgs e)
    {
        UpdateToSelectedSource();
    } 
    #endregion


    #region Properties

    /// <summary>
    /// Returns the folder the plugin is located in,
    /// if the user decides to load from a folder or if the selected combobox item is a PluginContainer
    /// </summary>
    string PluginFolder
    {
        get
        {
            if (FromFolderRadioButton.IsChecked.HasValue && FromFolderRadioButton.IsChecked.Value)
            {
                return FromFolderTextBox.Text;
            }

            if (FromInstalledPluginComboBox.SelectedItem is PluginContainer container)
            {
                return FileManager.GetDirectory(container.AssemblyLocation);
            }

            return null;
        }
    }

    #endregion
}
