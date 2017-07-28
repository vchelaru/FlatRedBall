using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ContentPipelinePlugin
{
    public class ContentPipelineController
    {
        SettingsSave settings;
        ContentPipelineControl control;

        public void Initialize(ContentPipelineControl control)
        {
            this.control = control;

            settings = SaveLoadLogic.LoadSettings();
            if(settings == null)
            {
                settings = new ContentPipelinePlugin.SettingsSave();
            }
            control.CheckBoxClicked += HandleCheckBoxClicked;
        }

        private void HandleCheckBoxClicked(object sender, EventArgs e)
        {
            settings.UseContentPipelineOnAllPngs = control.UseContentPipeline;
            SaveLoadLogic.SaveSettings(settings);
        }
    }
}
