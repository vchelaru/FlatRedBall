using GameCommunicationPlugin.GlueControl.CommandSending;
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

namespace GameCommunicationPlugin.GlueControl.Views
{
    /// <summary>
    /// Interaction logic for ProfilingControl.xaml
    /// </summary>
    public partial class ProfilingControl : UserControl
    {
        public ProfilingControl()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var dto = new Dtos.GetProfilingDataDto();

            var response = await CommandSender.Self.Send<Dtos.ProfilingDataDto>(dto);

            if(response.Succeeded)
            {
                SummaryTextBlock.Text = response.Data.SummaryData;
                CollisionTextBlock.Text = response.Data.CollisionData;
            }
        }
    }
}
