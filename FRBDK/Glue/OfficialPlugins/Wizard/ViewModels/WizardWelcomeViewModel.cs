using FlatRedBall.Glue.MVVM;
using Newtonsoft.Json;
using OfficialPluginsCore.Wizard.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPluginsCore.Wizard.ViewModels
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
                    if(TryParseJson<WizardData>(value, out WizardData deserialized))
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

        public WizardData DeserializedObject
        {
            get => Get<WizardData>();
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
