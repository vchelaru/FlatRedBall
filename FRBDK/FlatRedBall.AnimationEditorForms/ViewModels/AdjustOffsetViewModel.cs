using FlatRedBall.AnimationEditorForms.Controls;
using FlatRedBall.AnimationEditorForms.Preview;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FlatRedBall.AnimationEditorForms.ViewModels
{
    public class AdjustOffsetViewModel : ViewModel
    {
        #region Justify vs Adjust All

        public AdjustmentType AdjustmentType
        {
            get => Get<AdjustmentType>();
            set => Set(value);
        }

        [DependsOn(nameof(AdjustmentType))]
        public bool IsJustifyChecked
        {
            get => AdjustmentType == AdjustmentType.Justify;
            set
            {
                if(value)
                {
                    AdjustmentType = AdjustmentType.Justify;
                }
            }
        }

        [DependsOn(nameof(IsJustifyChecked))]
        public Visibility JustifyUiVisibility => IsJustifyChecked.ToVisibility();

        [DependsOn(nameof(AdjustmentType))]
        public bool IsAdjustAllChecked
        {
            get => AdjustmentType == AdjustmentType.AdjustOffset;
            set
            {
                if(value)
                {
                    AdjustmentType = AdjustmentType.AdjustOffset;
                }
            }
        }

        [DependsOn(nameof(IsAdjustAllChecked))]
        public Visibility AdjustAllUiVisibility => IsAdjustAllChecked.ToVisibility();

        #endregion

        #region Justification Properties

        public Justification Justification
        {
            get => Get<Justification>();
            set => Set(value);
        }

        public IEnumerable<Justification> AvailableJustifications => AvailableTypes<Justification>();
        public string JustificationLabelText
        {
            get => "Adjusts the offsets of all frames so that the bottoms all line up at 0,0. This is often used for platformers and other side-view games.";
        }

        #endregion

        #region Adjustment Properties

        public OffSetType OffsetType
        {
            get => Get<OffSetType>();
            set => Set(value);
        }

        [DependsOn(nameof(OffsetType))]
        public bool IsRelativeOffsetChecked
        {
            get => OffsetType == OffSetType.Relative;
            set
            {
                if(value)
                {
                    OffsetType = OffSetType.Relative;
                }
            }
        }

        [DependsOn(nameof(OffsetType))]
        public bool IsAbsoluteOffsetChecked
        {
            get => OffsetType == OffSetType.Absolute;
            set
            {
                if(value)
                {
                    OffsetType = OffSetType.Absolute;
                }
            }
        }

        [DependsOn(nameof(OffsetType))]
        public string AdjustmentTypeText
        {
            get
            {
                if (OffsetType == OffSetType.Relative) return "Modifies the existing RelativeX/Y of every frame by these amounts.";
                if (OffsetType == OffSetType.Absolute) return "Sets these exact values to the RelativeX/Y of every frame, overwriting what is currently there.";
                return null;
            }
        }

        public float? OffsetX
        {
            get => Get<float?>();
            set => Set(value);
        }

        public float? OffsetY
        {
            get => Get<float?>();
            set => Set(value);
        }

        public IEnumerable<Justification> AvailableJustification => AvailableTypes<Justification>();

        #endregion



        internal void ApplyOffsets()
        {
            switch (this.AdjustmentType)
            {
                case AdjustmentType.Justify:
                    ApplyJustifyOffsets();
                    break;
                case AdjustmentType.AdjustOffset:
                    switch (this.OffsetType)
                    {
                        case OffSetType.Absolute:
                            ApplyFrameOffsets(false);
                            break;
                        case OffSetType.Relative:
                            ApplyFrameOffsets(true);
                            break;
                    }
                    break;
            }

            WireframeManager.Self.RefreshAll();
            PreviewManager.Self.RefreshAll();
            PropertyGridManager.Self.Refresh();
        }

        private void ApplyFrameOffsets(bool isRelative)
        {
            var chain = SelectedState.Self.SelectedChain;

            if (chain != null)
            {
                foreach (var frame in chain.Frames)
                {
                    var texture = WireframeManager.Self.GetTextureForFrame(frame);

                    if (texture != null)
                    {
                        if (OffsetX != null) frame.RelativeX = (isRelative ? frame.RelativeX : 0) + OffsetX.Value;
                        if (OffsetY != null) frame.RelativeY = (isRelative ? frame.RelativeY : 0) + OffsetY.Value;
                    }
                }
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

        public AdjustOffsetViewModel()
        {
            AdjustmentType = AdjustmentType.Justify;
            OffsetType = OffSetType.Relative;
            
        }
        IEnumerable<T> AvailableTypes<T>()
        {

            var list = new List<T>();
            foreach (T item in Enum.GetValues(typeof(T)))
            {
                list.Add(item);
            }
            return list;
        }

    }
}
