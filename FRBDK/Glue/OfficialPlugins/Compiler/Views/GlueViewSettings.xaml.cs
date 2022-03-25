using OfficialPlugins.Compiler.ViewModels;
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
using ToolsUtilities;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace OfficialPlugins.Compiler.Views
{
    /// <summary>
    /// Interaction logic for GlueViewSettings.xaml
    /// </summary>
    public partial class GlueViewSettings : UserControl
    {
        public GlueViewSettingsViewModel ViewModel
        {
            get => DataContext as GlueViewSettingsViewModel;
            set
            {
                this.DataContext = value;
                this.DataUiGrid.Instance = value;

                ViewModel.PropertyChanged += HandlePropertyChaned;

                CustomizeDisplay();
            }
        }

        private void HandlePropertyChaned(object sender, PropertyChangedEventArgs e)
        {
            this.DataUiGrid.Refresh();
        }

        public GlueViewSettings()
        {
            InitializeComponent();
        }

        private void CustomizeDisplay()
        {
            foreach(var category in DataUiGrid.Categories)
            {
                category.Name = "";
                foreach(var member in category.Members)
                {
                    member.DisplayName = StringFunctions.InsertSpacesInCamelCaseString(member.DisplayName);
                }

                var whatToRemove = category.Members
                    .FirstOrDefault(item => item.Name == nameof(GlueViewSettingsViewModel.ShowWindowDefenderUi));

                if(whatToRemove != null)
                {
                    category.Members.Remove(whatToRemove);
                }
            }



            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.RestartScreenOnLevelContentChange), "Content");


            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.GridSize), "Grid and Markings");
            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.ShowScreenBoundsWhenViewingEntities), "Grid and Markings");
            
            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.SetBackgroundColor), "Grid and Markings");
            

            var restartScreenOnContentChangeMember = GetMember(nameof(ViewModel.RestartScreenOnLevelContentChange)); 
            restartScreenOnContentChangeMember.DetailText = "If unchecked, the game will only respond to file changes (like TMX) in edit mode";
            //this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.Show), "Grid and Markings");

            TypeMemberDisplayProperties properties = new TypeMemberDisplayProperties();

            AddColorProperties(nameof(ViewModel.BackgroundRed));
            AddColorProperties(nameof(ViewModel.BackgroundGreen));
            AddColorProperties(nameof(ViewModel.BackgroundBlue));
            void AddColorProperties(string propertyName)
            {
                var colorProperties = properties.GetOrCreateImdp(propertyName);
                colorProperties.Category = "Grid and Markings";
                colorProperties.IsHiddenDelegate = (notused) => ViewModel.SetBackgroundColor == false;

            }

            DataUiGrid.Apply(properties);
            DataUiGrid.Refresh();
        }

        InstanceMember GetMember(string name)
        {
            foreach(var category in DataUiGrid.Categories)
            {
                foreach(var member in category.Members)
                {
                    if(member.Name == name)
                    {
                        return member;
                    }
                }
            }
            return null;
        }


    }
}
