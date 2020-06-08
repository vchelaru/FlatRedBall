using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Glue.Parsing;
using System.IO;
using EditorObjects.Parsing;
using FlatRedBall.Glue.Events;

using FlatRedBall.Glue.GuiDisplay.Facades;

namespace FlatRedBall.Glue.Elements
{
    public class ObjectFinder : IObjectFinder
    {
        static ObjectFinder mSelf = new ObjectFinder();

        public static ObjectFinder Self
        {
            get
            {
                return mSelf;
            }
        }


        GlueProjectSave mGlueProjectSave;

        public GlueProjectSave GlueProject
        {
            get
            {
                return mGlueProjectSave;
            }
            set
            {
                mGlueProjectSave = value;
            }
        }



        public ObjectFinder()
        {
            NamedObjectSave.ToStringDelegate = NamedObjectSaveExtensionMethods.NamedObjectSaveToString;
            CustomVariable.ToStringDelegate = CustomVariableExtensionMethods.CustomVariableToString;
            ReferencedFileSave.ToStringDelegate = ReferencedFileSaveExtensionMethods.ReferencedFileSaveToString;
            EventResponseSave.ToStringDelegate = EventResponseSaveExtensionMethods.EventResponseSaveToString;
            StateSave.ToStringDelegate = StateSaveExtensionMethods.StateSaveToString;
        }


        public string MakeAbsoluteContent(string fileName)
        {
            if (FileManager.IsRelative(fileName))
            {
                return FacadeContainer.Self.ProjectValues.ContentDirectory + fileName;
            }
            else
            {
                return fileName;
            }
        }

#if GLUE
        [Obsolete("Use GluxCommands.GetReferencedFileSaveFromFile")]
#endif
        public ReferencedFileSave GetReferencedFileSaveFromFile(string fileName)
        {
            ////////////////Early Out//////////////////////////////////
            var invalidPathChars = Path.GetInvalidPathChars();
            if(invalidPathChars.Any( item => fileName.Contains(item)))
            {
                // This isn't a RFS, because it's got a bad path. Early out here so that FileManager.IsRelative doesn't throw an exception
                return null;
            }

            //////////////End Early Out////////////////////////////////


            fileName = fileName.ToLower();

            if (FileManager.IsRelative(fileName))
            {

                fileName = MakeAbsoluteContent(fileName);

            }

            fileName = FileManager.Standardize(fileName).ToLower();

            var project = GlueProject;
            if (project != null)
            {
                foreach (ScreenSave screenSave in project.Screens)
                {
                    foreach (ReferencedFileSave rfs in screenSave.ReferencedFiles)
                    {
                        string absoluteRfsFile = FileManager.Standardize(ObjectFinder.Self.MakeAbsoluteContent(rfs.Name)).ToLower();

                        if (absoluteRfsFile == fileName)
                        {
                            return rfs;
                        }
                    }
                }

                lock (project.Entities)
                {
                    foreach (EntitySave entitySave in project.Entities)
                    {
                        foreach (ReferencedFileSave rfs in entitySave.ReferencedFiles)
                        {
                            string absoluteRfsFile = FileManager.Standardize(ObjectFinder.Self.MakeAbsoluteContent(rfs.Name)).ToLower();

                            if (absoluteRfsFile == fileName)
                            {
                                return rfs;
                            }
                        }
                    }
                }

                foreach (ReferencedFileSave rfs in project.GlobalFiles)
                {
                    string absoluteRfsFile = FileManager.Standardize(ObjectFinder.Self.MakeAbsoluteContent(rfs.Name)).ToLower();

                    if (absoluteRfsFile == fileName)
                    {
                        return rfs;
                    }
                }
            }

            return null;
        }

        public List<ReferencedFileSave> GetReferencedFileSavesFromSource(string sourceFile)
        {
            List<ReferencedFileSave> toReturn = new List<ReferencedFileSave>();

            sourceFile = sourceFile.ToLower();
            if (GlueProject != null)
            {
                foreach (ScreenSave screenSave in GlueProject.Screens)
                {
                    foreach (ReferencedFileSave rfs in screenSave.ReferencedFiles)
                    {
                        if (rfs.IsFileSourceForThis(sourceFile))
                        {
                            toReturn.Add(rfs);
                        }
                    }
                }

                foreach (EntitySave entitySave in GlueProject.Entities)
                {
                    foreach (ReferencedFileSave rfs in entitySave.ReferencedFiles)
                    {
                        if (rfs.IsFileSourceForThis(sourceFile))
                        {
                            toReturn.Add(rfs);
                        }
                    }
                }

                foreach (ReferencedFileSave rfs in GlueProject.GlobalFiles)
                {
                    if (rfs.IsFileSourceForThis(sourceFile))
                    {
                        toReturn.Add(rfs);
                    }
                }
            }

            return toReturn;
        }

#if GLUE
        
#endif

