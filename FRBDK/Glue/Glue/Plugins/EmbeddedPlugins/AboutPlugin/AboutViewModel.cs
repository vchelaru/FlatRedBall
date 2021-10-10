using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Text;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.AboutPlugin
{
    public class AboutViewModel : ViewModel
    {
        public string CopyrightText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string VersionNumberText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string GluxVersionText
        {
            get => Get<string>();
            set => Set(value);
        }


        
    }
}
