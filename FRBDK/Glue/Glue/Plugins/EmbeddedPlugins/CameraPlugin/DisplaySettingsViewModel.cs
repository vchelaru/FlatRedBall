using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CameraPlugin
{
    public class DisplaySettingsViewModel : ViewModel
    {
        public bool GenerateDisplayCode
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(GenerateDisplayCode))]
        public Visibility AllPropertiesVisibility
        {
            get
            {
                if (GenerateDisplayCode) return Visibility.Visible;
                else return Visibility.Collapsed;
            }
        }

        public bool Is2D
        {
            get => Get<bool>();
            set => Set(value);
        }

        public int ResolutionWidth
        {
            get => Get<int>();
            set => Set(value);
        }

        public int ResolutionHeight
        {
            get => Get<int>();
            set => Set(value);
        }

        public bool FixedAspectRatio
        {
            get => Get<bool>();
            set => Set(value);
        }

        public decimal AspectRatioWidth
        {
            get => Get<decimal>();
            set => Set(value);
        }

        public decimal AspectRatioHeight
        {
            get => Get<decimal>();
            set => Set(value);
        }

        public bool SupportLandscape
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool SupportPortrait
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool RunInFullScreen
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool AllowWindowResizing
        {
            get => Get<bool>();
            set => Set(value);
        }

        public int Scale
        {
            get => Get<int>();
            set => Set(value);
        }

        [DependsOn(nameof(RunInFullScreen))]
        public bool IsScaleUiEnabled => RunInFullScreen == false;

        [DependsOn(nameof(RunInFullScreen))]
        public Visibility FullScreenScaleMessageVisibility => RunInFullScreen ? Visibility.Visible : Visibility.Collapsed;

        public int ScaleGum
        {
            get => Get<int>();
            set => Set(value);
        }

        public ResizeBehavior ResizeBehavior
        {
            get { return Get<ResizeBehavior>(); }
            set { Set(value); }
        }

        [DependsOn(nameof(ResizeBehavior))]
        public bool UseStretchResizeBehavior
        {
            get { return ResizeBehavior == ResizeBehavior.StretchVisibleArea; }
            set
            {
                if (value) ResizeBehavior = ResizeBehavior.StretchVisibleArea;
            }
        }

        [DependsOn(nameof(ResizeBehavior))]
        public bool UseIncreaseVisibleResizeBehavior
        {
            get { return ResizeBehavior == ResizeBehavior.IncreaseVisibleArea; }
            set
            {
                if (value) ResizeBehavior = ResizeBehavior.IncreaseVisibleArea;
            }
        }

        public WidthOrHeight DominantInternalCoordinates
        {
            get { return Get<WidthOrHeight>(); }
            set { Set(value); }
        }

        [DependsOn(nameof(DominantInternalCoordinates))]
        public bool UseHeightInternalCoordinates
        {
            get { return DominantInternalCoordinates == WidthOrHeight.Height; }
            set { if (value) DominantInternalCoordinates = WidthOrHeight.Height; }
        }

        [DependsOn(nameof(DominantInternalCoordinates))]
        public bool UseWidthInternalCoordinates
        {
            get { return DominantInternalCoordinates == WidthOrHeight.Width; }
            set { if (value) DominantInternalCoordinates = WidthOrHeight.Width; }
        }


        [DependsOn(nameof(FixedAspectRatio))]
        public Visibility AspectRatioValuesVisibility
        {
            get
            {
                if(FixedAspectRatio)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        [DependsOn(nameof(FixedAspectRatio))]
        [DependsOn(nameof(AspectRatioWidth))]
        [DependsOn(nameof(AspectRatioHeight))]
        [DependsOn(nameof(ResolutionWidth))]
        [DependsOn(nameof(ResolutionHeight))]
        public Visibility ShowAspectRatioMismatch
        {
            get
            {
                Visibility visibility = Visibility.Collapsed;
                
                if(FixedAspectRatio  )
                {
                    decimal desiredAspectRatio = 1;

                    if (AspectRatioHeight != 0)
                    {
                        desiredAspectRatio = AspectRatioWidth / AspectRatioHeight;
                    }

                    decimal resolutionAspectRatio = 1;

                    if (ResolutionHeight != 0)
                    {
                        resolutionAspectRatio = (decimal)ResolutionWidth / (decimal)ResolutionHeight;
                    }

                    if(desiredAspectRatio != resolutionAspectRatio)
                    {
                        visibility = Visibility.Visible;
                    }
                }

                return visibility;
            }
        }

        [DependsOn(nameof(ResolutionHeight))]
        public string KeepResolutionHeightConstantMessage => $"Keep game coordinates height at {ResolutionHeight}";

        [DependsOn(nameof(ResolutionWidth))]
        public string KeepResolutionWidthConstantMessage => $"Keep game coordinates width at {ResolutionWidth}";

        [DependsOn(nameof(Is2D))]
        public Visibility OnResizeUiVisibility
        {
            get
            {
                return Is2D ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // always show this even in 3D projects
        [DependsOn(nameof(HasGumProject))]
        public Visibility OnResizeGumUiVisibility
        {
            get
            {
                if (HasGumProject) return Visibility.Visible;
                else return Visibility.Collapsed;
            }
        }

        [DependsOn(nameof(HasGumProject))]
        public Visibility GumScaleVisibility => HasGumProject ? Visibility.Visible : Visibility.Collapsed;

        public bool HasGumProject
        {
            get
            {
                var rfs = FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject.GetAllReferencedFiles()
                    .FirstOrDefault(item => FlatRedBall.IO.FileManager.GetExtension(item.Name) == "gumx");

                return rfs != null;
            }
        }

        public ResizeBehavior ResizeGumBehavior
        {
            get { return Get<ResizeBehavior>(); }
            set { Set(value); }
        }

        [DependsOn(nameof(ResizeGumBehavior))]
        public bool UseStretchResizeGumBehavior
        {
            get { return ResizeGumBehavior == ResizeBehavior.StretchVisibleArea; }
            set
            {
                if (value) ResizeGumBehavior = ResizeBehavior.StretchVisibleArea;
            }
        }

        [DependsOn(nameof(ResizeGumBehavior))]
        public bool UseIncreaseVisibleResizeGumBehavior
        {
            get { return ResizeGumBehavior == ResizeBehavior.IncreaseVisibleArea; }
            set
            {
                if (value) ResizeGumBehavior = ResizeBehavior.IncreaseVisibleArea;
            }
        }

        Visibility supportedOrientationsLinkVisibility;
        public Visibility SupportedOrientationsLinkVisibility
        {
            get { return supportedOrientationsLinkVisibility; }
            set
            {
                base.ChangeAndNotify(ref supportedOrientationsLinkVisibility, value);
            }
        }

        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        public DisplaySettings SelectedOption
        {
            get => Get<DisplaySettings>();
            set
            {
                if (Set(value) && value != null)
                {
                    SetFrom(value);
                }
            }
        }

        public ObservableCollection<DisplaySettings> AvailableOptions
        {
            get; private set;
        } = new ObservableCollection<DisplaySettings>();

        public void SetFrom(DisplaySettings displaySettings)
        {
            this.Name = displaySettings.Name;

            this.GenerateDisplayCode = displaySettings.GenerateDisplayCode;

            this.Is2D = displaySettings.Is2D;

            this.ResolutionWidth = displaySettings.ResolutionWidth;

            this.ResolutionHeight = displaySettings.ResolutionHeight;

            this.FixedAspectRatio = displaySettings.FixedAspectRatio;

            this.AspectRatioWidth = displaySettings.AspectRatioWidth;

            this.AspectRatioHeight = displaySettings.AspectRatioHeight;

            this.SupportLandscape = displaySettings.SupportLandscape;

            this.SupportPortrait = displaySettings.SupportPortrait;

            this.RunInFullScreen = displaySettings.RunInFullScreen;

            this.AllowWindowResizing = displaySettings.AllowWindowResizing;

            this.Scale = displaySettings.Scale;
            this.ScaleGum = displaySettings.ScaleGum;

            this.ResizeBehavior = displaySettings.ResizeBehavior;
            this.ResizeGumBehavior = displaySettings.ResizeBehaviorGum;

            this.DominantInternalCoordinates = displaySettings.DominantInternalCoordinates;
        }

        public DisplaySettings ToDisplaySettings()
        {
            DisplaySettings toReturn = new DisplaySettings();

            toReturn.Name = this.Name;

            toReturn.GenerateDisplayCode = this.GenerateDisplayCode;

            toReturn.Is2D = this.Is2D;

            toReturn.ResolutionWidth = this.ResolutionWidth;

            toReturn.ResolutionHeight = this.ResolutionHeight;

            toReturn.FixedAspectRatio = this.FixedAspectRatio;

            toReturn.AspectRatioWidth = this.AspectRatioWidth;

            toReturn.AspectRatioHeight = this.AspectRatioHeight;

            toReturn.SupportLandscape = this.SupportLandscape;

            toReturn.SupportPortrait = this.SupportPortrait;

            toReturn.RunInFullScreen = this.RunInFullScreen;

            toReturn.AllowWindowResizing = this.AllowWindowResizing;

            toReturn.Scale = this.Scale;
            toReturn.ScaleGum = this.ScaleGum;

            toReturn.ResizeBehavior = this.ResizeBehavior;
            toReturn.ResizeBehaviorGum = this.ResizeGumBehavior;

            toReturn.DominantInternalCoordinates = this.DominantInternalCoordinates;

            return toReturn;
        }
    }
}
