using System.Collections.Generic;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using FlatRedBall.IO;
using System.IO;

namespace TMXGlueLib
{
    /// <remarks/>
    [XmlType(AnonymousType = true)]
// ReSharper disable InconsistentNaming
    [XmlRoot("mapTileset")]
    public class Tileset
// ReSharper restore InconsistentNaming
    {
        private TilesetImage[] _imageField;

        private mapTilesetTileOffset[] _tileOffsetField;

        private string _sourceField;

        [XmlIgnore]
        public string SourceDirectory
        {
            get
            {
                if (_sourceField != null && _sourceField.Contains("\\"))
                {
                    // add 1 to include the ending directory
                    return _sourceField.Substring(0, _sourceField.LastIndexOf('\\') + 1);
                }
                else
                {
                    return ".";
                }
            }
        }

        public static bool ShouldLoadValuesFromSource = true;

        [XmlAttribute("source", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string Source
        {
            get
            {
                return _sourceField;
            }
            set
            {
                this._sourceField = value;

                if (ShouldLoadValuesFromSource)
                {
                    LoadValuesFromSource();
                }
            }
        }

        [XmlIgnore]
        public bool IsShared
        {
            get
            {
                return !string.IsNullOrEmpty(Source);
            }
        }

        private void LoadValuesFromSource()
        {
            if (!string.IsNullOrEmpty(this._sourceField))
            {
                _sourceField = _sourceField.Replace("/", "\\");

                tileset xts = null;

                try
                {

                    xts = FileManager.XmlDeserialize<tileset>(_sourceField);
                }
                catch (FileNotFoundException)
                {
                    string fileAttemptedToLoad = _sourceField;
                    if (FileManager.IsRelative(_sourceField))
                    {
                        fileAttemptedToLoad = FileManager.RelativeDirectory + _sourceField;
                    }

                    string message = "Could not find the shared tsx file \n" + fileAttemptedToLoad + 
                        "\nIf this is a relative file name, then the loader will use " +
                        "the FileManager's RelativeDirectory to make the file absolute.  Therefore, be sure to set the FileManger's RelativeDirectory to the file represented by " +
                        "this fileset before setting this property if setting this property manually.";


                    throw new FileNotFoundException(message);
                }

                if (xts.image != null)
                {

                    Images = new TilesetImage[xts.image.Length];

                    Parallel.For(0, xts.image.Length, count =>
                    {
                        this.Images[count] = new TilesetImage
                        {
                            Source = xts.image[count].source,
                            height = xts.image[count].height != 0 ? xts.image[count].height : xts.tileheight,
                            width = xts.image[count].width != 0 ? xts.image[count].width : xts.tilewidth
                        };
                    });
                }
                this.Name = xts.name;
                this.Margin = xts.margin;
                this.Spacing = xts.spacing;
                this.Tileheight = xts.tileheight;
                this.Tilewidth = xts.tilewidth;
                this.Tiles = xts.tile;
            }
        }

        /// <remarks/>
        [XmlElement("tileoffset", Form = System.Xml.Schema.XmlSchemaForm.Unqualified, Order = 1)]
        public mapTilesetTileOffset[] Tileoffset
        {
            get
            {
                return this._tileOffsetField;
            }
            set
            {
                if (this._tileOffsetField == null || this._tileOffsetField.Length == 0)
                {
                    this._tileOffsetField = value;
                }
            }
        }


        /// <remarks/>
        [XmlElement("image", Form = System.Xml.Schema.XmlSchemaForm.Unqualified, Order = 2)]
        public TilesetImage[] Images
        {
            get
            {
                return this._imageField;
            }
            set
            {
                if (this._imageField == null || this._imageField.Length == 0)
                {
                    this._imageField = value;
                }
            }
        }


        public bool ShouldSerializeImage()
        {
            return string.IsNullOrEmpty(this.Source);
        }


        [XmlArray("terraintypes", Form = System.Xml.Schema.XmlSchemaForm.Unqualified, Order = 3)]
        public List<mapTilesetTerrain> terraintypes = new List<mapTilesetTerrain>();

        public bool ShouldSerializeterraintypes()
        {
            return string.IsNullOrEmpty(this.Source);
        }

        [XmlElement("tile", Form = System.Xml.Schema.XmlSchemaForm.Unqualified, Order = 4)]
        public List<mapTilesetTile> Tiles = new List<mapTilesetTile>();
        public bool ShouldSerializeTiles()
        {
            return string.IsNullOrEmpty(this.Source);
        }
        //{
        //    get
        //    {
        //        return this.tileField;
        //    }
        //    set
        //    {
        //        if (this.tileField != null && this.tileField.Length > 0)
        //        {
        //            return;
        //        }
        //        else
        //        {
        //            this.tileField = value;
        //        }
        //    }
        //}

        public void RefreshTileDictionary()
        {
            _tileDictionaryField = null;
        }


        private IDictionary<uint, mapTilesetTile> _tileDictionaryField;

        [XmlIgnore]
        public IDictionary<uint, mapTilesetTile> TileDictionary
        {
            get
            {
                lock (this)
                {
                    if (_tileDictionaryField == null)
                    {
                        _tileDictionaryField = new ConcurrentDictionary<uint, mapTilesetTile>();

                        if (Tiles != null)
                        {
                            //Parallel.ForEach(tile, (t) =>
                            //            {
                            //                if (t != null && !tileDictionaryField.ContainsKey((uint)t.id + 1))
                            //                {
                            //                    tileDictionaryField.Add((uint)t.id + 1, t);
                            //                }
                            //            });

                            foreach (var t in Tiles)
                            {
                                uint key = (uint)t.id + 1;
                                if (!_tileDictionaryField.ContainsKey(key))
                                {
                                    _tileDictionaryField.Add(key, t);
                                }
                            }
                        }

                        return _tileDictionaryField;

                    }
                    else
                    {
                        return _tileDictionaryField;
                    }
                }

            }
        }



        /// <remarks/>
        [XmlAttribute("firstgid")]
        public uint Firstgid
        {
            get;
            set;
        }

        /// <remarks/>
        [XmlAttribute("name")]
        public string Name
        {
            get;
            set;
        }

        public bool ShouldSerializeName()
        {
            return string.IsNullOrEmpty(this.Source);
        }

        /// <remarks/>
        [XmlAttribute("tilewidth")]
        public int Tilewidth
        {
            get;
            set;
        }

        public bool ShouldSerializeTilewidth()
        {
            return string.IsNullOrEmpty(this.Source);
        }

        /// <remarks/>
        [XmlAttribute("tileheight")]
        public int Tileheight
        {
            get;
            set;
        }

        public bool ShouldSerializeTileheight()
        {
            return string.IsNullOrEmpty(this.Source);
        }

        /// <remarks/>
        [XmlAttribute("spacing")]
        public int Spacing
        {
            get;
            set;
        }

        public bool ShouldSerializeSpacing()
        {
            return string.IsNullOrEmpty(this.Source);
        }

        /// <remarks/>
        [XmlAttribute("margin")]
        public int Margin
        {
            get;
            set;
        }

        public bool ShouldSerializeMargin()
        {
            return string.IsNullOrEmpty(this.Source);
        }

        public override string ToString()
        {
            string toReturn = this.Name;

            if (!string.IsNullOrEmpty(Source))
            {
                string sourceWithoutPath = FileManager.RemovePath(Source);
                toReturn += " (" + sourceWithoutPath + ")";
            }

            return toReturn;
        }
    }

    public class mapTilesetTerrain
    {
        public string name { get; set; }
        public int tile { get; set; }
    }
}
