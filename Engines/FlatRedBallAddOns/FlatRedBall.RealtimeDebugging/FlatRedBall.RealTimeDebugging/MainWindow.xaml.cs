using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FlatRedBall.RealTimeDebugging
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DataUiGridManager mDataUiGridManager;



        DispatcherTimer mTimer;

        public bool UpdatesInRealTime
        {
            get;
            set;
        }


        public MainWindow()
        {
            InitializeComponent();

            mDataUiGridManager = new DataUiGridManager();
            mDataUiGridManager.Initialize(this.DataUiGrid, this.TreeView);

            UpdateComboBox.DataContext = this;

            mTimer = new DispatcherTimer();
            mTimer.Interval = TimeSpan.FromMilliseconds( 100 );
            mTimer.Start();
            mTimer.Tick += HandleTimerTick2;
        }

        private void HandleTimerTick2(object sender, EventArgs e)
        {

            if (UpdatesInRealTime)
            {
                mDataUiGridManager.RefreshUi();
            }
        }

        private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
        {

        }





    }
}
