using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.SpritePlugin.ViewModels
{
    public class TextureCoordinateSelectionViewModel : ViewModel
    {
        public decimal LeftTexturePixel
        {
            get => Get<decimal>();
            set => Set(value);
        }

        public decimal TopTexturePixel
        {
            get => Get<decimal>();
            set => Set(value);
        }

        public decimal SelectedWidthPixels
        {
            get => Get<decimal>();
            set => Set(value);
        }

        public decimal SelectedHeightPixels
        {
            get => Get<decimal>();
            set => Set(value);
        }
    }
}
