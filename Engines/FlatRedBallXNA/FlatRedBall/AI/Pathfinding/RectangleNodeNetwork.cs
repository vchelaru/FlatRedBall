using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;

namespace FlatRedBall.AI.Pathfinding
{
    public class RectangleNodeNetwork : NodeNetwork
    {
        // let's keep a casted copy for speed:
        List<RectangleNode> rectangleNodes = new List<RectangleNode>();

        public Axis StripAxis { get; set;}

        List<AxisAlignedRectangle> rectangles = new List<AxisAlignedRectangle>();

        public override PositionedNode AddNode()
        {
            var node = new RectangleNode();

            base.AddNode(node);

            return node;
        }

        public void AddNode(RectangleNode node)
        {
            rectangleNodes.Add(node);
            base.AddNode(node);
        }

        public virtual List<PositionedNode> GetPathOrClosest(ref Vector3 startPoint, ref Vector3 endPoint)
        {
            var startNode = GetClosestNodeTo(ref startPoint);
            var  endNode = GetClosestNodeTo(ref endPoint);

            return GetPathOrClosest(startNode, endNode);
        }

        public override PositionedNode GetClosestNodeTo(ref Vector3 position)
        {
            PositionedNode closestNode = null;

            for (int i = 0; i < rectangleNodes.Count; i++)
            {
                var node = rectangleNodes[i];

                var halfWidth = node.Width / 2.0f;
                var halfHeight = node.Height / 2.0f;

                if (position.X >= node.X - halfWidth &&
                    position.X <= node.X + halfWidth &&
                    position.Y >= node.Y - halfHeight &&
                    position.Y <= node.Y + halfHeight)
                {
                    return node;
                }
            }
            return null;
        }

        public override void UpdateShapes()
        {
            if(Visible == false)
            {
                // remove all rectnagles
                for(int i = 0; i < rectangles.Count; i++)
                {
                    rectangles[i].Visible = false;
                }
            }
            else
            {
                for(int i = 0; i < rectangleNodes.Count; i++)
                {
                    var node = rectangleNodes[i];

                    var rect = new AxisAlignedRectangle();
                    rect.X = node.Position.X;
                    rect.Y = node.Position.Y;
                    rect.Width = node.Width;
                    rect.Height = node.Height;
                    rect.Visible = true;

                    rectangles.Add(rect);
                    // todo add it here
                }
            }

            base.UpdateShapes();
        }
        // todo - remove?
        public void LinkSorted()
        {
            ////////////Early Out///////////////
            if(rectangleNodes.Count == 0)
            {
                return;
            }
            //////////End Early Out/////////////

            if(StripAxis == Axis.X)
            {
                var height = rectangleNodes[0].Height;
                var heightTimesOnePointFive = height * 1.5f;

                for(int i = 0; i < rectangleNodes.Count-1; i++)
                {
                    var firstNode = rectangleNodes[i];
                    for(int otherNodeIndex = i+1; otherNodeIndex < rectangleNodes.Count; otherNodeIndex++)
                    {
                        var secondNode = rectangleNodes[otherNodeIndex];

                        // They can link if
                        // * They have different Y values. Same-Y values will never be linked because
                        //   if they were touching, they'd be merged.
                        // * If the Y is within width/2 (actually we'll do 3 to be safe)
                        // * If the X values overlap
                        // If the Y is > width*3, break the loop, they're too far away
                        if(firstNode.Y == secondNode.Y)
                        {
                            // do nothing, move on
                        }
                        else if (secondNode.Y - firstNode.Y > heightTimesOnePointFive)
                        {
                            break;
                        }
                        else
                        {
                            // it's within the Y distance, so let's see if X's overlap
                            if(firstNode.X - firstNode.Width/2.0f < secondNode.X + secondNode.Width/2.0f &&
                                firstNode.X + firstNode.Width/2.0f > secondNode.X - secondNode.Width/2.0f)
                            {
                                // they overlap, so link them!
                                firstNode.LinkTo(secondNode);
                            }
                        }
                    }
                }
            }
        }

    }
}
