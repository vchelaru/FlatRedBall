using FlatRedBall.Glue.MVVM;
using Newtonsoft.Json;
using OfficialPlugins.Wizard.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.Wizard.ViewModels
{
    public class WizardWelcomeViewModel : ViewModel
    {
        public string ConfigurationText
        {
            get => Get<string>();
            set
            {
                if(Set(value))
                {
                    if(TryParseJson<WizardViewModel>(value, out WizardViewModel deserialized))
                    {
                        DeserializedObject = deserialized;
                    }
                    else
                    {
                        DeserializedObject = null;
                    }
                }

            }
        }

        public WizardViewModel DeserializedObject
        {
            get => Get<WizardViewModel>();
            private set => Set(value);
        }

        [DependsOn(nameof(DeserializedObject))]
        public bool IsStartWithConfigurationEnabled => DeserializedObject != null;

        public bool TryParseJson<T>(string asString, out T result)
        {
            bool success = true;
            var settings = new JsonSerializerSettings
            {
                Error = (sender, args) => { success = false; args.ErrorContext.Handled = true; },
                MissingMemberHandling = MissingMemberHandling.Error
            };
            result = JsonConvert.DeserializeObject<T>(asString, settings);
            return success;
        }
    }
}
