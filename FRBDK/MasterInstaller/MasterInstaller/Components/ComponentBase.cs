using MasterInstaller.Components.Controls;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace MasterInstaller.Components
{
    public class EnableButtonEventArgs : EventArgs
    {
        public bool ButtonEnabled { get; set; }
        public string ButtonText { get; set; }
    }
    
    public abstract class ComponentBase
    {
        public event EventHandler NextClicked;

        BasePage wpfControl;       

        public BasePage MainControl
        {
            get
            {
                if (wpfControl == null)
                {
                    wpfControl = CreateControl();

                    wpfControl.NextClicked += delegate
                    {
                        NextClicked?.Invoke(this, null);
                    };

                }
                return wpfControl;
            }
        }

        public virtual async Task Show()
        {
        }

        protected abstract BasePage CreateControl();

        protected void OnNextClicked()
        {
            NextClicked?.Invoke(this, null);
        }
    }
}
