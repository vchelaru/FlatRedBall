using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.GlobalContentManagerPlugin.ViewModels
{
    internal class GlobalContentPropertiesViewModel : ViewModel
    {
        public bool GenerateLoadGlobalContentCode
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LoadAsynchronously
        {
            get => Get<bool>();
            set => Set(value);
        }

        internal void SetFrom(GlobalContentSettingsSave globalContentSettingsSave)
        {
            this.GenerateLoadGlobalContentCode = globalContentSettingsSave.GenerateLoadGlobalContentCode;
            this.LoadAsynchronously = globalContentSettingsSave.LoadAsynchronously;
        }

        internal void SetOn(GlobalContentSettingsSave globalContentSettingsSave)
        {
            globalContentSettingsSave.GenerateLoadGlobalContentCode = this.GenerateLoadGlobalContentCode;
            globalContentSettingsSave.LoadAsynchronously = this.LoadAsynchronously;
        }
    }
}
