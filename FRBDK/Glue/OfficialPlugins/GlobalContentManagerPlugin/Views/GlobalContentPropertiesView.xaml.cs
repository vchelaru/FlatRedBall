using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace OfficialPlugins.GlobalContentManagerPlugin.Views
{
    /// <summary>
    /// Interaction logic for GlobalContentPropertiesView.xaml
    /// </summary>
    public partial class GlobalContentPropertiesView : UserControl
    {
        ViewModels.GlobalContentPropertiesViewModel ViewModel => (ViewModels.GlobalContentPropertiesViewModel)DataContext;
        public GlobalContentPropertiesView()
        {
            InitializeComponent();
        }

        public void Refresh()
        {
            var vm = new ViewModels.GlobalContentPropertiesViewModel();

            // SetFrom before assigning PropertyChanged +=
            vm.SetFrom(GlueState.Self.CurrentGlueProject.GlobalContentSettingsSave);

            vm.PropertyChanged += HandleViewModelPropertyChanged;

            this.DataContext = vm;
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // get this on the primary thread:
            var vm = ViewModel;
            switch(e.PropertyName)
            {
                case nameof(ViewModels.GlobalContentPropertiesViewModel.GenerateLoadGlobalContentCode):
                case nameof(ViewModels.GlobalContentPropertiesViewModel.LoadAsynchronously):
                    TaskManager.Self.AddAsync(() =>
                    {
                        vm.SetOn(GlueState.Self.CurrentGlueProject.GlobalContentSettingsSave);
                        GlueCommands.Self.GluxCommands.SaveGlujFile();

                        // In the future this could expand to have lots of properties affecting how
                        // GlobalContent and Game1 generate, so let's just regenerate everything:
                        GlueCommands.Self.GenerateCodeCommands.GenerateGlobalContentCode();
                        GlueCommands.Self.GenerateCodeCommands.GenerateGame1();
                    }, "Setting GlobalContent Settings");
                    break;
            }
        }
    }
}
