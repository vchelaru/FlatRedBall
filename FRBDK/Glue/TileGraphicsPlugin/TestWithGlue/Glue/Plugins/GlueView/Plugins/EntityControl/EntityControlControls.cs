using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue;
using GlueView.Facades;
using FlatRedBall.Glue.SaveClasses;

namespace GlueViewTestPlugins.EntityControl
{
    #region Enums

    public enum ActionType
	{
		None,
		Move,
		Scale,
		Rotate,
		Everything
	}

    #endregion

    public partial class EntityControlControls : UserControl
    {
        #region Fields

        RuntimeOptions mRuntimeOptions;

        PropertyGridManager mPropertyGridManager;

		Color unselectedButtonColor;
		ElementRuntime currentElementRuntime;

        #endregion

        public ActionType TypeOfAction
		{
			get;
			set;
		}

		public ComboBox LayerComboBox
		{
			get
			{
				return layerComboBox;
			}
		}


        public void SetCurrentNamedObject(ElementRuntime elementRuntime, NamedObjectSave nos)
        {
            mPropertyGridManager.Show(elementRuntime, nos);
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="element">The ElementRuntime that is selected</param>
		public EntityControlControls(ElementRuntime element, RuntimeOptions runtimeOptions)
		{
            mRuntimeOptions = runtimeOptions;
			InitializeComponent();

			TypeOfAction = ActionType.None;
			unselectedButtonColor = noneButton.BackColor;
			noneButton.BackColor = Color.Red;

			currentElementRuntime = element;

            mPropertyGridManager = new PropertyGridManager(propertyGrid1, runtimeOptions);

		}
        
		//The button click events
		private void noneButton_Click(object sender, EventArgs e)
		{
			TypeOfAction = ActionType.None;
			noneButton.BackColor = Color.Red;
			moveButton.BackColor = unselectedButtonColor;
		}

		private void moveButton_Click(object sender, EventArgs e)
		{
			TypeOfAction = ActionType.Everything;
			moveButton.BackColor = Color.Red;
			noneButton.BackColor = unselectedButtonColor;
		}

        private void saveCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            mRuntimeOptions.ShouldSave = saveCheckBox.Checked;
        }
	}
}
