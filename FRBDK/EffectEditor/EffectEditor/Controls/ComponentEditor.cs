using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using EffectEditor.EffectComponents;
using EffectEditor.EffectComponents.HLSLInformation;

namespace EffectEditor.Controls
{
    public partial class ComponentEditor : UserControl
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// The component being edited by this editor
        /// </summary>
        #endregion
        public EffectComponent Component;

        #region XML Docs
        /// <summary>
        /// Used to store shader profile names
        /// </summary>
        #endregion
        private string[] mShaderProfileNames;


        #region XML Docs
        /// <summary>
        /// Called when the component changes
        /// </summary>
        #endregion
        public event EventHandler ComponentChanged;

        private void OnComponentChanged()
        {
            if (ComponentChanged != null)
                ComponentChanged(this, new EventArgs());
        }

        private Stack<int> mNameSelectionParams = new Stack<int>();

        private bool mSuspendComponentSelectionChange = false;

        #endregion

        #region Properties

        #region XML Docs
        /// <summary>
        /// Whether or not usage of components in shader code is allowed
        /// </summary>
        #endregion
        [Browsable(true), Category("Shader"), Description("Whether or not usage of components in shader code is allowed")]
        public bool AllowComponentUsage
        {
            get { return componentSelectionBox.Visible; }
            set
            {
                componentSelectionBox.Visible = value;
                componentSelectionLabel.Visible = value;

                if (value)
                {
                    componentFunctionCode.Dock = DockStyle.None;
                    componentFunctionCode.Anchor = ((AnchorStyles)((((
                        System.Windows.Forms.AnchorStyles.Top
                        | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
                    componentFunctionCode.Location = new Point(0, 0);
                    componentFunctionCode.Size = new Size(
                        splitContainer2.Panel1.Width,
                        splitContainer2.Panel1.Height - 27);
                }
                else
                {
                    componentFunctionCode.Anchor = AnchorStyles.None;
                    componentFunctionCode.Dock = DockStyle.Fill;
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// The available components to be used in this editor
        /// </summary>
        #endregion
        [Browsable(false)]
        public List<EffectComponent> AvailableComponents
        {
            set
            {
                mSuspendComponentSelectionChange = true;
                componentSelectionBox.DataSource = null;
                componentSelectionBox.DataSource = value;
                componentSelectionBox.SelectedIndex = -1;
                mSuspendComponentSelectionChange = false;
            }
        }

        #region XML Docs
        /// <summary>
        /// Whether or not shader selection is allowed (or just profile selection)
        /// Also sets exclusivity for vertex/pixel shader availability
        /// </summary>
        #endregion
        [Browsable(true),Category("Shader"),Description("Whether or not shader availability selection is allowed")]
        public bool AllowShaderSelection
        {
            get { return componentAvailabilityPanel.Visible; }
            set
            {
                componentAvailabilityPanel.Visible = value;
                shaderProfilePanel.Visible = !(componentAvailabilityPanel.Visible);

                if (!componentAvailabilityPanel.Visible)
                {
                    // Make sure one shader profile is selected
                    // (Vertex shader will be selected if both are the same)
                    if (vertexShaderCheckbox.Checked == pixelShaderCheckbox.Checked)
                    {
                        vertexShaderCheckbox.Checked = true;
                        pixelShaderCheckbox.Checked = false;
                    }

                    UpdateShaderProfiles();
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// Whether or not this is vertex-shader available
        /// (will also disable pixel shader if AllowShaderSelection is false)
        /// </summary>
        #endregion
        [Browsable(true), Category("Shader"), Description("Whether or not component is vertex shader available")]
        public bool IsVertexShader
        {
            get { return vertexShaderCheckbox.Checked; }
            set
            {
                vertexShaderCheckbox.Checked = value;
                if (!AllowShaderSelection)
                {
                    pixelShaderCheckbox.Checked = false;
                    UpdateShaderProfiles();
                    if (SemanticEnabled) UpdateSemantics();
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// Whether or not this is pixel-shader available
        /// (will also disable vertex shader if AllowShaderSelection is false)
        /// </summary>
        #endregion
        [Browsable(true), Category("Shader"), Description("Whether or not component is pixel shader available")]
        public bool IsPixelShader
        {
            get { return pixelShaderCheckbox.Checked; }
            set
            {
                pixelShaderCheckbox.Checked = value;
                if (!AllowShaderSelection)
                {
                    vertexShaderCheckbox.Checked = false;
                    UpdateShaderProfiles();
                    if (SemanticEnabled) UpdateSemantics();
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// Whether or not parameter semantics should be enabled
        /// </summary>
        #endregion
        [Browsable(true), Category("Shader"), Description("Whether or not parameters should have semantics")]
        public bool SemanticEnabled
        {
            get { return componentParameterInputs.SemanticEnabled; }
            set
            {
                componentParameterInputs.SemanticEnabled = value;
                componentParameterOutputs.SemanticEnabled = value;

                UpdateSemantics();
            }
        }

        #endregion

        #region Constructor

        public ComponentEditor()
        {
            InitializeComponent();

            // Populate Vertex and Pixel Shader profiles
            this.SuspendLayout();
            mShaderProfileNames = Enum.GetNames(typeof(Microsoft.Xna.Framework.Graphics.ShaderProfile));
            vertexShaderProfileSelector.Items.Clear();
            pixelShaderProfileSelector.Items.Clear();
            for (int i = 0; i < mShaderProfileNames.Length; i++)
            {
                if (mShaderProfileNames[i].Contains("VS"))
                {
                    vertexShaderProfileSelector.Items.Add(mShaderProfileNames[i]);
                }
                if (mShaderProfileNames[i].Contains("PS"))
                {
                    pixelShaderProfileSelector.Items.Add(mShaderProfileNames[i]);
                }
            }
            this.ResumeLayout(true);

            // Create a new component
            EffectComponent newComponent = new EffectComponent();
            newComponent.Name = "NewComponent";

            SetComponent(newComponent);
        }

        #endregion

        #region Methods

        #region XML Docs
        /// <summary>
        /// Updates available semantics
        /// </summary>
        #endregion
        private void UpdateSemantics()
        {
            // Only one list may be used
            this.SuspendLayout();
            componentParameterInputs.Semantics = (vertexShaderCheckbox.Checked) ?
                Semantics.VertexShader.Input : Semantics.PixelShader.Input;
            componentParameterOutputs.Semantics = (vertexShaderCheckbox.Checked) ?
                Semantics.VertexShader.Output : Semantics.PixelShader.Output;
            this.ResumeLayout(true);
        }

        #region XML Docs
        /// <summary>
        /// Updates available shader profiles when shader type is exclusive
        /// </summary>
        #endregion
        private void UpdateShaderProfiles()
        {
            this.SuspendLayout();
            shaderProfileSelector.Items.Clear();
            for (int i = 0; i < mShaderProfileNames.Length; i++)
            {
                if ((vertexShaderCheckbox.Checked && mShaderProfileNames[i].Contains("VS")) ||
                    (pixelShaderCheckbox.Checked && mShaderProfileNames[i].Contains("PS")))
                {
                    shaderProfileSelector.Items.Add(mShaderProfileNames[i]);
                }
            }
            this.ResumeLayout(true);
        }

        #region XML Docs
        /// <summary>
        /// Sets a new component on this editor - and updates parameters
        /// </summary>
        /// <param name="component">The component to set</param>
        #endregion
        public void SetComponent(EffectComponent component)
        {
            if (Component != component)
            {
                Component = component;

                componentNameBox.Text = Component.Name;
                vertexShaderCheckbox.Checked = Component.AvailableToVertexShader;
                pixelShaderCheckbox.Checked = Component.AvailableToPixelShader;
                vertexShaderProfileSelector.Text = Component.MinimumVertexShaderProfile;
                pixelShaderProfileSelector.Text = Component.MinimumPixelShaderProfile;
                componentParameterInputs.Parameters = Component.Parameters;
                componentParameterOutputs.Parameters = Component.ReturnType;
                componentFunctionCode.Text = Component.Code;

                if (!AllowShaderSelection)
                {
                    UpdateShaderProfiles();
                }

                UpdateComponent(false);
            }
        }

        private void UpdateComponent() { UpdateComponent(true); } 
        private void UpdateComponent(bool updateCallback)
        {
            componentFunctionStart.Text =
                Component.OutputStruct() + Environment.NewLine +
                Component.FunctionStart;
            componentFunctionEnd.Text = Component.FunctionEnd;

            if (updateCallback) OnComponentChanged();
        }

        public void SaveNameEditPosition()
        {
            mNameSelectionParams.Push(componentNameBox.SelectionLength);
            mNameSelectionParams.Push(componentNameBox.SelectionStart);
        }

        public void RestoreNameEditPosition()
        {
            componentNameBox.SelectionStart = mNameSelectionParams.Pop();
            componentNameBox.SelectionLength = mNameSelectionParams.Pop();
        }

        #region Events

        private void componentNameBox_TextChanged(object sender, EventArgs e)
        {
            Component.Name = componentNameBox.Text;
            UpdateComponent();
        }

        private void vertexShaderCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Component.AvailableToVertexShader = vertexShaderCheckbox.Checked;
            vertexShaderProfileSelector.Enabled = vertexShaderCheckbox.Checked;
            UpdateComponent();
        }

        private void pixelShaderCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Component.AvailableToPixelShader = pixelShaderCheckbox.Checked;
            pixelShaderProfileSelector.Enabled = pixelShaderCheckbox.Checked;
            UpdateComponent();
        }

        private void componentParameterInputs_ParametersChanged(object sender, EventArgs e)
        {
            Component.Parameters = componentParameterInputs.Parameters;
            UpdateComponent();
        }

        private void componentParameterOutputs_ParametersChanged(object sender, EventArgs e)
        {
            Component.ReturnType = componentParameterOutputs.Parameters;
            UpdateComponent();
        }

        private void vertexShaderProfileSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            Component.MinimumVertexShaderProfile = vertexShaderProfileSelector.Text;
            UpdateComponent();
        }

        private void pixelShaderProfileSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            Component.MinimumPixelShaderProfile = pixelShaderProfileSelector.Text;
            UpdateComponent();
        }

        private void componentFunctionCode_TextChanged(object sender, EventArgs e)
        {
            Component.Code = componentFunctionCode.Text;
            UpdateComponent();
        }

        #endregion

        private void componentSelectionBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (componentSelectionBox.SelectedIndex >= 0 && !mSuspendComponentSelectionChange)
            {
                // insert component text
                EffectComponent component = (EffectComponent)componentSelectionBox.SelectedItem;
                int start = componentFunctionCode.SelectionStart;
                string text = componentFunctionCode.Text;
                string functionCode = component.FunctionHelperString();

                text = text.Insert(componentFunctionCode.SelectionStart, functionCode);
                componentFunctionCode.Text = text;
                componentFunctionCode.SelectionStart = start;
                componentFunctionCode.SelectionLength = functionCode.Length;

                // reset index to -1
                componentSelectionBox.SelectedIndex = -1;
            }
        }

        #endregion
    }
}
