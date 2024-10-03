using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Instructions.Reflection;

using System.Xml.Serialization;
using FlatRedBall.Utilities;
using FlatRedBall.IO;
using System.IO;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Interfaces;

namespace FlatRedBall.Glue.SaveClasses
{
    public enum ElementType
    {
        Screen,
        Entity
    }

    public class ElementReference
    {
        public const string ScreenSubfolder = "GlueProject/Screens";
        public const string EntitySubfolder = "GlueProject/Entities";

        public ElementType ElementType
        {
            get;
            set;
        }

        public string Extension
        {
            get
            {
                switch (ElementType)
                {
                    case ElementType.Screen:
                        return GlueProjectSave.ScreenExtension;
                    case ElementType.Entity:
                        return GlueProjectSave.EntityExtension;
                }
                throw new InvalidOperationException();
            }
        }


        public string Subfolder
        {
            get
            {
                switch (ElementType)
                {
                    case ElementType.Screen:
                        return ScreenSubfolder;
                    case ElementType.Entity:
                        return EntitySubfolder;
                }
                throw new InvalidOperationException();
            }
        }


        public string Name;


//        public T ToElementSave<T>(string projectroot, string extension, GumLoadResult result, LinkLoadingPreference linkLoadingPreference = LinkLoadingPreference.PreferLinked) where T : ElementSave, new()
//        {
//            FilePath linkedName = null;
//            FilePath containedReferenceName = null;

//            if (!string.IsNullOrWhiteSpace(this.Link))
//            {
//                linkedName = projectroot + this.Link;

//            }
//            containedReferenceName = projectroot + Subfolder + "/" + Name + "." + extension;

//            if (linkedName != null && ToolsUtilities.FileManager.IsRelative(linkedName.Original))
//            {
//                linkedName = ToolsUtilities.FileManager.RelativeDirectory + linkedName.Original;
//            }
//            if (ToolsUtilities.FileManager.IsRelative(containedReferenceName.Original))
//            {
//                containedReferenceName = ToolsUtilities.FileManager.RelativeDirectory + containedReferenceName.Original;
//            }

//            if (linkedName?.Exists() == true)
//            {
//                T elementSave = FileManager.XmlDeserialize<T>(linkedName.FullPath);
//                return elementSave;
//            }
//#if ANDROID || IOS
//            else if (containedReferenceName != null && (linkedName == null || linkLoadingPreference == LinkLoadingPreference.PreferLinked))
//#else
//            else if (containedReferenceName.Exists() && (linkedName == null || linkLoadingPreference == LinkLoadingPreference.PreferLinked))
//#endif
//            {

//                T elementSave = FileManager.XmlDeserialize<T>(
//#if ANDROID || IOS
//                    containedReferenceName.Standardized);
//#else
//                    containedReferenceName.FullPath);
//#endif

//                if (Name != elementSave.Name)
//                {
//                    // The file name doesn't match the name of the element.  This can cause errors
//                    // at runtime so let's tell the user:
//                    result.ErrorMessage += "\nThe project references an element named " + Name + ", but the XML for this element has its name set to " + elementSave.Name + "\n";
//                }

//                return elementSave;
//            }
//            else
//            {
//                // I don't think we want to consider this an error anymore
//                // because Gum can handle it - it doesn't allow saving that 
//                // individual element and it shows a red ! next to the element.
//                // We should just tolerate this and let the user deal with it.
//                // If we do treat this as an error, then Gum goes into a state 
//                // where it can't save anything.
//                //errors += "\nCould not find the file name " + fullName;
//                // Update Feb 20, 2015
//                // But we can record it:
//                result.MissingFiles.Add(containedReferenceName.FullPath);


//                T elementSave = new T();

//                elementSave.Name = Name;
//                elementSave.IsSourceFileMissing = true;

//                return elementSave;
//            }
//        }
    }
}
