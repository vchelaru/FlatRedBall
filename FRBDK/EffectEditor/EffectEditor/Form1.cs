using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace EffectEditor
{
    using XnaColor = Microsoft.Xna.Framework.Graphics.Color;
    using System.Reflection;
    using FlatRedBall.Graphics.Model;
    using System.Threading;
    using EffectEditor.HelperClasses;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework;
    using System.IO;
    using EffectEditor.EffectComponents;
    using EffectEditor.EffectComponents.HLSLInformation;
    using System.Collections;

    public partial class EditorForm : Form
    {
        #region Fields

        Dictionary<String, PositionedModel> mModelDictionary = new Dictionary<string,PositionedModel>();
        ModelEditor mModelEditor;

        EffectComponent mEffectComponent;

        // The current effect
        EffectDefinition mEffectDefinition;

        List<EffectComponent> mLoadedComponents;

        private bool mSuspendTechniqueChange = false;
        private bool mSuspendPassChange = false;

        #endregion

        public EditorForm()
        {
            InitializeComponent();

            mEffectDefinition = new EffectDefinition("NewEffect");

            // Allow model drag-drop
            modelViewPanel.AllowDrop = true;
            modelViewPanel.DragEnter += new DragEventHandler(modelViewPanel_DragEnter);
            modelViewPanel.DragDrop += new DragEventHandler(modelViewPanel_DragDrop);

            // Allow effect drag-drop
            effectEditBox.AllowDrop = true;
            effectEditBox.DragEnter += new DragEventHandler(effectEditBox_DragEnter);
            effectEditBox.DragDrop += new DragEventHandler(effectEditBox_DragDrop);
            
            modelViewPanel.ModelLoaded = ModelLoaded;

            hlslInfoBox.ReadOnly = true;
            hlslInfoBox.Clear();

            effectParametersList.Parameters = mEffectDefinition.Parameters;
            UpdateStandardParameters();

            effectComponentsList.DataSource = mEffectDefinition.Components;

            effectTechniquesList.DataSource = mEffectDefinition.Techniques;

            // populate shader profiles boxes in techniques tab
            string[] shaderProfiles = Enum.GetNames(typeof(ShaderProfile));
            for (int i = 0; i < shaderProfiles.Length; i++)
            {
                if (shaderProfiles[i].Contains("VS")) passVertexShaderProfileBox.Items.Add(shaderProfiles[i]);
                if (shaderProfiles[i].Contains("PS")) passPixelShaderProfileBox.Items.Add(shaderProfiles[i]);
            }

            mLoadedComponents = new List<EffectComponent>();

            vertexShaderEditor.AllowComponentUsage = true;
            pixelShaderEditor.AllowComponentUsage = true;
        }

        #region Model View Panel Input

        private void frbPanel1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (modelViewPanel.Focused && e.KeyCode != Keys.Tab) e.IsInputKey = true;
        }

        private void modelViewPanel_MouseDown(object sender, MouseEventArgs e)
        {
            modelViewPanel.Focus();
        }

        private void bgColorComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            String colorName = bgColorComboBox.Text;

            if (colorName != "Custom...")
            {
                PropertyInfo colorProperty = typeof(XnaColor).GetProperty(colorName);
                if (colorProperty != null)
                {
                    XnaColor color = (XnaColor)colorProperty.GetValue(typeof(XnaColor),
                        BindingFlags.Static | BindingFlags.Public, null, null, null);

                    modelViewPanel.BackgroundColor = color;

                    redCustomColorBar.Value = color.R;
                    greenCustomColorBar.Value = color.G;
                    blueCustomColorBar.Value = color.B;
                }

                RLabel.Enabled = false;
                GLabel.Enabled = false;
                BLabel.Enabled = false;
                redCustomColorBar.Enabled = false;
                greenCustomColorBar.Enabled = false;
                blueCustomColorBar.Enabled = false;
            }
            else
            {
                RLabel.Enabled = true;
                GLabel.Enabled = true;
                BLabel.Enabled = true;
                redCustomColorBar.Enabled = true;
                greenCustomColorBar.Enabled = true;
                blueCustomColorBar.Enabled = true;
            }
        }

        private void CustomColorBar_Scroll(object sender, EventArgs e)
        {
            if (redCustomColorBar.Enabled)
            {
                modelViewPanel.BackgroundColor = new XnaColor(
                    (byte)redCustomColorBar.Value,
                    (byte)greenCustomColorBar.Value,
                    (byte)blueCustomColorBar.Value);
            }
        }

        private void modelSelectionBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (PositionedModel model in mModelDictionary.Values)
            {
                model.Visible = false;
            }

            String selectedName = (string)modelSelectionBox.Items[modelSelectionBox.SelectedIndex];

            // Check if the value is "Load Model..."
            if (selectedName == "Load Model...")
            {
                if (openModelFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (String file in openModelFileDialog.FileNames)
                    {
                        modelViewPanel.AddModel(file);
                    }
                }
            }
            else
            {
                // Set visible, and set effect properties
                PositionedModel selectedModel = mModelDictionary[selectedName];

                selectedModel.Visible = true;
                modelViewPanel.CurrentModel = selectedModel;

                SelectModel(selectedModel);

            }
        }

        void SelectModel(PositionedModel model)
        {
            mModelEditor = new ModelEditor(model);
            modelPropertyGrid.SelectedObject = mModelEditor;
        }

        delegate void ModelLoadedCallback(String name, PositionedModel model);
        private void ModelLoaded(String name, PositionedModel model)
        {
            if (this.InvokeRequired)
            {
                ModelLoadedCallback d = new ModelLoadedCallback(ModelLoaded);
                this.Invoke(d, new object[] { name, model });
            }
            else
            {
                // Add the new model
                if (mModelDictionary.ContainsKey(name))
                {
                    mModelDictionary[name] = model;
                }
                else
                {
                    mModelDictionary.Add(name, model);
                }

                // Repopulate the model list, and select the newest addition
                modelSelectionBox.Items.Clear();
                foreach (String key in mModelDictionary.Keys)
                {
                    modelSelectionBox.Items.Add(key);
                }

                // Add "Load Model..." item
                modelSelectionBox.Items.Add("Load Model...");

                modelSelectionBox.SelectedIndex = modelSelectionBox.Items.IndexOf(name);
            }
        }

        #endregion

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
               
        private void compileEffectButton_Click(object sender, EventArgs e)
        {
            // Get the effect source
            string effectSource = string.Empty;

            for (int i = 0; i < effectEditBox.Lines.Length; i++)
            {
                effectSource += effectEditBox.Lines[i] + Environment.NewLine;
            }

            // Start output
            effectCompileOutput.Clear();
            effectCompileOutput.AppendText("Compiling Effect..." + Environment.NewLine);

            // Compile the effect
            CompiledEffect effectCompiled = Effect.CompileEffectFromSource(
                effectSource,
                null, //new CompilerMacro[] { },
                null,
                CompilerOptions.None,
                TargetPlatform.Windows);

            // Check for errors
            if (!effectCompiled.Success)
            {
                effectCompileOutput.AppendText("There were  errors:" + Environment.NewLine);
                effectCompileOutput.AppendText("    " + effectCompiled.ErrorsAndWarnings + Environment.NewLine);
            }
            else
            {
                effectCompileOutput.AppendText("Success!");

                effectPropertyGrid.SelectedObject = null;

                // Apply the effect
                Effect effect = modelViewPanel.ApplyEffect(effectCompiled.GetEffectCode());

                // Set the property grid
                EffectPropertyEditor editor = new EffectPropertyEditor(effect);
                effectPropertyGrid.SelectedObject = editor;

                // Switch tabs
                mainTabControl.SelectedTab = mainTabControl.TabPages[0];
            }
        }

        #region Drag and Drop Effects

        void effectEditBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        void effectEditBox_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files != null && files.Length > 0)
            {
                effectEditBox.Clear();
                effectEditBox.LoadFile(files[0], RichTextBoxStreamType.PlainText);
            }
        }

        #endregion

        #region Drag and Drop Models

        void modelViewPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Check if the files list contains an X or XNB file
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                string extension = Path.GetExtension(files[0]).ToLower();
                bool canLoad = (extension == "x" || extension == "xnb");

                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        void modelViewPanel_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files != null && files.Length > 0 && Path.GetExtension(files[0]).ToLower() == "x" ||
                Path.GetExtension(files[0]).ToLower() == "xnb")
            {
                modelViewPanel.AddModel(files[0]);
            }
        }

        #endregion

        #region Components

        private void mainTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            componentToolStripMenuItem.Visible = (mainTabControl.SelectedTab.Text == "Component Editor");
        }

        private void newComponentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EffectComponent component = new EffectComponent();
            component.Name = "NewComponent";
            componentEditor.SetComponent(component);
        }

        private void saveComponentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveComponentDialog.ShowDialog() == DialogResult.OK)
            {
                componentEditor.Component.Save(saveComponentDialog.FileName);
            }
        }

        private void loadComponentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openComponentDialog.ShowDialog() == DialogResult.OK)
            {
                EffectComponent component = EffectComponent.FromFile(openComponentDialog.FileName);
                componentEditor.SetComponent(component);
            }
        }

        #endregion

        #region Effect Editing

        #region Components

        private void UpdateAvailableComponents()
        {
            mLoadedComponents.Clear();

            foreach (string componentFileName in mEffectDefinition.Components)
            {
                mLoadedComponents.Add(EffectComponent.FromFile(componentFileName));
            }

            vertexShaderEditor.AvailableComponents = mLoadedComponents;
            pixelShaderEditor.AvailableComponents = mLoadedComponents;
        }

        private void addEffectComponentButton_Click(object sender, EventArgs e)
        {
            if (openComponentDialog.ShowDialog() == DialogResult.OK)
            {
                mEffectDefinition.Components.Add(openComponentDialog.FileName);
                int selectedIndex = effectComponentsList.SelectedIndex;
                effectComponentsList.DataSource = null;
                effectComponentsList.DataSource = mEffectDefinition.Components;
                effectComponentsList.SelectedIndex = selectedIndex;

                UpdateAvailableComponents();
            }
        }

        private void removeEffectComponentButton_Click(object sender, EventArgs e)
        {
            if (effectComponentsList.SelectedIndex >= 0)
            {
                mEffectDefinition.Components.RemoveAt(effectComponentsList.SelectedIndex);
                int selectedIndex = effectComponentsList.SelectedIndex;
                if (selectedIndex >= mEffectDefinition.Components.Count)
                    selectedIndex = mEffectDefinition.Components.Count - 1;
                effectComponentsList.DataSource = null;
                effectComponentsList.DataSource = mEffectDefinition.Components;
                effectComponentsList.SelectedIndex = selectedIndex;

                UpdateAvailableComponents();
            }
        }

        private void effectComponentsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            removeEffectComponentButton.Enabled = (effectComponentsList.SelectedIndex >= 0);
        }

        #endregion

        #region Parameters

        private void UpdateStandardParameters()
        {
            int selectedIndex = standardParametersList.SelectedIndex;

            standardParametersList.Items.Clear();
            foreach (EffectParameterDefinition param in FRBConstants.StandardParameters)
            {
                if (!effectParametersList.Parameters.Contains(param))
                    standardParametersList.Items.Add(param);
            }

            if (selectedIndex >= standardParametersList.Items.Count)
                selectedIndex = standardParametersList.Items.Count - 1;
            standardParametersList.SelectedIndex = selectedIndex;
        }

        private void standardParametersList_SelectedIndexChanged(object sender, EventArgs e)
        {
            addStandardParameterButton.Enabled = (standardParametersList.SelectedIndex >= 0);
        }

        private void addStandardParameterButton_Click(object sender, EventArgs e)
        {
            EffectParameterDefinition param =
                (EffectParameterDefinition)standardParametersList.SelectedItem;

            mEffectDefinition.Parameters.Add(param);

            effectParametersList.Parameters = mEffectDefinition.Parameters;
            UpdateStandardParameters();
        }

        #endregion

        #region Vertex Shaders

        private void effectEdit_vertexShaderListChanged(object sender, EventArgs e)
        {
            EffectComponent currentSelection;
            int index = vertexShadersList.SelectedIndex;

            vertexShaderEditor.SaveNameEditPosition();

            if (vertexShadersList.SelectedIndex >= 0)
            {
                currentSelection = mEffectDefinition.VertexShaders[vertexShadersList.SelectedIndex];
                mEffectDefinition.VertexShaders.Sort();
                index = mEffectDefinition.VertexShaders.IndexOf(currentSelection);
            }
            
            vertexShadersList.DataSource = null;
            vertexShadersList.DataSource = mEffectDefinition.VertexShaders;

            mSuspendPassChange = true;
            passVertexShaderBox.DataSource = null;
            passVertexShaderBox.DataSource = mEffectDefinition.VertexShaders;
            mSuspendPassChange = false;

            vertexShadersList.SelectedIndex = index;

            vertexShaderEditor.RestoreNameEditPosition();

            vertexShaderEditor.Enabled = mEffectDefinition.VertexShaders.Count > 0;
        }
        
        private void vertexShadersList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (vertexShadersList.SelectedIndex != -1 &&
                vertexShadersList.Items.Count > 0)
            {
                EffectComponent component = (EffectComponent)vertexShadersList.SelectedItem;
                vertexShaderEditor.SetComponent(component);
            }
        }

        private void addVertexShaderButton_Click(object sender, EventArgs e)
        {
            EffectComponent component = new EffectComponent();
            component.Name = "NewVertexShader";
            component.IsInline = false;

            HlslSemantic inputSemantic = Semantics.Find("POSITION", true, true);
            inputSemantic.ResourceNumber = 0;
            component.Parameters.Add(new EffectParameterDefinition(
                "Position",
                inputSemantic.Type,
                inputSemantic));
            HlslSemantic outputSemantic = Semantics.Find("POSITION", true, false);
            outputSemantic.ResourceNumber = 0;
            component.ReturnType.Add(new EffectParameterDefinition(
                "Position",
                outputSemantic.Type,
                outputSemantic));

            mEffectDefinition.VertexShaders.Add(component);

            effectEdit_vertexShaderListChanged(sender, e);

            vertexShadersList.SelectedItem = component;

            if (mEffectDefinition.VertexShaders.Count > 0) vertexShaderEditor.Enabled = true;
        }

        private void vertexShaderEditor_ComponentChanged(object sender, EventArgs e)
        {
            effectEdit_vertexShaderListChanged(sender, e);

            vertexShadersList.SelectedItem = vertexShaderEditor.Component;
        }

        private void removeVertexShaderButton_Click(object sender, EventArgs e)
        {
            int selectedIndex = vertexShadersList.SelectedIndex;
            mEffectDefinition.VertexShaders.RemoveAt(selectedIndex);

            vertexShadersList.DataSource = null;
            vertexShadersList.DataSource = mEffectDefinition.VertexShaders;

            if (selectedIndex >= mEffectDefinition.VertexShaders.Count &&
                mEffectDefinition.VertexShaders.Count > 0)
                vertexShadersList.SelectedIndex = mEffectDefinition.VertexShaders.Count - 1;

            if (mEffectDefinition.VertexShaders.Count <= 0) vertexShaderEditor.Enabled = false;
        }

        #endregion

        #region Pixel Shaders

        private void effectEdit_pixelShaderListChanged(object sender, EventArgs e)
        {
            EffectComponent currentSelection;
            int index = pixelShadersList.SelectedIndex;

            pixelShaderEditor.SaveNameEditPosition();

            if (pixelShadersList.SelectedIndex >= 0)
            {
                currentSelection = mEffectDefinition.PixelShaders[pixelShadersList.SelectedIndex];
                mEffectDefinition.PixelShaders.Sort();
                index = mEffectDefinition.PixelShaders.IndexOf(currentSelection);
            }

            pixelShadersList.DataSource = null;
            pixelShadersList.DataSource = mEffectDefinition.PixelShaders;

            mSuspendPassChange = true;
            passPixelShaderBox.DataSource = null;
            passPixelShaderBox.DataSource = mEffectDefinition.PixelShaders;
            mSuspendPassChange = false;

            pixelShadersList.SelectedIndex = index;

            pixelShaderEditor.RestoreNameEditPosition();

            pixelShaderEditor.Enabled = mEffectDefinition.PixelShaders.Count > 0;
        }

        private void pixelShadersList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (pixelShadersList.SelectedIndex != -1 &&
                pixelShadersList.Items.Count > 0)
            {
                EffectComponent component = (EffectComponent)pixelShadersList.SelectedItem;
                pixelShaderEditor.SetComponent(component);
            }
        }

        private void addPixelShaderButton_Click(object sender, EventArgs e)
        {
            EffectComponent component = new EffectComponent();
            component.Name = "NewPixelShader";
            component.IsInline = false;

            HlslSemantic outputSemantic = Semantics.Find("COLOR", true, false);
            outputSemantic.ResourceNumber = 0;
            component.ReturnType.Add(new EffectParameterDefinition(
                "Color",
                outputSemantic.Type,
                outputSemantic));

            mEffectDefinition.PixelShaders.Add(component);

            effectEdit_pixelShaderListChanged(sender, e);

            pixelShadersList.SelectedItem = component;

            if (mEffectDefinition.PixelShaders.Count > 0) pixelShaderEditor.Enabled = true;
        }

        private void pixelShaderEditor_ComponentChanged(object sender, EventArgs e)
        {
            effectEdit_pixelShaderListChanged(sender, e);

            pixelShadersList.SelectedItem = pixelShaderEditor.Component;
        }

        private void removePixelShaderButton_Click(object sender, EventArgs e)
        {
            int selectedIndex = pixelShadersList.SelectedIndex;
            mEffectDefinition.PixelShaders.RemoveAt(selectedIndex);

            pixelShadersList.DataSource = null;
            pixelShadersList.DataSource = mEffectDefinition.PixelShaders;

            if (selectedIndex >= mEffectDefinition.PixelShaders.Count &&
                mEffectDefinition.PixelShaders.Count > 0)
                pixelShadersList.SelectedIndex = mEffectDefinition.PixelShaders.Count - 1;

            if (mEffectDefinition.PixelShaders.Count <= 0) pixelShaderEditor.Enabled = false;
        }

        #endregion

        #region Techniques

        private void addTechniqueButton_Click(object sender, EventArgs e)
        {
            EffectTechniqueDefinition technique = new EffectTechniqueDefinition("NewTechnique");
            technique.Passes.Add(new EffectPassDefinition(
                "Pass0",
                (mEffectDefinition.VertexShaders.Count > 0) ? mEffectDefinition.VertexShaders[0].Name : String.Empty,
                (mEffectDefinition.PixelShaders.Count > 0) ? mEffectDefinition.PixelShaders[0].Name : String.Empty,
                (mEffectDefinition.VertexShaders.Count > 0) ?
                    (ShaderProfile)Enum.Parse(typeof(ShaderProfile), mEffectDefinition.VertexShaders[0].MinimumVertexShaderProfile, true) : ShaderProfile.VS_1_1,
                (mEffectDefinition.PixelShaders.Count > 0) ?
                    (ShaderProfile)Enum.Parse(typeof(ShaderProfile), mEffectDefinition.PixelShaders[0].MinimumPixelShaderProfile, true) : ShaderProfile.PS_1_1));

            mEffectDefinition.Techniques.Add(technique);
            
            effectTechniquesList.DataSource = null;
            effectTechniquesList.DataSource = mEffectDefinition.Techniques;

            effectTechniquesList.SelectedIndex = mEffectDefinition.Techniques.IndexOf(technique);
        }

        private void removeTechniqueButton_Click(object sender, EventArgs e)
        {
            if (effectTechniquesList.SelectedIndex >= 0)
            {
                mEffectDefinition.Techniques.RemoveAt(effectTechniquesList.SelectedIndex);
                
                int selectedIndex = effectTechniquesList.SelectedIndex;
                if (selectedIndex >= mEffectDefinition.Techniques.Count)
                    selectedIndex = mEffectDefinition.Techniques.Count - 1;
                effectTechniquesList.DataSource = null;
                effectTechniquesList.DataSource = mEffectDefinition.Techniques;

                effectTechniquesList.SelectedIndex = selectedIndex;
            }
        }

        private void effectTechniquesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!mSuspendTechniqueChange)
            {
                effectTechniquePassesEditor.Enabled = (effectTechniquesList.SelectedIndex >= 0);

                if (effectTechniquesList.SelectedIndex >= 0)
                {
                    EffectTechniqueDefinition technique = mEffectDefinition.Techniques[
                        effectTechniquesList.SelectedIndex];

                    effectTechniquePassesList.DataSource = null;
                    effectTechniquePassesList.DataSource = technique.Passes;
                    effectTechniquePassesList.ClearSelected();

                    techniqueNameBox.Text = technique.Name;

                    if (technique.Passes.Count > 0)
                        effectTechniquePassesList.SelectedIndex = 0;
                }
            }
        }

        private void techniqueNameBox_TextChanged(object sender, EventArgs e)
        {
            mSuspendTechniqueChange = true;

            EffectTechniqueDefinition technique = mEffectDefinition.Techniques[effectTechniquesList.SelectedIndex];
            technique.Name = techniqueNameBox.Text;
            mEffectDefinition.Techniques[effectTechniquesList.SelectedIndex] = technique;

            effectTechniquesList.SuspendLayout();
            int selectedIndex = effectTechniquesList.SelectedIndex;
            effectTechniquesList.DataSource = null;
            effectTechniquesList.DataSource = mEffectDefinition.Techniques;
            effectTechniquesList.SelectedIndex = selectedIndex;
            effectTechniquesList.ResumeLayout();

            mSuspendTechniqueChange = false;
        }

        private void addPassButton_Click(object sender, EventArgs e)
        {
            EffectTechniqueDefinition technique = mEffectDefinition.Techniques[effectTechniquesList.SelectedIndex];

            technique.Passes.Add(new EffectPassDefinition(
                "Pass" + technique.Passes.Count.ToString(),
                (mEffectDefinition.VertexShaders.Count > 0) ? mEffectDefinition.VertexShaders[0].Name : String.Empty,
                (mEffectDefinition.PixelShaders.Count > 0) ? mEffectDefinition.PixelShaders[0].Name : String.Empty,
                (mEffectDefinition.VertexShaders.Count > 0) ?
                    (ShaderProfile)Enum.Parse(typeof(ShaderProfile), mEffectDefinition.VertexShaders[0].MinimumVertexShaderProfile, true) : ShaderProfile.VS_1_1,
                (mEffectDefinition.PixelShaders.Count > 0) ?
                    (ShaderProfile)Enum.Parse(typeof(ShaderProfile), mEffectDefinition.PixelShaders[0].MinimumPixelShaderProfile, true) : ShaderProfile.PS_1_1));

            mEffectDefinition.Techniques[effectTechniquesList.SelectedIndex] = technique;

            effectTechniquePassesList.DataSource = null;
            effectTechniquePassesList.DataSource = technique.Passes;

            effectTechniquePassesList.SelectedIndex = technique.Passes.Count - 1;
        }

        private void removePassButton_Click(object sender, EventArgs e)
        {
            EffectTechniqueDefinition technique = mEffectDefinition.Techniques[effectTechniquesList.SelectedIndex];
            technique.Passes.RemoveAt(effectTechniquePassesList.SelectedIndex);
            mEffectDefinition.Techniques[effectTechniquesList.SelectedIndex] = technique;

            int selectedIndex = effectTechniquePassesList.SelectedIndex;
            if (selectedIndex >= technique.Passes.Count) selectedIndex = technique.Passes.Count - 1;
            effectTechniquePassesList.DataSource = null;
            effectTechniquePassesList.DataSource = technique.Passes;
            effectTechniquePassesList.SelectedIndex = selectedIndex;
        }

        private void effectTechniquePassesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!mSuspendPassChange)
            {
                removePassButton.Enabled = (effectTechniquePassesList.SelectedIndex >= 0);
                effectTechniquePassesEditor.Panel2.Enabled = (effectTechniquePassesList.SelectedIndex >= 0);

                if (effectTechniquePassesList.SelectedIndex >= 0)
                {
                    EffectPassDefinition pass = ((EffectPassDefinition)effectTechniquePassesList.SelectedItem);
                    passNameBox.Text = pass.Name;

                    // vertex shaders
                    int shaderIndex = -1;
                    for (int i = 0; i < mEffectDefinition.VertexShaders.Count; i++)
                    {
                        if (mEffectDefinition.VertexShaders[i].Name == pass.VertexShaderName) shaderIndex = i;
                    }
                    passVertexShaderBox.SelectedIndex = shaderIndex;
                    passVertexShaderProfileBox.SelectedIndex = (passVertexShaderProfileBox.Items.Contains(pass.VertexShaderProfile.ToString()) ?
                        passVertexShaderProfileBox.Items.IndexOf(pass.VertexShaderProfile.ToString()) : -1);

                    // pixel shaders
                    shaderIndex = -1;
                    for (int i = 0; i < mEffectDefinition.PixelShaders.Count; i++)
                    {
                        if (mEffectDefinition.PixelShaders[i].Name == pass.PixelShaderName) shaderIndex = i;
                    }
                    passPixelShaderBox.SelectedIndex = shaderIndex;
                    passPixelShaderProfileBox.SelectedIndex = (passPixelShaderProfileBox.Items.Contains(pass.PixelShaderProfile.ToString()) ?
                        passPixelShaderProfileBox.Items.IndexOf(pass.PixelShaderProfile.ToString()) : -1);
                }
            }
        }

        private void pass_Changed(object sender, EventArgs e)
        {
            if (!mSuspendPassChange)
            {
                EffectPassDefinition pass = new EffectPassDefinition(
                    passNameBox.Text,
                    (passVertexShaderBox.SelectedIndex >= 0) ? mEffectDefinition.VertexShaders[passVertexShaderBox.SelectedIndex].Name : String.Empty,
                    (passPixelShaderBox.SelectedIndex >= 0) ? mEffectDefinition.PixelShaders[passPixelShaderBox.SelectedIndex].Name : String.Empty,
                    (passVertexShaderProfileBox.SelectedIndex >= 0) ?
                    (ShaderProfile)Enum.Parse(typeof(ShaderProfile), (string)passVertexShaderProfileBox.SelectedItem, true) : ShaderProfile.VS_1_1,
                    (passPixelShaderProfileBox.SelectedIndex >= 0) ?
                    (ShaderProfile)Enum.Parse(typeof(ShaderProfile), (string)passPixelShaderProfileBox.SelectedItem, true) : ShaderProfile.PS_1_1);

                mEffectDefinition.Techniques[effectTechniquesList.SelectedIndex].Passes[effectTechniquePassesList.SelectedIndex] = pass;

                mSuspendPassChange = true;
                int nameBoxStart = passNameBox.SelectionStart;
                int nameBoxLen = passNameBox.SelectionLength;
                int selectedIndex = effectTechniquePassesList.SelectedIndex;
                effectTechniquePassesList.DataSource = null;
                effectTechniquePassesList.DataSource = mEffectDefinition.Techniques[effectTechniquesList.SelectedIndex].Passes;
                effectTechniquePassesList.SelectedIndex = selectedIndex;
                passNameBox.SelectionStart = nameBoxStart;
                passNameBox.SelectionLength = nameBoxLen;
                mSuspendPassChange = false;
            }
        }

        #endregion

        #endregion


        private void compileEffectButton_Click_1(object sender, EventArgs e)
        {
            effectCompileOutput.Clear();

            effectEditBox.Text = mEffectDefinition.GetEffectCode();
            compileEffectButton_Click(sender, e);

            string output = effectCompileOutput.Text + Environment.NewLine + Environment.NewLine +
                mEffectDefinition.GetEffectCode();

            effectCompileOutput.Text = output;
        }

        private void loadEffectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openEffectDialog.ShowDialog() == DialogResult.OK)
            {
                mEffectDefinition = EffectDefinition.FromFile(openEffectDialog.FileName);

                this.SuspendLayout();

                effectComponentsList.DataSource = null;
                effectComponentsList.DataSource = mEffectDefinition.Components;
                UpdateAvailableComponents();

                effectParametersList.Parameters = mEffectDefinition.Parameters;
                UpdateStandardParameters();

                effectEdit_vertexShaderListChanged(null, new EventArgs());
                effectEdit_pixelShaderListChanged(null, new EventArgs());

                if (mEffectDefinition.VertexShaders.Count > 0) vertexShadersList.SelectedIndex = 0;
                if (mEffectDefinition.PixelShaders.Count > 0) pixelShadersList.SelectedIndex = 0;

                effectTechniquesList.DataSource = null;
                effectTechniquesList.DataSource = mEffectDefinition.Techniques;

                this.ResumeLayout();
            }
        }

        private void saveEffectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveEffectDialog.ShowDialog() == DialogResult.OK)
            {
                mEffectDefinition.Save(saveEffectDialog.FileName);
            }
        }
    }
}