        public ReferencedFileSave GetFirstCsvUsingClass(string className)
        {
            return GetFirstCsvUsingClass(className, null);
        }


        public ReferencedFileSave GetFirstCsvUsingClass(string className, IElement elementToLookIn)
        {
            if (GlueProject != null)
            {
                if (elementToLookIn != null)
                {
                    // Maybe we need to make this recursive?
                    foreach (ReferencedFileSave rfs in elementToLookIn.ReferencedFiles)
                    {
                        if (rfs.IsCsvOrTreatedAsCsv &&
                            rfs.GetTypeForCsvFile() == className)
                        {
                            return rfs;
                        }
                    }
                }
                else
                {
                    foreach (ScreenSave screenSave in GlueProject.Screens)
                    {
                        foreach (ReferencedFileSave rfs in screenSave.ReferencedFiles)
                        {
                            if (rfs.IsCsvOrTreatedAsCsv &&
                                rfs.GetTypeForCsvFile() == className)
                            {
                                return rfs;
                            }
                        }
                    }

                    foreach (EntitySave entitySave in GlueProject.Entities)
                    {
                        foreach (ReferencedFileSave rfs in entitySave.ReferencedFiles)
                        {
                            if (rfs.IsCsvOrTreatedAsCsv && rfs.GetTypeForCsvFile() == className)
                            {
                                return rfs;
                            }
                        }
                    }
                }

                className = FileManager.RemovePath(className);

                foreach (ReferencedFileSave rfs in GlueProject.GlobalFiles)
                {
                    if (rfs.IsCsvOrTreatedAsCsv && rfs.GetTypeForCsvFile() == className)
                    {
                        return rfs;
                    }
                }
            }

            return null;
        }

        public EntitySave GetEntitySave(string entityName)
        {
            // This method is called by NamedObjectSave whenever its
            // SourceClassType is set.  This is set when the project
            // is first deserialized, and when that happens the project
            // isn't valid yet.  Therefore, we should check to see if it
            // is valid.
            if (GlueProject != null)
            {
                return GlueProject.GetEntitySave(entityName);
            }

            return null;
        }

        public IElement GetElementUnqualified(string elementName)
        {

            if (elementName != null && GlueProject != null)
            {
                elementName = elementName.Replace('/', '\\');

                for (int i = 0; i < GlueProject.Entities.Count; i++)
                {
                    EntitySave entitySave = GlueProject.Entities[i];

                    if (FileManager.RemovePath(entitySave.Name) == elementName)
                    {
                        return entitySave;
                    }
                }

                for (int i = 0; i < GlueProject.Screens.Count; i++)
                {
                    ScreenSave screenSave = GlueProject.Screens[i];

                    if (FileManager.RemovePath(screenSave.Name) == elementName)
                    {
                        return screenSave;
                    }
                }
            }
            return null;

        }

        public List<IElement> GetElementsUnqualified(string elementName)
        {
            List<IElement> listToReturn = new List<IElement>();

            if (elementName != null)
            {
                elementName = elementName.Replace('/', '\\');

                for (int i = 0; i < GlueProject.Entities.Count; i++)
                {
                    EntitySave entitySave = GlueProject.Entities[i];

                    if (FileManager.RemovePath(entitySave.Name) == elementName)
                    {
                        listToReturn.Add(entitySave);
                    }
                }

                for (int i = 0; i < GlueProject.Screens.Count; i++)
                {
                    ScreenSave screenSave = GlueProject.Screens[i];

                    if (FileManager.RemovePath(screenSave.Name) == elementName)
                    {
                        listToReturn.Add(screenSave);
                    }
                }
            }
            return listToReturn;
        }

