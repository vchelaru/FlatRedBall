using GumPlugin.DataGeneration;
using GumPlugin.Managers;
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

namespace GumPlugin.Controls
{
    /// <summary>
    /// Interaction logic for GumControl.xaml
    /// </summary>
    public partial class GumControl : UserControl
    {

        public event PropertyChangedEventHandler PropertyChanged;

        public GumControl()
        {
            InitializeComponent();
        }

        private void HandleGenerateBehaviors(object sender, RoutedEventArgs e)
        {
            var project = AppState.Self.GumProjectSave;

            bool doesProjectAlreadyHaveBehavior =
                project.Behaviors.Any(item => item.Name == BehaviorGenerator.ButtonBehaviorName);

            if(!doesProjectAlreadyHaveBehavior)
            {
                var behaviorSave = BehaviorGenerator.CreateButtonBehavior();
                AppCommands.Self.AddBehavior(behaviorSave);
                AppCommands.Self.SaveGlux();



            }
        }
    }
}
