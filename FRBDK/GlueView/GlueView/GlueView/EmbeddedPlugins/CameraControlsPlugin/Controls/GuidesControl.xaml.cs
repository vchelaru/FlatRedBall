using FlatRedBall;
using FlatRedBall.Utilities;
using GlueView.EmbeddedPlugins.CameraControlsPlugin.ViewModels;
using GlueView.Facades;
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
using WpfDataUi.DataTypes;

namespace GlueView.EmbeddedPlugins.CameraControlsPlugin.Controls
{
    /// <summary>
    /// Interaction logic for GuidesControl.xaml
    /// </summary>
    public partial class GuidesControl : UserControl
    {
        CameraViewModel ViewModel
        {
            get
            {
                return DataContext as CameraViewModel;
            }
        }

        public GuidesControl()
        {
            InitializeComponent();

            this.DataContextChanged += HandleDataContextChanged;
        }


        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(ViewModel != null)
            {
                ViewModel.PropertyChanged += HandleViewModelPropertyChanged;
            }
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(CameraViewModel.PropertyGridDisplayObject):
                    this.DataGrid.Instance = ViewModel.PropertyGridDisplayObject;
                    RefreshCategories();
                    break;
            }
        }

        private void RefreshCategories()
        {
            DataGrid.Categories.Clear();

            MemberCategory category = new MemberCategory();
            category.Members.Add(new InstanceMember(nameof(Camera.X), this.DataGrid.Instance));
            category.Members.Add(new InstanceMember(nameof(Camera.Y), this.DataGrid.Instance));
            category.Members.Add(new InstanceMember(nameof(Camera.Z), this.DataGrid.Instance));

            var filteringMember = new InstanceMember("Filtering", null);
            filteringMember.CustomGetEvent += (notUsed) => 
                FlatRedBallServices.GraphicsOptions.TextureFilter == Microsoft.Xna.Framework.Graphics.TextureFilter.Linear;
            filteringMember.CustomGetTypeEvent += (notUsed) => typeof(bool);
            filteringMember.CustomSetEvent += (instance, value) =>
            {
                bool newValue = (bool)value;

                if (newValue)
                {
                    FlatRedBallServices.GraphicsOptions.TextureFilter = Microsoft.Xna.Framework.Graphics.TextureFilter.Linear;
                }
                else
                {
                    FlatRedBallServices.GraphicsOptions.TextureFilter = Microsoft.Xna.Framework.Graphics.TextureFilter.Point;
                }
            };

            category.Members.Add(filteringMember);

            // There's no built-in control for this, need to bring in XCeed and wrap it...or not, because
            // maybe the color value should really be part of Glue.
            //categories.Members.Add(new InstanceMember(nameof(Camera.BackgroundColor), this.DataGrid.Instance));
            category.Members.Add(new InstanceMember(nameof(Camera.FieldOfView), this.DataGrid.Instance));

            category.Members.Add(new InstanceMember(nameof(Camera.Orthogonal), this.DataGrid.Instance));
            category.Members.Add(new InstanceMember(nameof(Camera.OrthogonalWidth), this.DataGrid.Instance));
            category.Members.Add(new InstanceMember(nameof(Camera.OrthogonalHeight), this.DataGrid.Instance));

            category.Members.Add(new InstanceMember(nameof(Camera.FarClipPlane), this.DataGrid.Instance));

            foreach(var member in category.Members)
            {
                member.DisplayName = StringFunctions.InsertSpacesInCamelCaseString( member.Name);
            }

            DataGrid.Categories.Add(category);
        }

        internal void UpdateDisplayedValues()
        {
            DataGrid.Refresh();
        }

        private void ResetCameraButton_Click(object sender, RoutedEventArgs e)
        {
            // todo - apply settings from Glue
            var glueProject = GlueViewState.Self.CurrentGlueProject;

            if(glueProject.DisplaySettings != null)
            {
                var settings = glueProject.DisplaySettings;

                if(settings.Is2D)
                {
                    Camera.Main.UsePixelCoordinates();

                }
                else
                {
                    Camera.Main.Orthogonal = false;
                    Camera.Main.UsePixelCoordinates3D(0);
                }
            }
            else
            {
                Camera.Main.UsePixelCoordinates();
            }
        }
    }
}
