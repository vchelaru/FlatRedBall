using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;


using AIEditor.Gui;

using FlatRedBall;

using FlatRedBall.AI.Pathfinding;

using FlatRedBall.Graphics;

using FlatRedBall.Gui;

using FlatRedBall.Input;

using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using EditorObjects;

#if FRB_MDX
using Microsoft.DirectX;
#else
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Keys = Microsoft.Xna.Framework.Input.Keys;
#endif


namespace AIEditor
{
    #region Enums
    public enum EditingState
    {
        None,
        CreatingLink


    }
    #endregion

    public class EditingLogic
    {
        #region Fields

        private EditingState mEditingState = EditingState.None;

        private PositionedNode mNodeOver;
        private Link mLinkOver;
        private PositionedNode mLinkOverParent;

        private PositionedNode mNodeGrabbed;

        private List<PositionedNode> mCurrentNodes = new List<PositionedNode>();
        private ReadOnlyCollection<PositionedNode> mCurrentNodesReadOnly;

        private PositionedNode mCurrentLinkParent;
        private Link mCurrentLink;

        private Line mNewConnectionLine;

        private PathDisplay mPathDisplay = new PathDisplay();

        private PositionedObjectList<Text> mDistanceDisplay = new PositionedObjectList<Text>();

        Text mDebugText;

        ReactiveHud mReactiveHud;

        #endregion

        #region Properties

        public ReadOnlyCollection<PositionedNode> CurrentNodes
        {
            get { return mCurrentNodesReadOnly; }
        }

        public Link CurrentLink
        {
            get { return mCurrentLink; }
        }

        public PositionedNode CurrentLinkParent
        {
            get { return mCurrentLinkParent; }
        }


        public PositionedNode NodeOver
        {
            get
            {
                return mNodeOver;
            }
        }

        public Link LinkOver
        {
            get
            {
                return mLinkOver;
            }
        }


        public PositionedNode LinkOverParent
        {
            get
            {
                return mLinkOverParent;
            }
        }

        #endregion

        #region Methods

        #region Constructor

        public EditingLogic()
        {
            mNewConnectionLine = new Line();

            mCurrentNodesReadOnly = new ReadOnlyCollection<PositionedNode>(mCurrentNodes);

            mDebugText = TextManager.AddText("");

            mReactiveHud = new ReactiveHud();
        }

        #endregion

        #region Public Methods

        public void ClearPath()
        {
            mPathDisplay.ClearPath();
        }

        public void CopyCurrentPositionedNodes()
        {
            foreach (PositionedNode node in mCurrentNodes)
            {
                PositionedNode newNode = EditorData.NodeNetwork.AddNode();
                newNode.Position = node.Position;

                EditorData.NodeNetwork.Visible = true;
                EditorData.NodeNetwork.UpdateShapes();
            }
        }

        public void SelectNode(PositionedNode nodeToSelect)
        {
            #region Finding path between two nodes
            if (GuiData.ToolsWindow.IsFindPathToNodeButtonPressed && mCurrentNodes.Count != 0 && nodeToSelect != null)
            {
                // The button should come back up
                GuiData.ToolsWindow.IsFindPathToNodeButtonPressed = false;

                List<PositionedNode> positionedNodes =
                    EditorData.NodeNetwork.GetPath(mCurrentNodes[0], nodeToSelect);

                mPathDisplay.ShowPath(positionedNodes);

                if (positionedNodes.Count == 0)
                {
                    GuiManager.ShowMessageBox("The two nodes are not connected by links.", "Not Connected");
                }
            }
            #endregion

            #region else, simply selecting node
            else
            {
                mCurrentNodes.Clear();


                if (nodeToSelect != null)
                {
                    mCurrentNodes.Add(nodeToSelect);
                }
            }
            #endregion

        }

        void SelectLink(Link linkToSelect, PositionedNode linkParent)
        {
            mCurrentLink = linkToSelect;
            mCurrentLinkParent = linkParent;
        }

        public void Update()
        {
            if (GuiManager.DominantWindowActive == false)
            {
                //mDebugText.DisplayText = UndoManager.Instructions.Count.ToString(); ;
                Cursor cursor = GuiManager.Cursor;

                GuiData.Update();

                PerformKeyboardShortcuts();

                EditorObjects.CameraMethods.MouseCameraControl(SpriteManager.Camera);

                PerformCommandUIUpdate();

                mReactiveHud.Activity();

                #region Update the CommandDisplay

                if (mCurrentNodes.Count != 0)
                {
                    GuiData.CommandDisplay.Visible = true;
                    GuiData.CommandDisplay.Position = mCurrentNodes[0].Position;
                }
                else
                {
                    GuiData.CommandDisplay.Visible = false;
                }

                #endregion

                CursorLogic(cursor);

                UndoManager.EndOfFrameActivity();
            }
        }

