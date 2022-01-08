using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace GlueFormsCore.ViewModels
{
    public class AddScreenViewModel : ViewModel
    {
        public string ScreenName
        {
            get => Get<string>();
            set => Set(value);
        }

        [DependsOn(nameof(ScreenName))]
        public string NameValidationMessage
        {
            get
            {
                if(!NameVerifier.IsScreenNameValid(ScreenName, null, out string whyItIsntValid))
                {
                    return whyItIsntValid;
                }
                return null;
            }
        }

        [DependsOn(nameof(NameValidationMessage))]
        public Visibility ValidationVisibility => (!string.IsNullOrEmpty(NameValidationMessage)).ToVisibility();
    }
}