        public EntitySave GetEntitySaveUnqualified(string entityName)
        {
            if (GlueProject != null)
            {
                for (int i = 0; i < GlueProject.Entities.Count; i++)
                {
                    EntitySave entitySave = GlueProject.Entities[i];

                    if (FileManager.RemovePath(entitySave.Name) == entityName)
                    {
                        return entitySave;
                    }
                }
            }

            return null;
        }


        public ScreenSave GetScreenSave(string screenName)
        {
            if (screenName != null)
            {
                screenName = screenName.Replace('/', '\\');

                if (GlueProject != null)
                {
                    for (int i = 0; i < GlueProject.Screens.Count; i++)
                    {
                        ScreenSave screenSave = GlueProject.Screens[i];

                        if (screenSave.Name == screenName)
                        {
                            return screenSave;
                        }
                    }
                }
            }
            return null;
        }

        public ScreenSave GetScreenSaveUnqualified(string screenName)
        {
            if (GlueProject != null)
            {
                for (int i = 0; i < GlueProject.Screens.Count; i++)
                {
                    ScreenSave screenSave = GlueProject.Screens[i];

                    if (FileManager.RemovePath(screenSave.Name) == screenName)
                    {
                        return screenSave;
                    }
                }
            }

            return null;
        }

        public IElement GetIElement(string elementName)
        {
            IElement retval;

            retval = GetScreenSave(elementName);

            if (retval == null)
            {
                retval = GetEntitySave(elementName);
            }

            return retval;
        }


        public bool DoesEntityExist(string entityName)
        {
            return GetEntitySave(entityName) != null;
        }

        public CustomClassSave GetCustomClassFor(ReferencedFileSave rfs)
        {
            foreach(var customClass in mGlueProjectSave.CustomClasses)
            {
                if(customClass.CsvFilesUsingThis.Contains(rfs.Name))
                {
                    return customClass;
                }
            }
            return null;
        }

        public List<EntitySave> GetAllEntitiesThatInheritFrom(EntitySave baseEntity)
        {
            return GetAllEntitiesThatInheritFrom(baseEntity.Name);
        }


        public List<EntitySave> GetAllEntitiesThatInheritFrom(string baseEntity)
        {
            List<EntitySave> derivedEntities = new List<EntitySave>();

            for (int i = 0; i < GlueProject.Entities.Count; i++)
            {
                EntitySave entitySave = GlueProject.Entities[i];

                if (entitySave.InheritsFrom(baseEntity))
                {
                    derivedEntities.Add(entitySave);
                }
            }

            return derivedEntities;
        }


        public List<ScreenSave> GetAllScreensThatInheritFrom(ScreenSave baseScreen)
        {
            return GetAllScreensThatInheritFrom(baseScreen.Name);
        }


        public List<ScreenSave> GetAllScreensThatInheritFrom(string baseScreen)
        {
            List<ScreenSave> derivedScreens = new List<ScreenSave>();

            for (int i = 0; i < GlueProject.Screens.Count; i++)
            {
                ScreenSave screenSave = GlueProject.Screens[i];

                if (screenSave.InheritsFrom(baseScreen))
                {
                    derivedScreens.Add(screenSave);
                }
            }

            return derivedScreens;
        }

        public List<IElement> GetAllElementsThatInheritFrom(string elementName)
        {
            IElement element = GetIElement(elementName);

            return GetAllElementsThatInheritFrom(element);
        }

        public List<IElement> GetAllElementsThatInheritFrom(IElement baseElement)
        {
            var derivedElements = new List<IElement>();
            var count = 0;
            var isScreen = baseElement is ScreenSave;
            var isEntity = baseElement is EntitySave;

            if (baseElement is ScreenSave)
                count = GlueProject.Screens.Count;
            else if (baseElement is EntitySave)
                count = GlueProject.Entities.Count;

            for (var i = 0; i < count; i++)
            {
                bool add;
                IElement derivedElement;

                if (isScreen)
                {
                    derivedElement = GlueProject.Screens[i];
                    add = GlueProject.Screens[i].InheritsFrom(baseElement.Name);
                }
                else if (isEntity)
                {
                    derivedElement = GlueProject.Entities[i];
                    add = GlueProject.Entities[i].InheritsFrom(baseElement.Name);
                }
                else
                {
                    throw new NotImplementedException();
                }

                if (add)
                {
                    derivedElements.Add(derivedElement);
                }
            }

            return derivedElements;
        }

