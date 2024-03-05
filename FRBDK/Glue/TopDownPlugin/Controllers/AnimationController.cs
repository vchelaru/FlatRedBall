using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopDownPlugin.ViewModels;
using WpfDataUi.DataTypes;
using WpfDataUi;
using FlatRedBall.Glue.SaveClasses;
using Newtonsoft.Json;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.IO;
using System.ComponentModel;
using WpfDataUi.Controls;
using System.Windows;
using TopDownPlugin.Models;

namespace TopDownPlugin.Controllers;

public static class AnimationController
{
    #region Fields/properties

    static TopDownEntityViewModel topDownViewModel;
    public static TopDownEntityViewModel TopDownViewModel
    {
        get { return topDownViewModel; }
        set
        {
            if(value != topDownViewModel)
            {
                topDownViewModel = value;
                if(topDownViewModel != null)
                {
                    topDownViewModel.PropertyChanged += HandleTopDownViewModelPropertyChanged;
                }
            }
        }
    }

    static bool IsLoadingFromDisk = false;

    #endregion

    #region Initialize/Refresh

    public static void InitializeDataUiGridToNewViewModel(DataUiGrid dataUiGrid, AnimationRowViewModel viewModel)
    {
        var properties = new TypeMemberDisplayProperties();

        dataUiGrid.Instance = viewModel;
        dataUiGrid.Categories.First().HideHeader = true;

        InitializeInstanceMembers(dataUiGrid, viewModel, properties);

        dataUiGrid.InsertSpacesInCamelCaseMemberNames();

        if (viewModel != null)
        {
            RefreshAnimationNames(dataUiGrid, viewModel);

            RefreshMovementValueNames(dataUiGrid);

            viewModel.PropertyChanged += (sender, args) => HandleIndividualAnimationVmPropertyChanged(args.PropertyName, dataUiGrid, viewModel);
            dataUiGrid.Refresh();
        }
    }


