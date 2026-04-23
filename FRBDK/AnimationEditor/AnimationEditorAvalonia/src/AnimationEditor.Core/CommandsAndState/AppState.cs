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
    }
}
