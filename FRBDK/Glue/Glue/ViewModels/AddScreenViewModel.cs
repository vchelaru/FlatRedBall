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

        /// <summary>
        /// The containing Directory relative to the project. If blank, the Screen is added to the Screens folder. If not blank,
        /// this should contain the Screens\\ prefix. For example, a proper value might be: "Screens\\Level1\\".
        /// </summary>
        public string Directory
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool HasChangedScreenTextBox
        {
            get => Get<bool>();
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
