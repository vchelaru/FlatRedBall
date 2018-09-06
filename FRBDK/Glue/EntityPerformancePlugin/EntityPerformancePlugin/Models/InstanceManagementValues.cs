using EntityPerformancePlugin.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityPerformancePlugin.Models
{
    public class InstanceManagementValues
    {
        [JsonIgnore]
        public PropertyManagementMode PropertyManagementMode
        {
            get; set;
        } = PropertyManagementMode.FullyManaged;

        public List<string> SelectedProperties
        {
            get; set;
        } = new List<string>();

        public string Name { get; set; }

        public override string ToString()
        {
            return $"{Name} is {PropertyManagementMode} with {SelectedProperties.Count} selected properties";
        }
    }
}
