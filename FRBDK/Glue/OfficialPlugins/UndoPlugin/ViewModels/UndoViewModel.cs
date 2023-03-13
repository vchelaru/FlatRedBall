using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
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
            foreach(var item in Undos)
            {
                await item.Execute();
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
