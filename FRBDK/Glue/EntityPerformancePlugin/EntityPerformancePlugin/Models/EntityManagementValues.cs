using EntityPerformancePlugin.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityPerformancePlugin.Models
{
    public class EntityManagementValues
    {
        public string Name { get; set; }

        [JsonIgnore]
        public PropertyManagementMode PropertyManagementMode
        {
            get; set;
        } = PropertyManagementMode.FullyManaged;

        public List<string> SelectedProperties
        {
            get; set;
        } = new List<string>();

        public List<InstanceManagementValues> InstanceManagementValuesList
        {
            get; set;
        } = new List<InstanceManagementValues>();

    }
}