        private void CursorLogic(Cursor cursor)
        {
            GetObjectsOver();

            #region Create link or move objects with cursor
            // Don't do any logic if the cursor is over a window
            if (cursor.WindowOver == null && GuiData.CommandDisplay.IsCursorOverThis != true)
            {
                // mNodeOver is used in the following methods:


                switch (mEditingState)
                {

                    case EditingState.CreatingLink:

                        CreatingLinkUpdate();

                        break;

                    case EditingState.None:


                        CursorControlOverObjects();

                        break;

                }
            }
            #endregion
        }

        private void GetObjectsOver()
        {
            mNodeOver = GetNodeOver();

            if (mNodeOver != null)
            {
                mLinkOver = null;
                mLinkOverParent = null;
            }
            else
            {
                GetLinkOver(ref mLinkOver, ref mLinkOverParent);
            }


        }

        #endregion

        #region Private Methods

        private void CursorControlOverObjects()
        {
            Cursor cursor = GuiManager.Cursor;


            #region Pushing selects and grabs a Node or link
            if (cursor.PrimaryPush)
            {
                #region Check for nodes

                mNodeGrabbed = mNodeOver;
                cursor.SetObjectRelativePosition(mNodeGrabbed);

                SelectNode(mNodeGrabbed);

                #endregion

                #region Check for links

                if (mCurrentNodes.Count == 0)
                {
                    SelectLink(mLinkOver, mLinkOverParent);
                }

                #endregion
            }
            #endregion

            #region Holding the button down can be used to adjust node properties
            if (cursor.PrimaryDown)
            {
                PerformDraggingUpdate();

            }
            #endregion

            #region Clicking (releasing) the mouse lets go of grabbed Polygons

            if (cursor.PrimaryClick)
            {
                mNodeGrabbed = null;

                cursor.StaticPosition = false;

                cursor.ObjectGrabbed = null;

                TextManager.RemoveText(mDistanceDisplay);

            }

            #endregion

        }

        private void CreatingLinkUpdate()
        {
            Cursor cursor = GuiManager.Cursor;

            if (cursor.PrimaryClick)
            {
                // The user clicked, so see if the cursor is over a PositionedNode
                PositionedNode nodeOver = mNodeOver;

                if (nodeOver != null && nodeOver != mCurrentNodes[0] && mCurrentNodes[0].IsLinkedTo(nodeOver) == false)
                {
                    mCurrentNodes[0].LinkTo(nodeOver, (mCurrentNodes[0].Position - nodeOver.Position).Length() );
                }
                
            }

            if (cursor.PrimaryDown == false)
            {
                // If the user's mouse is not down then go back to normal editing mode
                mNewConnectionLine.Visible = false;
                mEditingState = EditingState.None;
                return;
            }
            else
            {
                mNewConnectionLine.Visible = true;
                mNewConnectionLine.RelativePoint1.X =
                    mNewConnectionLine.RelativePoint1.Y = 0;

                mNewConnectionLine.Position = mCurrentNodes[0].Position;

                mNewConnectionLine.RelativePoint2.X = cursor.WorldXAt(0) - mNewConnectionLine.Position.X;
                mNewConnectionLine.RelativePoint2.Y = cursor.WorldYAt(0) - mNewConnectionLine.Position.Y;

            }


        }

        private PositionedNode GetNodeOver()
        {
            Cursor cursor = GuiManager.Cursor;

            float worldX;
            float worldY;

            if (EditorData.NodeNetwork.Nodes.Count != 0)
            {
                // While GetVisibleNodeRadius lets us get the radius for any node, for now we'll just
                // assume that all nodes are on the same Z plane.  If this changes later, move the
                // GetVisibleNodeRadius call down into the loop where the nodeRadius variable is used.
                float nodeRadius = EditorData.NodeNetwork.GetVisibleNodeRadius(SpriteManager.Camera, 0);
                
                for(int i = 0; i < EditorData.NodeNetwork.Nodes.Count; i++)
                {
                    PositionedNode node = EditorData.NodeNetwork.Nodes[i];

                    worldX = cursor.WorldXAt(node.Z);
                    worldY = cursor.WorldYAt(node.Z);


                    if ((node.X - worldX) * (node.X - worldX) + (node.Y - worldY) * (node.Y - worldY) < nodeRadius*nodeRadius)
                    {
                        return node;
                    }
                }            
            }
            return null;
        }

