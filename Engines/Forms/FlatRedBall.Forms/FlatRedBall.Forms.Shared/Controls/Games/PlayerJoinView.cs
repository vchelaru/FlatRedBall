using FlatRedBall.Forms.Managers;
using FlatRedBall.Input;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;

namespace FlatRedBall.Forms.Controls.Games
{
    public class PlayerJoinView : FrameworkElement
    {
        #region Fields/Properties

        // Should we have a FrameworkElementTemplate?
        GraphicalUiElement innerPanel;
        public GraphicalUiElement InnerPanel => innerPanel;

        protected List<PlayerJoinViewItem> PlayerJoinViewItemsInternal = new List<PlayerJoinViewItem>();

        ReadOnlyCollection<PlayerJoinViewItem> playerJoinViewItemsReadOnly;
        public ReadOnlyCollection<PlayerJoinViewItem> PlayerJoinViewItems
        {
            get
            {
                if (playerJoinViewItemsReadOnly == null)
                {
                    playerJoinViewItemsReadOnly = new ReadOnlyCollection<PlayerJoinViewItem>(PlayerJoinViewItemsInternal);
                }
                return playerJoinViewItemsReadOnly;
            }
        }

        public List<Xbox360GamePad.Button> JoinButtons { get; private set; } = new List<Xbox360GamePad.Button>()
        {
            Xbox360GamePad.Button.Start
        };
        public List<Xbox360GamePad.Button> UnjoinButtons { get; private set; } = new List<Xbox360GamePad.Button>()
        {
            Xbox360GamePad.Button.Back
        };

        public List<Keys> JoinKeys { get; private set; } = new List<Keys>()
        {
            Keys.Enter
        };
        public List<Keys> UnjoinKeys { get; private set; } = new List<Keys>()
        {
            Keys.Escape
        };

        public static string KeyboardName { get; set; } = "Keyboard";

        #endregion

        public PlayerJoinView() : base() { }

        public PlayerJoinView(GraphicalUiElement visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            innerPanel = Visual.GetGraphicalUiElementByName("InnerPanelInstance");
            innerPanel.Children.CollectionChanged += HandleInnerPanelCollectionChanged;
#if DEBUG
            if (innerPanel == null)
            {
                throw new Exception(
                    $"This PlayerJoinView was created with a Gum component ({Visual?.ElementSave}) " +
                    "that does not have an instance called 'InnerPanelInstance'. A 'InnerPanelInstance' instance must be added.");
            }
#endif

            RefreshPlayerJoinViewItemsList();

            for(int i = 0; i < PlayerJoinViewItems.Count; i++)
            {
                UpdateJoinedStateForIndex(i, true);
            }

            Visual.RemovedFromGuiManager += HandleRemovedFromGuiManager;

            base.ReactToVisualChanged();
        }

        private void HandleRemovedFromGuiManager(object sender, EventArgs e)
        {
            FrameworkElementManager.Self.RemoveFrameworkElement(this);
        }

        private void HandleInnerPanelCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshPlayerJoinViewItemsList();

            for (int i = 0; i < PlayerJoinViewItems.Count; i++)
            {
                UpdateJoinedStateForIndex(i, true);
            }
        }

        public void SubscribeToGamepadEvents()
        {
            InputManager.ControllerConnectionEvent += HandleControllerConnected;

            RefreshPlayerJoinViewItemsList();

            for (int i = 0; i < PlayerJoinViewItems.Count; i++)
            {
                UpdateJoinedStateForIndex(i, true);
            }

            FrameworkElementManager.Self.AddFrameworkElement(this);
        }

        private void HandleControllerConnected(object sender, InputManager.ControllerConnectionEventArgs e)
        {
            var index = e.PlayerIndex;

            UpdateJoinedStateForIndex(index, true);
        }

