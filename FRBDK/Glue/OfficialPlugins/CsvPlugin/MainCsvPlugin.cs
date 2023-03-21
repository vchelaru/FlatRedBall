using EditorObjects.SaveClasses;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace OfficialPluginsCore.CsvNewFilePlugin
{
    [Export(typeof(PluginBase))]
    public class MainCsvPlugin : EmbeddedPlugin
    {
        FilePath OpenOfficeExecutablePath = "C:/Program Files/LibreOffice/program/soffice.exe";

        public override void StartUp()
        {
            AssignEvents();

       
        }

        private void AssignEvents()
        {
            this.AddNewFileOptionsHandler += HandleAddNewFileOptions;

            this.ReactToLoadedGlux += HandleGluxLoad;
        }

        private void HandleGluxLoad()
        {
            var hasOdsAssociation = GetIfHasOdsAssociation();

            var hasLibreOfficeInstalled = GetIfHasLibreOfficeInstalled();

            if(!hasOdsAssociation)
            {
                AddOdsAssociation();
            }

            if(!AvailableAssetTypes.Self.AllAssetTypes.Any(item => item.Extension == "ods"))
            {
                AddOdsAssetTypeInfo();
            }
        }

        private void AddOdsAssetTypeInfo()
        {
            AssetTypeInfo ati = new AssetTypeInfo();
            ati.Extension = "ods";
            ati.FriendlyName = $"Open Office/Libre Office Spreadsheet (ods)";

            AvailableAssetTypes.Self.AddAssetType(ati);
        }

        private bool GetIfHasOdsAssociation()
        {
            var hasOds = GlueState.Self.GlueSettingsSave?.BuildToolAssociations.Any(item => item.SourceFileType == "ods") == true;
            return hasOds;
        }

        private object GetIfHasLibreOfficeInstalled()
        {
            return OpenOfficeExecutablePath.Exists();
        }

        private void AddOdsAssociation()
        {
            //<BuildToolAssociation>
            //  <BuildTool>C:/Program Files/LibreOffice/program/soffice.exe</BuildTool>
            //  <IsBuildToolAbsolute>true</IsBuildToolAbsolute>
            //  <SourceFileType>odf</SourceFileType>
            //  <DestinationFileType>csv</DestinationFileType>
            //  <IncludeDestination>false</IncludeDestination>
            //  <SourceFileArgumentPrefix>--headless --convert-to csv</SourceFileArgumentPrefix>
            //</BuildToolAssociation>
            var assocation = new BuildToolAssociation();
            assocation.BuildTool = OpenOfficeExecutablePath.FullPath;
            assocation.IsBuildToolAbsolute = true;
            assocation.SourceFileType = "ods";
            assocation.DestinationFileType = "csv";
            assocation.IncludeDestination = false;
            assocation.SourceFileArgumentPrefix = "--headless --convert-to csv";

            GlueState.Self.GlueSettingsSave.BuildToolAssociations.Add(assocation);

            GlueCommands.Self.GluxCommands.SaveSettings();
        }

        private void HandleAddNewFileOptions(CustomizableNewFileWindow newFileWindow)
        {
            var spreadsheetBox = new GroupBox();
            spreadsheetBox.Header = "Runtime Type";
            var stack = new StackPanel();
            spreadsheetBox.Content = stack;
            spreadsheetBox.Visibility = Visibility.Collapsed;
            newFileWindow.AddCustomUi(spreadsheetBox);

            var dictionaryRadio = new RadioButton();
            dictionaryRadio.Content = "Dictionary";
            dictionaryRadio.IsChecked = true;
            stack.Children.Add(dictionaryRadio);

            var listRadio = new RadioButton();
            listRadio.Content = "List";
            stack.Children.Add(listRadio);

            bool IsSpreadsheet(AssetTypeInfo ati) =>
                ati?.FriendlyName == "Spreadsheet (.csv)";

            newFileWindow.SelectionChanged += (not, used) =>
            {
                var ati = newFileWindow.SelectedItem;
                spreadsheetBox.Visibility = IsSpreadsheet(ati).ToVisibility();
            };

            newFileWindow.GetCreationOption += () =>
            {
                var ati = newFileWindow.SelectedItem;
                if(IsSpreadsheet(ati))
                {
                    if (dictionaryRadio.IsChecked == true)
                    {
                        return "Dictionary";
                    }
                    else
                    {
                        return "List";
                    }
                }
                else
                {
                    return null;
                }
            };
        }
    }
}
