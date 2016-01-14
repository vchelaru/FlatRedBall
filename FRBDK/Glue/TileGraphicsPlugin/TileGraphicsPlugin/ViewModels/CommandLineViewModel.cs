using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using TMXGlueLib;

namespace TileGraphicsPlugin.ViewModels
{
    #region Enums

    public enum LayerVisibility
    {
        Ignore,
        Match,
        Skip
    }

    public enum BuiltFileType
    {
        Scene,
        TiledBinary,
        NodeNetwork,
        ShapeCollection,
        CSV
    }

    
    #endregion

    public class CommandLineViewModel : ViewModel
    {
        #region Fields

        float mScale = 1;
        float mXOffset;
        float mYOffset;
        float mZOffset;
        LayerVisibility mLayerVisibility;
        bool mCopyImages = true;

        bool mRequireTile = true;

        string mLayerName;

        TiledMapSave.CSVPropertyType mCsvPropertyType;

        
        ReferencedFileSave mReferencedFileSave;

        bool mSuppressBuild;

        #endregion

        #region Properties

        #region Visibility

        public Visibility ScaleVisible
        {
            get
            {
                var toReturn = BuiltFileType == ViewModels.BuiltFileType.Scene ||
                    BuiltFileType == ViewModels.BuiltFileType.TiledBinary;

                return toReturn.ToVisibility();
            }
        }

        public Visibility OffsetVisible
        {
            get
            {
                var bft = BuiltFileType;

                var toReturn = bft == ViewModels.BuiltFileType.NodeNetwork ||
                    bft == ViewModels.BuiltFileType.Scene ||
                    bft == ViewModels.BuiltFileType.ShapeCollection ||
                    bft == ViewModels.BuiltFileType.TiledBinary;

                return toReturn.ToVisibility();
            }
        }

        public Visibility LayerVisibilityVisible
        {
            get
            {
                var bft = BuiltFileType;

                var toReturn = bft == ViewModels.BuiltFileType.Scene ||
                    bft == ViewModels.BuiltFileType.TiledBinary ||
                    bft == ViewModels.BuiltFileType.NodeNetwork ||
                    bft == ViewModels.BuiltFileType.ShapeCollection;

                return toReturn.ToVisibility();
            }
        }

        public Visibility CopyImagesVisible
        {
            get
            {
                var bft = BuiltFileType;

                var toReturn = bft == ViewModels.BuiltFileType.Scene ||
                    bft == ViewModels.BuiltFileType.TiledBinary;

                return toReturn.ToVisibility();
            }
        }

        public Visibility RequireTileVisible
        {
            get
            {
                var toReturn = BuiltFileType == ViewModels.BuiltFileType.NodeNetwork;

                return toReturn.ToVisibility();
            }
        }


        public Visibility LayerNameVisible
        {
            get
            {
                var toReturn = BuiltFileType == ViewModels.BuiltFileType.ShapeCollection ||
                    BuiltFileType == ViewModels.BuiltFileType.CSV;

                return toReturn.ToVisibility();
            }
        }

        public Visibility CsvPropertyTypeVisible
        {
            get
            {
                var toReturn = BuiltFileType == ViewModels.BuiltFileType.CSV;

                return toReturn.ToVisibility();
            }
        }


        #endregion

        #region Command line properties

        public float Scale
        {
            get { return mScale; }
            set { ChangeAndNotify(ref mScale, value, "Scale"); UpdateCommandLineString(); }
        }

        public float XOffset
        {
            get { return mXOffset; }
            set { ChangeAndNotify(ref mXOffset, value, "XOffset"); UpdateCommandLineString(); }
        }

        public float YOffset
        {
            get { return mYOffset; }
            set { ChangeAndNotify(ref mYOffset, value, "YOffset"); UpdateCommandLineString(); }
        }

        public float ZOffset
        {
            get { return mZOffset; }
            set { ChangeAndNotify(ref mZOffset, value, "ZOffset"); UpdateCommandLineString(); }
        }