        public List<IElement> GetAllBaseElementsRecursively(IElement derivedElement)
        {
            var baseElements = new List<IElement>();

            while (!string.IsNullOrEmpty(derivedElement.BaseElement))
            {
                var baseElement = GetIElement(derivedElement.BaseElement);

                if (baseElement == null)
                {
                    break;
                }
                else
                {
                    baseElements.Add(baseElement);
                    derivedElement = baseElement;
                }
            }

            return baseElements;
        }

        public List<ReferencedFileSave> GetAllReferencedFiles()
        {
            if (GlueProject != null)
            {
                return GlueProject.GetAllReferencedFiles();
            }
            else
            {
                return new List<ReferencedFileSave>();
            }
        }

        public List<NamedObjectSave> GetAllNamedObjectsThatUseElement(IElement element)
        {
            if (element is EntitySave)
            {
                return GetAllNamedObjectsThatUseEntity(element as EntitySave);
            }
            else
            {
                return new List<NamedObjectSave>();
            }
        }


        public List<NamedObjectSave> GetAllNamedObjectsThatUseEntity(EntitySave entity)
        {
            return GetAllNamedObjectsThatUseEntity(entity.Name);
        }


        public List<NamedObjectSave> GetAllNamedObjectsThatUseEntity(string baseEntity)
        {
            List<NamedObjectSave> namedObjects = new List<NamedObjectSave>();

            foreach (EntitySave entitySave in GlueProject.Entities)
            {
                GetAllNamedObjectsThatUseElement(entitySave.NamedObjects, namedObjects, baseEntity, SourceType.Entity);
            }


            foreach (ScreenSave screenSave in GlueProject.Screens)
            {
                GetAllNamedObjectsThatUseElement(screenSave.NamedObjects, namedObjects, baseEntity, SourceType.Entity);
            }

            return namedObjects;
        }


        private void GetAllNamedObjectsThatUseElement(List<NamedObjectSave> sourceList, List<NamedObjectSave> listToAddTo, string name, SourceType sourceType)
        {
            foreach (NamedObjectSave nos in sourceList)
            {
                if (nos.SourceType == sourceType && nos.SourceClassType == name)
                {
                    listToAddTo.Add(nos);
                }
                else if (nos.SourceType == SourceType.FlatRedBallType && nos.IsList && nos.SourceClassGenericType == name)
                {
                    listToAddTo.Add(nos);
                }

                GetAllNamedObjectsThatUseElement(nos.ContainedObjects, listToAddTo, name, sourceType);
            }

        }



        public INamedObjectContainer GetNamedObjectContainer(string namedObjectContainerName)
        {
            EntitySave entitySave = ObjectFinder.Self.GetEntitySave(namedObjectContainerName);

            if (entitySave != null)
            {
                return entitySave;
            }
            else
            {
                return ObjectFinder.Self.GetScreenSave(namedObjectContainerName);
            }
        }


        //public IBehaviorContainer GetBehaviorContainer(string behaviorContainerName)
        //{
        //    EntitySave entitySave = GetEntitySave(behaviorContainerName);

        //    if (entitySave != null)
        //    {
        //        return entitySave;
        //    }
        //    else
        //    {
        //        return GetScreenSave(behaviorContainerName);
        //    }
        //}


        public IElement GetVariableContainer(CustomVariable customVariable)
        {
            return GlueProject.GetElementContaining(customVariable);
        }

        public IElement GetElementContaining(NamedObjectSave namedObjectSave)
        {
            for (int i = 0; i < ObjectFinder.Self.GlueProject.Screens.Count; i++)
            {
                if (IsContainedInListOrAsChild(ObjectFinder.Self.GlueProject.Screens[i].NamedObjects, namedObjectSave))
                {
                    return ObjectFinder.Self.GlueProject.Screens[i];
                }
            }
            for (int i = 0; i < ObjectFinder.Self.GlueProject.Entities.Count; i++)
            {
                if (IsContainedInListOrAsChild(ObjectFinder.Self.GlueProject.Entities[i].NamedObjects, namedObjectSave))
                {
                    return ObjectFinder.Self.GlueProject.Entities[i];
                }
            }

            return null;

        }

