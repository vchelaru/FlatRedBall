using FlatRedBall.Glue.Managers;
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
        public static PathViewModel MainViewModel { get; set; }
        static NamedObjectSave nos => GlueState.Self.CurrentNamedObjectSave;

        static CustomVariableInNamedObject Variable
        {
            get
            {
                var variable = nos.GetCustomVariable(AssetTypeInfoManager.PathsVariableName);

                if(variable == null)
                {
                    variable = new CustomVariableInNamedObject();
                    variable.Member = AssetTypeInfoManager.PathsVariableName;
                    variable.Type = "string";
                    variable.Value = "";
                    nos.InstructionSaves.Add(variable);
                }

                return variable;
            }
        }

        static string PathSegmentString => Variable.Value as string;

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
                    segmentVm.PropertyChanged += HandlePathSegmentViewModelPropertyChanged;
                    segmentVm.CloseClicked += HandleSegmentRemoveClicked;
                    MainViewModel.PathSegments.Add(segmentVm);
                }
            }

            MainViewModel.UpdateModelOnChanges = true;
        }

        public static void CreateNewSegmentViewModel()
        {
            var newSegment = new PathSegmentViewModel();
            newSegment.Y = 20;
            newSegment.PropertyChanged += HandlePathSegmentViewModelPropertyChanged;
            newSegment.CloseClicked += HandleSegmentRemoveClicked;
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
                ApplyVmToSegment(segmentVm, pathSegment);
                pathSegments.Add(pathSegment);
            }

            GlueCommands.Self.DoOnUiThread(() => Variable.Value = JsonConvert.SerializeObject(pathSegments));
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
                GlueCommands.Self.GluxCommands.SaveGlux();
                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

            }, $"Modifying Segment for {nos}");
        }


    }
}
