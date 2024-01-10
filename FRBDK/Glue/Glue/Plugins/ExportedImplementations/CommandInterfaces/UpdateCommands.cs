using System;
using System.Reflection;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using System.Xml.Serialization;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    class UpdateCommands : IUpdateCommands
    {
        private static IGlueCommands GlueCommand
        {
            get { return GlueCommands.Self; }
        }

        private static void RefreshElement(IElement element)
        {
            var glueCommands = GlueCommand;

            glueCommands.RefreshCommands.RefreshTreeNodeFor(element as GlueElement);
            Application.DoEvents();
            glueCommands.RefreshCommands.RefreshSelection();
            Application.DoEvents();
            glueCommands.RefreshCommands.RefreshPropertyGrid();
            glueCommands.GenerateCodeCommands.GenerateElementCode(element as GlueElement);
        }

        private static void CopyObject(object itemFrom, object itemTo)
        {
            foreach (var fieldFrom in itemFrom.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (fieldFrom.IsDefined(typeof (XmlIgnoreAttribute), true)) continue;

                var fieldTo = itemTo.GetType().GetField(fieldFrom.Name);

                if(fieldTo != null)
                {
                    fieldTo.SetValue(itemTo ,fieldFrom.GetValue(itemFrom));
                }
            }

            foreach (var propertyFrom in itemFrom.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (propertyFrom.IsDefined(typeof(XmlIgnoreAttribute), true)) continue;

                var propertyTo = itemTo.GetType().GetProperty(propertyFrom.Name);

                if (propertyTo != null && propertyTo.CanWrite)
                {
                    try
                    {
                        propertyTo.SetValue(itemTo, propertyFrom.GetValue(itemFrom, null), null);
                    }
                    catch (System.ArgumentException argumentException)
                    {
                        throw new ArgumentException($"Error trying to set {propertyTo.PropertyType} {propertyTo.Name} " +
                            $"to {itemTo}", argumentException);
                    }
                }
            }
        }

        #region IUpdateCommands Members


        public void Add(IElement element)
        {
            throw new NotImplementedException();
        }

        public void Add(string containerName, NamedObjectSave namedObjectSave)
        {
            throw new NotImplementedException();
        }

        public void Add(string containerName, CustomVariable customVariable)
        {
            throw new NotImplementedException();
        }

        public void Add(string containerName, StateSave stateSave)
        {
            throw new NotImplementedException();
        }

        public void Add(string containerName, StateSaveCategory stateSaveCategory)
        {
            throw new NotImplementedException();
        }

        public void Update(IElement element)
        {
            var currentElement = ObjectFinder.Self.GetElement(element.Name);

            CopyObject(element, currentElement);
            RefreshElement(currentElement);

            // I don't think we want to save here.  An
            // update can happen whenever the .glux is loaded
            // and we don't want to save right after loading.
            //GlueCommand.GluxCommands.SaveProjectAndElements();
        }

        public void Update(string containerName, NamedObjectSave namedObjectSave)
        {
            var container = ObjectFinder.Self.GetElement(containerName);
            var currentNos = container.GetNamedObjectRecursively(namedObjectSave.FieldName);

            CopyObject(namedObjectSave, currentNos);
            RefreshElement(container);
            //GlueCommand.GluxCommands.SaveProjectAndElements();
        }

        public void Update(string containerName, CustomVariable customVariable)
        {
            var container = ObjectFinder.Self.GetElement(containerName);
            var currentNos = container.GetCustomVariableRecursively(customVariable.Name);

            CopyObject(customVariable, currentNos);
            RefreshElement(container);
            //GlueCommand.GluxCommands.SaveProjectAndElements();
        }

        public void Update(string containerName, StateSave stateSave)
        {
            var container = ObjectFinder.Self.GetElement(containerName);
            var currentNos = container.GetState(stateSave.Name);

            CopyObject(stateSave, currentNos);
            RefreshElement(container);
            //GlueCommand.GluxCommands.SaveProjectAndElements();
        }

        public void Update(string containerName, StateSaveCategory stateSaveCategory)
        {
            var container = ObjectFinder.Self.GetElement(containerName);
            var currentNos = container.GetStateCategory(stateSaveCategory.Name);

            CopyObject(stateSaveCategory, currentNos);
            RefreshElement(container);
            //GlueCommand.GluxCommands.SaveProjectAndElements();
        }

        public void Remove(IElement element)
        {
            throw new NotImplementedException();
        }

        public void Remove(string containerName, NamedObjectSave namedObjectSave)
        {
            throw new NotImplementedException();
        }

        public void Remove(string containerName, CustomVariable customVariable)
        {
            throw new NotImplementedException();
        }

        public void Remove(string containerName, StateSave stateSave)
        {
            throw new NotImplementedException();
        }

        public void Remove(string containerName, StateSaveCategory stateSaveCategory)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
