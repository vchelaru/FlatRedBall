using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;

using FlatRedBall.Math.Geometry;
using FlatRedBall.Math.Splines;

using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using FlatRedBall.Localization;

using System.Linq;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#endif

namespace GlueTestProject.Screens
{
	public partial class NodeNetworkScreen
	{

		void CustomInitialize()
		{
            this.LayeredNodeNetworkObject.LayerToDrawOn = this.LayerInstance;

            if(LayerInstance.Polygons.Contains(this.LayeredNodeNetworkObject.NodeVisibleRepresentation[0]) == false)
            {
                throw new Exception("Setting LayerToDrawOn after a NodeNetwork is visible does not move it to a Layer");
            }

            var node2 = NodeNetworkFile.Nodes.FirstOrDefault(item=>item.Name == "2");
            if(node2 == null)
            {
                throw new Exception("Could not find a node with the name 2");
            }

            node2.Active = false;
            var path = NodeNetworkFile.GetPath(NodeNetworkFile.Nodes.First(item => item.Name == "1"), NodeNetworkFile.Nodes.First(item => item.Name == "3"));
            if(path != null && path.Count != 0)
            {
                throw new Exception("No path should be found between nodes 1 and 3 because node 2 is not active.");
            }

            node2.Active = true;
            path = NodeNetworkFile.GetPath(NodeNetworkFile.Nodes.First(item => item.Name == "1"), NodeNetworkFile.Nodes.First(item => item.Name == "3"));
            if (path.Count == 0)
            {
                throw new Exception("Path should be found between nodes 1 and 3 now that node 2 is active.");
            }
        }

		void CustomActivity(bool firstTimeCalled)
		{
            if (!firstTimeCalled)
            {
                IsActivityFinished = true;
            }
		}

		void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