        public LayerVisibility LayerVisibility
        {
            get { return mLayerVisibility; }
            set { ChangeAndNotify(ref mLayerVisibility, value, "LayerVisibility"); UpdateCommandLineString(); }
        }


        public bool CopyImages
        {
            get { return mCopyImages; }
            set { ChangeAndNotify(ref mCopyImages, value, "CopyImages"); UpdateCommandLineString(); }
        }

        public bool RequireTile
        {
            get { return mRequireTile; }
            set { ChangeAndNotify(ref mRequireTile, value, "RequireTile"); UpdateCommandLineString(); }
        }

        public string LayerName
        {
            get { return mLayerName; }
            set { ChangeAndNotify(ref mLayerName, value, "LayerName"); UpdateCommandLineString(); }
        }

        public TiledMapSave.CSVPropertyType CsvPropertyType
        {
            get { return mCsvPropertyType; }
            set { ChangeAndNotify(ref mCsvPropertyType, value, "CsvPropertyType"); UpdateCommandLineString(); }
        }

        #endregion

        public IEnumerable LayerVisibilityOptions
        {
            get
            {
                return Enum.GetValues(typeof(LayerVisibility));
            }
        }
        public IEnumerable CsvPropertyTypeOptions
        {
            get
            {
                return Enum.GetValues(typeof(TiledMapSave.CSVPropertyType));
            }
        }






        public BuiltFileType BuiltFileType
        {
            get
            {
                if(ReferencedFileSave == null)
                {
                    return ViewModels.BuiltFileType.Scene;
                }

                string extension = FileManager.GetExtension(ReferencedFileSave.Name);

                switch(extension)
                {
                    case "scnx":
                        return ViewModels.BuiltFileType.Scene;
                        //break;
                    case "tilb":
                        return ViewModels.BuiltFileType.TiledBinary;
                        //break;
                    case "shcx":
                        return ViewModels.BuiltFileType.ShapeCollection;
                        //break;
                    case "nntx":
                        return ViewModels.BuiltFileType.NodeNetwork;
                        //break;
                    case "csv":
                        return ViewModels.BuiltFileType.CSV;
                        //break;

                }

                throw new NotImplementedException();
            }
        }

        public ReferencedFileSave ReferencedFileSave
        {
            get
            {
                return mReferencedFileSave;
            }
            set
            {
                mReferencedFileSave = value;

                if(mReferencedFileSave != null)
                {
                    mSuppressBuild = true;

                    CommandLineString = mReferencedFileSave.AdditionalArguments;

                    NotifyPropertyChanged("CommandLineString");

                    NotifyPropertyChanged("ScaleVisible");
                    NotifyPropertyChanged("OffsetVisible");
                    NotifyPropertyChanged("LayerVisibilityVisible");
                    NotifyPropertyChanged("CopyImagesVisible");
                    NotifyPropertyChanged("RequireTileVisible");
                    NotifyPropertyChanged("LayerNameVisible");
                    NotifyPropertyChanged("CsvPropertyTypeVisible");

                    mSuppressBuild = false;

                }
            }
        }

        public void UpdateCommandLineString()
        {
            NotifyPropertyChanged("CommandLineString");

            var file = ReferencedFileSave;
            if (file != null && !mSuppressBuild)
            {
                if (CommandLineChanged != null)
                {
                    CommandLineChanged();
                }

            }
        }

