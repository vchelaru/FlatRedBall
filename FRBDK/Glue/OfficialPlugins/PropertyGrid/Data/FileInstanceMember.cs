using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDataUi.DataTypes;

namespace OfficialPlugins.VariableDisplay.Data
{
    class FileInstanceMember : DataGridItem
    {

        public event Action View;

        public void OnView()
        {
            View?.Invoke();

        }
    }
}
