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
            if (e.PropertyName == nameof(GlueViewSettingsViewModel.ShowScreenBounds))
            {
                // DataUiGrid re-adds a member to the end of its category when a hidden delegate flips it
                // back to visible, instead of preserving its original position, so put it back underneath
                // ShowScreenBounds instead of letting it drift to the end of the category (after the
                // background-color fields).
                MoveLockScreenBoundsMemberUnderShowScreenBounds();
            }

            this.DataUiGrid.Refresh();
        }

        private void MoveLockScreenBoundsMemberUnderShowScreenBounds()
        {
            var showScreenBoundsMember = GetMember(nameof(ViewModel.ShowScreenBounds));
            var lockToWorldSpaceMember = GetMember(nameof(ViewModel.LockScreenBoundsToWorldSpace));

            if (showScreenBoundsMember != null && lockToWorldSpaceMember != null)
            {
                var members = lockToWorldSpaceMember.Category.Members;
                var currentIndex = members.IndexOf(lockToWorldSpaceMember);

                if (currentIndex >= 0)
                {
                    members.RemoveAt(currentIndex);
                    var showScreenBoundsIndex = members.IndexOf(showScreenBoundsMember);
                    var insertIndex = showScreenBoundsIndex >= 0 ? showScreenBoundsIndex + 1 : members.Count;
                    members.Insert(System.Math.Min(insertIndex, members.Count), lockToWorldSpaceMember);
                }
            }
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
                    member.DisplayName = Localization.Texts.ResourceManager.GetString(member.DisplayName, Localization.Texts.Culture);
                }

                var whatToRemove = category.Members
                    .FirstOrDefault(item => item.Name == nameof(GlueViewSettingsViewModel.ShowWindowDefenderUi));
                if(whatToRemove != null)
                {
                    category.Members.Remove(whatToRemove);
                }
            }



            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.RestartScreenOnLevelContentChange), Localization.Texts.Content); // content

            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.ShowGrid), Localization.Texts.GridAndMarkings);
            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.GridAlpha), Localization.Texts.GridAndMarkings);
            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.GridSize), Localization.Texts.GridAndMarkings);
            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.ShowScreenBounds), Localization.Texts.GridAndMarkings);
            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.LockScreenBoundsToWorldSpace), Localization.Texts.GridAndMarkings);
            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.SetBackgroundColor), Localization.Texts.GridAndMarkings);

            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.EnableSnapping), Localization.Texts.Snapping);
            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.SnapSize), Localization.Texts.Snapping);
            this.DataUiGrid.MoveMemberToCategory(nameof(ViewModel.PolygonPointSnapSize), Localization.Texts.Snapping);


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
                colorProperties.PreferredDisplayer = typeof(WpfDataUi.Controls.SliderDisplay);
            }

            var lockScreenBoundsProperties = properties.GetOrCreateImdp(nameof(ViewModel.LockScreenBoundsToWorldSpace));
            lockScreenBoundsProperties.Category = Localization.Texts.GridAndMarkings;
            lockScreenBoundsProperties.DisplayName = Localization.Texts.LockScreenBoundsToWorldSpace;
            lockScreenBoundsProperties.IsHiddenDelegate = (notused) => ViewModel.ShowScreenBounds == false;

            DataUiGrid.Apply(properties);

            ApplyColorSliderRange(nameof(ViewModel.BackgroundRed));
            ApplyColorSliderRange(nameof(ViewModel.BackgroundGreen));
            ApplyColorSliderRange(nameof(ViewModel.BackgroundBlue));
            void ApplyColorSliderRange(string propertyName)
            {
                var member = GetMember(propertyName);
                if (member != null)
                {
                    member.PropertiesToSetOnDisplayer[nameof(WpfDataUi.Controls.SliderDisplay.MinValue)] = 0.0;
                    member.PropertiesToSetOnDisplayer[nameof(WpfDataUi.Controls.SliderDisplay.MaxValue)] = 255.0;
                }
            }

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
                        listMessageBox.Message = "Commands for the current element:";
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
