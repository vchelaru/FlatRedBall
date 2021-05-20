using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using System.Runtime.InteropServices;

namespace FlatRedBall.Glue.GuiDisplay
{
    public class AvailableFileStringConverter : TypeConverterWithNone
    {
        #region Fields
        public const string UseDefaultString = "<USE DEFAULT>";
        bool mShowNewFileOption = true;

        #endregion

        #region Properties

        public IElement CurrentElement
        {

            get;
            set;
        }

        public bool ShowNewFileOption
        {
            get { return mShowNewFileOption; }
            set { mShowNewFileOption = value; }
        }

        public bool RemovePathAndExtension
        {
            get;
            set;
        }


        public string QualifiedRuntimeTypeName
        {
            get;
            set;
        }

        public string UnqualifiedRuntimeTypeName
        {
            get;
            set;
        }

        #endregion

        public AvailableFileStringConverter(IElement element)
            : base()
        {
            IncludeNoneOption = true;
            CurrentElement = element;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string x, string y);


        List<string> stringToReturn = new List<string>();
        public override StandardValuesCollection
                     GetStandardValues(ITypeDescriptorContext context)
        {
            stringToReturn.Clear();

            if(IncludeNoneOption)
            {
                // 
                stringToReturn.Add("<NONE>");
            }
            else
            {
                stringToReturn.Add("");
            }

            IElement currentElementSave = CurrentElement;

            List<string> listToSort = new List<string>();

            if (currentElementSave != null)
            {
                // Let's use 
                //foreach (ReferencedFileSave rfs in currentElementSave.ReferencedFiles)
                foreach (ReferencedFileSave rfs in currentElementSave.GetAllReferencedFileSavesRecursively())
                {
                    bool shouldInclude = (string.IsNullOrEmpty(QualifiedRuntimeTypeName) && string.IsNullOrEmpty(UnqualifiedRuntimeTypeName)) ||
                        (!string.IsNullOrEmpty(QualifiedRuntimeTypeName) && IsRfsOfQualifiedRuntimeType(rfs, QualifiedRuntimeTypeName)) ||
                        (!string.IsNullOrEmpty(UnqualifiedRuntimeTypeName) && IsRfsOfUnqualifiedRuntimeType(rfs, UnqualifiedRuntimeTypeName));

                    if (shouldInclude)
                    {
                        if (RemovePathAndExtension)
                        {
                            listToSort.Add(FileManager.RemovePath(FileManager.RemoveExtension(rfs.Name)));
                        }
                        else
                        {
                            listToSort.Add(rfs.Name);
                        }
                    }
                }
            }

            listToSort.Sort(StrCmpLogicalW);

            stringToReturn.AddRange(listToSort);
            return new StandardValuesCollection(stringToReturn);
        }


        bool IsRfsOfType(ReferencedFileSave rfs, Type type)
        {
            AssetTypeInfo ati = rfs.GetAssetTypeInfo();

            if (ati != null)
            {
                return ati.QualifiedRuntimeTypeName.QualifiedType == type.FullName;
            }
            else
            {
                return false;
            }
        }

        bool IsRfsOfQualifiedRuntimeType(ReferencedFileSave rfs, string qualifiedRuntimeType)
        {
            AssetTypeInfo ati = rfs.GetAssetTypeInfo();

            if (ati != null)
            {
                return ati.QualifiedRuntimeTypeName.QualifiedType == qualifiedRuntimeType;
            }
            else
            {
                return false;
            }
        }

        bool IsRfsOfUnqualifiedRuntimeType(ReferencedFileSave rfs, string unqualifiedRuntimeType)
        {
            AssetTypeInfo ati = rfs.GetAssetTypeInfo();

            if (ati != null)
            {
                return ati.RuntimeTypeName == unqualifiedRuntimeType;
            }
            else
            {
                return false;
            }
        }
    }
}
