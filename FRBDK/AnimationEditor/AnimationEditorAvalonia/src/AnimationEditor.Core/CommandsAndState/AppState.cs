using AnimationEditor.Core.Data;
using FlatRedBall.Content.AnimationChain;

namespace AnimationEditor.Core.CommandsAndState
{
    /// <summary>
    /// Holds lightweight, UI-independent app state. Properties that were previously
    /// delegated to WireframeManager or PropertyGridManager are now stored directly
    /// here and raised as events so UI layers can respond.
    /// </summary>
    public class AppState : Singleton<AppState>
    {
        /// <summary>
        /// The absolute path of the project (.gluj/.glux) that this .achx belongs to.
        /// When set, the tool won't prompt the user to copy files that are part of the project.
        /// </summary>
        public string ProjectFolder { get; set; }

        private UnitType _unitType;
        public UnitType UnitType
        {
            get => _unitType;
            set
            {
                _unitType = value;
                ApplicationEvents.Self.CallWireframeTextureChange();
            }
        }

        private int _wireframeZoomValue = 100;
        public int WireframeZoomValue
        {
            get => _wireframeZoomValue;
            set
            {
                _wireframeZoomValue = value;
                ApplicationEvents.Self.CallAfterZoomChange();
            }
        }

        private bool _isSnapToGridChecked;
        public bool IsSnapToGridChecked
        {
            get => _isSnapToGridChecked;
            set => _isSnapToGridChecked = value;
        }

        private int _gridSize = 16;
        public int GridSize
        {
            get => _gridSize;
            set => _gridSize = value;
        }

        public AnimationFrameSave CurrentFrame => SelectedState.Self.SelectedFrame;

        // ── PL11: Sprite alignment ────────────────────────────────────────────

        private SpriteAlignment _spriteAlignment = SpriteAlignment.Center;
        /// <summary>
        /// Controls how the preview sprite is placed relative to the FlatRedBall origin.
        /// Default matches the FRB default: <see cref="SpriteAlignment.Center"/>.
        /// </summary>
        public SpriteAlignment SpriteAlignment
        {
            get => _spriteAlignment;
            set => _spriteAlignment = value;
        }

        // ── PL12: Preview offset multiplier ──────────────────────────────────

        private float _offsetMultiplier = 1f;
        /// <summary>
        /// Divides stored <c>RelativeX/Y</c> before displaying in the property panel,
        /// and multiplies back when the user sets a value.
        /// Persisted in <c>AESettingsSave</c>.  Default 1 (no scaling).
        /// </summary>
        public float OffsetMultiplier
        {
            get => _offsetMultiplier;
            set => _offsetMultiplier = value == 0f ? 1f : value; // guard: 0 treated as 1
        }
    }
}
