using FlatRedBall.Input;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;

namespace FlatRedBall.Forms.Controls.Games
{
    public class PlayerJoinView : FrameworkElement
    {
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

            base.ReactToVisualChanged();
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
                    item.ConnectedJoinedState = ConnectedJoinedState.Connected;
                }
                else
                {
                    item.ConnectedJoinedState = ConnectedJoinedState.NotConnected;
                }

                if(force)
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
    }
}
