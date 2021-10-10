using OfficialPlugins.Compiler.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ToolsUtilities;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace OfficialPlugins.Compiler.Views
{
    /// <summary>
    /// Interaction logic for GlueViewSettings.xaml
    /// </summary>
    public partial class GlueViewSettings : UserControl
    {
        public GlueViewSettingsViewModel ViewModel
        {
            get => this.DataUiGrid.Instance as GlueViewSettingsViewModel;
            set
            {
                this.DataUiGrid.Instance = value;
                CustomizeDisplay();
            }
        }

        public GlueViewSettings()
        {
            InitializeComponent();

        }

        private void CustomizeDisplay()
        {
            foreach(var category in DataUiGrid.Categories)
            {
                category.Name = "";
                foreach(var member in category.Members)
                {
                    member.DisplayName = StringFunctions.InsertSpacesInCamelCaseString(member.DisplayName);
                }
            }
        }


    }
}
