using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls.Games
{
    public enum ConnectedJoinedState
    {
        NotConnected,
        Connected,
        ConnectedAndJoined
    }

    public class PlayerJoinViewItem : FrameworkElement
    {
        ConnectedJoinedState connectedJoinedState;

        public ConnectedJoinedState ConnectedJoinedState
        {
            get => connectedJoinedState;
            set
            {
                if(value != connectedJoinedState)
                {
                    connectedJoinedState = value;
                    UpdateState();
                    PushValueToViewModel();
                }
            }
        }

        public PlayerJoinViewItem() : base() { }

        public PlayerJoinViewItem(GraphicalUiElement visual) : base(visual) { }

        protected override void UpdateState()
        {
            const string category = "PlayerJoinCategoryState";
            switch(this.connectedJoinedState)
            {
                case ConnectedJoinedState.NotConnected:
                    Visual.SetProperty(category, "NotConnected");
                    break;
                case ConnectedJoinedState.Connected:
                    Visual.SetProperty(category, "Connected");

                    break;
                case ConnectedJoinedState.ConnectedAndJoined:
                    Visual.SetProperty(category, "ConnectedAndJoined");

                    break;
            }
            base.UpdateState();
        }
    }
}
