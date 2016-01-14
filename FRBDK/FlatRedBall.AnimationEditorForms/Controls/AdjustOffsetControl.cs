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
        Justify
    }

    #endregion

    public partial class AdjustOffsetControl : UserControl
    {


        #region Properties

        public AdjustmentType AdjustmentType
        {
            get
            {
                return AdjustmentType.Justify;
            }
        }

        public Justification Justification
        {
            get
            {
                return (Justification)comboBox1.SelectedItem;
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
        }

        private void FillAlignments()
        {
            foreach (var value in Enum.GetValues(typeof(Justification)))
            {
                comboBox1.Items.Add(value);
            }

            comboBox1.Text = comboBox1.Items[0].ToString();
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
                case AnimationEditorForms.Controls.AdjustmentType.Justify:

                    ApplyJustifyOffsets();

                    break;
            }

            WireframeManager.Self.RefreshAll();
            PreviewManager.Self.RefreshAll();
            PropertyGridManager.Self.Refresh();
        }

        private void ApplyJustifyOffsets()
        {
            switch (this.Justification)
            {
                case AnimationEditorForms.Controls.Justification.Bottom:
                    AnimationChainSave chain = SelectedState.Self.SelectedChain;

                    if (chain != null)
                    {
                        foreach (var frame in chain.Frames)
                        {
                            Texture2D texture = WireframeManager.Self.GetTextureForFrame(frame);

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
    }
}
