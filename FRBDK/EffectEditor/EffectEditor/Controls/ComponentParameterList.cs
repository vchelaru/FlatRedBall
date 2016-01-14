using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using EffectEditor.EffectComponents.HLSLInformation;
using EffectEditor.EffectComponents;
using System.Collections;

namespace EffectEditor.Controls
{
    public partial class ComponentParameterList : UserControl
    {
        private List<EffectParameterDefinition> mParameters;

        [Browsable(false)]
        public List<EffectParameterDefinition> Parameters
        {
            get { return mParameters; }
            set
            {
                mParameters = value;
                UpdateList();
            }
        }

        private List<HlslSemantic> mSemantics;

        [Browsable(false)]
        public List<HlslSemantic> Semantics
        {
            get { return mSemantics; }
            set
            {
                mSemantics = value;
                SetSemantics();
            }
        }

        private bool mSemanticEnabled = true;

        [Browsable(true), DefaultValue(true)]
        public bool SemanticEnabled
        {
            get { return mSemanticEnabled; }
            set
            {
                mSemanticEnabled = value;
                if (!SemanticEnabled)
                {
                    semanticLabel.Visible = false;
                    semanticBox.Visible = false;
                    semanticNumberBox.Visible = false;

                    // resize
                    int height = semanticBox.Height;

                    paramEditPanel.Height -= height;
                    paramEditPanel.Top += height;

                    newParamButton.Top += height;
                    deleteParamButton.Top += height;
                    parameterListBox.Height += height;
                }
            }
        }

        private bool mStorageClassEnabled = true;

        [Browsable(true), DefaultValue(true)]
        public bool StorageClassEnabled
        {
            get { return mStorageClassEnabled; }
            set
            {
                mStorageClassEnabled = value;
                updateStorageClass();
            }
        }

        private void updateStorageClass()
        {
            if (!StorageClassEnabled)
            {
                storageClassLabel.Visible = false;
                storageClassBox.Visible = false;

                // resize
                int height = semanticBox.Height;

                paramEditPanel.Height -= height;
                paramEditPanel.Top += height;

                newParamButton.Top += height;
                deleteParamButton.Top += height;
                parameterListBox.Height += height;

                semanticLabel.Top -= height;
                semanticBox.Top -= height;
                semanticNumberBox.Top -= height;
            }
            else
            {
                storageClassBox.Items.Clear();

                string[] storageClassNames = Enum.GetNames(typeof(StorageClass));

                storageClassBox.Items.AddRange(storageClassNames);

                storageClassBox.SelectedText = "None";
            }
        }

        bool LoadingNewParam = false; // used to stop params from raising change events while switching

        public event EventHandler ParametersChanged;

        public ComponentParameterList()
        {
            mParameters = new List<EffectParameterDefinition>();

            InitializeComponent();

            parameterListBox.DataSource = mParameters;

            // Initialize type values
            string[] typeNames = Enum.GetNames(typeof(HlslType));
            for (int i = 0; i < typeNames.Length; i++)
            {
                paramTypeBox.Items.Add(typeNames[i]);
            }
            paramTypeBox.SelectedText = "Float";

            updateStorageClass();
        }

        private void SetSemantics()
        {
            semanticBox.Items.Clear();
            if (mSemantics != null)
            {
                foreach (HlslSemantic semantic in mSemantics)
                {
                    semanticBox.Items.Add(semantic);
                }
            }
        }

        private void DoParametersChanged()
        {
            if (ParametersChanged != null)
            {
                ParametersChanged(this, new EventArgs());
            }
        }

        private void paramEdited(object sender, EventArgs e)
        {
            if (!LoadingNewParam &&
                paramNameBox.Text != "" && paramTypeBox.SelectedItem != null)
            {
                EditParameter(parameterListBox.SelectedIndex);
                DoParametersChanged();
            }
        }

