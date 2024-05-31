﻿using FlatRedBall.Glue.MVVM;
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

        [DependsOn(nameof(IsPostProcessingChecked))]
        public Visibility PostProcessOptionsVisibility => IsPostProcessingChecked.ToVisibility();

        // todo - realtime validation - does the file exist?
    }

    enum ShaderType
    {
        PostProcessing,
        Sprite,
        Empty,
    }
}