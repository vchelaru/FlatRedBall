using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall;

namespace EditorObjects.NodeNetworks
{
    public class NodeNetworkEditorManager
    {
        #region Fields

        NodeNetwork mNodeNetwork = null;

        #endregion


        public void Activity()
        {
            if (mNodeNetwork != null)
            {
                mNodeNetwork.UpdateShapes();
            }
        }

        public void AddNodeNetworkMenus(MenuStrip menuStripToAddTo)
        {
            MenuItem menuItem = menuStripToAddTo.AddItem("NodeNetwork");

            menuItem.AddItem("Load NodeNetwork...").Click += new GuiMessage(LoadNodeNetworkClick);
            menuItem.AddItem("Close NodeNetwork").Click += new GuiMessage(CloseNodeNetworkClick);
        }

        void CloseNodeNetworkClick(Window callingWindow)
        {
            if (mNodeNetwork != null)
            {
                mNodeNetwork.Visible = false;
                mNodeNetwork = null;
            }
        }

        void LoadNodeNetworkClick(Window callingWindow)
        {
            FileWindow fileWindow = GuiManager.AddFileWindow();
            fileWindow.SetToLoad();
            fileWindow.Filter = "NodeNetwork XML File (*.nntx)|*.nntx";

            fileWindow.OkClick += new GuiMessage(OnLoadNodeNetworkOk);
        }

        void OnLoadNodeNetworkOk(Window callingWindow)
        {
            CloseNodeNetworkClick(null);

            string fileName = ((FileWindow)callingWindow).Results[0];

            mNodeNetwork = FlatRedBallServices.Load<NodeNetwork>(fileName);

            mNodeNetwork.Visible = true;
        }



    }
}
