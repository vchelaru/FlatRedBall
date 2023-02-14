using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Math.Paths;
using Newtonsoft.Json;
using OfficialPlugins.PathPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;

namespace OfficialPlugins.PathPlugin.Managers
{
    public static class ViewModelManager
    {
        #region Fields/Properties

        public static PathViewModel MainViewModel { get; set; }
        static NamedObjectSave nos => GlueState.Self.CurrentNamedObjectSave;

        static CustomVariableInNamedObject Variable
        {
            get
            {
                var variableName = MainViewModel?.VariableName ?? AssetTypeInfoManager.PathsVariableName;

                var variable = nos?.GetCustomVariable(variableName);

                if(variable == null && nos != null)
                {
                    variable = new CustomVariableInNamedObject();
                    variable.Member = variableName;
                    variable.Type = "string";
                    variable.Value = "";
                    nos.InstructionSaves.Add(variable);
                }

                return variable;
            }
        }

        static string PathSegmentString => Variable?.Value as string;

        #endregion

        internal static void HandlePathViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ////////Early Out////////////////
            if (MainViewModel.UpdateModelOnChanges == false)
            {
                return;
            }
            //////End Early Out//////////////

        }

        internal static void UpdateViewModelToModel()
        {
            MainViewModel.UpdateModelOnChanges = false;

            MainViewModel.PathSegments.Clear();

            var serializedSegments = PathSegmentString;
            if(!string.IsNullOrEmpty(serializedSegments))
            {
                var pathSegments = JsonConvert.DeserializeObject<List<PathSegment>>(PathSegmentString);

                foreach(var segment in pathSegments)
                {
                    var segmentVm = new PathSegmentViewModel();
                    segmentVm.X = segment.EndX;
                    segmentVm.Y = segment.EndY;
                    segmentVm.Angle = segment.ArcAngle;
                    segmentVm.SegmentType = segment.SegmentType;
                    AssignSegmentEvents(segmentVm);
                    MainViewModel.PathSegments.Add(segmentVm);
                }
            }

            MainViewModel.UpdateModelOnChanges = true;
        }

        static void AssignSegmentEvents(PathSegmentViewModel vm)
        {
            vm.PropertyChanged += HandlePathSegmentViewModelPropertyChanged;
            vm.CloseClicked += HandleSegmentRemoveClicked;

            vm.MoveUpClicked += (_) =>
            {
                var index = MainViewModel.PathSegments.IndexOf(vm);

                if (index > 0)
                {
                    MainViewModel.PathSegments.Remove(vm);
                    MainViewModel.PathSegments.Insert(index - 1, vm);
                }
            };
            vm.MoveDownClicked += (_) =>
            {
                var index = MainViewModel.PathSegments.IndexOf(vm);

                if (index < MainViewModel.PathSegments.Count - 1)
                {
                    MainViewModel.PathSegments.Remove(vm);
                    MainViewModel.PathSegments.Insert(index + 1, vm);
                }
            };
            vm.CopyClicked += (_) =>
            {
                var newVm = new PathSegmentViewModel();
                AssignSegmentEvents(newVm);
                newVm.X = vm.X;
                newVm.Y = vm.Y;
                newVm.SegmentType = vm.SegmentType;
                newVm.Angle = vm.Angle;
                MainViewModel.PathSegments.Add(newVm);

            };
        }

        public static void CreateNewSegmentViewModel()
        {
            var newSegment = new PathSegmentViewModel();
            newSegment.Y = 20;
            AssignSegmentEvents(newSegment);
            MainViewModel.PathSegments.Add(newSegment);
        }

        private static void HandleSegmentRemoveClicked(PathSegmentViewModel segmentVm)
        {
            MainViewModel.PathSegments.Remove(segmentVm);
        }

        internal static void HandlePathSegmentsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ////////Early Out////////////////
            if(MainViewModel.UpdateModelOnChanges == false)
            {
                return;
            }
            //////End Early Out//////////////


            TaskManager.Self.Add(() =>
            {
                UpdateModelToViewModel();
                SendChangeToPluginManager();
                GlueCommands.Self.GluxCommands.SaveGlux();
                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

            }, $"Modifying Segment List for {nos}");
        }

        private static void UpdateModelToViewModel()
        {
            List<PathSegment> pathSegments = new List<PathSegment>();

            foreach(var segmentVm in MainViewModel.PathSegments)
            {
                var pathSegment = new PathSegment();
                pathSegment.AngleUnit = AngleUnit.Degrees;
                ApplyVmToSegment(segmentVm, pathSegment);
                pathSegments.Add(pathSegment);
            }

            GlueCommands.Self.DoOnUiThread(() =>
            {
                Variable.Value = JsonConvert.SerializeObject(pathSegments);
            });
        }

        private static void ApplyVmToSegment(PathSegmentViewModel segmentVm, PathSegment segment)
        {
            segment.EndX = segmentVm.X;
            segment.EndY = segmentVm.Y;
            segment.ArcAngle = segmentVm.Angle;
            segment.SegmentType = segmentVm.SegmentType;
        }

        internal static void HandlePathSegmentViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ////////Early Out////////////////
            if (MainViewModel.UpdateModelOnChanges == false)
            {
                return;
            }
            //////End Early Out//////////////

            var segmentVm = sender as PathSegmentViewModel;

            TaskManager.Self.Add(() =>
            {
                UpdateModelToViewModel();
                SendChangeToPluginManager();
                GlueCommands.Self.GluxCommands.SaveGlux();
                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

            }, $"Modifying Segment for {nos}");
        }

        private static void SendChangeToPluginManager()
        {
            GlueCommands.Self.DoOnUiThread(() =>
            {
                PluginManager.ReactToNamedObjectChangedValue(
                    MainViewModel?.VariableName ?? AssetTypeInfoManager.PathsVariableName,
                    null,
                    nos);
            });

        }
    }
}
