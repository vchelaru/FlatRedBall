using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MasterInstaller.Components.Controls;

namespace MasterInstaller.Components.MainComponents.VisualStudioInformation
{
    class VisualStudioInfoComponent : ComponentBase
    {
        protected override BasePage CreateControl()
        {

            return new VisualStudioInfoControl();
        }
    }
}
