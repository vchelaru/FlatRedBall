using PlatformerPluginCore.ViewModels;
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
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace PlatformerPluginCore.Views
{
    /// <summary>
    /// Interaction logic for AnimationRow.xaml
    /// </summary>
    public partial class AnimationRow : UserControl
    {
        AnimationRowViewModel ViewModel => DataContext as AnimationRowViewModel;
        public AnimationRow()
        {
            InitializeComponent();

            DataContextChanged += HandleDataContextChanged;
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var properties = new TypeMemberDisplayProperties();


            DataUiGrid.Instance = this.DataContext;
            DataUiGrid.Categories.First().HideHeader = true;

            DataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.MinXVelocityAbsolute), "Velocity");
            DataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.MaxXVelocityAbsolute), "Velocity");

            DataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.MinYVelocity), "Velocity");
            DataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.MaxYVelocity), "Velocity");
            var category = DataUiGrid.Categories.First(item => item.Name == "Velocity");
            //category.Width = 400;

            DataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.AnimationSpeedAssignment), "Animation Speed");

            {
                var prop = new InstanceMemberDisplayProperties();
                prop.Name = nameof(AnimationRowViewModel.AbsoluteXVelocityAnimationSpeedMultiplier);
                prop.Category = "Animation Speed";
                prop.IsHiddenDelegate = (member) => ViewModel.AnimationSpeedAssignment != AnimationSpeedAssignment.BasedOnMultiplier;
                properties.DisplayProperties.Add(prop);
            }

            {
                var prop = new InstanceMemberDisplayProperties();
                prop.Name = nameof(AnimationRowViewModel.AbsoluteYVelocityAnimationSpeedMultiplier);
                prop.Category = "Animation Speed";
                prop.IsHiddenDelegate = (member) => ViewModel.AnimationSpeedAssignment != AnimationSpeedAssignment.BasedOnMultiplier;
                properties.DisplayProperties.Add(prop);
            }


            DataUiGrid.Apply(properties);

            DataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.OnGroundRequirement), "Movement Type");
            var member = DataUiGrid.GetInstanceMember(nameof(AnimationRowViewModel.OnGroundRequirement));
            member.PropertiesToSetOnDisplayer[nameof(NullableBoolDisplay.TrueText)] = "Ground Only";
            member.PropertiesToSetOnDisplayer[nameof(NullableBoolDisplay.FalseText)] = "Air Only";
            member.PropertiesToSetOnDisplayer[nameof(NullableBoolDisplay.NullText)] = "Either";

            DataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.MovementName), "Movement Type");

            DataUiGrid.InsertSpacesInCamelCaseMemberNames();
        }
    }
}