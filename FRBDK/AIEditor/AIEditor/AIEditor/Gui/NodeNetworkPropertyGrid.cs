using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;

using FlatRedBall.AI.Pathfinding;

using FlatRedBall.Gui;

namespace AIEditor.Gui
{
    public class NodeNetworkPropertyGrid : PropertyGrid<NodeNetwork>
    {
        #region Fields

        ListDisplayWindow mNodes;

        #endregion


        #region Event

        

        #endregion

        #region Event Methods

        void NameChange(Window callingWindow)
        {
            PropertyGrid<PositionedNode> newWindow = mNodes.LastChildWindowCreated as PropertyGrid<PositionedNode>;

            PositionedNode selectedNode = newWindow.SelectedObject;

            FlatRedBall.Utilities.StringFunctions.MakeNameUnique<PositionedNode>(
                selectedNode, this.SelectedObject.Nodes);
        }

        void mNodes_AfterChildWindowCreated(Window callingWindow)
        {
            PropertyGrid<PositionedNode> newWindow = mNodes.LastChildWindowCreated as PropertyGrid<PositionedNode>;


            newWindow.SetMemberChangeEvent("Name", NameChange);
        }

        #endregion

        private static void SelectNodeInListBox(Window callingWindow)
        {
            CollapseListBox asListBox = callingWindow as CollapseListBox;

            PositionedNode node = asListBox.GetFirstHighlightedObject() as PositionedNode;

            EditorData.EditingLogic.SelectNode(node);
        }

        #region Methods

        public NodeNetworkPropertyGrid()
            : base(GuiManager.Cursor)
        {
            GuiManager.AddWindow(this);

            ExcludeMember("NodeColor");
            ExcludeMember("LinkColor");
            ExcludeMember("NodeVisibleRepresentation");
            ExcludeMember("LayerToDrawOn");

            mNodes = new ListDisplayWindow(GuiManager.Cursor);
            mNodes.Resizable = true;
            ReplaceMemberUIElement("Nodes", mNodes);
            mNodes.ListBox.Highlight += SelectNodeInListBox;
            mNodes.ShowPropertyGridOnStrongSelect = true;
            mNodes.ScaleY = 5;

            mNodes.AfterChildWindowCreated += new GuiMessage(mNodes_AfterChildWindowCreated);


            Name = "NodeNetwork Properties";
        }



        public void Update()
        {
            if (mNodes.AreHighlightsMatching(EditorData.EditingLogic.CurrentNodes) == false)
            {
                mNodes.HighlightObjectNoCall(null, false);

                foreach (PositionedNode node in EditorData.EditingLogic.CurrentNodes)
                {
                    mNodes.HighlightObjectNoCall(node, true);

                    

                }

                if (EditorData.EditingLogic.CurrentNodes.Count != 0 && mNodes.LastChildWindowCreated != null)
                {
                    ((PropertyGrid<PositionedNode>)mNodes.LastChildWindowCreated).SelectedObject =
                        EditorData.EditingLogic.CurrentNodes[0];
                }
            }
        }

        #endregion

    }
}