        public string CommandLineString
        {
            set
            {
                mScale = 1;
                mLayerVisibility = ViewModels.LayerVisibility.Ignore;
                mXOffset = 0;
                mYOffset = 0;
                mZOffset = 0;

                mCopyImages = true;

                if(value != null)
                {
                    var character = new char[]{' '};
                    var strings = value.Split(character, StringSplitOptions.RemoveEmptyEntries);

                    foreach(var arg in strings)
                    {
                        ApplySingleArgument(arg);

                    }

                }
            }
            get
            {
                var stringBuiler = new StringBuilder();

                if (ScaleVisible == Visibility.Visible)
                {
                    stringBuiler.Append("scale=" + Scale.ToString() + " ");
                }

                if (LayerVisibilityVisible == Visibility.Visible)
                {
                    stringBuiler.Append("layervisibilitybehavior=" + LayerVisibility.ToString() + " ");
                }

                if (OffsetVisible == Visibility.Visible)
                {
                    stringBuiler.Append("offset=" + XOffset.ToString() + "," + YOffset.ToString() + "," + ZOffset.ToString() + " ");
                }

                if (CopyImagesVisible == Visibility.Visible)
                {
                    stringBuiler.Append("copyimages=" + CopyImages.ToString().ToLowerInvariant() + " ");
                }

                if (RequireTileVisible == Visibility.Visible)
                {
                    stringBuiler.Append("requiretile=" + RequireTile.ToString().ToLowerInvariant() + " ");
                }

                if (LayerNameVisible == Visibility.Visible && !string.IsNullOrEmpty(LayerName))
                {
                    stringBuiler.Append("layername=" + LayerName + " ");
                }

                if (CsvPropertyTypeVisible == Visibility.Visible)
                {
                    stringBuiler.Append("type=" + CsvPropertyType.ToString());
                }

                return stringBuiler.ToString();
            }
        }

        #endregion

        public event Action CommandLineChanged;

        
        
        #region Methods

        private void ApplySingleArgument(string arg)
        {
            var argToLower = arg.ToLowerInvariant();

            if (argToLower.StartsWith("scale="))
            {
                try
                {
                    float scaleValue = 1;
                    scaleValue = StringFunctions.GetFloatAfter("scale=", arg);
                    Scale = scaleValue;
                }
                catch
                {
                    // no big deal
                }
            }
            else if (argToLower.StartsWith("layervisibilitybehavior="))
            {
                try
                {
                    string valueAfter = arg.Substring("layervisibilitybehavior=".Length);
                    var valueAsEnum = (LayerVisibility)Enum.Parse(typeof(LayerVisibility), valueAfter, ignoreCase:true);
                    LayerVisibility = valueAsEnum;
                }
                catch
                {
                    // no big deal
                }

            }
            else if (argToLower.StartsWith("offset="))
            {
                try
                {
                    string substring = arg.Substring("offset=".Length);

                    var splitSubstring = substring.Split(',');

                    XOffset = float.Parse(splitSubstring[0]);
                    YOffset = float.Parse(splitSubstring[1]);
                    ZOffset = float.Parse(splitSubstring[2]);
                }
                catch
                {
                    // no big deal
                }

            }

            else if (argToLower.StartsWith("copyimages="))
            {
                try
                {
                    string substring = arg.Substring("copyimages=".Length);

                    if (substring.ToLowerInvariant() == "false")
                    {
                        CopyImages = false;
                    }
                    else
                    {
                        CopyImages = true;
                    }
                }
                catch
                {
                    // no big deal
                }

            }
            else if(argToLower.StartsWith("requiretile="))
            {
                try
                {
                    string substring = arg.Substring("requiretile=".Length);

                    if (substring.ToLowerInvariant() == "false")
                    {
                        RequireTile = false;
                    }
                    else
                    {
                        RequireTile = true;
                    }
                }
                catch
                {
                    // no big deal
                }
            }
            else if (argToLower.StartsWith("layername="))
            {
                try
                {
                    string substring = arg.Substring("layername=".Length);

                    LayerName = substring;
                }
                catch
                {
                    // no big deal
                }
            }
            else if(argToLower.StartsWith("type="))
            {
                try
                {
                    string valueAfter = arg.Substring("type=".Length);
                    var valueAsEnum = (TiledMapSave.CSVPropertyType)Enum.Parse(typeof(TiledMapSave.CSVPropertyType), valueAfter, ignoreCase:true);
                    CsvPropertyType = valueAsEnum;
                }
                catch
                {
                    // no big deal
                }
            }
        }

        #endregion
    }

    #region Extension Method Classes

    static class BoolExtensions
    {
        public static System.Windows.Visibility ToVisibility(this bool value)
        {
            if(value)
            {
                return System.Windows.Visibility.Visible;
            }
            else
            {
                return System.Windows.Visibility.Collapsed;
            }
        }
    }

    #endregion
}
