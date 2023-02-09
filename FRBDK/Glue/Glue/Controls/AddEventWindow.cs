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
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GlueFormsCore.ViewModels;

namespace FlatRedBall.Glue.Controls
{
    #region Events

    public enum CustomEventType
    {
        Exposed,
        Tunneled,
        New
    }

    #endregion

    public partial class AddEventWindow : Form
    {
        #region Properties

        AddEventViewModel viewModel;
        public AddEventViewModel ViewModel
        {
            get => viewModel;
            set
            {
                viewModel = value;
                if(viewModel != null)
                {
                    SetFrom(viewModel);
                }
            }
        }

        public CustomEventType DesiredEventType
        {
            get
            {
                if (radExistingEvent.Checked)
                    return CustomEventType.Exposed;
                else if (radTunnelEvent.Checked)
                    return CustomEventType.Tunneled;
                else
                    return CustomEventType.New;
            }
            set
            {
                switch (value)
                {
                    case CustomEventType.Exposed:
                        radExistingEvent.Checked = true;
                        break;
                    case CustomEventType.Tunneled:
                        radTunnelEvent.Checked = true;
                        break;
                    default:
                        radCreateNewEvent.Checked = true;
                        break;
                }
            }
        }

        public string ResultName
		{
			get 
            {
                if (radExistingEvent.Checked)
                    return this.AvailableEventsComboBox.Text;
                else if (radTunnelEvent.Checked)
                    return this.AlternativeNameTextBox.Text;
                else
                    return this.textBox1.Text;
            }
		}

        public string SourceVariable
        {
            get
            {
                if (radExistingEvent.Checked)
                {
                    object selectedItem = this.AvailableEventsComboBox.SelectedItem;

                    return ((ExposableEvent)selectedItem).Variable;
                }
                else
                {
                    return null;
                }
            }
        }

        public BeforeOrAfter BeforeOrAfter
        {
            get
            {
                object selectedItem = this.AvailableEventsComboBox.SelectedItem;

                return ((ExposableEvent)selectedItem).BeforeOrAfter;
            }
        }

        public string ResultDelegateType
        {
            get
            {
                if (radCreateNewEvent.Checked)
                {
                    string toReturn = this.AvailableTypesComboBox.Text;
                    if (toReturn.Contains("<T>"))
                    {
                        toReturn = toReturn.Replace("<T>", 
                            "<" + GenericTypeTextBox.Text +">");

                    }
                    return toReturn;
                }
                else if (radTunnelEvent.Checked)
                {

                }
                else if (radExistingEvent.Checked)
                {


                }

                return "";

            }
        }
        //public string ResultType
        //{
        //    get 
        //    {
        //        if (this.tabControl1.SelectedTab.Text == "Expose Existing")
        //        {
        //            if (EditorLogic.CurrentEntitySave != null)
        //            {
        //                string type = ExposedVariableManager.GetMemberTypeForEntity(ResultName);

        //                return TypeManager.ConvertToCommonType(type);

        //            }
        //            else
        //            {
        //                string type = ExposedVariableManager.GetMemberTypeForScreen(ResultName);

        //                return TypeManager.ConvertToCommonType(type);
        //            }
        //        }
        //        else if (this.tabControl1.SelectedTab.Text == "Tunneling")
        //        {
        //            NamedObjectSave nos = EditorLogic.CurrentElement.GetNamedObjectRecursively(TunnelingObjectComboBox.Text);
        //            string type = ExposedVariableManager.GetMemberTypeForNamedObject(nos, this.TunnelingVariableComboBox.Text);

        //            return TypeManager.ConvertToCommonType(type);
        //        }
        //        else
        //        {
        //            return this.comboBox1.Text;
        //        }
        //        //return this.comboBox1.Text; 
        //    }
        //}

