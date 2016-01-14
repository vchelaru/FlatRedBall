using EditorObjects;
using FlatRedBall;
using SplineEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Threading;
using ToolTemplate;

namespace SplineEditorXna4.Gui.Forms
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class CameraPropertiesWindow : Window
    {
        #region Fields

        DispatcherTimer mTimer;

        CameraBoundsViewModel mBoundsVm;

        #endregion

        public CameraPropertiesWindow(ReactiveHud reactiveHud)
        {
            InitializeComponent();

            this.DataUiGrid.Instance = Camera.Main;

            mTimer = new DispatcherTimer();
            mTimer.Interval = TimeSpan.FromMilliseconds(100);
            mTimer.Start();
            mTimer.Tick += HandleTimerTick2;

            this.DataUiGrid.IgnoreAllMembers();

            this.DataUiGrid.MembersToIgnore.Remove("Orthogonal");



            this.DataUiGrid.PropertyChange += HandlePropertyChange;
            this.BoundsDataUiGrid.PropertyChange += HandlePropertyChange;

            mBoundsVm = new CameraBoundsViewModel(
                reactiveHud.CameraBounds);
            BoundsDataUiGrid.Instance = mBoundsVm;

        }

        private void HandlePropertyChange(string arg1, WpfDataUi.EventArguments.PropertyChangedArgs arg2)
        {
            UpdateMemberVisibility();
        }

        private void HandleTimerTick2(object sender, EventArgs e)
        {
            this.DataUiGrid.Refresh();
        }


        private void UpdateMemberVisibility()
        {
            UpdateCameraMemberVisibility();

            UpdateBoundsMemberVisibility();
        }

        private void UpdateBoundsMemberVisibility()
        {
            if (mBoundsVm.Orthogonal)
            {
                BoundsUnignore("OrthogonalWidth");
                BoundsUnignore("OrthogonalHeight");
            }
            else
            {
                BoundsUnignore("OrthogonalWidth");
                BoundsUnignore("OrthogonalHeight");
            }

        }

        private void UpdateCameraMemberVisibility()
        {
            Camera camera = Camera.Main;

            if (camera.Orthogonal)
            {
                CameraUnignore("OrthogonalWidth");
                CameraUnignore("OrthogonalHeight");
            }
            else
            {
                CameraIgnore("OrthogonalWidth");
                CameraIgnore("OrthogonalHeight");
            }
        }

        private void CameraUnignore(string member)
        {
            if (DataUiGrid.MembersToIgnore.Contains(member))
            {
                DataUiGrid.MembersToIgnore.Remove(member);
            }
        }

        private void BoundsUnignore(string member)
        {
            if (BoundsDataUiGrid.MembersToIgnore.Contains(member))
            {
                BoundsDataUiGrid.MembersToIgnore.Remove(member);
            }
        }


        private void CameraIgnore(string member)
        {
            if (!DataUiGrid.MembersToIgnore.Contains(member))
            {
                DataUiGrid.MembersToIgnore.Add(member);
            }
        }

        private void BoundsIgnore(string member)
        {
            if (!BoundsDataUiGrid.MembersToIgnore.Contains(member))
            {
                BoundsDataUiGrid.MembersToIgnore.Add(member);
            }
        }


    }
}
