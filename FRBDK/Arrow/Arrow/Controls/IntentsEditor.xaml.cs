using FlatRedBall.Arrow.DataTypes;
using FlatRedBall.Arrow.Gui;
using FlatRedBall.Arrow.Managers;
using FlatRedBall.Arrow.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace FlatRedBall.Arrow.Controls
{
    /// <summary>
    /// Interaction logic for IntentsEditor.xaml
    /// </summary>
    public partial class IntentsEditor : UserControl, INotifyPropertyChanged
    {
        #region Fields

        ArrowIntentVm mCurrentArrowIntentVm;

        ArrowIntentComponentVm mCurrentArrowIntentComponentVm;

        #endregion

        #region Properties

        ArrowProjectVm ViewModel
        {
            get
            {
                return DataContext as ArrowProjectVm;
            }
        }

        public ArrowIntentVm CurrentArrowIntentVm
        {
            get 
            { 
                return mCurrentArrowIntentVm; 
            }
            set 
            { 
                mCurrentArrowIntentVm = value;

                NotifyPropertyChanged("CurrentArrowIntentVm");
            }        
        }

        public ArrowIntentComponentVm CurrentArrowIntentComponentVm
        {
            get
            {
                return mCurrentArrowIntentComponentVm;
            }
            set
            {
                mCurrentArrowIntentComponentVm = value;
                DataGridUi.Instance = mCurrentArrowIntentComponentVm;
                DataGridUi.Refresh();
                NotifyPropertyChanged("CurrentArrowIntentComponentVm");
            }
        }


        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Constructor

        public IntentsEditor()
        {
            InitializeComponent();

            ComponentsListBox.DataContext = this;
            //this.DataGridUi.DataContext = this;

            this.DataGridUi.PropertyChange += HandlePropertyChanged;

            this.DataGridUi.MembersToIgnore.Add("Model");
        }

        

        #endregion

        private void AddItemClick(object sender, RoutedEventArgs e)
        {
            TextInputWindow tiw = new TextInputWindow();
            tiw.Text = "Enter new Intent name";
            var result = tiw.ShowDialog();
            if(result.HasValue && result.Value)
            {
                ViewModel.AddNewIntent(tiw.Result);

                SaveEverything();
            }
        }

        private void RemoveItemClick(object sender, RoutedEventArgs e)
        {
            ArrowIntentVm vm = AllIntentListBox.SelectedItem as ArrowIntentVm;
            var result = MessageBox.Show("Are you sure you want to delete " + vm.Name + "?",
                "Delete " + vm.Name + "?", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                ArrowState.Self.CurrentArrowProjectVm.Intents.Remove(vm);

                SaveEverything();
            }
        }

        private static void SaveEverything()
        {
            ArrowCommands.Self.File.SaveGlux();
            ArrowCommands.Self.File.SaveProject();
        }

        private void AddComponentClick(object sender, RoutedEventArgs e)
        {
            if(CurrentArrowIntentVm == null)
            {
                MessageBox.Show("Select an intent first to add a component");
            }
            else
            {
                CurrentArrowIntentVm.AddNewIntentComponent();


                ArrowCommands.Self.File.SaveGlux();
                ArrowCommands.Self.File.SaveProject();
            }
        }

        private void DeleteComponentClick(object sender, RoutedEventArgs e)
        {
            if (CurrentArrowIntentComponentVm == null)
            {
                MessageBox.Show("Select a component to delete");
            }
            else
            {
                // Make sure this pushes to the model
                CurrentArrowIntentVm.Components.Remove(CurrentArrowIntentComponentVm);

                ArrowCommands.Self.File.SaveGlux();
                ArrowCommands.Self.File.SaveProject();
            }
        }

        void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        private void AllIntentListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.CurrentArrowIntentVm = this.AllIntentListBox.SelectedItem as ArrowIntentVm;

        }

        private void ComponentsListBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            CurrentArrowIntentComponentVm = ComponentsListBox.SelectedItem as ArrowIntentComponentVm;

            ArrowIntentComponentDisplayer aicd = new ArrowIntentComponentDisplayer();


            var properties = aicd.GetTypedMemberDisplayProperties();

            this.DataGridUi.Apply(properties);
        }

        void HandlePropertyChanged(string arg1, WpfDataUi.EventArguments.PropertyChangedArgs arg2)
        {
            SaveEverything();
        }

    }
}
