using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.SaveClasses;
using System.Windows.Forms;
using FlatRedBall.Glue;
using GlueView.Facades;
using FlatRedBall.Glue.Elements;

namespace GlueViewTestPlugins.EntityControl
{
    public class PropertyGridManager
    {
        #region Fields

        RuntimeOptions mRuntimeOptions;

        ElementRuntime mCurrentElementRuntime;
        NamedObjectSave mCurrentNos;

        NamedObjectPropertyGridDisplayer mNosDisplayer = new NamedObjectPropertyGridDisplayer();

        PropertyGrid mPropertyGrid;

        #endregion

        public PropertyGridManager(PropertyGrid propertyGrid, RuntimeOptions runtimeOptions)
        {
            mRuntimeOptions = runtimeOptions;
            propertyGrid.PropertyValueChanged += new PropertyValueChangedEventHandler(OnPropertyValueChanged);
            mNosDisplayer.DisplayMode = NamedObjectPropertyGridDisplayer.DisplayModes.VariablesOnly;
            mPropertyGrid = propertyGrid;
        }

        void OnPropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            string propertyName = e.ChangedItem.Label;

            CustomVariable customVariable = null;
            


            if (mCurrentNos.SourceType == SourceType.Entity && !string.IsNullOrEmpty(mCurrentNos.SourceClassType))
            {
                IElement entitySave = ObjectFinder.Self.GetIElement(mCurrentNos.SourceClassType);
                customVariable = entitySave.GetCustomVariable(propertyName).Clone();
            }
            else
            {
                customVariable = new CustomVariable();
                customVariable.Name = propertyName;
            }
            customVariable.DefaultValue = e.ChangedItem.Value;

            customVariable.Type = mCurrentNos.GetCustomVariable(propertyName).Type;
            mCurrentElementRuntime.SetCustomVariable(customVariable);

            if (mRuntimeOptions.ShouldSave)
            {

                GlueViewCommands.Self.GlueProjectSaveCommands.SaveGlux();
            }
        }

        public void Show(ElementRuntime elementRuntime, NamedObjectSave nosToShow)
        {
            mCurrentElementRuntime = elementRuntime;
            if (nosToShow != null)
            {
                nosToShow.UpdateCustomProperties();
            }
            mCurrentNos = nosToShow;
            mNosDisplayer.CurrentElement = GlueViewState.Self.CurrentElement;
            mNosDisplayer.Instance = mCurrentNos;
            mNosDisplayer.PropertyGrid = mPropertyGrid;

        }


    }
}
