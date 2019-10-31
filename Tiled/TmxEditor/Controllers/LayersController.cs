using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TmxEditor.Events;
using TmxEditor.PropertyGridDisplayers;
using TmxEditor.UI;
using TMXGlueLib;

namespace TmxEditor.Controllers
{
    public class LayersController : Singleton<LayersController>
    {
        #region Fields

        TreeView mLayerTreeView;


        TiledMapSave mTiledMapSave;

        MapLayerDisplayer mDisplayer;

        #endregion

        #region Properties

        PropertyGrid PropertyGrid
        {
            get
            {
                return mDisplayer.PropertyGrid;
            }
        }

        public TiledMapSave TiledMapSave
        {
            set
            {
                mTiledMapSave = value;
                RefreshAll();
            }
        }

        public MapLayer CurrentMapLayer
        {
            get
            {
                TreeNode selectedNode = mLayerTreeView.SelectedNode;
                if (selectedNode != null)
                {
                    return selectedNode.Tag as MapLayer;
                }
                return null;
            }
        }

        public property CurrentLayerProperty
        {
            get
            {
                return mDisplayer.CurrentLayerProperty;


            }
        }

        #endregion

        #region Events
        public event EventHandler AnyTileMapChange;
        #endregion

        public void Initialize(TreeView layerTreeView, PropertyGrid propertyGrid)
        {
            mLayerTreeView = layerTreeView;

            mLayerTreeView.AfterSelect += mLayerTreeView_Click;

            mDisplayer = new MapLayerDisplayer();
            mDisplayer.PropertyGrid = propertyGrid;

            mDisplayer.PropertyGrid.PropertyValueChanged += HandlePropertyValueChangeInternal;
        }

        void HandlePropertyValueChangeInternal(object s, PropertyValueChangedEventArgs e)
        {
            if (AnyTileMapChange != null)
            {
                var args = new TileMapChangeEventArgs();
                args.ChangeType = ChangeType.Other;

                AnyTileMapChange(this, args);
            }

        }

        void mLayerTreeView_Click(object sender, EventArgs e)
        {
            MapLayer mapLayer = null;

            if (mLayerTreeView.SelectedNode != null)
            {
                mapLayer = mLayerTreeView.SelectedNode.Tag as MapLayer;
                mDisplayer.Instance = mapLayer;
            }
        }

        public void RefreshAll()
        {
            // We'll do the inefficient way for now, but move to an efficient way when it matters

            mLayerTreeView.Nodes.Clear();

            if (mTiledMapSave != null)
            {
                foreach (var layer in mTiledMapSave.Layers)
                {
                    TreeNode node = new TreeNode(layer.Name);
                    node.Tag = layer;
                    mLayerTreeView.Nodes.Add(node);
                }
            }

        }

        internal void HandleAddPropertyClick()
        {
            var layer = AppState.Self.CurrentMapLayer;
            if (layer == null)
            {
                MessageBox.Show("You must first select a Layer");
            }
            else
            {
                NewPropertyWindow window = new NewPropertyWindow();
                DialogResult result = window.ShowDialog();

                if (result == DialogResult.OK)
                {
                    string name = window.ResultName;
                    string type = window.ResultType;
                    var newProperty = new TMXGlueLib.property();
                    SetPropertyNameFromNameAndType(name, type, newProperty);
                    layer.properties.Add(newProperty);
                    mDisplayer.UpdateDisplayedProperties();
                    mDisplayer.PropertyGrid.Refresh();

                    if (AnyTileMapChange != null)
                    {
                        AnyTileMapChange(this, null);
                    }
                }

            }
        }

        public static void SetPropertyNameFromNameAndType(string name, string type, property propertyToSet)
        {
            if (!string.IsNullOrEmpty(type))
            {
                propertyToSet.name = name + " (" + type + ")";

            }
            else
            {
                propertyToSet.name = name;
            }
        }

        internal void HandleRemovePropertyClick()
        {
            property property = AppState.Self.CurrentLayerProperty;

            if(property != null)
            {
                var result = 
                    MessageBox.Show("Are you sure you'd like to remove the property " + property.name + "?", "Remove property?", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    AppState.Self.CurrentMapLayer.properties.Remove(property);
                    mDisplayer.UpdateDisplayedProperties();
                    mDisplayer.PropertyGrid.Refresh();
                    if (AnyTileMapChange != null)
                    {
                        AnyTileMapChange(this, null);
                    }
                }
            }
        }

        internal void HandleListRightClick(MouseEventArgs e)
        {
            mLayerTreeView.ContextMenuStrip.Items.Clear();
            TreeNode node = this.mLayerTreeView.GetNodeAt(e.X, e.Y);
            this.mLayerTreeView.SelectedNode = node;

            if (node != null)
            {
                object tag = node.Tag;



            }

        }

        internal void HandlePropertyGridRightClick(EventArgs e)
        {

        }

        private void HandleEditVariableClick(object sender, EventArgs e)
        {

            var property = CurrentLayerProperty;
            if (property != null)
            {
                NewPropertyWindow npw = new NewPropertyWindow();
                npw.FromCombinedPropertyName(property.name);

                var dialogResult = npw.ShowDialog();

                if (dialogResult == DialogResult.OK)
                {
                    SetPropertyNameFromNameAndType(npw.ResultName, npw.ResultType, property);

                    mDisplayer.UpdateDisplayedProperties();
                    mDisplayer.PropertyGrid.Refresh();

                    if (AnyTileMapChange != null)
                    {
                        AnyTileMapChange(this, null);
                    }
                }
            }
        }

        internal void UpdatePropertyGridContextMenu(SelectedGridItemChangedEventArgs e)
        {

            var menu = PropertyGrid.ContextMenuStrip;

            PropertyGrid.ContextMenuStrip.Items.Clear();

            if (PropertyGrid.SelectedGridItem != null)
            {
                string label = PropertyGrid.SelectedGridItem.Label;

                if (!string.IsNullOrEmpty(label))
                {
                    menu.Items.Add("Edit Variable", null, HandleEditVariableClick);
                }
            }
        }
    }
}
