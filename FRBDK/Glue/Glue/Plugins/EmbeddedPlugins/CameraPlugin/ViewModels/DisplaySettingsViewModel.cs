using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CameraPlugin
{
    #region Enums

    public enum Perspective
    {
        Perspective2D,
        Perspective3D
    }

    #endregion

    #region AspectRatioViewModel Class
    public class AspectRatioViewModel : ViewModel
    {
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

        public Visibility Visibility
        {
            get => Get<Visibility>();
            set => Set(value);
        }
        public DisplaySettingsViewModel ParentViewModel { get; internal set; }
    }

    #endregion

    public class DisplaySettingsViewModel : ViewModel
    {
        #region Fields/Properties

        public bool GenerateDisplayCode
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(GenerateDisplayCode))]
        public Visibility AllPropertiesVisibility => GenerateDisplayCode.ToVisibility();

        #region Filtering

        public TextureFilter TextureFilter
        {
            get => Get<TextureFilter>();
            set => Set(value);
        }

        [DependsOn(nameof(TextureFilter))]
        public bool IsPointFilter
        {
            get => TextureFilter == TextureFilter.Point;
            set
            {
                if(value)
                {
                    TextureFilter = TextureFilter.Point;
                }
            }
        }

        [DependsOn(nameof(TextureFilter))]
        public bool IsLinearFilter
        {
            get => TextureFilter == TextureFilter.Linear;
            set
            {
                if(value)
                {
                    TextureFilter = TextureFilter.Linear;
                }
            }
        }

        #endregion

        #region Perspective

        public Perspective Perspective
        {
            get => Get<Perspective>();
            set => Set(value);
        }

        [DependsOn(nameof(Perspective))]
        public bool Is2D
        {
            get => Perspective == Perspective.Perspective2D;
            set
            {
                if(value)
                {
                    Perspective = Perspective.Perspective2D;
                }
            }
        }

        [DependsOn(nameof(Perspective))]
        public bool Is3D
        {
            get => Perspective == Perspective.Perspective3D;
            set
            {
                if(value)
                {
                    Perspective = Perspective.Perspective3D;
                }
            }
        }

        #endregion

        #region Resolution Width/Height
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
        #endregion

        #region Aspect Ratio

        public AspectRatioBehavior AspectRatioBehavior
        {
            get => Get<AspectRatioBehavior>();
            set
            {
                if (Set(value))
                {
                    RefreshAspectRatioVisibility();
                }
            }
        }

        private void RefreshAspectRatioVisibility()
        {
            AspectRatio1.Visibility = 
                (AspectRatioBehavior == AspectRatioBehavior.FixedAspectRatio || 
                AspectRatioBehavior == AspectRatioBehavior.RangedAspectRatio
                    ).ToVisibility();
            AspectRatio2.Visibility =
                (AspectRatioBehavior == AspectRatioBehavior.RangedAspectRatio)
                    .ToVisibility();
        }

        [DependsOn(nameof(AspectRatioBehavior))]
        public Visibility DashVisibility => (AspectRatioBehavior == AspectRatioBehavior.RangedAspectRatio)
                    .ToVisibility();

        [DependsOn(nameof(AspectRatioBehavior))]
        public bool IsVariableAspectRatio
        {
            get => AspectRatioBehavior == AspectRatioBehavior.NoAspectRatio;
            set
            {
                if(value)
                {
                    AspectRatioBehavior = AspectRatioBehavior.NoAspectRatio;
                }
            }
        }

        [DependsOn(nameof(AspectRatioBehavior))]
        public bool IsFixedAspectRatio
        {
            get => AspectRatioBehavior == AspectRatioBehavior.FixedAspectRatio;
            set
            {
                if (value)
                {
                    AspectRatioBehavior = AspectRatioBehavior.FixedAspectRatio;
                }
            }
        }

        [DependsOn(nameof(AspectRatioBehavior))]
        public bool IsRangedAspectRatio
        {
            get => AspectRatioBehavior == AspectRatioBehavior.RangedAspectRatio;
            set
            {
                if (value)
                {
                    AspectRatioBehavior = AspectRatioBehavior.RangedAspectRatio;
                }
            }
        }

        public AspectRatioViewModel AspectRatio1
        {
            get => Get<AspectRatioViewModel>();
            set => Set(value);
        }

        public AspectRatioViewModel AspectRatio2
        {
            get => Get<AspectRatioViewModel>();
            set => Set(value);
        }

        [DependsOn(nameof(AspectRatioBehavior))]
        [DependsOn(nameof(AspectRatio1))]
        [DependsOn(nameof(AspectRatio2))]
        [DependsOn(nameof(ResolutionWidth))]
        [DependsOn(nameof(ResolutionHeight))]
        public Visibility ShowAspectRatioMismatch
        {
            get
            {
                Visibility visibility = Visibility.Collapsed;
                
                if(AspectRatioBehavior == AspectRatioBehavior.FixedAspectRatio)
                {
                    bool isMismatch = GetIfIsMismatch(AspectRatio1);
                    visibility = isMismatch.ToVisibility();
                }
                else if(AspectRatioBehavior == AspectRatioBehavior.RangedAspectRatio)
                {
                    bool isMismatch = GetIfIsMismatch(AspectRatio1) || GetIfIsMismatch(AspectRatio2);
                    visibility = isMismatch.ToVisibility();
                }

                return visibility;
            }
        }

        private bool GetIfIsMismatch(AspectRatioViewModel aspectRatioViewModel)
        {
            decimal desiredAspectRatio = 1;

            if (aspectRatioViewModel.AspectRatioHeight != 0)
            {
                desiredAspectRatio = aspectRatioViewModel.AspectRatioWidth / aspectRatioViewModel.AspectRatioHeight;
            }

            decimal resolutionAspectRatio = 1;

            if (ResolutionHeight != 0)
            {
                resolutionAspectRatio = (decimal)ResolutionWidth / (decimal)ResolutionHeight;
            }

            var isMismatch =
                desiredAspectRatio != resolutionAspectRatio;
            return isMismatch;
        }

        [DependsOn(nameof(ResolutionHeight))]
        public string KeepResolutionHeightConstantMessage => $"Keep game world height at {ResolutionHeight}";

        [DependsOn(nameof(ResolutionWidth))]
        public string KeepResolutionWidthConstantMessage => $"Keep game world width at {ResolutionWidth}";

        [DependsOn(nameof(AspectRatioBehavior))]
        public string WidthHeightSelectionText
        {
            get
            {
                if(AspectRatioBehavior == AspectRatioBehavior.FixedAspectRatio)
                {
                    return "The aspect ratio does not match the calculated resolution aspect ratio";
                }
                else if(AspectRatioBehavior == AspectRatioBehavior.RangedAspectRatio)
                {
                    return "The aspect ratio may not match the calculated resolution aspect ratio";
                }
                else
                {
                    return "";
                }
            }
        }

        #endregion

        #region Landscape/Portrait

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

        #endregion

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

        #region Scale/Zoom

        public int Scale
        {
            get => Get<int>();
            set => Set(value);
        }

        [DependsOn(nameof(IsScaleUiEnabled))]
        public Visibility GameResolutionLabelVisibility => IsScaleUiEnabled.ToVisibility();

        [DependsOn(nameof(Scale))]
        [DependsOn(nameof(ResolutionWidth))]
        [DependsOn(nameof(ResolutionHeight))]
        public string GameDisplayResolutionText => $"Game Display Resolution: {ResolutionWidth*Scale/100}x{ResolutionHeight*Scale/100}";

        [DependsOn(nameof(RunInFullScreen))]
        public bool IsScaleUiEnabled => RunInFullScreen == false;

        [DependsOn(nameof(RunInFullScreen))]
        public Visibility FullScreenScaleMessageVisibility => 
            RunInFullScreen ? Visibility.Visible : Visibility.Collapsed;

        public int ScaleGum
        {
            get => Get<int>();
            set => Set(value);
        }

        public Visibility EffectiveGumScaleVisibility =>
            // always show it so there's no confusion
            Visibility.Visible;
            //(ScaleGum != 100).ToVisibility();


        [DependsOn(nameof(Scale))]
        [DependsOn(nameof(ScaleGum))]
        public string EffectiveFontScaleContent
        {
            get
            {
                var effectiveScale = (int)(100 * ((ScaleGum / 100m) * (Scale / 100m)));
                return $"Effective Gum Scale:{effectiveScale}%";
            }
        }
        #endregion

        public ResizeBehavior ResizeBehavior
        {
            get => Get<ResizeBehavior>(); 
            set => Set(value); 
        }

        [DependsOn(nameof(ResizeBehavior))]
        public bool UseStretchResizeBehavior
        {
            get => ResizeBehavior == ResizeBehavior.StretchVisibleArea; 
            set
            {
                if (value) ResizeBehavior = ResizeBehavior.StretchVisibleArea;
            }
        }

        [DependsOn(nameof(ResizeBehavior))]
        public bool UseIncreaseVisibleResizeBehavior
        {
            get => ResizeBehavior == ResizeBehavior.IncreaseVisibleArea;
            set
            {
                if (value) ResizeBehavior = ResizeBehavior.IncreaseVisibleArea;
            }
        }

        public WidthOrHeight DominantInternalCoordinates
        {
            get => Get<WidthOrHeight>(); 
            set => Set(value); 
        }

        [DependsOn(nameof(DominantInternalCoordinates))]
        public bool UseHeightInternalCoordinates
        {
            get => DominantInternalCoordinates == WidthOrHeight.Height; 
            set { if (value) DominantInternalCoordinates = WidthOrHeight.Height; }
        }

        [DependsOn(nameof(DominantInternalCoordinates))]
        public bool UseWidthInternalCoordinates
        {
            get => DominantInternalCoordinates == WidthOrHeight.Width;
            set { if (value) DominantInternalCoordinates = WidthOrHeight.Width; }
        }

        [DependsOn(nameof(Is2D))]
        public Visibility OnResizeUiVisibility
        {
            get =>
                Is2D ? Visibility.Visible : Visibility.Collapsed;
        }

        // always show this even in 3D projects
        [DependsOn(nameof(HasGumProject))]
        public Visibility OnResizeGumUiVisibility
        {
            get
            {
                return HasGumProject.ToVisibility();
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

        #endregion

        public DisplaySettingsViewModel()
        {
            AspectRatio1 = new AspectRatioViewModel();
            AspectRatio2 = new AspectRatioViewModel();

            AspectRatio1.ParentViewModel = this;
            AspectRatio2.ParentViewModel = this;

            RefreshAspectRatioVisibility();

            AspectRatio1.PropertyChanged += (_, _) => NotifyPropertyChanged(nameof(AspectRatio1));
            AspectRatio2.PropertyChanged += (_, _) => NotifyPropertyChanged(nameof(AspectRatio2));
        }

        public void SetFrom(DisplaySettings displaySettings)
        {
            this.Name = displaySettings.Name;

            this.GenerateDisplayCode = displaySettings.GenerateDisplayCode;

            this.Is2D = displaySettings.Is2D;
            this.Is3D = !displaySettings.Is2D;

            this.ResolutionWidth = displaySettings.ResolutionWidth;

            this.ResolutionHeight = displaySettings.ResolutionHeight;

            // todo- handle different types here:
            //this.IsFixedAspectRatio = displaySettings.FixedAspectRatio;
            this.AspectRatioBehavior = displaySettings.AspectRatioBehavior;

            this.AspectRatio1.AspectRatioWidth = displaySettings.AspectRatioWidth;
            this.AspectRatio1.AspectRatioHeight = displaySettings.AspectRatioHeight;

            this.AspectRatio2.AspectRatioWidth = displaySettings.AspectRatioWidth2;
            this.AspectRatio2.AspectRatioHeight = displaySettings.AspectRatioHeight2;

            this.SupportLandscape = displaySettings.SupportLandscape;

            this.SupportPortrait = displaySettings.SupportPortrait;

            this.RunInFullScreen = displaySettings.RunInFullScreen;

            this.AllowWindowResizing = displaySettings.AllowWindowResizing;

            this.TextureFilter = (TextureFilter)displaySettings.TextureFilter;

            this.Scale = displaySettings.Scale;
            this.ScaleGum = displaySettings.ScaleGum;

            this.ResizeBehavior = displaySettings.ResizeBehavior;
            this.ResizeGumBehavior = displaySettings.ResizeBehaviorGum;

            this.DominantInternalCoordinates = displaySettings.DominantInternalCoordinates;
        }

        public DisplaySettings ToDisplaySettings()
        {
            DisplaySettings toReturn = new DisplaySettings();

            toReturn.SetDefaults();

            toReturn.Name = this.Name;

            toReturn.GenerateDisplayCode = this.GenerateDisplayCode;

            toReturn.Is2D = this.Is2D;

            toReturn.ResolutionWidth = this.ResolutionWidth;

            toReturn.ResolutionHeight = this.ResolutionHeight;

            toReturn.AspectRatioBehavior = this.AspectRatioBehavior;

            toReturn.AspectRatioWidth = this.AspectRatio1.AspectRatioWidth;
            toReturn.AspectRatioHeight = this.AspectRatio1.AspectRatioHeight;

            toReturn.AspectRatioWidth2 = this.AspectRatio2.AspectRatioWidth;
            toReturn.AspectRatioHeight2 = this.AspectRatio2.AspectRatioHeight;

            toReturn.SupportLandscape = this.SupportLandscape;

            toReturn.SupportPortrait = this.SupportPortrait;

            toReturn.RunInFullScreen = this.RunInFullScreen;

            toReturn.TextureFilter = (int)this.TextureFilter;

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
