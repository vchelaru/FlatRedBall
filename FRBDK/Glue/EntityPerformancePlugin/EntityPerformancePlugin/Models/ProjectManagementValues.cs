using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityPerformancePlugin.Models
{
    public class ProjectManagementValues
    {
        public List<EntityManagementValues> EntityManagementValueList
        {
            get; set;
        } = new List<EntityManagementValues>();
    }
}