        #region XML Docs
        /// <summary>
        /// Edits the parameter in the list with the selected index
        /// </summary>
        #endregion
        private void EditParameter(int index)
        {
            // updates the parameter in the list
            EffectParameterDefinition param = (EffectParameterDefinition)mParameters[index];
            param.Name = paramNameBox.Text;
            param.Type.Type = (HlslType)Enum.Parse(typeof(HlslType), (string)paramTypeBox.SelectedItem);

            if (param.Type.Type == HlslType.Texture || (string)paramTypeSizeBox.SelectedItem == "Scalar")
            {
                param.Type.IsVector = false;
                param.Type.IsArray = false;
                param.Type.IsMatrix = false;
            }
            else if ((string)paramTypeSizeBox.SelectedItem == "Vector")
            {
                param.Type.IsVector = true;
                param.Type.IsArray = false;
                param.Type.IsMatrix = false;
                param.Type.Size = (int)paramTypeSizeA.Value;
            }
            else if ((string)paramTypeSizeBox.SelectedItem == "Array")
            {
                param.Type.IsVector = false;
                param.Type.IsArray = true;
                param.Type.IsMatrix = false;
                param.Type.Size = (int)paramTypeSizeA.Value;
            }
            else if ((string)paramTypeSizeBox.SelectedItem == "Matrix")
            {
                param.Type.IsVector = false;
                param.Type.IsArray = false;
                param.Type.IsMatrix = true;
                param.Type.Size = (int)paramTypeSizeA.Value;
                param.Type.MatrixColumns = (int)paramTypeSizeB.Value;
            }

            // semantic
            param.HasSemantic = mSemanticEnabled;
            if (mSemanticEnabled)
            {
                param.Semantic = mSemantics[semanticBox.SelectedIndex];
                param.Semantic.ResourceNumber = (int)semanticNumberBox.Value;
            }

            // storage class
            param.HasStorageClass = mStorageClassEnabled;
            if (mStorageClassEnabled)
            {
                if (storageClassBox.SelectedItem == null || storageClassBox.SelectedText == String.Empty) param.StorageClass = StorageClass.None;
                else param.StorageClass = (StorageClass)Enum.Parse(typeof(StorageClass), storageClassBox.SelectedText, true);
            }

            mParameters[index] = param;
            
            UpdateList();

            parameterListBox.SelectedItem = param;
        }

        private void paramTypeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            HlslType type = (HlslType)Enum.Parse(typeof(HlslType), (string)paramTypeBox.SelectedItem);

            if (type != HlslType.Texture)
            {
                paramTypeSizeBox.Visible = true;
            }
            else
            {
                paramTypeSizeBox.Visible = false;
                paramTypeSizeA.Visible = false;
                paramTypeSizeB.Visible = false;
                paramSizeDividerLabel.Visible = false;
            }

            paramEdited(sender, e);
        }

        private void setSizeBoxVisibilities()
        {
            if ((string)paramTypeSizeBox.SelectedItem == "Scalar")
            {
                paramTypeSizeA.Visible = false;
                paramTypeSizeB.Visible = false;
                paramSizeDividerLabel.Visible = false;
            }
            else if ((string)paramTypeSizeBox.SelectedItem == "Vector")
            {
                paramTypeSizeA.Visible = true;
                paramTypeSizeA.Minimum = 1;
                paramTypeSizeA.Maximum = 4;
                paramTypeSizeB.Visible = false;
                paramSizeDividerLabel.Visible = false;
            }
            else if ((string)paramTypeSizeBox.SelectedItem == "Array")
            {
                paramTypeSizeA.Visible = true;
                paramTypeSizeA.Minimum = 1;
                paramTypeSizeA.Maximum = 2048;
                paramTypeSizeB.Visible = false;
                paramSizeDividerLabel.Visible = false;
            }
            else if ((string)paramTypeSizeBox.SelectedItem == "Matrix")
            {
                paramTypeSizeA.Visible = true;
                paramTypeSizeA.Minimum = 1;
                paramTypeSizeA.Maximum = 4;
                paramTypeSizeB.Visible = true;
                paramTypeSizeB.Minimum = 1;
                paramTypeSizeB.Maximum = 4;
                paramSizeDividerLabel.Visible = true;
            }
        }

