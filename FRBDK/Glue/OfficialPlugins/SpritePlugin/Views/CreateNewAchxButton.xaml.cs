using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GlueFormsCore.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace OfficialPlugins.SpritePlugin.Views
{
    /// <summary>
    /// Interaction logic for CreateNewAchxButton.xaml
    /// </summary>
    public partial class CreateNewAchxButton : UserControl, IDataUi
    {
        public CreateNewAchxButton()
        {
            InitializeComponent();
        }

        public InstanceMember InstanceMember { get; set; }

        public bool SuppressSettingProperty { get; set; }

        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {

        }

        public ApplyValueResult TryGetValueOnUi(out object result)
        {
            result = null;
            return ApplyValueResult.Success;
        }

        public ApplyValueResult TrySetValueOnUi(object value)
        {
            return ApplyValueResult.Success;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AddNewFileViewModel viewModel = new ();

            var achxAti = AvailableAssetTypes.Self.AllAssetTypes
                .FirstOrDefault(item => item.Extension == "achx");
            viewModel.ForcedType = achxAti;
            viewModel.SelectedAssetTypeInfo = achxAti;



            GlueCommands.Self.DialogCommands.ShowAddNewFileDialogAsync(viewModel, GlueState.Self.CurrentElement);

        }
    }
}
