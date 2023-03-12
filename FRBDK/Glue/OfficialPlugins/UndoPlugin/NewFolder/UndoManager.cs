using FlatRedBall.Glue.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OfficialPlugins.UndoPlugin.NewFolder
{
    internal static class UndoManager
    {
        #region Variables Changed

        internal static void ReactToChangedVariables(List<PluginManager.NamedObjectSaveVariableChange> changes)
        {
            foreach(var change in changes)
            {

            }
        }

        #endregion

        #region Undo

        private static void HandleUndo()
        {
            throw new NotImplementedException();
        }

        internal static void ReactToCtrlKey(Key key)
        {
            if(key == Key.Z)
            {
                HandleUndo();
            }
        }

        #endregion
    }
}