        public string TunnelingObject
        {
            get
            {
                if (radTunnelEvent.Checked)
                {
                    return TunnelingObjectComboBox.Text;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                TunnelingObjectComboBox.Text = value;
            }
        }

        public string TunnelingEvent
        {
            get
            {
                if (radTunnelEvent.Checked)
                {
                    return TunnelingEventComboBox.Text;
                }
                else
                {
                    return null;
                }
            }
        }

        public string OverridingType
        {
            get
            {
                if (OverridingPropertyTypeComboBox.SelectedIndex >= 0)
                    return OverridingPropertyTypeComboBox.Text;
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

        public AddEventWindow()
		{
			InitializeComponent();

            TypeConverterHelper.InitializeClasses();

			StartPosition = FormStartPosition.Manual;

			Location = new Point(MainGlueWindow.MousePosition.X - this.Width/2, MainGlueWindow.MousePosition.Y - this.Height / 2);


            FillTunnelingObjects();

            FillAvailableDelegateTypes();

            FillTypeConverters();

            UpdateGenericUiVisibility();
		}

        private void SetFrom(AddEventViewModel viewModel)
        {
            this.DesiredEventType = viewModel.DesiredEventType;

            this.TunnelingObjectComboBox.SelectedItem = viewModel.TunnelingObject;

            foreach(var item in this.TunnelingEventComboBox.Items)
            {
                if(item.ToString() == viewModel.TunnelingEvent)
                {
                    this.TunnelingEventComboBox.SelectedItem = item;
                    break;
                }
            }

            foreach (ExposableEvent variableName in ViewModel.ExposableEvents)
            {
                AvailableEventsComboBox.Items.Add(variableName);
            }
            if(ViewModel.ExposableEvents.Count > 0)
            {
                if (AvailableEventsComboBox.Items.Count > 0)
                    AvailableEventsComboBox.SelectedIndex = 0;
            }


        }

        private void FillAvailableDelegateTypes()
        {
            List<string> availableDelegates = AvailableDelegateTypeConverter.GetAvailableDelegates();

            foreach (var value in availableDelegates)
            {
                this.AvailableTypesComboBox.Items.Add(value);
            }
        }

        private void FillTypeConverters()
        {
            List<string> converters = AvailableCustomVariableTypeConverters.GetAvailableConverters();

            foreach (string converter in converters)
                TypeConverterComboBox.Items.Add(converter);

            if (TypeConverterComboBox.Items.Count > 0)
                TypeConverterComboBox.SelectedIndex = 0;
        }

        
        //private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    mLastText = comboBox1.Text;
        //}

        private void AddVariableWindow_Load(object sender, EventArgs e)
        {
        //    comboBox1.Text = mLastText;

            
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
                radCreateNewEvent.Checked &&
                GlueState.Self.CurrentEntitySave != null &&
                ExposedVariableManager.IsMemberDefinedByPositionedObject(this.textBox1.Text)
                )
            {
                System.Windows.Forms.MessageBox.Show("The variable " + this.textBox1.Text + " is " +
                    "already defined by the engine.  You can expose this variable or select a different name.");
                e.Cancel = true;
            }
        }

        private void FillTunnelingObjects()
        {
            List<string> availableObjects = AvailableNamedObjectsAndFiles.GetAvailableObjects(false, false, GlueState.Self.CurrentElement, null);

            foreach (string availableObject in availableObjects)
                this.TunnelingObjectComboBox.Items.Add(availableObject);

            if (TunnelingObjectComboBox.Items.Count > 0)
                TunnelingObjectComboBox.SelectedIndex = 0;
        }


        private void FillTunnelableEventsFor(NamedObjectSave nos)
        {

            List<ExposableEvent> availableEvents = null;
            if (nos != null)
            {

                availableEvents = ExposedEventManager.GetExposableEventsFor(nos, GlueState.Self.CurrentElement);
            }

            if (availableEvents != null)
            {
                availableEvents.Sort();

                this.TunnelingEventComboBox.Items.Clear();
                if (availableEvents != null)
                {
                    foreach (ExposableEvent availableVariable in availableEvents)
                    {
                        this.TunnelingEventComboBox.Items.Add(availableVariable);
                    }

                }
            }
        }

        private void TunnelingVariableComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            AlternativeNameTextBox.Text = 
                TunnelingObjectComboBox.Text + 
                TunnelingEventComboBox.Text;
        }

        private void TunnelingObjectComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string nameOfNamedObject = TunnelingObjectComboBox.Text;

            NamedObjectSave nos = GlueState.Self.CurrentElement.GetNamedObjectRecursively(nameOfNamedObject);
            FillTunnelableEventsFor(nos);
        }


        private void radCreateNewEvent_CheckedChanged(object sender, EventArgs e)
        {
            pnlTunnelEvent.Visible = false;
            pnlNewEvent.Visible = false;
            pnlExistingEvent.Visible = false;

            if (radCreateNewEvent.Checked)
                pnlNewEvent.Visible = true;
            else if (radExistingEvent.Checked)
                pnlExistingEvent.Visible = true;
            else if (radTunnelEvent.Checked)
                pnlTunnelEvent.Visible = true;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            this.EnsureOnScreen();
        }

        private void AvailableTypesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateGenericUiVisibility();
        }

        private void UpdateGenericUiVisibility()
        {
            bool showGenericUi = AvailableTypesComboBox.Text.Contains("<T>");

            this.GenericTypeLabel.Visible = showGenericUi;
            this.GenericTypeTextBox.Visible = showGenericUi;
        }
        #endregion


    }
}
