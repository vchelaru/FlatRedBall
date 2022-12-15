using FlatRedBall.Input;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls.Games
{
    #region Enums

    public enum ConnectedJoinedState
    {
        NotConnected,
        Connected,
        ConnectedAndJoined
    }

    #endregion

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

        GamepadLayout gamepadLayout;
        public GamepadLayout GamepadLayout
        {
            get => gamepadLayout;
            set
            {
                if (value != gamepadLayout)
                { 
                    gamepadLayout= value;
                    UpdateState();
                    PushValueToViewModel();
                }
            }
        }

        public string ControllerDisplayName
        {
            get => controllerDisplayNameTextInstance?.RawText;
            set
            {
                if(controllerDisplayNameTextInstance != null)
                {
                    controllerDisplayNameTextInstance.RawText = value;
                }
            }
        }

        RenderingLibrary.Graphics.Text controllerDisplayNameTextInstance;

        bool isUsingKeyboardAsBackup;
        public bool IsUsingKeyboardAsBackup
        {
            get => isUsingKeyboardAsBackup;
            set
            {
                if(value != isUsingKeyboardAsBackup)
                {
                    isUsingKeyboardAsBackup = value;

                    UpdateState();
                }
            }
        }

        public PlayerJoinViewItem() : base() { }

        public PlayerJoinViewItem(GraphicalUiElement visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            if(Visual != null)
            {
                var textComponent = base.Visual.GetGraphicalUiElementByName("ControllerDisplayNameTextInstance");
                controllerDisplayNameTextInstance = textComponent?.RenderableComponent as RenderingLibrary.Graphics.Text;

                UpdateState();
                Visual.RemovedFromGuiManager += HandleRemoveFromManagers;
            }

            base.ReactToVisualChanged();
        }

        private void HandleRemoveFromManagers(object sender, EventArgs e)
        {

        }

        internal void ForceUpdateState() => UpdateState();

        protected override void UpdateState()
        {
            if(this.ConnectedJoinedState == ConnectedJoinedState.NotConnected && IsUsingKeyboardAsBackup)
            {
                ConnectedJoinedState = ConnectedJoinedState.Connected;
                ControllerDisplayName = PlayerJoinView.KeyboardName;
                GamepadLayout = GamepadLayout.Keyboard;
            }

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

            const string gamepadLayoutCategory = "GamepadLayoutCategoryState";
            Visual.SetProperty(gamepadLayoutCategory, gamepadLayout.ToString());

            base.UpdateState();
        }
    }
}
