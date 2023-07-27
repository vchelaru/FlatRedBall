using FlatRedBall.Attributes;
using OfficialPlugins.PathPlugin.Managers;
using OfficialPlugins.PathPlugin.ViewModels;
using OfficialPlugins.VariableDisplay;
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
using WpfDataUi;
using WpfDataUi.DataTypes;
using InstanceMember = WpfDataUi.DataTypes.InstanceMember;

namespace OfficialPlugins.PathPlugin.Views
{
    /// <summary>
    /// Interaction logic for PathView.xaml
    /// </summary>
    public partial class PathView : UserControl, IDataUi
    {
        PathViewModel ViewModel => DataContext as PathViewModel;

        public Action<int?> SelectIndex;

        public PathView()
        {
            InitializeComponent();

            var viewModel = new PathViewModel();
            viewModel.PropertyChanged += ViewModelManager.HandlePathViewModelPropertyChanged;
            viewModel.PathSegments.CollectionChanged += ViewModelManager.HandlePathSegmentsCollectionChanged;

            DataContext = viewModel;
            ViewModelManager.MainViewModel = viewModel;
        }

        InstanceMember instanceMember;
        public InstanceMember InstanceMember 
        {
            get => instanceMember;
            set
            {


                bool instanceMemberChanged = instanceMember != value;
                if (instanceMember != null && instanceMemberChanged)
                {
                    instanceMember.PropertyChanged -= HandlePropertyChange;
                }
                instanceMember = value;
                if (instanceMember != null && instanceMemberChanged)
                {
                    instanceMember.PropertyChanged += HandlePropertyChange;
                }

                // update view model
                var asDataGridItem = value as DataGridItem;
                var variableName = asDataGridItem.UnmodifiedVariableName;
                // name must be set before updating the VM
                ViewModel.VariableName = variableName;
                ViewModelManager.UpdateViewModelToModel();
            }
        }

        private void HandlePropertyChange(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InstanceMember.Value))
            {
                this.Refresh();

            }
        }

        private void CloseSegmentClicked(PathSegmentViewModel sender)
        {
            ViewModel.PathSegments.Remove(sender);
        }

        public bool SuppressSettingProperty { get; set; }

        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            ViewModelManager.UpdateViewModelToModel();
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

        private void AddToPathButtonClicked(object sender, RoutedEventArgs e)
        {
            ViewModelManager.CreateNewSegmentViewModel();
        }

        public void FocusIndex(int index)
        {
            if(ViewModel?.PathSegments.Count > index && index > -1)
            {
                ViewModel.PathSegments[index].RequestFocusTextBox(index);
            }
        }
    }
}
