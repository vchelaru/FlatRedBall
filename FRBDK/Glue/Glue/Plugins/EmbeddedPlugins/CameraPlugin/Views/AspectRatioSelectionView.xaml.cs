using GlueFormsCore.Plugins.EmbeddedPlugins.CameraPlugin.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CameraPlugin.Views
{
    /// <summary>
    /// Interaction logic for AspectRatioSelectionView.xaml
    /// </summary>
    public partial class AspectRatioSelectionView : UserControl
    {
        AspectRatioViewModel ViewModel => DataContext as AspectRatioViewModel;


        public AspectRatioSelectionView()
        {
            InitializeComponent();
        }


        private void AspectRatioDropdownClick(object sender, RoutedEventArgs e)
        {
            AspectRatioDropDown.Items.Clear();
            void Add(decimal aspectWide, decimal aspectTall, string alternativeText = null)
            {
                var vm = new AspectRatioDropDownViewModel(aspectWide, aspectTall, alternativeText);
                var menuItem = new MenuItem();
                menuItem.Header = vm;
                menuItem.Click += (not, used) =>
                {
                    ViewModel.AspectRatioWidth = aspectWide;
                    ViewModel.AspectRatioHeight = aspectTall;
                };
                AspectRatioDropDown.Items.Add(menuItem);
            }

            var calculated = CalculateCurrentAspectRatio();

            if (calculated.AspectWide != 0)
            {
                Add(calculated.AspectWide, calculated.AspectTall, $"Match current ({calculated.AspectWide}:{calculated.AspectTall})");
            }

            Add(4, 3);
            Add(16, 10);
            Add(16, 9);
            Add(21, 9);

            AspectRatioDropDown.IsOpen = true;

        }

        private (decimal AspectWide, decimal AspectTall) CalculateCurrentAspectRatio()
        {
            if (ViewModel.ParentViewModel.ResolutionHeight <= 0 || ViewModel.ParentViewModel.ResolutionWidth <= 0)
            {
                return (1, 1);
            }

            var aspectToMatch = ViewModel.ParentViewModel.ResolutionWidth / (decimal)ViewModel.ParentViewModel.ResolutionHeight;

            if (ViewModel.ParentViewModel.ResolutionWidth == ViewModel.ParentViewModel.ResolutionHeight)
            {
                return (1, 1);
            }
            else if (ViewModel.ParentViewModel.ResolutionHeight > ViewModel.ParentViewModel.ResolutionWidth)
            {
                for (decimal height = 1; height < 50; height++)
                {
                    for (decimal width = 1; width <= height; width++)
                    {
                        var calculatedAspect = width / height;

                        if (calculatedAspect == aspectToMatch)
                        {
                            return (width, height);
                        }
                    }
                }
            }
            else if (ViewModel.ParentViewModel.ResolutionWidth > ViewModel.ParentViewModel.ResolutionHeight)
            {
                for (decimal width = 1; width <= 50; width++)
                {
                    for (decimal height = 1; height < width; height++)
                    {
                        var calculatedAspect = width / height;

                        if (calculatedAspect == aspectToMatch)
                        {
                            return (width, height);
                        }
                    }
                }
            }

            return (0, 0);
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tBox = (TextBox)sender;
                DependencyProperty prop = TextBox.TextProperty;

                BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
                if (binding != null) { binding.UpdateSource(); }
            }
        }
    }

}
