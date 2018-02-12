using DialogTreePlugin.SaveClasses;
using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DialogTreePlugin.ViewModels
{
    public class DialogTreeLocalizationEntryViewModel : ViewModel
    {
        string dialogId;
        public string DialogId
        {
            get => dialogId;
            set { base.ChangeAndNotify(ref dialogId, value); }
        }

        string localizedText;
        public string LocalizedText
        {
            get => localizedText;
            set { base.ChangeAndNotify(ref localizedText, value); }
        }
    }
}
