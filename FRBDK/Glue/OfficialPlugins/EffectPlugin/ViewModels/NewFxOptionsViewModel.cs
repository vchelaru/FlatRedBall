using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                return $"Include {FxFileName}.cs PostProcess file";
            }
        }

        // todo - realtime validation - does the file exist?
    }
}
