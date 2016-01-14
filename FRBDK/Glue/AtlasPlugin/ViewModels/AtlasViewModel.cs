using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasPlugin.ViewModels
{
    class AtlasViewModel
    {
        public string Folder
        {
            get; set;
        }

        public string DisplayName
        {
            get
            {
                if(string.IsNullOrEmpty(Folder))
                {
                    return "{Entire Content Folder}";
                }
                else
                {
                    return Folder;
                }
            }
        }

    }
}
