using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.AnimationEditorForms.Data;
using FlatRedBall.AnimationEditorForms.Converters;
using FlatRedBall.Content.AnimationChain;

namespace FlatRedBall.AnimationEditorForms.Controls
{
    public partial class TileMapInfoWindow : UserControl
    {
        PropertyGridDisplayer mDisplayer;
        TileMapInformation mTileMapInformation;

        public TileMapInformation TileMapInformation
        {
            get
            {
                return mTileMapInformation;
            }
            set
            {
                mTileMapInformation = value;

                if (mTileMapInformation != null)
                {
                    mDisplayer.Instance = mTileMapInformation;
                    mDisplayer.PropertyGrid = this.propertyGrid1;
                }
                else
                {
                    mDisplayer.Instance = null;
                }
            }
        }

        public event EventHandler ValueChanged;

        public TileMapInfoWindow()
        {
            InitializeComponent();

            mDisplayer = new PropertyGridDisplayer();

            mDisplayer.ExcludeMember("TileWidth");
            mDisplayer.ExcludeMember("TileHeight");

            mDisplayer.IncludeMember("Cell Width", 
                typeof(string), 
                SetNumberOfTilesWide,
                GetTileWidth,
                new SpriteSheetCellWidthConverter());

            mDisplayer.IncludeMember("Cell Height",
                typeof(string),
                SetNumberOfTilesTall,
                GetTileHeight,
                new SpriteSheetCellWidthConverter());


        }

        public void SetNumberOfTilesWide(object sender, MemberChangeArgs args)
        {
            string asString = (string)args.Value;
            int valueToSet;

            if (asString.Contains("cells") && SelectedState.Self.SelectedTexture != null)
            {
                asString = asString.Replace(" cells", "");

                int cellsWide = int.Parse(asString);

                valueToSet = SelectedState.Self.SelectedTexture.Width / cellsWide;
            }
            else
            {
                int.TryParse((string)args.Value, out valueToSet);
                //valueToSet = int.Parse((string)args.Value);
            }
            if (TileMapInformation != null)
            {
                TileMapInformation.TileWidth = valueToSet;
            }
        }

        public void SetNumberOfTilesTall(object sender, MemberChangeArgs args)
        {
            string asString = (string)args.Value;
            int valueToSet;

            if (asString.Contains("cells") && SelectedState.Self.SelectedTexture != null)
            {
                asString = asString.Replace(" cells", "");

                int cellsTall = int.Parse(asString);

                valueToSet = SelectedState.Self.SelectedTexture.Height / cellsTall;
            }
            else
            {
                // If it fails that's okay...I think.
                int.TryParse((string)args.Value, out valueToSet);
            }
            if (TileMapInformation != null)
            {
                TileMapInformation.TileHeight = valueToSet;
            }
        }

        object GetTileWidth()
        {
            return TileMapInformation.TileWidth;
        }

        object GetTileHeight()
        {
            return TileMapInformation.TileHeight;
        }
        
        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (ValueChanged != null)
            {
                ValueChanged(this, null);
            }
        }

    }
}