        private void UpdateJoinedStateForIndex(int index, bool force)
        {
            // Make this lazy, allowing the user to add and remove items whenever they want. By checking
            // and obtaining references as late as possible we handle the most number of cases of creating
            // controls.
            if(index > PlayerJoinViewItemsInternal.Count)
            {
                RefreshPlayerJoinViewItemsList();
            }

            // Now that the items are refreshed, try to update the specific one:
            if (index <= PlayerJoinViewItemsInternal.Count)
            {
                var item = PlayerJoinViewItemsInternal[index];

                var isConnected = index <= InputManager.Xbox360GamePads.Length &&
                    InputManager.Xbox360GamePads[index].IsConnected;

                if(isConnected)
                {
                    var gamepad = InputManager.Xbox360GamePads[index];
                    item.ControllerDisplayName = gamepad.Capabilities.DisplayName;
                    item.ConnectedJoinedState = ConnectedJoinedState.Connected;
                    item.GamepadLayout = gamepad.GamepadLayout;
                }
                else if(item.IsUsingKeyboardAsBackup)
                {
                    item.ConnectedJoinedState = ConnectedJoinedState.Connected;
                    item.ControllerDisplayName = KeyboardName; // should it be something else? Localized? Need to think about this...

                    item.GamepadLayout = GamepadLayout.Keyboard;
                }
                else
                {
                    item.ConnectedJoinedState = ConnectedJoinedState.NotConnected;
                    item.GamepadLayout = GamepadLayout.Unknown;
                }


                if (force)
                {
                    item.ForceUpdateState();
                }
            }
        }

        private void RefreshPlayerJoinViewItemsList()
        {
            PlayerJoinViewItemsInternal.Clear();

            if(innerPanel != null)
            {
                foreach(var item in innerPanel.Children)
                {
                    var playerJoinViewItem = (item as GraphicalUiElement)?.FormsControlAsObject as PlayerJoinViewItem;

                    if(playerJoinViewItem != null)
                    {
                        PlayerJoinViewItemsInternal.Add(playerJoinViewItem);
                    }
                }
            }
        }

        public override void Activity()
        {
            var gamepads = InputManager.Xbox360GamePads;

            for(int i = 0; i < gamepads.Length; i++)
            {
                var gamepad = gamepads[i];

                if(gamepad.IsConnected && i < PlayerJoinViewItemsInternal.Count && 
                    // Keyboard is handled down below
                    PlayerJoinViewItemsInternal[i].GamepadLayout != GamepadLayout.Keyboard)
                {
                    TestJoinUnjoin(gamepad, i);
                }
            }

            foreach(var item in PlayerJoinViewItemsInternal)
            {
                if(item.GamepadLayout == GamepadLayout.Keyboard)
                {
                    TestJoinUnjoinWithKeyboard(item);
                }
            }

            base.Activity();
        }

        private void TestJoinUnjoin(Xbox360GamePad gamepad, int index)
        {
            var joinItem = PlayerJoinViewItemsInternal[index];

            if(joinItem.ConnectedJoinedState == ConnectedJoinedState.Connected)
            {
                foreach(var button in JoinButtons)
                {
                    if(gamepad.ButtonPushed(button))
                    {
                        joinItem.ConnectedJoinedState = ConnectedJoinedState.ConnectedAndJoined;
                        break;
                    }
                }
            }
            else if(joinItem.ConnectedJoinedState == ConnectedJoinedState.ConnectedAndJoined)
            {
                foreach (var button in UnjoinButtons)
                {
                    if(gamepad.ButtonPushed(button))
                    {
                        joinItem.ConnectedJoinedState = ConnectedJoinedState.Connected;
                    }
                }
            }

        }

        private void TestJoinUnjoinWithKeyboard(PlayerJoinViewItem joinItem)
        {
            var keyboard = InputManager.Keyboard;

            if (joinItem.ConnectedJoinedState == ConnectedJoinedState.Connected)
            {
                foreach (var key in JoinKeys)
                {
                    if ( keyboard.KeyPushed(key))
                    {
                        joinItem.ConnectedJoinedState = ConnectedJoinedState.ConnectedAndJoined;
                        break;
                    }
                }
            }
            else if (joinItem.ConnectedJoinedState == ConnectedJoinedState.ConnectedAndJoined)
            {
                foreach (var key in UnjoinKeys)
                {
                    if (keyboard.KeyPushed(key))
                    {
                        joinItem.ConnectedJoinedState = ConnectedJoinedState.Connected;
                    }
                }
            }

        }
    }
}
