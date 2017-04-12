using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CameraPlugin
{
    public class DisplaySettingsViewModel : ViewModel
    {
        bool generateDisplayCode;
        public bool GenerateDisplayCode
        {
            get { return generateDisplayCode; }
            set { base.ChangeAndNotify(ref generateDisplayCode, value); }
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


        bool is2D;
        public bool Is2D
        {
            get { return is2D; }
            set { base.ChangeAndNotify(ref is2D, value); }
        }

        int resolutionWidth;
        public int ResolutionWidth
        {
            get { return resolutionWidth; }
            set { base.ChangeAndNotify(ref resolutionWidth, value); }
        }

        int resolutionHeight;
        public int ResolutionHeight
        {
            get { return resolutionHeight; }
            set { base.ChangeAndNotify(ref resolutionHeight, value); }
        }

        bool fixedAspectRatio;
        public bool FixedAspectRatio
        {
            get { return fixedAspectRatio; }
            set { base.ChangeAndNotify(ref fixedAspectRatio, value); }
        }

        decimal aspectRatioWidth;
        public decimal AspectRatioWidth
        {
            get { return aspectRatioWidth; }
            set { base.ChangeAndNotify(ref aspectRatioWidth, value); }
        }

        decimal aspectRatioHeight;
        public decimal AspectRatioHeight
        {
            get { return aspectRatioHeight; }
            set { base.ChangeAndNotify(ref aspectRatioHeight, value); }
        }

        bool supportLandscape;
        public bool SupportLandscape
        {
            get { return supportLandscape; }
            set { base.ChangeAndNotify(ref supportLandscape, value); }
        }

        bool supportPortrait;
        public bool SupportPortrait
        {
            get { return supportPortrait; }
            set { base.ChangeAndNotify(ref supportPortrait, value); }
        }

        bool runInFullScreen;
        public bool RunInFullScreen
        {
            get { return runInFullScreen; }
            set { base.ChangeAndNotify(ref runInFullScreen, value);  }
        }


        bool allowWindowResizing;
        public bool AllowWindowResizing
        {
            get { return allowWindowResizing; }
            set { base.ChangeAndNotify(ref allowWindowResizing, value); }
        }

        int scale;
        public int Scale
        {
            get { return scale; }
            set { base.ChangeAndNotify(ref scale, value); }
        }

        ResizeBehavior resizeBehavior;
        public ResizeBehavior ResizeBehavior
        {
            get { return resizeBehavior; }
            set { base.ChangeAndNotify(ref resizeBehavior, value); }
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

        [DependsOn(nameof(Is2D))]
        public Visibility OnResizeUiVisibility
        {
            get
            {
                if (Is2D) return Visibility.Visible;
                else return Visibility.Collapsed;
            }
        }

        public void SetFrom(DisplaySettings displaySettings)
        {
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

            this.ResizeBehavior = displaySettings.ResizeBehavior;
        }

        public DisplaySettings ToDisplaySettings()
        {
            DisplaySettings toReturn = new DisplaySettings();

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

            toReturn.ResizeBehavior = this.ResizeBehavior;

            return toReturn;
        }
    }
}
