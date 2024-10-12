using FlatRedBall.Glue.MVVM;
using OfficialPlugins.EffectPlugin.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OfficialPlugins.EffectPlugin.ViewModels
{
    internal class NewFxOptionsViewModel : ViewModel
    {
        public bool IsIncludePostProcessCsFileChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string FxFileName
        {
            get => Get<string>();
            set => Set(value);
        }

        [DependsOn(nameof(FxFileName))]
        public string IncludePostProcessCsMessage
        {
            get
            {
                var fileName = !string.IsNullOrWhiteSpace(FxFileName) ? FxFileName : "PostProcess";
                return $"Include {fileName}.cs PostProcess file";
            }
        }
        
        public ShaderType ShaderType { get => Get<ShaderType>(); set => Set(value); }
        
        [DependsOn(nameof(ShaderType))]
        public bool IsPostProcessingChecked
        {
            get => ShaderType == ShaderType.PostProcessing;
            set
            {
                if (value)
                {
                    ShaderType = ShaderType.PostProcessing;
                }
            }
        }
        
        [DependsOn(nameof(ShaderType))]
        public bool IsSpriteChecked
        {
            get => ShaderType == ShaderType.Sprite;
            set
            {
                if (value)
                {
                    ShaderType = ShaderType.Sprite;
                }
            }
        }

        [DependsOn(nameof(ShaderType))]
        public bool IsEmptyChecked
        {
            get => ShaderType == ShaderType.Empty;
            set
            {
                if (value)
                {
                    ShaderType = ShaderType.Empty;
                }
            }
        }


        public ShaderContentsType ShaderContentsType
        {
            get => Get<ShaderContentsType>();
            set => Set(value);
        }

        public bool IsGradientContentsChecked
        {
            get => ShaderContentsType == ShaderContentsType.GradientColors;
            set { if (value) ShaderContentsType = ShaderContentsType.GradientColors; }
        }

        public bool IsSaturationChecked
        {
            get => ShaderContentsType == ShaderContentsType.Saturation;
            set { if (value) ShaderContentsType = ShaderContentsType.Saturation; }
        }

        public bool IsBloomChecked
        {
            get => ShaderContentsType == ShaderContentsType.Bloom;
            set { if (value) ShaderContentsType = ShaderContentsType.Bloom; }
        }

        [DependsOn(nameof(IsPostProcessingChecked))]
        public Visibility PostProcessOptionsVisibility => IsPostProcessingChecked.ToVisibility();

        // todo - realtime validation - does the file exist?

        public NewFxOptionsViewModel()
        {
            IsIncludePostProcessCsFileChecked = true;
        }
    }

    enum ShaderType
    {
        PostProcessing,
        Sprite,
        Empty,
    }


}
