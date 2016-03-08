using MasterInstaller.Components.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MasterInstaller.Components.InstallableComponents.FRBDK
{
    class FileAssociationControl : BasePage
    {
        public FileAssociationControl() : base()
        {
            CreateCheckBox();
        }

        private void CreateCheckBox()
        {
            var checkBox = new CheckBox();

            checkBox.Content = "Set FRBDK file assocations";

            LeftPanel.Children.Add(checkBox);
        }
    }
}
