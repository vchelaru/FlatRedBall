using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.PlatformerPlugin.ViewModels;
using Newtonsoft.Json;
using PlatformerPluginCore.SaveClasses;
using PlatformerPluginCore.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace PlatformerPluginCore.Controllers
{
    public static class AnimationController
    {
        #region Fields/properties

        static PlatformerEntityViewModel platformerViewModel;
        public static PlatformerEntityViewModel PlatformerViewModel
        {
            get => platformerViewModel;
            set
            {
                if (value != platformerViewModel)
                {
                    platformerViewModel = value;
                    if(platformerViewModel != null)
                    {
                        platformerViewModel.PropertyChanged += HandlePlatformerViewModelPropertyChanged;
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

            dataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.MinXVelocityAbsolute), "Velocity");
            dataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.MaxXVelocityAbsolute), "Velocity");

            dataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.MinYVelocity), "Velocity");
            dataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.MaxYVelocity), "Velocity");

            var velocityFirstGridLength = new GridLength(150);
            dataUiGrid.GetInstanceMember(nameof(AnimationRowViewModel.MinXVelocityAbsolute)).FirstGridLength = velocityFirstGridLength;
            dataUiGrid.GetInstanceMember(nameof(AnimationRowViewModel.MaxXVelocityAbsolute)).FirstGridLength = velocityFirstGridLength;
            dataUiGrid.GetInstanceMember(nameof(AnimationRowViewModel.MinYVelocity)).FirstGridLength = velocityFirstGridLength;
            dataUiGrid.GetInstanceMember(nameof(AnimationRowViewModel.MaxYVelocity)).FirstGridLength = velocityFirstGridLength;

            dataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.AnimationSpeedAssignment), "Animation Speed");

            {
                var prop = new InstanceMemberDisplayProperties();
                prop.Name = nameof(AnimationRowViewModel.AbsoluteXVelocityAnimationSpeedMultiplier);
                prop.Category = "Animation Speed";
                prop.IsHiddenDelegate = (member) => viewModel.AnimationSpeedAssignment != AnimationSpeedAssignment.BasedOnVelocityMultiplier;
                properties.DisplayProperties.Add(prop);
            }

            {
                var prop = new InstanceMemberDisplayProperties();
                prop.Name = nameof(AnimationRowViewModel.AbsoluteYVelocityAnimationSpeedMultiplier);
                prop.Category = "Animation Speed";
                prop.IsHiddenDelegate = (member) => viewModel.AnimationSpeedAssignment != AnimationSpeedAssignment.BasedOnVelocityMultiplier;
                properties.DisplayProperties.Add(prop);
            }
            {
                var prop = new InstanceMemberDisplayProperties();
                prop.Name = nameof(AnimationRowViewModel.MaxSpeedXRatioMultiplier);
                prop.Category = "Animation Speed";
                prop.IsHiddenDelegate = (member) => viewModel.AnimationSpeedAssignment != AnimationSpeedAssignment.BasedOnMaxSpeedRatioMultiplier;
                properties.DisplayProperties.Add(prop);
            }
            {
                var prop = new InstanceMemberDisplayProperties();
                prop.Name = nameof(AnimationRowViewModel.MaxSpeedYRatioMultiplier);
                prop.Category = "Animation Speed";
                prop.IsHiddenDelegate = (member) => viewModel.AnimationSpeedAssignment != AnimationSpeedAssignment.BasedOnMaxSpeedRatioMultiplier;
                properties.DisplayProperties.Add(prop);
            }

            dataUiGrid.Apply(properties);

            dataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.OnGroundRequirement), "Movement Type");
            var member = dataUiGrid.GetInstanceMember(nameof(AnimationRowViewModel.OnGroundRequirement));
            member.PropertiesToSetOnDisplayer[nameof(NullableBoolDisplay.TrueText)] = "Ground Only";
            member.PropertiesToSetOnDisplayer[nameof(NullableBoolDisplay.FalseText)] = "Air Only";
            member.PropertiesToSetOnDisplayer[nameof(NullableBoolDisplay.NullText)] = "Either";

            foreach(var category in dataUiGrid.Categories)
            {
                category.CategoryBorderThickness = 0;
                category.Members.RemoveAll(item => item.PropertyType == typeof(System.Windows.Input.ICommand));
            }

            dataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.MovementName), "Movement Type");

            dataUiGrid.MoveMemberToCategory(nameof(AnimationRowViewModel.CustomCondition), "Movement Type");
        }

        public static void LoadAnimationFilesFromDisk(GlueElement currentElement)
        {
            var location = PlatformerAnimationsFileLocationFor(currentElement);


            AllPlatformerAnimationValues allAnimationValues = null;

            if (location.Exists())
            {
                // try to deserialize
                try
                {
                    var text = System.IO.File.ReadAllText(location.FullPath);

                    allAnimationValues = JsonConvert.DeserializeObject<AllPlatformerAnimationValues>(text);
                }
                catch
                {
                    // do nothing, it's corrupt/missing?
                }
            }

            if(allAnimationValues == null)
            {
                allAnimationValues = new AllPlatformerAnimationValues();
            }

            IsLoadingFromDisk = true;

            platformerViewModel.AnimationRows.Clear();
            foreach(var item in allAnimationValues.Values)
            {
                var rowVm = new AnimationRowViewModel();
                platformerViewModel.AssignAnimationRowEvents(rowVm);

                rowVm.SetFrom(item);
                platformerViewModel.AnimationRows.Add(rowVm);
            }

            IsLoadingFromDisk = false;
        }

        private static void RefreshAnimationNames(DataUiGrid dataUiGrid, AnimationRowViewModel viewModel)
        {
            var member = dataUiGrid.GetInstanceMember(nameof(AnimationRowViewModel.AnimationName));
            member.CustomOptions = new List<object>();

            ////////////////////Early Outs///////////////
            var entity = GlueState.Self.CurrentEntitySave;
            if(entity == null)
            {
                return;
            }
            var nosSprite = entity.AllNamedObjects.FirstOrDefault(item => item.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Sprite);
            if(nosSprite == null)
            {
                return;
            }

            var achxName = ObjectFinder.Self.GetValueRecursively(nosSprite, entity, nameof(FlatRedBall.Sprite.AnimationChains)) as string;
            if(string.IsNullOrEmpty(achxName))
            {
                return;
            }
            var rfs = entity.GetReferencedFileSaveByInstanceNameRecursively(achxName);
            if(rfs == null)
            {
                return;
            }
            FilePath absoluteFile = GlueCommands.Self.GetAbsoluteFilePath(rfs);
            if(!absoluteFile.Exists())
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
            if(acls == null)
            {
                return;
            }
            /////////////////End Early Outs////////////////

            bool onlyIncludeWithAppendedLeftRight = viewModel.HasLeftAndRight;

            if(onlyIncludeWithAppendedLeftRight)
            {
                var names = new HashSet<string>();
                foreach (var animation in acls.AnimationChains.OrderBy(item => item.Name)) 
                {
                    if(animation.Name.ToLowerInvariant().EndsWith("left"))
                    {
                        names.Add(animation.Name.Substring(0, animation.Name.Length - "left".Length));
                    }
                    else if(animation.Name.ToLowerInvariant().EndsWith("right"))
                    {
                        names.Add(animation.Name.Substring(0, animation.Name.Length - "right".Length));
                    }
                }
                foreach(var name in names)
                {
                    member.CustomOptions.Add(name);
                }
            }
            else
            {
                foreach(var animation in acls.AnimationChains)
                {
                    member.CustomOptions.Add(animation.Name);
                }
            }
        }

        private static void RefreshMovementValueNames(DataUiGrid dataUiGrid)
        {
            var entity = GlueState.Self.CurrentEntitySave;
            List<string> options = GetOptionsForEntityRecursively(entity, listToAddTo:null, includeNull:true);

            if (options != null)
            {
                var member = dataUiGrid.GetInstanceMember(nameof(AnimationRowViewModel.MovementName));
                member.CustomOptions = options.Select(item => (object)item).ToList();
            }


        }

        private static List<string> GetOptionsForEntityRecursively(EntitySave entity, List<string> listToAddTo, bool includeNull)
        {
            ReferencedFileSave platformerValuesRfs = null;
            FilePath csvFilePath = null;
            List<string> options = null;

            platformerValuesRfs = entity?.GetAllReferencedFileSavesRecursively().FirstOrDefault(item =>
                item.Name.EndsWith("PlatformerValuesStatic.csv") ||
                // old name:
                item.Name.EndsWith("PlatformerValues.csv"));
            if (platformerValuesRfs != null)
            {
                csvFilePath = GlueCommands.Self.GetAbsoluteFilePath(platformerValuesRfs);
            }
            if (csvFilePath?.Exists() == true)
            {
                if(listToAddTo == null)
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

            if(baseEntity != null)
            {
                GetOptionsForEntityRecursively(baseEntity, options, includeNull:false);
            }

            return options;
        }

        #endregion

        #region Handle Changes

        private static void HandlePlatformerViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            
            switch(e.PropertyName)
            {
                case (nameof(PlatformerEntityViewModel.AnimationRows)):
                    if(!IsLoadingFromDisk)
                    {
                        SaveOrDeleteViewModelFileFor(GlueState.Self.CurrentElement);
                        if(GlueState.Self.CurrentElement != null)
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
                case nameof(AnimationRowViewModel.HasLeftAndRight):
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

        public static FilePath PlatformerAnimationsFileLocationFor(GlueElement element) =>
                GlueCommands.Self.GetAbsoluteFilePath(element).RemoveExtension() + ".PlatformerAnimations.json";


        private static void SaveOrDeleteViewModelFileFor(GlueElement currentElement)
        {
            FilePath whereToSave = PlatformerAnimationsFileLocationFor(currentElement);

            if(PlatformerViewModel.AnimationRows.Count > 0)
            {
                var model = new AllPlatformerAnimationValues();

                foreach(var animationVm in PlatformerViewModel.AnimationRows)
                {
                    var individualModel = new IndividualPlatformerAnimationValues();
                    animationVm.ApplyTo(individualModel);
                    model.Values.Add(individualModel);
                }

                var whatToSave = JsonConvert.SerializeObject(model, Formatting.Indented);



                try
                {
                    GlueCommands.Self.TryMultipleTimes(() => System.IO.File.WriteAllText(whereToSave.FullPath, whatToSave));
                }
                catch(Exception e)
                {
                    GlueCommands.Self.PrintError(e.ToString());
                }
            }
            else
            {
                // delete the file if it exists
                if(whereToSave.Exists())
                {
                    try
                    {
                        GlueCommands.Self.TryMultipleTimes(() => System.IO.File.Delete(whereToSave.FullPath));
                    }
                    catch(Exception e)
                    {
                        GlueCommands.Self.PrintError(e.ToString());
                    }
                }
            }
        }
    }
}
