using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using FlatRedBall.Arrow.GlueView;
using FlatRedBall.Glue;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Content.Scene;
using FlatRedBall.Math.Geometry;
using ArrowDataConversion;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Arrow.DataTypes;

namespace FlatRedBall.Arrow.Managers
{
    public class ArrowCommands : Singleton<ArrowCommands>
    {
        #region Fields

        RelationshipManager mRelationshipManager = new RelationshipManager();

        #endregion

        #region Properties

        public AddCommands Add
        {
            get;
            private set;
        }

        public DeleteCommands Delete
        {
            get;
            private set;
        }



        public GuiCommands GuiCommands
        {
            get;
            private set;
        }

        public FileCommands File
        {
            get;
            private set;
        }

        public GlueViewCommands View
        {
            get;
            private set;
        }

        public CopyPasteCommands CopyPaste
        {
            get;
            private set;
        }

        #endregion

        #region Methods

        public ArrowCommands()
        {
            Add = new AddCommands();
            Delete = new DeleteCommands();
            GuiCommands = new GuiCommands();
            File = new FileCommands();
            View = new GlueViewCommands();
            CopyPaste = new CopyPasteCommands();
        }

        internal void Initialize(System.Windows.Controls.ItemsControl allElementsTreeView,
            TreeView singleElementTreeView, MenuItem deleteMenuItem, MenuItem copyMenuItem)
        {
            GuiCommands.Initialize(allElementsTreeView, singleElementTreeView);
            Delete.Initialize(deleteMenuItem);
        }

        internal void UpdateToSelectedInstance()
        {
            SelectionManager.Self.UpdateToSelectedElementOrInstance();
            ArrowCommands.Self.Delete.PopulateDeleteMenuFromArrowState();
            PropertyGridManager.Self.UpdateToSelectedInstance();
        }

        internal void UpdateToSelectedElement()
        {

            var element = ArrowState.Self.CurrentGlueElement;
            if (element != null)
            {
                GluxManager.ShowElement(element.Name);
                GluxManager.Update();
                ArrowState.Self.CurrentArrowElementVm.Refresh();
                //ArrowCommands.Self.GuiCommands.RefreshSingleElementTreeView();
                SelectionManager.Self.UpdateToSelectedElementOrInstance();
                
                ArrowState.Self.RaiseCurrentArrowElementChangedEvent();

            }
            ArrowCommands.Self.Delete.PopulateDeleteMenuFromArrowState();

        }

        public object UpdateInstanceValuesFromRuntime(ElementRuntime runtime)
        {
            object instance =
                RelationshipManager.Self.InstanceForElementRuntime(runtime);

            object whatToPullValuesFrom = runtime;
            if (runtime.DirectObjectReference != null)
            {
                whatToPullValuesFrom = runtime.DirectObjectReference;
            }
            PositionedObject whatToPullFromAsPo = whatToPullValuesFrom as PositionedObject;

            if (instance is CircleSave)
            {
                CircleSave save = (instance as CircleSave);
                save.SetFrom(whatToPullValuesFrom as Circle);
                SetSavePositionsFromRelativeValues(whatToPullFromAsPo, save);
            }
            else if (instance is SpriteSave)
            {
                SpriteSave save = (instance as SpriteSave);
                save.SetFrom(whatToPullValuesFrom as Sprite);
                SetSavePositionsFromRelativeValues(whatToPullFromAsPo, save);
            }
            else if (instance is AxisAlignedRectangleSave)
            {
                AxisAlignedRectangleSave save = (instance as AxisAlignedRectangleSave);
                save.SetFrom(whatToPullValuesFrom as AxisAlignedRectangle);
                SetSavePositionsFromRelativeValues(whatToPullFromAsPo, save);
            }
            else if (instance is ArrowElementInstance)
            {

                ArrowElementInstance save = (instance as ArrowElementInstance);

                if (whatToPullFromAsPo != null)
                {
                    if (whatToPullFromAsPo.Parent == null)
                    {
                        save.SetVariable("X", whatToPullFromAsPo.X);
                        save.SetVariable("Y", whatToPullFromAsPo.Y);
                    }
                    else
                    {
                        save.SetVariable("X", whatToPullFromAsPo.RelativeX);
                        save.SetVariable("Y", whatToPullFromAsPo.RelativeY);
                    }
                }
                
                // We can't do this because this object technically doesn't have X and Y properties
                //SetSavePositionsFromRelativeValues(whatToPullFromAsPo, save);
            }
            else
            {
                throw new Exception("Saving of type " + instance.GetType() + " is not supported");
            }

            return instance;
        }

        private static void SetSavePositionsFromRelativeValues(PositionedObject whatToPullFromAsPo, object saveObject)
        {
            if (whatToPullFromAsPo.Parent != null)
            {
                var lateBinderInstance = LateBinder.GetInstance(saveObject.GetType());
                lateBinderInstance.SetValue(saveObject, "X", whatToPullFromAsPo.RelativeX);
                lateBinderInstance.SetValue(saveObject, "Y", whatToPullFromAsPo.RelativeY);
                lateBinderInstance.SetValue(saveObject, "Z", whatToPullFromAsPo.RelativeZ);
            }
        }

        public void UpdateNosFromArrowInstance(object instance, NamedObjectSave currentNos)
        {
            if (currentNos != null)
            {
                GeneralSaveConverter converter = mRelationshipManager.ConverterFor(instance);

                converter.AddVariablesForAllProperties(instance, currentNos);

                currentNos.UpdateCustomProperties();
            }
        }

        #endregion
    }
}
