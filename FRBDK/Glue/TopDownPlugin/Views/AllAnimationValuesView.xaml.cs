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
using TopDownPlugin.ViewModels;

namespace TopDownPlugin.Views
{
    /// <summary>
    /// Interaction logic for AllAnimationValuesView.xaml
    /// </summary>
    public partial class AllAnimationValuesView : UserControl
    {
        public AllAnimationValuesView()
        {
            InitializeComponent();

            var viewModel = new AllAnimationValuesViewModel();

            viewModel.AnimationRows.Add(new AnimationRowViewModel
            {

            });
            viewModel.AnimationRows.Add(new AnimationRowViewModel
            {

            });
            viewModel.AnimationRows.Add(new AnimationRowViewModel
            {

            });

            var animations = viewModel.AnimationRows[0];
            animations.Animations.Add(new AnimationSetViewModel { AnimationSetName = "first" });
            animations.Animations.Add(new AnimationSetViewModel { AnimationSetName = "segundo" });
            
            animations = viewModel.AnimationRows[1];
            animations.Animations.Add(new AnimationSetViewModel { AnimationSetName = "scorby" });
            animations.Animations.Add(new AnimationSetViewModel { AnimationSetName = "chiefto" });
            animations.Animations.Add(new AnimationSetViewModel { AnimationSetName = "scamby" });
            
            animations = viewModel.AnimationRows[2];
            animations.Animations.Add(new AnimationSetViewModel { AnimationSetName = "gumbertaft" });
            animations.Animations.Add(new AnimationSetViewModel { AnimationSetName = "skeel" });
            animations.Animations.Add(new AnimationSetViewModel { AnimationSetName = "noplay" });
            animations.Animations.Add(new AnimationSetViewModel { AnimationSetName = "giminasto" });


            this.DataContext = viewModel;
        }
    }
}
