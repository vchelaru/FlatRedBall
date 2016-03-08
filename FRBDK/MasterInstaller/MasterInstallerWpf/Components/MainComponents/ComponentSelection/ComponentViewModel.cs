using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MasterInstaller.Components.MainComponents.CustomSetup
{
    public class ComponentViewModel
    {
        InstallableComponentBase backingData;

        public InstallableComponentBase BackingData
        {
            get
            {
                return backingData;
            }
            set
            {
                backingData = value;
                if(backingData != null)
                {
                    Name = backingData.Name;
                }
            }
        }

        public string Name
        {
            get;
            set;
        }

        public bool IsSelected
        {
            get;
            set;
        }
    }
}
