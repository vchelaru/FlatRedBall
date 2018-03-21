using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Content.AnimationChain;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.AnimationEditorForms.Preview;

namespace FlatRedBall.AnimationEditorForms.Controls
{
    #region Enums

    public enum Justification
    {
        Bottom
    }

    public enum AdjustmentType
    {
        None,
        Justify,
        AdjustOffset,
    }

    public enum OffSetType
    {
        None,
        Absolute,
        Relative,
    }

    #endregion

    public partial class AdjustOffsetControl : UserControl
    {

        #region Properties

        public AdjustmentType AdjustmentType
        {
            get
            {
                if (JustifyRadioButton.Checked) return AdjustmentType.Justify;
                if (AdjustAllOffsetsRadioButton.Checked) return AdjustmentType.AdjustOffset;
                return AdjustmentType.None;
            }
        }

        public OffSetType OffSetType
        {
            get
            {
                if (AdjustAbsoluteRadioButton.Checked) return OffSetType.Absolute;
                if (AdjustRelativeRadioButton.Checked) return OffSetType.Relative;
                return OffSetType.None;
            }
        }

        public Justification Justification
        {
            get
            {
                return (Justification)JustificationComboBox.SelectedItem;
            }
        }

        #endregion

        #region Events

        public event EventHandler OkClick;
        public event EventHandler CancelClick;


        #endregion

        #region Methods

        public AdjustOffsetControl()
        {
            InitializeComponent();

            FillAlignments();

            UpdateInfoLabel();
        }

        private void FillAlignments()
        {
            foreach (var value in Enum.GetValues(typeof(Justification)))
            {
                JustificationComboBox.Items.Add(value);
            }

            JustificationComboBox.Text = JustificationComboBox.Items[0].ToString();
        }

        #endregion

        private void HandleOkClick(object sender, EventArgs e)
        {
            if (OkClick != null)
            {
                OkClick(this, null);
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            if (CancelClick != null)
            {
                CancelClick(this, null);
            }
        }

        internal void ApplyOffsets()
        {
            switch (this.AdjustmentType)
            {
                case AdjustmentType.Justify:
                    ApplyJustifyOffsets();
                    break;
                case AdjustmentType.AdjustOffset:
                    switch (this.OffSetType)
                    {
                        case OffSetType.Absolute:
                            ApplyFrameOffsets(OffsetXTextbox.Text, OffsetYTextBox.Text, false);
                            break;
                        case OffSetType.Relative:
                            ApplyFrameOffsets(OffsetXTextbox.Text, OffsetYTextBox.Text, true);
                            break;
                    }
                    break;
            }

            WireframeManager.Self.RefreshAll();
            PreviewManager.Self.RefreshAll();
            PropertyGridManager.Self.Refresh();
        }

        private void ApplyFrameOffsets(string xString, string yString, bool isRelative)
        {
            var chain = SelectedState.Self.SelectedChain;

            var shouldAdjustX = !string.IsNullOrWhiteSpace(xString);
            var shouldAdjustY = !string.IsNullOrWhiteSpace(yString);

            StringToValue(xString, out float xOffset);
            StringToValue(yString, out float yOffset);

            if (chain != null && (shouldAdjustX || shouldAdjustY))
            {
                foreach (var frame in chain.Frames)
                {
                    var texture = WireframeManager.Self.GetTextureForFrame(frame);

                    if (texture != null)
                    {
                        if (shouldAdjustX) frame.RelativeX = (isRelative ? frame.RelativeX : 0) + xOffset;
                        if (shouldAdjustY) frame.RelativeY = (isRelative ? frame.RelativeY : 0) + yOffset;
                    }
                }
            }
        }

        private void StringToValue(string stringValue, out float outValue)
        {
            if (string.IsNullOrWhiteSpace(stringValue)) outValue = 0;
            else
            {
                float.TryParse(stringValue, out outValue);
            }
        }

        private void ApplyJustifyOffsets()
        {
            switch (this.Justification)
            {
                case Justification.Bottom:
                    var chain = SelectedState.Self.SelectedChain;

                    if (chain != null)
                    {
                        foreach (var frame in chain.Frames)
                        {
                            var texture = WireframeManager.Self.GetTextureForFrame(frame);

                            if (texture != null)
                            {
                                float textureAmount = texture.Height * (frame.BottomCoordinate - frame.TopCoordinate);

                                // AnimationFrames treat positive Y as up
                                frame.RelativeY = (textureAmount / 2.0f) / PreviewManager.Self.OffsetMultiplier;

                            }
                        }
                    }

                    break;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateInfoLabel();
        }

        private void UpdateInfoLabel()
        {
            this.InformationLabel.Text = "";
            var selectedJustification = (Justification)JustificationComboBox.SelectedItem;
            switch (selectedJustification)
            {
                case Justification.Bottom:
                    this.InformationLabel.Text = "Adjusts the offsets of all frames so that the bottoms all line up at 0,0. This is often used for platformers and other side-view games";
                    break;

            }
            if (AdjustRelativeRadioButton.Checked) AdjustmentTypeLabel.Text = "Modifies the existing RelativeX/Y of every frame by these amounts.";
            if (AdjustAbsoluteRadioButton.Checked) AdjustmentTypeLabel.Text = "Sets these exact values to the RelativeX/Y of every frame, overwriting what is currently there.";

        }

        private void JustifyRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            UpdateAdjustOffsetPanels();
        }

        private void AdjustAllOffsetsRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            UpdateAdjustOffsetPanels();
        }

        private void AdjustAbsoluteRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            UpdateAdjustOffsetPanels();
        }

        private void AdjustRelativeRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            UpdateAdjustOffsetPanels();
        }

        private void UpdateAdjustOffsetPanels()
        {
            JustifyPanel.Visible = JustifyRadioButton.Checked;
            AdjustAllPanel.Visible = AdjustAllOffsetsRadioButton.Checked;
            UpdateInfoLabel();
        }
    }
}
