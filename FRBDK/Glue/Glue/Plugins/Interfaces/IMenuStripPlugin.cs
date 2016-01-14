using System.Windows.Forms;

namespace FlatRedBall.Glue.Plugins.Interfaces
{
    public interface IMenuStripPlugin : IPlugin
    {
        void InitializeMenu(MenuStrip menuStrip);
    }
}
