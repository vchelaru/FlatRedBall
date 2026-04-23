using AnimationEditor.Core.Data;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimationEditor.Core
{
    public class SelectionSnapshot
    {
        public AnimationChainSave AnimationChainSave;
        public AnimationFrameSave AnimationFrameSave;
    }

    public class SelectedState : Singleton<SelectedState>
    {
        private AnimationChainSave _selectedChain;
        private AnimationFrameSave _selectedFrame;
        private AxisAlignedRectangleSave _selectedRectangle;
        private CircleSave _selectedCircle;
        private List<object> _selectedNodes = new List<object>();

        private SelectionSnapshot mSnapshot = new SelectionSnapshot();

        public event Action SelectionChanged;

        public AnimationChainListSave AnimationChainListSave =>
            ProjectManager.Self.AnimationChainListSave;

        public AnimationChainSave SelectedChain
        {
            get => _selectedChain;
            set
            {
                _selectedChain = value;
                _selectedFrame = null;
                _selectedRectangle = null;
                _selectedCircle = null;
                SelectionChanged?.Invoke();
            }
        }

        public AnimationFrameSave SelectedFrame
        {
            get => _selectedFrame;
            set
            {
                _selectedFrame = value;
                if (value != null)
                {
                    // Automatically set the parent chain
                    _selectedChain = FindChainForFrame(value);
                }
                _selectedRectangle = null;
                _selectedCircle = null;
                SelectionChanged?.Invoke();
            }
        }

        public AxisAlignedRectangleSave SelectedRectangle
        {
            get => _selectedRectangle;
            set
            {
                _selectedRectangle = value;
                if (value != null) _selectedCircle = null;
                SelectionChanged?.Invoke();
            }
        }

        public CircleSave SelectedCircle
        {
            get => _selectedCircle;
            set
            {
                _selectedCircle = value;
                if (value != null) _selectedRectangle = null;
                SelectionChanged?.Invoke();
            }
        }

        public object SelectedShape => (object)_selectedRectangle ?? _selectedCircle;

        public List<AnimationChainSave> SelectedChains { get; set; } = new List<AnimationChainSave>();

        public List<AnimationFrameSave> SelectedFrames
        {
            get
            {
                var frames = _selectedNodes.OfType<AnimationFrameSave>().ToList();
                if (frames.Count == 0 && _selectedFrame != null)
                    frames.Add(_selectedFrame);
                return frames;
            }
        }

        public List<AxisAlignedRectangleSave> SelectedRectangles =>
            _selectedNodes.OfType<AxisAlignedRectangleSave>().ToList();

        public List<CircleSave> SelectedCircles =>
            _selectedNodes.OfType<CircleSave>().ToList();

        /// <summary>
        /// Multi-selection bag. Can hold AnimationChainSave, AnimationFrameSave,
        /// AxisAlignedRectangleSave, or CircleSave objects.
        /// </summary>
        public List<object> SelectedNodes
        {
            get => _selectedNodes;
            set
            {
                _selectedNodes = value ?? new List<object>();
                SelectionChanged?.Invoke();
            }
        }

        public string SelectedTextureName
        {
            get
            {
                if (_selectedFrame != null)
                    return _selectedFrame.TextureName;
                if (_selectedChain?.Frames.Count > 0)
                    return _selectedChain.Frames[0].TextureName;
                return null;
            }
        }

        public TileMapInformation SelectedTileMapInformation
        {
            get
            {
                var fileName = _selectedFrame?.TextureName
                    ?? (_selectedChain?.Frames.Count > 0 ? _selectedChain.Frames[0].TextureName : null);

                if (!string.IsNullOrEmpty(fileName))
                    return ProjectManager.Self.TileMapInformationList.GetTileMapInformation(fileName);

                return null;
            }
        }

        public SelectionSnapshot Snapshot
        {
            get => mSnapshot;
            set => mSnapshot = value;
        }

        private AnimationChainSave FindChainForFrame(AnimationFrameSave frame)
        {
            if (AnimationChainListSave == null) return null;
            foreach (var chain in AnimationChainListSave.AnimationChains)
            {
                if (chain.Frames.Contains(frame))
                    return chain;
            }
            return null;
        }
    }
}
