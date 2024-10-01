using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.GuiDisplay
{
    public class AvailableNamedObjectsAndFiles : TypeConverter
    {
        #region Fields

        public bool IncludeReferencedFiles
        {
            get;
            set;
        }

        public IElement CurrentElement
        {

            get;
            set;
        }

        public ScreenSave CurrentScreenSave
        {
            get
            {
                return CurrentElement as ScreenSave;
            }
        }

        public EntitySave CurrentEntitySave
        {
            get
            {
                return CurrentElement as EntitySave;
            }
        }

        /// <summary>
        /// Sets the restriction of types to return. For example "FlatRedBall.Sprite" would restrict
        /// the returned values to only of type Sprite.
        /// </summary>
        public string NamedObjectTypeRestriction
        {
            get; set;
        }

        #endregion

        public override bool GetStandardValuesSupported(
                       ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(
                           ITypeDescriptorContext context)
        {
            return true;
        }

        public AvailableNamedObjectsAndFiles(GlueElement element)
            : base()
        {
            this.CurrentElement = element;
            IncludeReferencedFiles = true;
        }

        public static List<string> GetAvailableObjects(bool addNone, bool includeReferencedfiles, IElement currentElement, string namedObjectTypeRestriction)
        {
            if(currentElement == null)
            {
                throw new ArgumentNullException(nameof(currentElement));
            }
            stringListToReturn.Clear();

            if (addNone)
            {
                stringListToReturn.Add("<NONE>");
            }

            #region Add the NamedObjectSaves

            List<NamedObjectSave> namedObjects = null;


            namedObjects = currentElement.NamedObjects;

            AddContainedNamedObjects(namedObjects, namedObjectTypeRestriction);

            #endregion

            if (includeReferencedfiles)
            {
                AddAvailableReferencedFiles(currentElement);
            }
            stringListToReturn.Sort();
            return stringListToReturn;
        }

        private static void AddAvailableReferencedFiles(IElement currentElement)
        {
            List<ReferencedFileSave> referencedFiles = new List<ReferencedFileSave>(); ;

            if (currentElement as EntitySave != null)
            {
                // Don't do anything here... yet
                //referencedFiles = EditorLogic.CurrentEntitySave.ReferencedFiles;
            }
            else
            {
                referencedFiles = (currentElement as ScreenSave).ReferencedFiles;
            }

            // Loop through the named objects and add them here
            foreach (ReferencedFileSave referencedFile in referencedFiles)
            {
                stringListToReturn.Add(FileManager.RemovePath(FileManager.RemoveExtension(referencedFile.GetInstanceName())));
            }
        }

        private static void AddContainedNamedObjects(List<NamedObjectSave> namedObjects, string namedObjectTypeRestriction)
        {
            // Loop through the named objects and add them here
            foreach (NamedObjectSave namedObject in namedObjects)
            {
                bool shouldAdd = namedObjectTypeRestriction == null || namedObject.GetAssetTypeInfo()?.QualifiedRuntimeTypeName.QualifiedType == namedObjectTypeRestriction;

                if(shouldAdd)
                {
                    stringListToReturn.Add(namedObject.InstanceName);

                    AddContainedNamedObjects(namedObject.ContainedObjects, namedObjectTypeRestriction);
                }
            }
        }

        static List<string> stringListToReturn = new List<string>();
        public override System.ComponentModel.TypeConverter.StandardValuesCollection
                     GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> stringList = GetAvailableObjects(true, IncludeReferencedFiles, CurrentElement, this.NamedObjectTypeRestriction);

            return new System.ComponentModel.TypeConverter.StandardValuesCollection(stringList);
        }
    }
}
