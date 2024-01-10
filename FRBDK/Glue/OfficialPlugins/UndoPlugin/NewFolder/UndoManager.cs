using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using OfficialPlugins.UndoPlugin.ViewModels;
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
        public static UndoViewModel UndoViewModel { get; set;}

        #region Variables Changed

        internal static void HandleNamedObjectValueChanges(List<VariableChangeArguments> changes)
        {
            var undoGroup = new UndoGroup();
            foreach (var change in changes)
            {
                if(change.RecordUndo)
                {

                    var undo = new UndoVariableAssignment();

                    var element = ObjectFinder.Self.GetElementContaining(change.NamedObject);

                    undo.ElementName = element?.Name;
                    undo.NamedObjectName = change.NamedObject.InstanceName;
                    undo.VariableName = change.ChangedMember;
                    undo.OldValue = change.OldValue;

                    undoGroup.Undos.Add(undo);
                }

            }

            if(undoGroup.Undos.Count > 0)
            {
                UndoViewModel.Undos.Add(undoGroup);
            }
        }

        #endregion

        #region Undo

        public static async void HandleUndo()
        {
            var lastUndo = UndoViewModel.Undos.LastOrDefault();

            if(lastUndo != null)
            {
                UndoViewModel.Undos.RemoveAt(UndoViewModel.Undos.Count - 1);

                await lastUndo.Execute();

            }
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
