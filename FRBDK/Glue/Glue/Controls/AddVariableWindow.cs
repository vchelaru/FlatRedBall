using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Glue;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Utilities;
using FlatRedBall.Glue.TypeConversions;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.Controls
{
    #region CustomVariableType enum

    public enum CustomVariableType
    {
        Exposed,
        Tunneled,
        New
    }

    #endregion

    public partial class AddVariableWindow : Form
    {
        #region Fields



        #endregion

        #region Properties

        public CustomVariableType DesiredVariableType
        {
            get
            {
                if (radExistingVariable.Checked)
                    return CustomVariableType.Exposed;
                else if (radTunnelVariable.Checked)
                    return CustomVariableType.Tunneled;
                else
                    return CustomVariableType.New;
            }
            set
            {
                switch (value)
                {
                    case CustomVariableType.Exposed:
                        radExistingVariable.Checked = true;
                        break;
                    case CustomVariableType.Tunneled:
                        radTunnelVariable.Checked = true;
                        break;
                    case CustomVariableType.New:
                        radCreateNewVariable.Checked = true;
                        break;
                }
            }
        }

        public string ResultName
		{
			get 
            {
                if (radExistingVariable.Checked)
                    return this.AvailableVariablesComboBox.Text;
                else if (radTunnelVariable.Checked)
                    return this.AlternativeNameTextBox.Text;
                else
                    return this.NewVariableNameTextBox.Text;
            }
		}

		public string ResultType
		{
			get 
            {
                if (radExistingVariable.Checked)
                {
                    if (EditorLogic.CurrentEntitySave != null)
                    {
                        string type = ExposedVariableManager.GetMemberTypeForEntity(ResultName, EditorLogic.CurrentEntitySave);

                        return TypeManager.ConvertToCommonType(type);

                    }
                    else
                    {
                        string type = ExposedVariableManager.GetMemberTypeForScreen(ResultName, EditorLogic.CurrentScreenSave);

                        return TypeManager.ConvertToCommonType(type);
                    }
                }
                else if (radTunnelVariable.Checked)
                {
                    NamedObjectSave nos = EditorLogic.CurrentElement.GetNamedObjectRecursively(TunnelingObjectComboBox.Text);
                    string type = ExposedVariableManager.GetMemberTypeForNamedObject(nos, this.TunnelingVariableComboBox.Text);

                    return TypeManager.ConvertToCommonType(type);
                }
                else
                {
                    object value = this.NewTypeListBox.SelectedItem;
                    return value as string;
                }
                //return this.comboBox1.Text; 
            }
		}

        public string TunnelingObject
        {
            get
            {
                if (radTunnelVariable.Checked)
                    return TunnelingObjectComboBox.Text;
                else
                    return null;
            }
            set
            {
                TunnelingObjectComboBox.Text = value;
            }
        }

        public string TunnelingVariable
        {
            get
            {
                if (radTunnelVariable.Checked)
                    return TunnelingVariableComboBox.Text;
                else
                    return null;
            }
        }

        public string OverridingType
        {
            get 
            {
                if (OverridingVariableTypeComboBox.SelectedIndex > 0)
                    return OverridingVariableTypeComboBox.Text;
                else
                    return null;
            }
        }

        public string TypeConverter
        {
            get { return TypeConverterComboBox.Text; }
        }

        #endregion

        #region Methods

        public AddVariableWindow(IElement element)
		{
			InitializeComponent();

            TypeConverterHelper.InitializeClasses();

			StartPosition = FormStartPosition.Manual;
			Location = new Point(MainGlueWindow.MousePosition.X - this.Width/2, 
                System.Math.Max(0,MainGlueWindow.MousePosition.Y - Height/2));
            

            FillExposableVariables();

            FillTunnelingObjects();

            FillOverridingTypesComboBox();

            FillTypeConverters();

            FillNewVariableTypes(element);
		}

        private void FillNewVariableTypes(IElement element)
        {
            List<string> newVariableTypes = ExposedVariableManager.GetAvailableNewVariableTypes(allowNone:false);

            foreach (string s in newVariableTypes)
            {
                this.NewTypeListBox.Items.Add(s);
            }

            this.NewTypeListBox.SelectedIndex = 0;
        }

        private List<string> GetNewVariableTypes()
        {
            throw new NotImplementedException();
        }

        private void FillTypeConverters()
        {
            List<string> converters = AvailableCustomVariableTypeConverters.GetAvailableConverters();

            foreach (string converter in converters)
                TypeConverterComboBox.Items.Add(converter);

            if (TypeConverterComboBox.Items.Count > 0)
                TypeConverterComboBox.SelectedIndex = 0;
        }

        private void FillOverridingTypesComboBox()
        {
            foreach (string propertyType in ExposedVariableManager.AvailablePrimitives)
            {
                OverridingVariableTypeComboBox.Items.Add(propertyType);
            }

            if (OverridingVariableTypeComboBox.Items.Count > 0)
            {
                OverridingVariableTypeComboBox.SelectedIndex = 0;
            }
        }

        private void FillExposableVariables()
        {
            List<string> availableVariables = null;

            if (EditorLogic.CurrentEntitySave != null)
            {
                availableVariables = ExposedVariableManager.GetExposableMembersFor(EditorLogic.CurrentEntitySave, true).Select(item=>item.Member).ToList();

            }
            else if (EditorLogic.CurrentScreenSave != null)
            {
                availableVariables = ExposedVariableManager.GetExposableMembersFor(EditorLogic.CurrentScreenSave, true).Select(item => item.Member).ToList();
            }

            if (availableVariables != null)
            {
                // We don't want to expose things like velocity an acceleration in Glue
                List<string> velocityAndAccelerationVariables = ExposedVariableManager.GetPositionedObjectRateVariables();
                // We also don't want to expose relative values - the user just simply sets the value and the state/variable handles
                // whether it sets relative or absolute depending on whether the Entity is attached or not.
                // This behavior used to not exist, but users never knew when to use relative or absolute, and
                // that level of control is not really needed...if it is, custom code can probably handle it.
                List<string> relativeVariables = ExposedVariableManager.GetPositionedObjectRelativeValues();

                foreach (string variableName in availableVariables)
                {
                    if (!velocityAndAccelerationVariables.Contains(variableName) && !relativeVariables.Contains(variableName))
                    {
                        AvailableVariablesComboBox.Items.Add(variableName);
                    }
                }

                if (AvailableVariablesComboBox.Items.Count > 0)
                    AvailableVariablesComboBox.SelectedIndex = 0;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void AddVariableWindow_Load(object sender, EventArgs e)
        {

            
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {

            }
        }

        private void AddVariableWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            // See if if the user is trying to create a reserved variable
            if (this.DialogResult == System.Windows.Forms.DialogResult.OK &&
                this.radCreateNewVariable.Checked && 
                EditorLogic.CurrentEntitySave != null &&
                ExposedVariableManager.IsMemberDefinedByPositionedObject(this.NewVariableNameTextBox.Text)
                )
            {
                System.Windows.Forms.MessageBox.Show("The variable " + this.NewVariableNameTextBox.Text + " is " +
                    "already defined by the engine.  You can expose this variable or select a different name.");
                e.Cancel = true;
            }
        }

        private void FillTunnelingObjects()
        {
            List<string> availableObjects = AvailableNamedObjectsAndFiles.GetAvailableObjects(false, true, GlueState.Self.CurrentElement);
            foreach (string availableObject in availableObjects)
            {
                this.TunnelingObjectComboBox.Items.Add(availableObject);
            }
        }



        private void TunnelingVariableComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            AlternativeNameTextBox.Text = 
                TunnelingObjectComboBox.Text + 
                TunnelingVariableComboBox.Text;
        }

        private void TunnelingObjectComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string nameOfNamedObject = TunnelingObjectComboBox.Text;

            NamedObjectSave nos = EditorLogic.CurrentElement.GetNamedObjectRecursively(nameOfNamedObject);

            PopulateTunnelingVariables(nameOfNamedObject, nos);
        }

        private void PopulateTunnelingVariables(string nameOfNamedObject, NamedObjectSave nos)
        {
            List<string> availableVariables = null;
            if (nos != null)
            {
                availableVariables = ExposedVariableManager.GetExposableMembersFor(nos).Select(item=>item.Member).ToList();


                // We should remove any variables that are already tunneled into
                foreach (CustomVariable customVariable in EditorLogic.CurrentElement.CustomVariables)
                {
                    if (customVariable.SourceObject == nameOfNamedObject)
                    {
                        // Reverse loop since we're removing things
                        for (int i = availableVariables.Count - 1; i > -1; i--)
                        {
                            if (availableVariables[i] == customVariable.SourceObjectProperty)
                            {
                                availableVariables.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }
            }

            if (availableVariables != null)
            {
                availableVariables.Sort();

                this.TunnelingVariableComboBox.Items.Clear();
                if (availableVariables != null)
                {
                    // We don't want to expose things like velocity an acceleration in Glue
                    List<string> velocityAndAccelerationVariables = ExposedVariableManager.GetPositionedObjectRateVariables();
                    // We also don't want to expose relative values - the user just simply sets the value and the state/variable handles
                    // whether it sets relative or absolute depending on whether the Entity is attached or not.
                    // This behavior used to not exist, but users never knew when to use relative or absolute, and
                    // that level of control is not really needed...if it is, custom code can probably handle it.
                    List<string> relativeVariables = ExposedVariableManager.GetPositionedObjectRelativeValues();


                    foreach (string availableVariable in availableVariables)
                    {
                        if (!velocityAndAccelerationVariables.Contains(availableVariable) && !relativeVariables.Contains(availableVariable))
                        {
                            this.TunnelingVariableComboBox.Items.Add(availableVariable);
                        }
                    }

                }
            }
        }

        private void radCreateNewVariable_CheckedChanged(object sender, EventArgs e)
        {
            TunnelVariablePanel.Visible = false;
            NewVariablePanel.Visible = false;
            ExistingVariablePanel.Visible = false;

            if (radCreateNewVariable.Checked)
            {
                NewVariablePanel.Visible = true;
                this.NewVariableNameTextBox.Focus();

            }
            else if (radExistingVariable.Checked)
            {
                ExistingVariablePanel.Visible = true;
            }
            else if (radTunnelVariable.Checked)
            {
                TunnelVariablePanel.Visible = true;
            }
        }

        #endregion

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            this.EnsureOnScreen();
        }

        private void NewVariableTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void NewVariableTypeComboBox_DropDown(object sender, EventArgs e)
        {
            ComboBox senderComboBox = (ComboBox)sender;
            int width = senderComboBox.DropDownWidth;
            System.Drawing.Graphics g = senderComboBox.CreateGraphics();
            Font font = senderComboBox.Font;
            int vertScrollBarWidth =
                (senderComboBox.Items.Count > senderComboBox.MaxDropDownItems)
                ? SystemInformation.VerticalScrollBarWidth : 0;
            int newWidth;

            foreach (string s in ((ComboBox)sender).Items)
            {
                newWidth = (int)g.MeasureString(s, font).Width
                    + vertScrollBarWidth;

                if (width < newWidth)
                {
                    width = newWidth;
                }
            }

            senderComboBox.DropDownWidth = width;
        }

        private void NewTypeListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.NewVariableNameTextBox.Focus();
        }

        private void NewTypeListBox_Click(object sender, EventArgs e)
        {
            this.NewVariableNameTextBox.Focus();
        }
    }
}