    private static void InitializeInstanceMembers(DataUiGrid dataUiGrid, AnimationRowViewModel viewModel, TypeMemberDisplayProperties properties)
    {
        dataUiGrid.Categories.First(item => item.Name == "Uncategorized").Width = 250;

        {
            var instanceMember = dataUiGrid.GetInstanceMember(nameof(AnimationRowViewModel.Notes));
            instanceMember.PreferredDisplayer = typeof(MultiLineTextBoxDisplay);
            instanceMember.PropertiesToSetOnDisplayer[(nameof(MultiLineTextBoxDisplay.IsAboveBelowLayout))] = true;
        }

        dataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.MinVelocityAbsolute), Localization.Texts.Velocity);
        dataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.MaxVelocityAbsolute), Localization.Texts.Velocity);

        dataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.MinMovementInputAbsolute), "Input");
        dataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.MaxMovementInputAbsolute), "Input");

        var velocityFirstGridLength = new GridLength(150);
        dataUiGrid.GetInstanceMember(nameof(AnimationRowViewModel.MinVelocityAbsolute)).FirstGridLength = velocityFirstGridLength;
        dataUiGrid.GetInstanceMember(nameof(AnimationRowViewModel.MaxVelocityAbsolute)).FirstGridLength = velocityFirstGridLength;

        dataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.AnimationSpeedAssignment), Localization.Texts.AnimationSpeed);

        {
            var prop = new InstanceMemberDisplayProperties();
            prop.Name = nameof(AnimationRowViewModel.AbsoluteVelocityAnimationSpeedMultiplier);
            prop.Category = Localization.Texts.AnimationSpeed;
            prop.IsHiddenDelegate = (member) => viewModel.AnimationSpeedAssignment != AnimationSpeedAssignment.BasedOnVelocityMultiplier;
            properties.DisplayProperties.Add(prop);
        }

        {
            var prop = new InstanceMemberDisplayProperties();
            prop.Name = nameof(AnimationRowViewModel.MaxSpeedRatioMultiplier);
            prop.Category = Localization.Texts.AnimationSpeed;
            prop.IsHiddenDelegate = (member) => viewModel.AnimationSpeedAssignment != AnimationSpeedAssignment.BasedOnMaxSpeedRatioMultiplier;
            properties.DisplayProperties.Add(prop);
        }

        dataUiGrid.Apply(properties);

        foreach (var category in dataUiGrid.Categories)
        {
            category.CategoryBorderThickness = 0;
            category.Members.RemoveAll(item => item.PropertyType == typeof(System.Windows.Input.ICommand));
        }

        dataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.MovementName), Localization.Texts.MovementType);

        dataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.CustomCondition), Localization.Texts.MovementType);
    }


    public static void LoadAnimationFilesFromDisk(GlueElement currentElement)
    {
        var location = TopDownAnimationsFileLocationFor(currentElement);


        AllTopDownAnimationValues allAnimationValues = null;

        if (location.Exists())
        {
            // try to deserialize
            try
            {
                var text = System.IO.File.ReadAllText(location.FullPath);

                allAnimationValues = JsonConvert.DeserializeObject<AllTopDownAnimationValues>(text);
            }
            catch
            {
                // do nothing, it's corrupt/missing?
            }
        }

        if (allAnimationValues == null)
        {
            allAnimationValues = new AllTopDownAnimationValues();
        }

        IsLoadingFromDisk = true;

        topDownViewModel.AnimationRows.Clear();
        foreach (var item in allAnimationValues.Values)
        {
            var rowVm = new AnimationRowViewModel();
            topDownViewModel.AssignAnimationRowEvents(rowVm);

            rowVm.SetFrom(item);
            topDownViewModel.AnimationRows.Add(rowVm);
        }

        IsLoadingFromDisk = false;
    }

    private static void RefreshAnimationNames(DataUiGrid dataUiGrid, AnimationRowViewModel viewModel)
    {
        var member = dataUiGrid.GetInstanceMember(nameof(AnimationRowViewModel.AnimationName));
        member.CustomOptions = new List<object>();

        ////////////////////Early Outs///////////////
        var entity = GlueState.Self.CurrentEntitySave;
        if (entity == null)
        {
            return;
        }
        var nosSprite = entity.AllNamedObjects.FirstOrDefault(item => item.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Sprite);
        if (nosSprite == null)
        {
            return;
        }

        var achxName = ObjectFinder.Self.GetValueRecursively(nosSprite, entity, nameof(FlatRedBall.Sprite.AnimationChains)) as string;
        if (string.IsNullOrEmpty(achxName))
        {
            return;
        }
        var rfs = entity.GetReferencedFileSaveByInstanceNameRecursively(achxName);
        if (rfs == null)
        {
            return;
        }
        FilePath absoluteFile = GlueCommands.Self.GetAbsoluteFilePath(rfs);
        if (!absoluteFile.Exists())
        {
            return;
        }
        AnimationChainListSave acls = null;
        try
        {
            acls = AnimationChainListSave.FromFile(absoluteFile.FullPath);
        }
        catch
        {
            // do nothing?
        }
        if (acls == null)
        {
            return;
        }
        /////////////////End Early Outs////////////////

        bool onlyIncludeWithAppendedDirection = viewModel.IsDirectionFacingAppended;

        if (onlyIncludeWithAppendedDirection)
        {
            var names = new HashSet<string>();
            foreach (var animation in acls.AnimationChains.OrderBy(item => item.Name))
            {
                if (animation.Name.ToLowerInvariant().EndsWith("left"))
                {
                    names.Add(animation.Name.Substring(0, animation.Name.Length - "left".Length));
                }
                else if (animation.Name.ToLowerInvariant().EndsWith("right"))
                {
                    names.Add(animation.Name.Substring(0, animation.Name.Length - "right".Length));
                }
                else if (animation.Name.ToLowerInvariant().EndsWith("up"))
                {
                    names.Add(animation.Name.Substring(0, animation.Name.Length - "up".Length));
                }
                else if (animation.Name.ToLowerInvariant().EndsWith("down"))
                {
                    names.Add(animation.Name.Substring(0, animation.Name.Length - "down".Length));
                }
                else if (animation.Name.ToLowerInvariant().EndsWith("upright"))
                {
                    names.Add(animation.Name.Substring(0, animation.Name.Length - "upright".Length));
                }
                else if (animation.Name.ToLowerInvariant().EndsWith("upleft"))
                {
                    names.Add(animation.Name.Substring(0, animation.Name.Length - "upleft".Length));
                }
                else if (animation.Name.ToLowerInvariant().EndsWith("downright"))
                {
                    names.Add(animation.Name.Substring(0, animation.Name.Length - "downright".Length));
                }
                else if (animation.Name.ToLowerInvariant().EndsWith("downleft"))
                {
                    names.Add(animation.Name.Substring(0, animation.Name.Length - "downleft".Length));
                }
            }
            foreach (var name in names)
            {
                member.CustomOptions.Add(name);
            }
        }
        else
        {
            foreach (var animation in acls.AnimationChains)
            {
                member.CustomOptions.Add(animation.Name);
            }
        }
    }

    private static void RefreshMovementValueNames(DataUiGrid dataUiGrid)
    {
        var entity = GlueState.Self.CurrentEntitySave;
        List<string> options = GetOptionsForEntityRecursively(entity, listToAddTo: null, includeNull: true);

        if (options != null)
        {
            var member = dataUiGrid.GetInstanceMember(nameof(AnimationRowViewModel.MovementName));
            member.CustomOptions = options.Select(item => (object)item).ToList();
        }


    }

    private static List<string> GetOptionsForEntityRecursively(EntitySave entity, List<string> listToAddTo, bool includeNull)
    {
        ReferencedFileSave topDownValuesRfs = null;
        FilePath csvFilePath = null;
        List<string> options = null;

        topDownValuesRfs = entity?.GetAllReferencedFileSavesRecursively().FirstOrDefault(item =>
            item.Name.EndsWith("TopDownValuesStatic.csv") ||
            // old name:
            item.Name.EndsWith("TopDownValues.csv"));
        if (topDownValuesRfs != null)
        {
            csvFilePath = GlueCommands.Self.GetAbsoluteFilePath(topDownValuesRfs);
        }
        if (csvFilePath?.Exists() == true)
        {
            if (listToAddTo == null)
            {
                options = AvailableSpreadsheetValueTypeConverter.GetAvailableValues(csvFilePath, false, includeNull);
            }
            else
            {
                listToAddTo.AddRange(AvailableSpreadsheetValueTypeConverter.GetAvailableValues(csvFilePath, false, includeNull));
                options = listToAddTo;
            }
        }

        var baseEntity = ObjectFinder.Self.GetBaseElement(entity) as EntitySave;

        if (baseEntity != null)
        {
            GetOptionsForEntityRecursively(baseEntity, options, includeNull: false);
        }

        return options;
    }


    #endregion

    #region Handle Changes

    private static void HandleTopDownViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {

        switch (e.PropertyName)
        {
            case (nameof(TopDownEntityViewModel.AnimationRows)):
                if (!IsLoadingFromDisk)
                {
                    SaveOrDeleteViewModelFileFor(GlueState.Self.CurrentElement);
                    if (GlueState.Self.CurrentElement != null)
                    {
                        GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                    }
                }
                break;
        }
    }

    private static void HandleIndividualAnimationVmPropertyChanged(string propertyName, DataUiGrid dataUiGrid, AnimationRowViewModel viewModel)
    {
        switch (propertyName)
        {
            case nameof(AnimationRowViewModel.IsDirectionFacingAppended):
                RefreshAnimationNames(dataUiGrid, viewModel);
                dataUiGrid.Refresh();

                break;
        }

        // could this be spammy? It's certainly easier to always save on any change.
        if (!IsLoadingFromDisk)
        {
            SaveOrDeleteViewModelFileFor(GlueState.Self.CurrentElement);
            if (GlueState.Self.CurrentElement != null)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
            }
        }
    }

    #endregion

    public static FilePath TopDownAnimationsFileLocationFor(GlueElement element) =>
        GlueCommands.Self.GetAbsoluteFilePath(element).RemoveExtension() + ".TopDownAnimations.json";


    private static void SaveOrDeleteViewModelFileFor(GlueElement currentElement)
    {
        var whereToSave = TopDownAnimationsFileLocationFor(currentElement);

        if (TopDownViewModel.AnimationRows.Count > 0)
        {
            var model = new AllTopDownAnimationValues();

            foreach (var animationVm in TopDownViewModel.AnimationRows)
            {
                var individualModel = new IndividualTopDownAnimationValues();
                animationVm.ApplyTo(individualModel);
                model.Values.Add(individualModel);
            }

            var whatToSave = JsonConvert.SerializeObject(model, Formatting.Indented);



            try
            {
                GlueCommands.Self.TryMultipleTimes(() => System.IO.File.WriteAllText(whereToSave.FullPath, whatToSave));
            }
            catch (Exception e)
            {
                GlueCommands.Self.PrintError(e.ToString());
            }
        }
        else
        {
            // delete the file if it exists
            if (whereToSave.Exists())
            {
                try
                {
                    GlueCommands.Self.TryMultipleTimes(() => System.IO.File.Delete(whereToSave.FullPath));
                }
                catch (Exception e)
                {
                    GlueCommands.Self.PrintError(e.ToString());
                }
            }
        }
    }

}
