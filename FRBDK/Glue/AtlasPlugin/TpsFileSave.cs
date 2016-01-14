using System.IO;
using System.Xml.Serialization;
using FlatRedBall.IO;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace AtlasPlugin
{
    [System.Serializable()]
    [XmlType(IncludeInSchema = false)]
    public enum ItemsChoiceType1
    {
        @double,
        @enum,
        @false,
        @int,
        key,
        @string,
        @struct,
        @uint,
    }

    [System.Serializable()]
    [XmlType(IncludeInSchema = false)]
    public enum ItemsChoiceType2
    {
        QSize,
        array,
        @enum,
        @false,
        filename,
        @int,
        key,
        map,
        @string,
        @struct,
        @true,
        @uint,
    }

    [System.Serializable()]
    [XmlType(IncludeInSchema = false)]
    public enum ItemsChoiceType
    {
        QSize,
        @double,
        @false,
        key,
        @string,
    }

    public class TpsLoadResult
    {
        public string ErrorMessage
        {
            get;
            set;
        }

        public string MissingFile
        {
            get;
            private set;
        }
    }

    [System.Serializable()]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false, ElementName = "data")]
    public class TpsFileSave
    {
        #region Fields
        private dataStruct _structField;
        private string _versionField;
        #endregion

        #region Properties

        public dataStruct @struct
        {
            get { return _structField; }
            set { _structField = value; }
        }

        [XmlIgnore]
        public IEnumerable<string> AtlasFilters
        {
            get
            {
                var autoSdSettings = AutoSdSettings;

                if(autoSdSettings != null && autoSdSettings.@struct != null)
                { 
                    foreach(var atlasEntry in autoSdSettings.@struct)
                    {
                        var spriteFilterIndex = IndexIn("spriteFilter", atlasEntry.Items);

                        if(spriteFilterIndex != -1)
                        {
                            var toReturn = atlasEntry.Items[spriteFilterIndex + 1] as string;

                            yield return toReturn;
                        }
                    }
                }
            }
        }

        [XmlAttribute()]
        public string Version
        {
            get { return _versionField; }
            set { _versionField = value; }
        }

        [XmlIgnore]
        public string Filename
        {
            get
            {
                var index = IndexInItems("fileName");

                if (index != -1) return @struct.Items[index + 1] as string;

                return null;
            }
            set
            {
                var index = IndexInItems("fileName");

                if (index != -1) @struct.Items[index + 1] = value;
            }
        }
        private dataStructArray AutoSdSettings
        {
            get
            {
                var indexOfAutoSDSettings = IndexInItems("autoSDSettings");

                if (indexOfAutoSDSettings != -1)
                {
                    var array = _structField.Items[indexOfAutoSDSettings + 1] as dataStructArray;

                    return array;
                }

                return null;
            }
        }

        private dataStructMap DataFileNames
        {
            get
            {
                var indexOfFileList = IndexInItems("dataFileNames");

                if (indexOfFileList != -1)
                {
                    var array = _structField.Items[indexOfFileList + 1] as dataStructMap;

                    return array;
                }

                return null;
            }
        }

        private string TextureFileNames
        {
            get
            {
                var indexOfTextureFiles = IndexInItems("textureFileName");

                if (indexOfTextureFiles != -1)
                {
                    var fileName = _structField.Items[indexOfTextureFiles + 1] as string;

                    return fileName;
                }

                return null;
            }
            set
            {
                var indexOfTextureFiles = IndexInItems("textureFileName");

                if (indexOfTextureFiles != -1)
                {
                    _structField.Items[indexOfTextureFiles + 1] = value;
                }
            }
        }

        private dataStructArray SmartFolders
        {
            get
            {
                var indexOfSmartFolders = IndexInItems("fileList");

                if (indexOfSmartFolders != -1)
                {
                    var smartFolders = _structField.Items[indexOfSmartFolders + 1] as dataStructArray;

                    return smartFolders;
                }

                return null;
            }
            set
            {
                var indexOfSmartFolders = IndexInItems("fileList");

                if (indexOfSmartFolders != -1) _structField.Items[indexOfSmartFolders + 1] = value;
            }
        }
        #endregion

        #region Constructor

        #endregion

        #region Methods
        /// <summary>
        /// Creates a tps file from scratch.
        /// </summary>
        internal void SetDefaultValues()
        {
            _versionField = "1.0";
            _structField = new dataStruct {type = "Settings"};

            var structField = new List<object>(); 
            var elementName = new List<ItemsChoiceType2>();
    
            FillStruct(structField, elementName);
            _structField.Items = structField.ToArray();
            _structField.ItemsElementName = elementName.ToArray();

            SetupFile();
        }

        /// <summary>
        /// Fills the lists of element names and values for the tps file settings.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="elementNames"></param>
        private void FillStruct(List<object> values, List<ItemsChoiceType2> elementNames)
        {
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("fileFormatVersion");
            elementNames.Add(ItemsChoiceType2.@int);
            values.Add(3);
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("texturePackerVersion");
            elementNames.Add(ItemsChoiceType2.@string);
            values.Add("4.0.1");
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("fileName");
            elementNames.Add(ItemsChoiceType2.@string);
            values.Add("");
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("autoSDSettings");
            elementNames.Add(ItemsChoiceType2.array);
            values.Add(new dataStructArray {@struct = new[] {CreateNewAtlasSettings("")}});
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("allowRotation");
            elementNames.Add(ItemsChoiceType2.@false);
            values.Add(new object());
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("premultiplyAlpha");
            elementNames.Add(ItemsChoiceType2.@false);
            values.Add(new object());
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("shapeDebug");
            elementNames.Add(ItemsChoiceType2.@false);
            values.Add(new object());
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("dpi");
            elementNames.Add(ItemsChoiceType2.@uint);
            values.Add(72);
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("dataFormat");
            elementNames.Add(ItemsChoiceType2.@string);
            values.Add("monogame");
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("textureFileName");
            elementNames.Add(ItemsChoiceType2.filename);
            values.Add("");
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("flipPVR");
            elementNames.Add(ItemsChoiceType2.@false);
            values.Add(new object());
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("pvrCompressionQuality");
            elementNames.Add(ItemsChoiceType2.@enum);
            var enumStruct = new dataStructEnum {type = "SettingsBase::PvrCompressionQuality", Value = "PVR_QUALITY_NORMAL"};
            values.Add(enumStruct);
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("atfCompressionData");
            elementNames.Add(ItemsChoiceType2.@false);
            values.Add(new object());
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("mipMapMinSize");
            elementNames.Add(ItemsChoiceType2.@uint);
            values.Add(32768);
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("etc1CompressionQuality");
            elementNames.Add(ItemsChoiceType2.@enum);
            enumStruct = new dataStructEnum
            {
                type = "SettingsBase::Etc1CompressionQuality",
                Value = "ETC1_QUALITY_LOW_PERCEPTUAL"
            };
            values.Add(enumStruct);
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("dxtCompressionMode");
            elementNames.Add(ItemsChoiceType2.@enum);
            enumStruct = new dataStructEnum { type = "SettingsBase::DxtCompressionMode", Value = "DXT_PERCEPTUAL" };
            values.Add(enumStruct);
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("jxrColorFormat");
            elementNames.Add(ItemsChoiceType2.@enum);
            enumStruct = new dataStructEnum { type = "SettingsBase::JpegXrColorMode", Value = "JXR_YUV444" };
            values.Add(enumStruct);
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("jxrTrimFlexBits");
            elementNames.Add(ItemsChoiceType2.@uint);
            values.Add(0);
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("jxrCompressionLevel");
            elementNames.Add(ItemsChoiceType2.@uint);
            values.Add(0);
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("ditherType");
            elementNames.Add(ItemsChoiceType2.@enum);
            enumStruct = new dataStructEnum { type = "SettingsBase::DitherType", Value = "NearestNeighbour" };
            values.Add(enumStruct);
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("backgroundColor");
            elementNames.Add(ItemsChoiceType2.@uint);
            values.Add(0);
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("libGdx");
            elementNames.Add(ItemsChoiceType2.@struct);
            values.Add(CreateLibGDXStruct());
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("shapePadding");
            elementNames.Add(ItemsChoiceType2.@uint);
            values.Add(0);
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("jpgQuality");
            elementNames.Add(ItemsChoiceType2.@uint);
            values.Add(80);
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("pngOptimizationLevel");
            elementNames.Add(ItemsChoiceType2.@uint);
            values.Add(1);
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("webpQualityLevel");
            elementNames.Add(ItemsChoiceType2.@uint);
            values.Add(101);
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("textureSubPath");
            elementNames.Add(ItemsChoiceType2.@string);
            values.Add("");
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("textureFormat");
            elementNames.Add(ItemsChoiceType2.@enum);
            values.Add(new dataStructEnum { type = "SettingsBase::TextureFormat", Value = "png" });
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("borderPadding");
            elementNames.Add(ItemsChoiceType2.@uint);
            values.Add(0);
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("maxTextureSize");
            elementNames.Add(ItemsChoiceType2.QSize);
            values.Add(CreateSizeStruct(2048, 2048));
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("fixedTextureSize");
            elementNames.Add(ItemsChoiceType2.QSize);
            values.Add(CreateSizeStruct(-1, -1));
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("reduceBorderArtifacts");
            elementNames.Add(ItemsChoiceType2.@false);
            values.Add(new object());
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("algorithmSettings");
            elementNames.Add(ItemsChoiceType2.@struct);
            values.Add(CreateAlgorithmSettings());
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("andEngine");
            elementNames.Add(ItemsChoiceType2.@struct);
            values.Add(CreateAndEngineStruct());
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("dataFileNames");
            elementNames.Add(ItemsChoiceType2.map);
            values.Add(CreateMapStruct());
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("multiPack");
            elementNames.Add(ItemsChoiceType2.@false);
            values.Add(new object());
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("forceIdenticalLayout");
            elementNames.Add(ItemsChoiceType2.@false);
            values.Add(new object());
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("outputFormat");
            elementNames.Add(ItemsChoiceType2.@enum);
            values.Add(new dataStructEnum { type = "SettingsBase::OutputFormat", Value = "RGBA8888" });
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("contentProtection");
            elementNames.Add(ItemsChoiceType2.@struct);
            values.Add(new dataStructStruct
            {
                type = "ContentProtection",
                ItemsElementName = new[] { ItemsChoiceType1.key, ItemsChoiceType1.@string },
                Items = new object[] { "key", "" }
            });
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("autoAliasEnabled");
            elementNames.Add(ItemsChoiceType2.@true);
            values.Add(new object());
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("trimSpriteNames");
            elementNames.Add(ItemsChoiceType2.@false);
            values.Add(new object());
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("prependSmartFolderName");
            elementNames.Add(ItemsChoiceType2.@true);
            values.Add(new object());
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("cleanTransparentPixels");
            elementNames.Add(ItemsChoiceType2.@true);
            values.Add(new object());
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("globalSpriteSettings");
            elementNames.Add(ItemsChoiceType2.@struct);
            values.Add(CreateSpriteSettings());
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("fileList");
            elementNames.Add(ItemsChoiceType2.@array);
            values.Add(new dataStructArray { filename = "../", @struct = new dataStructArrayStruct[0] });
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("ignoreFileList");
            elementNames.Add(ItemsChoiceType2.@array);
            values.Add(new dataStructArray { @struct = new dataStructArrayStruct[0] });
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("replaceList");
            elementNames.Add(ItemsChoiceType2.@array);
            values.Add(new dataStructArray { @struct = new dataStructArrayStruct[0] });
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("ignoredWarnings");
            elementNames.Add(ItemsChoiceType2.@array);
            values.Add(new dataStructArray { @struct = new dataStructArrayStruct[0] });
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("commonDivisorX");
            elementNames.Add(ItemsChoiceType2.@uint);
            values.Add(1);
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("commonDivisorY");
            elementNames.Add(ItemsChoiceType2.@uint);
            values.Add(1);
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("packNormalMaps");
            elementNames.Add(ItemsChoiceType2.@false);
            values.Add(new object());
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("autodetectNormalMaps");
            elementNames.Add(ItemsChoiceType2.@true);
            values.Add(new object());
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("normalMapFilter");
            elementNames.Add(ItemsChoiceType2.@string);
            values.Add("");
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("normalMapSuffix");
            elementNames.Add(ItemsChoiceType2.@string);
            values.Add("");
            elementNames.Add(ItemsChoiceType2.key);
            values.Add("normalMapSheetFileName");
            elementNames.Add(ItemsChoiceType2.@filename);
            values.Add("");
        }

        private dataStructStruct CreateSpriteSettings()
        {
            var spriteSettings = new dataStructStruct
                                 {
                                     type = "SpriteSettings",
                                     ItemsElementName = new ItemsChoiceType1[18],
                                     Items = new object[18]
                                 };
            spriteSettings.ItemsElementName[0] = ItemsChoiceType1.key;
            spriteSettings.Items[0] = "scale";
            spriteSettings.ItemsElementName[1] = ItemsChoiceType1.@double;
            spriteSettings.Items[1] = (byte)1;
            spriteSettings.ItemsElementName[2] = ItemsChoiceType1.key;
            spriteSettings.Items[2] = "scaleMode";
            spriteSettings.ItemsElementName[3] = ItemsChoiceType1.@enum;
            spriteSettings.Items[3] = new dataStructEnum { type = "ScaleMode", Value = "Smooth" };
            spriteSettings.ItemsElementName[4] = ItemsChoiceType1.key;
            spriteSettings.Items[4] = "extrude";
            spriteSettings.ItemsElementName[5] = ItemsChoiceType1.@uint;
            spriteSettings.Items[5] = 1;
            spriteSettings.ItemsElementName[6] = ItemsChoiceType1.key;
            spriteSettings.Items[6] = "trimThreshold";
            spriteSettings.ItemsElementName[7] = ItemsChoiceType1.@uint;
            spriteSettings.Items[7] = 1;
            spriteSettings.ItemsElementName[8] = ItemsChoiceType1.key;
            spriteSettings.Items[8] = "trimMargin";
            spriteSettings.ItemsElementName[9] = ItemsChoiceType1.@uint;
            spriteSettings.Items[9] = 1;
            spriteSettings.ItemsElementName[10] = ItemsChoiceType1.key;
            spriteSettings.Items[10] = "trimMode";
            spriteSettings.ItemsElementName[11] = ItemsChoiceType1.@enum;
            spriteSettings.Items[11] = new dataStructEnum { type = "SpriteSettings::TrimMode", Value = "Trim" };
            spriteSettings.ItemsElementName[12] = ItemsChoiceType1.key;
            spriteSettings.Items[12] = "tracerTolerance";
            spriteSettings.ItemsElementName[13] = ItemsChoiceType1.@int;
            spriteSettings.Items[13] = 200;
            spriteSettings.ItemsElementName[14] = ItemsChoiceType1.key;
            spriteSettings.Items[14] = "heuristicMask";
            spriteSettings.ItemsElementName[15] = ItemsChoiceType1.@false;
            spriteSettings.Items[15] = new object();
            spriteSettings.ItemsElementName[16] = ItemsChoiceType1.key;
            spriteSettings.Items[16] = "pivotPoint";
            spriteSettings.ItemsElementName[17] = ItemsChoiceType1.@enum;
            spriteSettings.Items[17] = new dataStructEnum { type = "SpriteSettings::PivotPoint", Value = "Center" };

            return spriteSettings;
        }

        private dataStructMap CreateMapStruct()
        {
            var mapStruct = new dataStructMap {type = "GFileNameMap", items = new object[4]};
            mapStruct.items[0] = "classfile";
            mapStruct.items[1] = new dataStructMapStruct {type = "DataFile", key = "name", filename = ""};
            mapStruct.items[2] = "datafile";
            mapStruct.items[3] = new dataStructMapStruct { type = "DataFile", key = "name", filename = "" };

            return mapStruct;
        }

        private dataStructQSize CreateSizeStruct(int width, int height)
        {
            var qSize = new dataStructQSize {Items = new object[4]};
            qSize.Items[0] = "width";
            qSize.Items[1] = width;
            qSize.Items[2] = "height";
            qSize.Items[3] = height;

            return qSize;
        }

        private dataStructStruct CreateAlgorithmSettings()
        {
            var dataStruct = new dataStructStruct
                             {
                                 type = "AlgorithmSettings",
                                 ItemsElementName = new ItemsChoiceType1[14],
                                 Items = new object[14]
                             };
            dataStruct.ItemsElementName[0] = ItemsChoiceType1.key;
            dataStruct.Items[0] = "algorithm";
            dataStruct.ItemsElementName[1] = ItemsChoiceType1.@enum;
            dataStruct.Items[1] = new dataStructEnum {type = "AlgorithmSettings::AlgorithmId", Value = "MaxRects"};
            dataStruct.ItemsElementName[2] = ItemsChoiceType1.key;
            dataStruct.Items[2] = "freeSizeMode";
            dataStruct.ItemsElementName[3] = ItemsChoiceType1.@enum;
            dataStruct.Items[3] = new dataStructEnum { type = "AlgorithmSettings::AlgorithmFreeSizeMode", Value = "Best" };
            dataStruct.ItemsElementName[4] = ItemsChoiceType1.key;
            dataStruct.Items[4] = "sizeConstraints";
            dataStruct.ItemsElementName[5] = ItemsChoiceType1.@enum;
            dataStruct.Items[5] = new dataStructEnum { type = "AlgorithmSettings::SizeConstraints", Value = "POT" };
            dataStruct.ItemsElementName[6] = ItemsChoiceType1.key;
            dataStruct.Items[6] = "forceSquared";
            dataStruct.ItemsElementName[7] = ItemsChoiceType1.@false;
            dataStruct.Items[7] = new object();
            dataStruct.ItemsElementName[8] = ItemsChoiceType1.key;
            dataStruct.Items[8] = "forceWordAligned";
            dataStruct.ItemsElementName[9] = ItemsChoiceType1.@false;
            dataStruct.Items[9] = new object();
            dataStruct.ItemsElementName[10] = ItemsChoiceType1.key;
            dataStruct.Items[10] = "maxRects";
            dataStruct.ItemsElementName[11] = ItemsChoiceType1.@struct;
            dataStruct.Items[11] = CreateDataStruct("AlgorithmMaxRectsSettings", new List<string> {"heuristic"},
                new List<dataStructEnum>
                {
                    new dataStructEnum {type = "AlgorithmMaxRectsSettings::Heuristic", Value = "Best"}
                });
            dataStruct.ItemsElementName[12] = ItemsChoiceType1.key;
            dataStruct.Items[12] = "basic";
            dataStruct.ItemsElementName[13] = ItemsChoiceType1.@struct;
            dataStruct.Items[13] = CreateDataStruct("AlgorithmBasicSettings", new List<string> { "sortBy", "order" },
                new List<dataStructEnum>
                {
                    new dataStructEnum {type = "AlgorithmBasicSettings::SortBy", Value = "Best"},
                    new dataStructEnum {type = "AlgorithmBasicSettings::Order", Value = "Ascending"}
                });

            return dataStruct;
        }

        private dataStructStructStruct CreateDataStruct(string type, List<string> keys, List<dataStructEnum> values)
        {
            var dataStruct = new dataStructStructStruct
                             {
                                 type = type,
                                 Items = new object[keys.Count()*2]
                             };

            var count = 0;
            for (var element = 0; element < keys.Count(); element++)
            {
                dataStruct.Items[count] = keys[element];
                count++;
                dataStruct.Items[count] = values[element];
                count++;
            }            

            return dataStruct;
        }

        private dataStructStruct CreateAndEngineStruct()
        {
            var dataStruct = new dataStructStruct
                             {
                                 type = "AndEngine",
                                 ItemsElementName = new ItemsChoiceType1[8],
                                 Items = new object[8]
                             };
            dataStruct.ItemsElementName[0] = ItemsChoiceType1.key;
            dataStruct.Items[0] = "minFilter";
            dataStruct.ItemsElementName[1] = ItemsChoiceType1.@enum;
            dataStruct.Items[1] = new dataStructEnum { type = "AndEngine::MinFilter", Value = "Linear" };
            dataStruct.ItemsElementName[2] = ItemsChoiceType1.key;
            dataStruct.Items[2] = "packageName";
            dataStruct.ItemsElementName[3] = ItemsChoiceType1.@string;
            dataStruct.Items[3] = "Texture";
            dataStruct.ItemsElementName[4] = ItemsChoiceType1.key;
            dataStruct.Items[4] = "wrap";
            dataStruct.ItemsElementName[5] = ItemsChoiceType1.@struct;
            dataStruct.Items[5] = CreateDataStruct("AndEngineWrap", new List<string> {"s", "t"},
                new List<dataStructEnum>
                {
                    new dataStructEnum {type = "AndEngineWrap::Wrap", Value = "Clamp"},
                    new dataStructEnum {type = "AndEngineWrap::Wrap", Value = "Clamp"}
                });
            dataStruct.ItemsElementName[6] = ItemsChoiceType1.key;
            dataStruct.Items[6] = "magFilter";
            dataStruct.ItemsElementName[7] = ItemsChoiceType1.@enum;
            dataStruct.Items[7] = new dataStructEnum { type = "AndEngine::MagFilter", Value = "MagLinear" };

            return dataStruct;
        }

        private dataStructStruct CreateLibGDXStruct()
        {
            var dataStruct = new dataStructStruct
                             {
                                 type = "LibGDX",
                                 ItemsElementName = new ItemsChoiceType1[2],
                                 Items = new object[2]
                             };
            dataStruct.ItemsElementName[0] = ItemsChoiceType1.key;
            dataStruct.Items[0] = "filtering";
            dataStruct.ItemsElementName[1] = ItemsChoiceType1.@struct;
            var dataStructStruct = new dataStructStructStruct {type = "LibGDXFiltering", Items = new object[4]};
            dataStructStruct.Items[0] = "x";
            dataStructStruct.Items[1] = new dataStructEnum
                                        {
                                            type = "LibGDXFiltering::Filtering",
                                            Value = "Linear"
                                        };
            dataStructStruct.Items[2] = "y";
            dataStructStruct.Items[3] = new dataStructEnum
                                        {
                                            type = "LibGDXFiltering::Filtering",
                                            Value = "Linear"
                                        };
            dataStruct.Items[1] = dataStructStruct;

            return dataStruct;
        }

        /// <summary>
        /// Generate the settings for an atlas of the given folder.
        /// </summary>
        /// <param name="folderToAdd"></param>
        /// <returns></returns>
        private dataStructArrayStruct CreateNewAtlasSettings(string folderToAdd)
        {
            var newSettings = new dataStructArrayStruct
                              {
                                  type = "AutoSDSettings",
                                  Items = new object[10],
                                  ItemsElementName = new ItemsChoiceType[10]
                              };
            newSettings.ItemsElementName[0] = ItemsChoiceType.key;
            newSettings.Items[0] = "scale";
            newSettings.ItemsElementName[1] = ItemsChoiceType.@double;
            newSettings.Items[1] = (byte) 1;
            newSettings.ItemsElementName[2] = ItemsChoiceType.key;
            newSettings.Items[2] = "extension";
            newSettings.ItemsElementName[3] = ItemsChoiceType.@string;

            string processedFolder = folderToAdd;
            if(processedFolder.EndsWith("/"))
            {
                processedFolder = processedFolder.Substring(0, processedFolder.Length - 1);
            }
            var extension = "_" + processedFolder.Replace("/", "_");
            newSettings.Items[3] = "_" + processedFolder.Replace("/", "_");
            newSettings.ItemsElementName[4] = ItemsChoiceType.key;
            newSettings.Items[4] = "spriteFilter";
            newSettings.ItemsElementName[5] = ItemsChoiceType.@string;
            newSettings.Items[5] = folderToAdd;
            newSettings.ItemsElementName[6] = ItemsChoiceType.key;
            newSettings.Items[6] = "acceptFractionalValues";
            newSettings.ItemsElementName[7] = ItemsChoiceType.@false;
            newSettings.Items[7] = new object();
            newSettings.ItemsElementName[8] = ItemsChoiceType.key;
            newSettings.Items[8] = "maxTextureSize";
            newSettings.ItemsElementName[9] = ItemsChoiceType.QSize;
            var qSize = new dataStructQSize { Items = new object[4] };
            qSize.Items[0] = "width";
            qSize.Items[1] = -1;
            qSize.Items[2] = "height";
            qSize.Items[3] = -1;
            newSettings.Items[9] = qSize;

            if (string.IsNullOrEmpty(folderToAdd))
            {
                newSettings.Items[3] = "";
                newSettings.Items[5] = "";
            }

            return newSettings;
        }

        internal void AddAtlas(string folderToAdd)
        {
            var autoSdSettings = AutoSdSettings;

            var tempList = autoSdSettings.@struct.ToList();
            tempList.Add(CreateNewAtlasSettings(folderToAdd));

            autoSdSettings.@struct = tempList.ToArray();
        }

        internal void RemoveAtlas(string folderToRemove)
        {
            var atlasSettings = AutoSdSettings.@struct.ToList();
            foreach (var atlas in atlasSettings)
            {
                var whatToLookFor = folderToRemove;

                int spriteFilterKey = IndexIn("spriteFilter", atlas.Items);

                if (spriteFilterKey != -1)
                {
                    var spriteFilterValueIndex = spriteFilterKey + 1;

                    if (atlas.Items[spriteFilterValueIndex] as string == whatToLookFor)
                    {
                        atlasSettings.Remove(atlas);
                        break;
                    }
                }
            }
            AutoSdSettings.@struct = atlasSettings.ToArray();
        }

        public void ClearAtlases()
        {
            AutoSdSettings.@struct = new dataStructArrayStruct[0];
        }

        public static TpsFileSave Load(string filename, out TpsLoadResult result)
        {
            result = new TpsLoadResult();

            if (string.IsNullOrEmpty(filename))
            {
                result.ErrorMessage = "Passed null file name, could not load TpsFileSave";
                return null;
            }

            TpsFileSave tps = null;

            try
            {
                tps = FileManager.XmlDeserialize<TpsFileSave>(filename);

                tps.Filename = filename.Replace("/", "\\");
                tps.SetupFile();
            }
            catch (FileNotFoundException)
            {
                result.ErrorMessage = "The Texture Packer Settings file does not exist";
                return null;
            }
            catch (IOException ex)
            {
                result.ErrorMessage = ex.Message;
                return null;
            }

            tps.SetupFile();

            return tps;
        }

        public void Save(string filename)
        {
            var directory = FileManager.GetDirectory(filename);
            if(Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }

            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");

            var s = new XmlSerializer(typeof (TpsFileSave));
            using (var writer = new StreamWriter(filename))
            {
                this.Filename = filename;
                s.Serialize(writer, this, ns);
            }
        }

        /// <summary>
        /// Calls TexturePacker.exe to generate the atlas data and png files.
        /// </summary>
        public void CreateAtlasFiles()
        {
            var processInfo = new ProcessStartInfo
                              {
                                  FileName = @"C:\Program Files\CodeAndWeb\TexturePacker\bin\TexturePacker.exe",
                                  Arguments =  Filename + " --force-publish",
                                  UseShellExecute = false,
                                  RedirectStandardOutput = true,
                                  RedirectStandardError = true,
                                  CreateNoWindow = true
                              };
            var tpProcess = new Process {StartInfo = processInfo};
            try
            {
                tpProcess.Start();
                var tpOutput = tpProcess.StandardOutput.ReadToEnd();
                var tpError = tpProcess.StandardError.ReadToEnd();

                PluginManager.ReceiveOutput(tpOutput);
                PluginManager.ReceiveError(tpError);
            }
            catch (Exception e)
            {
                throw e.InnerException;
            }
        }

        /// <summary>
        /// Sets the appropriate names for the atlas data file and png file as well as the tps filename.
        /// </summary>
        private void SetupFile()
        {
            var filename = FileManager.RemoveExtension(FileManager.RemovePath(GlueState.Self.CurrentGlueProjectFileName));

            dataStructMapStruct classStruct = null;
            dataStructMapStruct dataStruct = null;
            for (var i = 0; i < DataFileNames.items.Length; i++)
            {
                if (DataFileNames.items[i] as string == "classfile") classStruct = DataFileNames.items[i + 1] as dataStructMapStruct;
                if (DataFileNames.items[i] as string == "datafile") dataStruct = DataFileNames.items[i + 1] as dataStructMapStruct;
            }

            if (classStruct != null) classStruct.filename = filename + ".cs";
            if (dataStruct != null) dataStruct.filename = filename + "{v}.atlas";
            TextureFileNames = filename + "{v}.png";

            var filenameIndex = IndexInItems("fileName");
            @struct.Items[filenameIndex + 1] = Filename;
        }

        public static int IndexIn(string value, object[] objects)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] is string && (objects[i] as string) == value)
                {
                    return i;
                }
            }

            return -1;

        }

        private int IndexInItems(string value)
        {
            return IndexIn(value, _structField.Items);
        }
        #endregion
    }

}