        private void paramTypeSizeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            setSizeBoxVisibilities();
            paramEdited(sender, e);
        }

        private void parameterListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (parameterListBox.SelectedIndex >= 0) SelectParameter(parameterListBox.SelectedIndex);
        }

        private void deleteParamButton_Click(object sender, EventArgs e)
        {
            RemoveParameter(parameterListBox.SelectedIndex);
        }

        private void UpdateList()
        {
            int selectIndex = parameterListBox.SelectedIndex;
            int nameSelectStart = paramNameBox.SelectionStart;
            int nameSelectLength = paramNameBox.SelectionLength;
            if (selectIndex >= 0 && parameterListBox.Items.Count > 0)
            {
                EffectParameterDefinition param = mParameters[parameterListBox.SelectedIndex];
                mParameters.Sort();
                selectIndex = mParameters.IndexOf(param);
            }

            parameterListBox.DataSource = null;
            parameterListBox.DataSource = mParameters;

            if (selectIndex >= 0 && parameterListBox.Items.Count > 0)
            {
                parameterListBox.SelectedIndex = selectIndex;
                paramNameBox.SelectionStart = nameSelectStart;
                paramNameBox.SelectionLength = nameSelectLength;
            }
        }

        private void newParamButton_Click(object sender, EventArgs e)
        {
            AddParameter(new EffectParameterDefinition(
                "NewParameter", new HlslTypeDefinition(HlslType.Float)));
        }

        public void RemoveParameter(int index)
        {
            mParameters.RemoveAt(index);

            parameterListBox.DataSource = null;
            parameterListBox.DataSource = mParameters;

            UpdateList();

            if (index >= mParameters.Count)
            {
                index--;
            }
            if (index >= 0) parameterListBox.SelectedIndex = index;
            else
            {
                deleteParamButton.Enabled = false;
                paramEditPanel.Enabled = false;
            }

            DoParametersChanged();
        }

        public void AddParameter(EffectParameterDefinition param)
        {
            mParameters.Add(param);
            mParameters.Sort();

            UpdateList();

            parameterListBox.SelectedItem = param;

            DoParametersChanged();
        }

        public void SelectParameter(int index)
        {
            LoadingNewParam = true;

            deleteParamButton.Enabled = true;
            paramEditPanel.Enabled = true;

            EditParameter((EffectParameterDefinition)mParameters[index]);

            LoadingNewParam = false;
        }

        public void EditParameter(EffectParameterDefinition param)
        {
            // populate the editing panel
            paramNameBox.Text = param.Name;
            paramTypeBox.SelectedItem = param.Type.Type.ToString();

            if (param.Name != HlslType.Texture.ToString())
            {
                if (param.Type.IsVector)
                {
                    paramTypeSizeBox.SelectedItem = "Vector";
                    paramTypeSizeA.Value = param.Type.Size;
                }
                else if (param.Type.IsArray)
                {
                    paramTypeSizeBox.SelectedItem = "Array";
                    paramTypeSizeA.Value = param.Type.Size;
                }
                else if (param.Type.IsMatrix)
                {
                    paramTypeSizeBox.SelectedItem = "Matrix";
                    paramTypeSizeA.Value = param.Type.Size;
                    paramTypeSizeB.Value = param.Type.MatrixColumns;
                }
                else
                {
                    paramTypeSizeBox.SelectedItem = "Scalar";
                }
            }

            bool semanticChanged = false;

            if (mSemanticEnabled)
            {
                if (param.Semantic.Name == null || param.Semantic.Name == String.Empty)
                {
                    param.Semantic = mSemantics[0];
                    param.Semantic.ResourceNumber = 0;
                    semanticChanged = true;
                }

                // Find the index to select
                int index = -1;
                if (semanticBox.Items.Contains(param.Semantic))
                {
                    index = semanticBox.Items.IndexOf(param.Semantic);
                }
                else
                {
                    for (int i = 0; i < semanticBox.Items.Count; i++)
                    {
                        if (param.Semantic.Name.Equals(((HlslSemantic)semanticBox.Items[i]).Name))
                        {
                            index = i;
                        }
                    }
                }
                if (index == -1) index = 0;

                semanticBox.SelectedIndex = index;
            }

            if (mStorageClassEnabled)
            {
                storageClassBox.Text = (param.StorageClass == StorageClass.None) ?
                    String.Empty : param.StorageClass.ToString();
            }

            if (semanticChanged) EditParameter(parameterListBox.SelectedIndex);
        }

        private void storageClassBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            paramEdited(sender, e);
        }

        private void semanticBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SemanticEnabled)
            {
                semanticNumberBox.Visible = mSemantics[semanticBox.SelectedIndex].MultipleResourcesSupported;

                paramEdited(sender, e);
            }
        }

        private void semanticNumberBox_ValueChanged(object sender, EventArgs e)
        {
            paramEdited(sender, e);
        }
    }
}
