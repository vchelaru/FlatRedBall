using System.Windows.Media;

namespace OfficialPlugins.TreeViewPlugin.ViewModels
{
    class BookmarkViewModel
    {
        public string Text { get; set; }
        public ImageSource ImageSource { get; set; }

        public override string ToString() => Text;
    }
}
