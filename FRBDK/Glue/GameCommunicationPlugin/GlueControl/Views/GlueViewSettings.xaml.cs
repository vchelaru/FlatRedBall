using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GameCommunicationPlugin.GlueControl.CommandSending;
using GameCommunicationPlugin.GlueControl.Dtos;
using GameCommunicationPlugin.GlueControl.ViewModels;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ToolsUtilities;
using WpfDataUi.DataTypes;

namespace GameCommunicationPlugin.GlueControl.Views
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

                ViewModel.PropertyChanged += HandlePropertyChanged;

                CustomizeDisplay();
            }
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
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



            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.RestartScreenOnLevelContentChange), Localization.MenuIds.ContentId, Localization.Texts.Content); // content

            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.ShowGrid), Localization.MenuIds.GridAndMarkingsId, Localization.Texts.GridAndMarkings);
            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.GridAlpha), Localization.MenuIds.GridAndMarkingsId, Localization.Texts.GridAndMarkings);
            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.GridSize), Localization.MenuIds.GridAndMarkingsId, Localization.Texts.GridAndMarkings);
            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.ShowScreenBoundsWhenViewingEntities), Localization.MenuIds.GridAndMarkingsId, Localization.Texts.GridAndMarkings);
            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.SetBackgroundColor), Localization.MenuIds.GridAndMarkingsId, Localization.Texts.GridAndMarkings);

            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.EnableSnapping), Localization.MenuIds.SnappingId, Localization.Texts.Snapping);
            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.SnapSize), Localization.MenuIds.SnappingId, Localization.Texts.Snapping);
            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.PolygonPointSnapSize), Localization.MenuIds.SnappingId, Localization.Texts.Snapping);


            var restartScreenOnContentChangeMember = GetMember(nameof(ViewModel.RestartScreenOnLevelContentChange));
            restartScreenOnContentChangeMember.DetailText = Localization.Texts.WarningLocalizationFileChange;
            //this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.Show), "Grid and Markings");

            TypeMemberDisplayProperties properties = new TypeMemberDisplayProperties();

            AddColorProperties(nameof(ViewModel.BackgroundRed));
            AddColorProperties(nameof(ViewModel.BackgroundGreen));
            AddColorProperties(nameof(ViewModel.BackgroundBlue));
            void AddColorProperties(string propertyName)
            {
                var colorProperties = properties.GetOrCreateImdp(propertyName);
                colorProperties.Category = Localization.Texts.GridAndMarkings;
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

        private async void HandleShowGameCommandRerunList(object sender, RoutedEventArgs e)
        {
            string errorMessage = null;
            if(CommandSender.Self.IsConnected)
            {
                var dto = new Dtos.GetGlueToGameCommandRerunList();
                var generalResponse = await CommandSender.Self.Send(dto);

                if(generalResponse.Succeeded)
                {
                    if(!string.IsNullOrEmpty(generalResponse.Data))
                    {
                        var response = JsonConvert.DeserializeObject<GetCommandsDtoResponse>(generalResponse.Data);

                        var listMessageBox = new ListBoxWindowWpf();

                        foreach(var item in response.Commands)
                        {
                            listMessageBox.AddItem(item);
                        }

                        listMessageBox.ShowDialog();
                    }
                    else
                    {
                        errorMessage = Localization.Texts.ErrorGameThrewNullString;
                    }
                }
            }
            else
            {
                errorMessage = Localization.Texts.ErrorGameMustRunToGetStoredCommands;
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox(errorMessage);
            }
        }

    }
}
