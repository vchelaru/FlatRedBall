using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.UndoPlugin.ViewModels
{
    internal class UndoViewModel : ViewModel
    {
        public ObservableCollection<UndoGroup> Undos { get; private set; } = new ObservableCollection<UndoGroup>();

    }

    class UndoGroup
    {
        public List<UndoBase> Undos { get; set; } = new List<UndoBase>();

        public async Task Execute()
        {
            //////////////Early Out/////////////////
            if(Undos.Count  == 0) return;
            ////////////End Early Out///////////////

            if (await Undos[0].TryExecuteGroup(Undos))
            {
                // done!
                //return;
            }
            else
            {
                foreach(var item in Undos)
                {
                    await item.Execute();
                }
            }
        }

        public override string ToString()
        {
            string toReturn = string.Empty;
            bool addNewline = false;
            foreach(var item in Undos)
            {
                if(addNewline)
                {
                    toReturn += "\n";
                }
                toReturn += item.ToString();

                addNewline = true;
            }

            return toReturn;
        }
    }

    internal abstract class UndoBase
    {
        public abstract Task Execute();

        public abstract Task<bool> TryExecuteGroup(List<UndoBase> undos);
    }

    internal class UndoVariableAssignment : UndoBase
    {
        public string ElementName { get; set; }
        public string NamedObjectName { get; set; }

        public string VariableName { get; set; }

        public object OldValue { get; set; }

        public override string ToString()
        {
            return $"{ElementName}.{NamedObjectName}.{VariableName} = {OldValue}";
        }

        public override async Task<bool> TryExecuteGroup(List<UndoBase> undos)
        {
            if(undos.All(item => item is UndoVariableAssignment))
            {
                List<NosVariableAssignment> assignments = new List<NosVariableAssignment>();
                foreach (UndoVariableAssignment undo in undos)
                {
                    var element = ObjectFinder.Self.GetElement(undo.ElementName);
                    var namedObject = element?.GetNamedObjectRecursively(undo.NamedObjectName);


                    var assignment = new NosVariableAssignment
                    {
                        VariableName = undo.VariableName,
                        NamedObjectSave = namedObject,
                        Value = undo.OldValue
                    };
                    assignments.Add(assignment);
                }
                if(assignments.Count > 0)
                {
                    await GlueCommands.Self.GluxCommands.SetVariableOnList(assignments, recordUndo: false);
                }
            }

            return false;
        }

        public override async Task Execute()
        {
            var element = ObjectFinder.Self.GetElement(ElementName);
            var namedObject = element?.GetNamedObjectRecursively(NamedObjectName);

            if(namedObject != null )
            {

                await GlueCommands.Self.GluxCommands.SetVariableOnAsync(namedObject, VariableName, OldValue,

                    updateUi:true,
                    recordUndo:false);
            }
        }
    }
}
