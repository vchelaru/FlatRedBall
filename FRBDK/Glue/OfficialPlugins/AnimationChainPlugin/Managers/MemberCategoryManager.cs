using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using GlueFormsCore.ViewModels;
using OfficialPlugins.AnimationChainPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace OfficialPlugins.AnimationChainPlugin.Managers;

internal class MemberCategoryManager
{
    private readonly NameVerifier _nameVerifier;
    private readonly IDialogCommands _dialogCommands;

    const int FirstGridLength = 60;

    public MemberCategoryManager(NameVerifier nameVerifier, IDialogCommands dialogCommands)
    {
        _nameVerifier = nameVerifier;
        _dialogCommands = dialogCommands;
    }

    #region Animation Chain

    public void SetMemberCategories(DataUiGrid grid, AnimationChainViewModel selectedAnimationChain, AchxViewModel allAnimations)
    {
        grid.Categories.Clear();

        grid.Categories.AddRange(CreateMemberCategories(selectedAnimationChain, allAnimations));
    }

    private List<MemberCategory> CreateMemberCategories(AnimationChainViewModel selectedAnimationChain, AchxViewModel allAnimations)
    {
        var isAtlas = selectedAnimationChain.FilePath.Extension == "atlas";
        var isFullAchx = !isAtlas;

        List<MemberCategory> toReturn = new List<MemberCategory>();

        var mainCategory = new MemberCategory();
        toReturn.Add(mainCategory);

        var nameMember = Add(nameof(AnimationChainViewModel.Name), isReadOnly:false);
        nameMember.CustomSetPropertyEvent += (assignedInstance, args) =>
        {
            var castedVm = (AnimationChainViewModel)assignedInstance;
            var newName = (string)args.Value;
            if (!_nameVerifier.IsAnimationNameValid(newName, selectedAnimationChain, allAnimations, out string whyNotvalid))
            {
                // Show some form of validation like a popup
                _dialogCommands.ShowMessageBox($"The name '{newName}' is not valid:\n{whyNotvalid}");
                args.IsAssignmentCancelled = true;
            }
            else
            {
                castedVm.Name = newName;
            }
        };

        if(isFullAchx)
        {
            Add(nameof(AnimationChainViewModel.Duration), isReadOnly: true);
        }

        return toReturn;

        InstanceMember Add(string propertyName, bool isReadOnly)
        {
            var member = new InstanceMember(propertyName, selectedAnimationChain);
            member.IsReadOnly = isReadOnly;
            member.FirstGridLength = new System.Windows.GridLength(FirstGridLength);
            mainCategory.Members.Add(member);
            return member;
        }
    }

    #endregion

    #region Animation Frame

    public void SetMemberCategories(DataUiGrid grid, AnimationFrameViewModel animationFrame)
    {
        var list = new List<MemberCategory>();

        var currentCategory = new MemberCategory();
        list.Add(currentCategory);

        var isAtlas = animationFrame.Parent?.FilePath.Extension == "atlas";
        var isFullAchx = !isAtlas;

        var member = Add(nameof(animationFrame.RelativeTextureName), canWrite:true, typeof(FileSelectionDisplay));
        member.PropertiesToSetOnDisplayer[nameof(FileSelectionDisplay.Filter)] = "PNG|*.png";

        if(isFullAchx)
        {
            Add(nameof(animationFrame.LengthInSeconds), canWrite: true);
        }

        currentCategory = new MemberCategory();
        list.Add(currentCategory);
        currentCategory.Name = "Texture Coordinates";

        Add(nameof(animationFrame.X), canWrite:true);
        Add(nameof(animationFrame.Y), canWrite: true);
        Add(nameof(animationFrame.Width), canWrite: true);
        Add(nameof(animationFrame.Height), canWrite: true);

        if(isFullAchx)
        {
            Add(nameof(animationFrame.FlipHorizontal), canWrite:true);
            Add(nameof(animationFrame.FlipVertical), canWrite: true);
        }

        if(isFullAchx)
        {
            currentCategory = new MemberCategory();
            list.Add(currentCategory);
            currentCategory.Name = "Offset";

            Add(nameof(animationFrame.RelativeX), canWrite:true);
            Add(nameof(animationFrame.RelativeY), canWrite:true);
        }

        grid.Categories.Clear();
        grid.Categories.AddRange(list);
        grid.InsertSpacesInCamelCaseMemberNames();

        return;

        InstanceMember Add(string propertyName, bool canWrite = false, Type preferredDisplayer = null)
        {
            var member = new InstanceMember(propertyName, animationFrame);
            if(preferredDisplayer != null)
            {
                member.PreferredDisplayer = preferredDisplayer;
            }
            member.IsReadOnly = !canWrite;
            member.FirstGridLength = new System.Windows.GridLength(FirstGridLength);
            currentCategory.Members.Add(member);
            return member;
        }
    }

    #endregion

    #region Circle

    public void SetMemberCategories(DataUiGrid grid, CircleViewModel circle)
    {
        var list = new List<MemberCategory>();

        var mainCategory = new MemberCategory();
        list.Add(mainCategory);

        Add(nameof(circle.Name));
        Add(nameof(circle.X));
        Add(nameof(circle.Y));
        Add(nameof(circle.Radius));

        void Add(string propertyName, bool canWrite = false)
        {
            var member = new InstanceMember(propertyName, circle);
            member.IsReadOnly = !canWrite;
            member.FirstGridLength = new System.Windows.GridLength(FirstGridLength);
            mainCategory.Members.Add(member);

        }

        grid.Categories.Clear();
        grid.Categories.AddRange(list);
        grid.InsertSpacesInCamelCaseMemberNames();

    }

    #endregion
}
