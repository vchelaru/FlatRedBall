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
using PolygonEditor.ViewModels;

namespace PolygonEditor.Controls
{
    /// <summary>
    /// Interaction logic for MainControl.xaml
    /// </summary>
    public partial class MainControl : UserControl
    {
        private MainGame _mainGame;

        ShapeCollectionViewModel ViewModel
        {
            get
            {
                return DataContext as ShapeCollectionViewModel;
            }
        }

        public MainControl()
        {
            InitializeComponent();

            Loaded += OnLoaded;

            this.DataContext = new ShapeCollectionViewModel();

        }

        public void Load(string absoluteFileName)
        {
            this.ViewModel.Load(absoluteFileName);
        }

        private void OnLoaded(Object sender, RoutedEventArgs routedEventArgs)
        {
            bool inDesignTime = System.ComponentModel.DesignerProperties.GetIsInDesignMode(
                new DependencyObject());
            if (!inDesignTime)
            {
                _mainGame = new MainGame(this.frbControl);
            }
        }


    }
}