        public IElement GetElementContaining(ReferencedFileSave referencedFileSave)
        {

            for (int i = 0; i < ObjectFinder.Self.GlueProject.Screens.Count; i++)
            {
                if (ObjectFinder.Self.GlueProject.Screens[i].ReferencedFiles.Contains(referencedFileSave))
                {
                    return ObjectFinder.Self.GlueProject.Screens[i];
                }
            }
            for (int i = 0; i < ObjectFinder.Self.GlueProject.Entities.Count; i++)
            {
                if (ObjectFinder.Self.GlueProject.Entities[i].ReferencedFiles.Contains(referencedFileSave))
                {
                    return ObjectFinder.Self.GlueProject.Entities[i];
                }
            }

            return null;
        }

        public IElement GetElementContaining(EventResponseSave ers)
        {
            for (int i = 0; i < ObjectFinder.Self.GlueProject.Screens.Count; i++)
            {
                foreach (EventResponseSave possibleErs in ObjectFinder.Self.GlueProject.Screens[i].Events)
                {
                    if (possibleErs == ers)
                    {
                        return ObjectFinder.Self.GlueProject.Screens[i];
                    }
                }
            }
            for (int i = 0; i < ObjectFinder.Self.GlueProject.Entities.Count; i++)
            {
                foreach (EventResponseSave possibleErs in ObjectFinder.Self.GlueProject.Entities[i].Events)
                {
                    if (possibleErs == ers)
                    {
                        return ObjectFinder.Self.GlueProject.Entities[i];
                    }
                }
            }

            return null;

        }

        bool IsContainedInListOrAsChild(List<NamedObjectSave> namedObjects, NamedObjectSave objectToFind)
        {
            foreach (NamedObjectSave nos in namedObjects)
            {
                if (nos == objectToFind || IsContainedInListOrAsChild(nos.ContainedObjects, objectToFind))
                {
                    return true;
                }
            }
            return false;
        }

        public  IElement GetElementContaining(StateSave stateSave)
        {
            return GlueProject.GetElementContaining(stateSave);
        }

        public IElement GetElementContaining(CustomVariable customVariable)
        {
            if (GlueProject != null)
            {
                return GlueProject.GetElementContaining(customVariable);
            }
            else
            {
                return null;
            }
        }

        public IElement GetElementContaining(StateSaveCategory category)
        {
            if(GlueProject != null)
            {
                IEnumerable<IElement> screens = GlueProject.Screens;
                IEnumerable<IElement> entities = GlueProject.Entities;
                return screens.Concat(entities)
                    .FirstOrDefault(item => 
                        item.StateCategoryList.Contains(category));

            }
            else
            {
                return null;
            }
        }

        public List<IElement> GetAllElementsReferencingFile(string rfsName)
        {
            List<IElement> returnList = new List<IElement>();

            foreach (ScreenSave screenSave in GlueProject.Screens)
            {
                foreach (ReferencedFileSave rfs in screenSave.ReferencedFiles)
                {
                    if (rfs.Name == rfsName)
                    {
                        returnList.Add(screenSave);
                        break;
                    }
                }
            }

            foreach (EntitySave entitySave in GlueProject.Entities)
            {
                foreach (ReferencedFileSave rfs in entitySave.ReferencedFiles)
                {
                    if (rfs.Name == rfsName)
                    {
                        returnList.Add(entitySave);
                        break;
                    }
                }
            }

            return returnList;
        }

        public List<ReferencedFileSave> GetMatchingReferencedFiles(ReferencedFileSave rfs)
        {
            // The same file 
            // can be referenced 
            // in multiple parts in 
            // a Glue project.  For example,
            // one .scnx file may be used in two
            // unrelated Entities.  Or a RFS may be
            // both in GlobalContent as well as in an
            // Entity.  In this case, the two RFS's should
            // be linked.  Changing a property on one (like
            // whether to use the content pipeline) should be
            // also changed on the other.
            List<ReferencedFileSave> toReturn = new List<ReferencedFileSave>();

            List<ReferencedFileSave> allRfses = GetAllReferencedFiles();

            foreach (ReferencedFileSave possibleRfs in allRfses)
            {
                if (possibleRfs != rfs && possibleRfs.Name == rfs.Name)
                {
                    toReturn.Add(possibleRfs);
                }
            }

            return toReturn;
        }


    }



}
