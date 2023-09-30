using FlatRedBall.Glue.MVVM;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.LoadRecentFilesPlugin.ViewModels
{
    internal class RecentItemViewModel : ViewModel
    {
        public string FullPath
        {
            get => Get<string>();
            set => Set(value);
        }

        [DependsOn(nameof(FullPath))]
        public string StrippedName => !string.IsNullOrEmpty(FullPath)
            ? FileManager.RemovePath(FullPath)
            : "";

        public bool IsFavorite
        {
            get => Get<bool>();
            set => Set(value);
        }

        public event Action RemoveClicked;

        public void HandleRemoveClicked() => RemoveClicked?.Invoke();

        [DependsOn(nameof(IsFavorite))]
        public BitmapImage FavoriteImage
        {
            get
            {
                string sourceName;
                if(IsFavorite)
                {
                    sourceName = "/Content/Icons/StarFilled.png";
                }
                else
                {
                    sourceName = "/Content/Icons/StarOutline.png";
                }

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(sourceName, UriKind.Relative);
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
    }
}