        private void GetLinkOver(ref Link linkOver, ref PositionedNode linkOverParent)
        {
            Cursor cursor = GuiManager.Cursor;

            float worldX;
            float worldY;

            if (EditorData.NodeNetwork.Nodes.Count != 0)
            {
                float tolerance = 5 / SpriteManager.Camera.PixelsPerUnitAt(0);


                for (int i = 0; i < EditorData.NodeNetwork.Nodes.Count; i++)
                {
                    PositionedNode node = EditorData.NodeNetwork.Nodes[i];

                    worldX = cursor.WorldXAt(node.Z);
                    worldY = cursor.WorldYAt(node.Z);

                    for (int linkIndex = 0; linkIndex < node.Links.Count; linkIndex++)
                    {
                        Segment segment = new Segment(
                            node.Position, node.Links[linkIndex].NodeLinkingTo.Position);

                        float distance = segment.DistanceTo(worldX, worldY);

                        if (distance < tolerance)
                        {
                            linkOverParent = node;
                            linkOver = node.Links[linkIndex];
                            return;
                        }

                    }
                }
            }
            linkOverParent = null;
            linkOver = null;
        }

        private void PerformCommandUIUpdate()
        {
            Cursor cursor = GuiManager.Cursor;

            if (cursor.WindowOver != null || mCurrentNodes.Count == 0)
                return;

            if (cursor.PrimaryPush)
            {
                if (GuiData.CommandDisplay.IsCursorOverCreateLinkIcon)
                {
                    mEditingState = EditingState.CreatingLink;
                }
            }

        }

        private void PerformDraggingUpdate()
        {
            Cursor cursor = GuiManager.Cursor;

            if (mNodeGrabbed != null)
            {
                if (GuiData.ToolsWindow.IsMoveButtonPressed)
                {
                    PositionedObjectMover.MouseMoveObject(mNodeGrabbed);

                    foreach (Link link in mNodeGrabbed.Links)
                    {
                        link.Cost = (mNodeGrabbed.Position - link.NodeLinkingTo.Position).Length();

                        // Currently links are two-way, so make sure that the cost is updated both ways
                        PositionedNode nodeLinkedTo = link.NodeLinkingTo;
                        foreach (Link otherLink in nodeLinkedTo.Links)
                        {
                            if (otherLink.NodeLinkingTo == mNodeGrabbed)
                            {
                                otherLink.Cost = link.Cost;
                            }
                        }

                    }

                    UpdateDistanceDisplay();

                    EditorData.NodeNetwork.UpdateShapes();
                }
            }
        }

        private void PerformKeyboardShortcuts()
        {
            if (InputManager.Keyboard.KeyPushedConsideringInputReceiver(Keys.Delete))
            {
                #region Delete Nodes

                if (mCurrentNodes.Count != 0)
                {
                    for (int i = 0; i < mCurrentNodes.Count; i++)
                    {
                        EditorData.NodeNetwork.Remove(mCurrentNodes[i]);
                    }

                    EditorData.NodeNetwork.UpdateShapes();

                    SelectNode(null);
                }

                #endregion

                #region Delete Links

                if (mCurrentLink != null)
                {
                    PositionedNode firstNode = mCurrentLinkParent;

                    PositionedNode otherNode = mCurrentLink.NodeLinkingTo;

                    firstNode.BreakLinkBetween(otherNode);

                    EditorData.NodeNetwork.UpdateShapes();

                    SelectLink(null, null);

                }

                #endregion
            }
        }

        private void UpdateDistanceDisplay()
        {
            int numberOfLinks = mNodeGrabbed.Links.Count;
            
            while (mDistanceDisplay.Count < numberOfLinks)
            {
                mDistanceDisplay.Add(TextManager.AddText(""));
            }

            for (int i = 0; i < numberOfLinks; i++)
            {
#if FRB_MDX
                mDistanceDisplay[i].Position = 
                    Vector3.Scale(( mNodeGrabbed.Position + mNodeGrabbed.Links[i].NodeLinkingTo.Position ), .5f);
#else
                mDistanceDisplay[i].Position = (mNodeGrabbed.Position + mNodeGrabbed.Links[i].NodeLinkingTo.Position) * .5f;
#endif
                mDistanceDisplay[i].DisplayText =
                    (mNodeGrabbed.Position - mNodeGrabbed.Links[i].NodeLinkingTo.Position).Length().ToString();
            }

        }

        #endregion

        #endregion

    }
}
