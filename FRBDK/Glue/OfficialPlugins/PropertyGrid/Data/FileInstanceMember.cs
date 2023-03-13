using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDataUi.DataTypes;

namespace OfficialPlugins.VariableDisplay.Data
{
    public interface IFileInstanceMember
    {
        event Action View;

        void OnView();
    }

    class FileInstanceMember : DataGridItem, IFileInstanceMember
    {

        public event Action View;

        public void OnView()
        {
            View?.Invoke();

        }
    }
}
