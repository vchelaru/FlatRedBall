using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.FrbSourcePlugin.ViewModels
{
    public class AddFrbSourceViewModel
    {
        public string FrbRootFolder { get; set; }
        public string GumRootFolder { get; set; }
        public bool IncludeGumSkia { get; set; }
    }
}
