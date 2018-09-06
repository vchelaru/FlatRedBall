using EntityPerformancePlugin.Enums;
using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityPerformancePlugin.ViewModels
{
    public class InstanceViewModel : ViewModel
    {
        public string Name
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public string Type
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public List<string> SelectedProperties
        {
            get; set;
        } = new List<string>();

        public PropertyManagementMode PropertyManagementMode { get; set; }

        public override string ToString()
        {
            return $"{Name} {PropertyManagementMode} with {SelectedProperties.Count} properties";
        }
    }
}
