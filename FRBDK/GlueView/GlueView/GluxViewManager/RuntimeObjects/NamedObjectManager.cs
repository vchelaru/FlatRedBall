using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Graphics;
using FlatRedBall.Math;
using FlatRedBall.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Localization;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Math.Geometry;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math.Splines;

namespace FlatRedBall.Glue.RuntimeObjects
{
    public static class NamedObjectManager
    {
        
        public static object LoadObjectForNos<T>(NamedObjectSave namedObjectSave, IElement elementSave, Layer layerToPutOn,
            PositionedObjectList<ElementRuntime> listToPopulate, ElementRuntime elementRuntime) where T : new()
        {
            if (string.IsNullOrEmpty(namedObjectSave.SourceName) || 
                namedObjectSave.SourceName == "<NONE>" || 
                namedObjectSave.SetByDerived)
            {
                return null;
            }
            else
            {

                Type typeOfT = typeof(T);

                object toReturn = null;

                if (typeOfT == typeof(Scene) || typeOfT == typeof(ShapeCollection) || typeOfT == typeof(EmitterList) || typeOfT == typeof(SplineList))
                {
                    toReturn = LoadObject<T>(namedObjectSave, elementSave, layerToPutOn, listToPopulate, elementRuntime);
                }

                return toReturn;

            }
        }

        private static object LoadObject<T>(NamedObjectSave namedObjectSave, IElement elementSave, Layer layerToPutOn,
            PositionedObjectList<ElementRuntime> listToPopulate, ElementRuntime elementRuntime) where T : new()
        {
            int length = namedObjectSave.SourceName.Length;
            // need to use the last index of ( in case the name has a "(" in it)
            //int indexOfType = namedObjectSave.SourceName.IndexOf("(");
            int indexOfType = namedObjectSave.SourceName.LastIndexOf("(");

            string objectType = namedObjectSave.SourceName.Substring(indexOfType, length - (indexOfType));
            string sourceFile = namedObjectSave.SourceFile;

            object toReturn = null;
            object loadedObject = null;
            bool pullsFromEntireObject = false;
            ReferencedFileSave rfs = elementSave.GetReferencedFileSaveRecursively(sourceFile);

            loadedObject = GetObjectIfFileIsContained(sourceFile, elementRuntime);

            pullsFromEntireObject = loadedObject != null;

            if (loadedObject == null)
            {
                loadedObject = LoadObjectIntoElementRuntime<T>(namedObjectSave, elementRuntime, rfs, elementSave);

            }
            ElementRuntime newElementRuntime = CreateNewOrGetExistingElementRuntime(namedObjectSave, layerToPutOn, 
                listToPopulate, elementRuntime);

            object toAddTo = null;
            if (!namedObjectSave.IsEntireFile)
            {
                toAddTo = new T();
                newElementRuntime.ReferencedFileRuntimeList.Add(toAddTo);
            }

            Layer layerToAddTo = GetLayerToAddTo(namedObjectSave, layerToPutOn, elementRuntime);

            if (loadedObject != null)
            {
                // This might be null if the NOS references a file that doesn't exist.
                // This is usually not a valid circumstance but it's something that can
                // occur with tools modifying the .glux and not properly verifying that the
                // file exists.  GView should tolerate this invalid definition.

                toReturn = CreateRuntimeObjectForNamedObject(namedObjectSave, elementSave, elementRuntime, objectType,
                loadedObject,
                newElementRuntime,
                toAddTo,
                layerToAddTo, rfs, pullsFromEntireObject);

            }
            newElementRuntime.DirectObjectReference = toReturn;


            return toReturn;

        }

        private static object GetObjectIfFileIsContained(string sourceFile, ElementRuntime elementRuntime)
        {
            if(elementRuntime.EntireScenes.ContainsKey(sourceFile))
            {
                return elementRuntime.EntireScenes[sourceFile];
            }

            if(elementRuntime.EntireShapeCollections.ContainsKey(sourceFile))
            {
                return elementRuntime.EntireShapeCollections[sourceFile];
            }

            if(elementRuntime.EntireEmitterLists.ContainsKey(sourceFile))
            {
                return elementRuntime.EntireEmitterLists[sourceFile];
            }


            if(elementRuntime.EntireNodeNetworks.ContainsKey(sourceFile))
            {
                return elementRuntime.EntireNodeNetworks[sourceFile];
            }

            if(elementRuntime.EntireSplineLists.ContainsKey(sourceFile))
            {
                return elementRuntime.EntireSplineLists[sourceFile];
            }

            return null;

        }

        private static T LoadObjectIntoElementRuntime<T>(NamedObjectSave namedObjectSave, ElementRuntime elementRuntime, ReferencedFileSave rfs, IElement container)
        {
            object loadedObject = null;

            string fileName = FileManager.Standardize(ElementRuntime.ContentDirectory + namedObjectSave.SourceFile);

            if (rfs != null && (!rfs.IsSharedStatic || container is ScreenSave))
            {
                foreach (var kvp in elementRuntime.ReferencedFileRuntimeList.LoadedRfses)
                {
                    if (namedObjectSave.SourceFile.ToLower() == kvp.Key.ToLower())
                    {
                        loadedObject = kvp.Value;
                        break;
                    }
                }
            }
            else if (FileManager.FileExists(fileName))
            {
                loadedObject = FlatRedBallServices.Load<T>(fileName, GluxManager.ContentManagerName);
                elementRuntime.ReferencedFileRuntimeList.Add(loadedObject);
            }

            // If the rfs is only loaded when referenced, we want to give the
            // above a chance to see if it's already been loaded.  If it's not
            // then this is the first time it's referenced, so let's load it.
            if (rfs != null && rfs.LoadedOnlyWhenReferenced && loadedObject == null)
            {
                loadedObject = FlatRedBallServices.Load<T>(fileName, GluxManager.ContentManagerName);
                elementRuntime.ReferencedFileRuntimeList.Add(loadedObject);
                if (!rfs.IsSharedStatic || container is ScreenSave)
                {
                    AddObjectToManagers(loadedObject);
                }
            }
            return (T)loadedObject;
        }

        private static void AddObjectToManagers(object loadedObject)
        {
            if (loadedObject is Scene)
            {
                ((Scene)loadedObject).AddToManagers();
            }
            else if (loadedObject is ShapeCollection)
            {
                ((ShapeCollection)loadedObject).AddToManagers();
            }
            else if (loadedObject is EmitterList)
            {
                ((EmitterList)loadedObject).AddToManagers();
            }
            else if (loadedObject is SplineList)
            {
                ((SplineList)loadedObject).AddToManagers();
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        

        private static ElementRuntime CreateNewOrGetExistingElementRuntime(NamedObjectSave namedObjectSave, Layer layerToPutOn, PositionedObjectList<ElementRuntime> listToPopulate, ElementRuntime parent)
        {
            ElementRuntime newOrExisting = null;
            for (int i = 0; i < listToPopulate.Count; i++)
            {
                if (listToPopulate[i].AssociatedNamedObjectSave == namedObjectSave)
                {
                    newOrExisting = listToPopulate[i];
                    break;
                }
            }

            if (newOrExisting == null)
            {
                newOrExisting = new ElementRuntime(null, layerToPutOn, namedObjectSave, parent.CreationOptions.OnBeforeVariableSet, parent.CreationOptions.OnAfterVariableSet);
                newOrExisting.Name = namedObjectSave.InstanceName;
                listToPopulate.Add(newOrExisting);
            }

            return newOrExisting;
        }



        private static object CreateRuntimeObjectForNamedObject(NamedObjectSave objectToLoad, IElement container, ElementRuntime elementRuntime, string objectType,
            object objectJustLoaded,
            ElementRuntime newElementRuntime, 
            object toAddTo,
            Layer layerToAddTo, ReferencedFileSave rfs, bool pullsFromEntireNamedObject)
        {

            bool shouldClone = rfs != null && (rfs.IsSharedStatic && !(container is ScreenSave)) && !pullsFromEntireNamedObject;
            object toReturn = null;

            // This could have a ( in the name in a file like .scnx, so use the last (
            //int indexOfType = objectToLoad.SourceName.IndexOf("(");
            int indexOfType = objectToLoad.SourceName.LastIndexOf("(");

            string objectName = objectToLoad.SourceName.Substring(0, indexOfType - 1);

            switch (objectType)
            {
                case "(Scene)":
                    {
                        Scene scene = objectJustLoaded as Scene;

                        foreach (Text text in scene.Texts)
                        {
                            text.AdjustPositionForPixelPerfectDrawing = true;
                            if (ObjectFinder.Self.GlueProject.UsesTranslation)
                            {
                                text.DisplayText = LocalizationManager.Translate(text.DisplayText);
                            }
                        }

                        if (shouldClone)
                        {
                            scene = scene.Clone();
                            elementRuntime.EntireScenes.Add(objectToLoad.SourceFile, scene);

                            newElementRuntime.ReferencedFileRuntimeList.LoadedScenes.Add(scene);

                            scene.AddToManagers(layerToAddTo);

                        }
                        toReturn = scene;
                    }
                    break;


                case "(Sprite)":
                    {
                        Sprite loadedSprite = null;
                        Scene scene = objectJustLoaded as Scene;
                        if (scene != null)
                        {
                            loadedSprite = scene.Sprites.FindByName(objectName);
                        }

                        if (loadedSprite == null)
                        {
                            System.Windows.Forms.MessageBox.Show("There is a missing Sprite called\n\n" + objectName + "\n\n" +
                                "in the object\n\n" + elementRuntime.Name + "\n\n" +
                                "This probably happened if someone changed the name of a Sprite in a .scnx file but didn't update " +
                                "the associated object in Glue", "Missing Sprite");
                        }
                        else
                        {
                            if (shouldClone)
                            {
                                loadedSprite = loadedSprite.Clone();
                                (toAddTo as Scene).Sprites.Add(loadedSprite);
                                SpriteManager.AddToLayer(loadedSprite, layerToAddTo);

                            }
                        }
                        toReturn = loadedSprite;
                    }
                    break;


                case "(SpriteFrame)":
                    {
                        Scene scene = objectJustLoaded as Scene;
                        SpriteFrame loadedSpriteFrame = scene.SpriteFrames.FindByName(objectName);
                        if (loadedSpriteFrame != null)
                        {
                            if (shouldClone)
                            {
                                loadedSpriteFrame = loadedSpriteFrame.Clone();
                                (toAddTo as Scene).SpriteFrames.Add(loadedSpriteFrame);
                                SpriteManager.AddToLayer(loadedSpriteFrame, layerToAddTo);
                            }
                        }
                        toReturn = loadedSpriteFrame;
                    }
                    break;

                case "(SpriteGrid)":
                    {
                        Scene scene = objectJustLoaded as Scene;
                        SpriteGrid spriteGrid = null;
                        for (int i = 0; i < scene.SpriteGrids.Count; i++)
                        {
                            if (scene.SpriteGrids[i].Name == objectName)
                            {
                                spriteGrid = scene.SpriteGrids[i];
                                break;
                            }
                        }
                        if (spriteGrid != null)
                        {
                            if (shouldClone)
                            {
                                spriteGrid = spriteGrid.Clone();
                                (toAddTo as Scene).SpriteGrids.Add(spriteGrid);
                                spriteGrid.Layer = layerToAddTo;
                                spriteGrid.PopulateGrid();
                                spriteGrid.RefreshPaint();
                                spriteGrid.Manage();
                            }


                        }
                        toReturn = spriteGrid;
                    }
                    break;
                    
                case "(Text)":
                    {
                        Scene scene = objectJustLoaded as Scene;

                        Text loadedText = scene.Texts.FindByName(objectName);
                        if (loadedText != null)
                        {
                            if (shouldClone)
                            {
                                loadedText = loadedText.Clone();
                                (toAddTo as Scene).Texts.Add(loadedText);

                                TextManager.AddToLayer(loadedText, layerToAddTo);

                            }
                            loadedText.AdjustPositionForPixelPerfectDrawing = true;
                            if (LocalizationManager.HasDatabase)
                            {
                                loadedText.DisplayText = LocalizationManager.Translate(loadedText.DisplayText);
                            }
                        }
                        toReturn = loadedText;
                    }
                    break;
                case "(ShapeCollection)":
                    {
                        ShapeCollection shapeCollection = objectJustLoaded as ShapeCollection;

                        if (shouldClone)
                        {
                            shapeCollection = shapeCollection.Clone();
                            elementRuntime.EntireShapeCollections.Add(objectToLoad.SourceFile, shapeCollection);

                            newElementRuntime.ReferencedFileRuntimeList.LoadedShapeCollections.Add(shapeCollection);

                            shapeCollection.AddToManagers(layerToAddTo);

                        }
                        // Most cases are handled below in an AttachTo method, but 
                        // ShapeCollection isn't a PositionedObject so we have to do it manually here
                        if (objectToLoad.AttachToContainer)
                        {
                            shapeCollection.AttachTo(elementRuntime, true);
                        }
                        toReturn = shapeCollection;
                    }
                    break;

                case "(AxisAlignedCube)":
                    {
                        ShapeCollection shapeCollection = objectJustLoaded as ShapeCollection;
                        AxisAlignedCube loadedAxisAlignedCube = shapeCollection.AxisAlignedCubes.FindByName(objectName);


                        if (shouldClone)
                        {
                            loadedAxisAlignedCube = loadedAxisAlignedCube.Clone();
                            (toAddTo as ShapeCollection).AxisAlignedCubes.Add(loadedAxisAlignedCube);
                            ShapeManager.AddToLayer(loadedAxisAlignedCube, layerToAddTo);
                        }

                        toReturn = loadedAxisAlignedCube;
                    }
                    break;


                case "(AxisAlignedRectangle)":
                    {
                        ShapeCollection shapeCollection = objectJustLoaded as ShapeCollection;
                        AxisAlignedRectangle loadedAxisAlignedRectangle = shapeCollection.AxisAlignedRectangles.FindByName(objectName);


                        if (shouldClone)
                        {
                            loadedAxisAlignedRectangle = loadedAxisAlignedRectangle.Clone();
                            (toAddTo as ShapeCollection).AxisAlignedRectangles.Add(loadedAxisAlignedRectangle);
                            ShapeManager.AddToLayer(loadedAxisAlignedRectangle, layerToAddTo);
                        }

                        toReturn = loadedAxisAlignedRectangle;
                    }
                    break;

                case "(Circle)":
                    {
                        ShapeCollection shapeCollection = objectJustLoaded as ShapeCollection;
                        Circle loadedCircle = shapeCollection.Circles.FindByName(objectName);


                        if (shouldClone)
                        {
                            loadedCircle = loadedCircle.Clone();
                            (toAddTo as ShapeCollection).Circles.Add(loadedCircle);
                            ShapeManager.AddToLayer(loadedCircle, layerToAddTo);
                        }

                        toReturn = loadedCircle;
                    }
                    break;

                case "(Polygon)":
                    {
                        ShapeCollection shapeCollection = objectJustLoaded as ShapeCollection;
                        Polygon loadedPolygon = shapeCollection.Polygons.FindByName(objectName);


                        if (shouldClone)
                        {
                            loadedPolygon = loadedPolygon.Clone();
                            (toAddTo as ShapeCollection).Polygons.Add(loadedPolygon);
                            ShapeManager.AddToLayer(loadedPolygon, layerToAddTo);
                        }

                        toReturn = loadedPolygon;
                    }
                    break;

                case "(Sphere)":
                    {
                        ShapeCollection shapeCollection = objectJustLoaded as ShapeCollection;
                        Sphere loadedSphere = shapeCollection.Spheres.FindByName(objectName);


                        if (shouldClone)
                        {
                            loadedSphere = loadedSphere.Clone();
                            (toAddTo as ShapeCollection).Spheres.Add(loadedSphere);
                            ShapeManager.AddToLayer(loadedSphere, layerToAddTo);
                        }

                        toReturn = loadedSphere;
                    }
                    break;

                case "(Capsule2D)":
                    {
                        ShapeCollection shapeCollection = objectJustLoaded as ShapeCollection;
                        Capsule2D loadedCapsule2D = shapeCollection.Capsule2Ds.FindByName(objectName);


                        if (shouldClone)
                        {
                            loadedCapsule2D = loadedCapsule2D.Clone();
                            (toAddTo as ShapeCollection).Capsule2Ds.Add(loadedCapsule2D);
                            ShapeManager.AddToLayer(loadedCapsule2D, layerToAddTo);
                        }

                        toReturn = loadedCapsule2D;
                    }
                    break;
                case "(Emitter)":
                    {
                        EmitterList emitterList = objectJustLoaded as EmitterList;
                        Emitter loadedEmitter = emitterList.FindByName(objectName);

                        if (shouldClone && loadedEmitter != null)
                        {
                            loadedEmitter = loadedEmitter.Clone();
                            (toAddTo as EmitterList).Add(loadedEmitter);
                            SpriteManager.AddEmitter(loadedEmitter, layerToAddTo);
                        }
                        toReturn = loadedEmitter;
                    }
                    break;
                case "(EmitterList)":
                    {
                        EmitterList emitterList = objectJustLoaded as EmitterList;

                        if(shouldClone && emitterList != null)
                        {
                            emitterList = emitterList.Clone();

                            foreach(var item in emitterList)
                            {
                                SpriteManager.AddEmitter(item);
                            }
                        }

                        toReturn = emitterList;
                    }
                    break;
                case "(NodeNetwork)":
                    {
                        NodeNetwork nodeNetwork = objectJustLoaded as NodeNetwork;

                        if (shouldClone)
                        {
                            nodeNetwork = nodeNetwork.Clone();

                            elementRuntime.EntireNodeNetworks.Add(objectToLoad.SourceFile, nodeNetwork);

                            newElementRuntime.ReferencedFileRuntimeList.LoadedNodeNetworks.Add(nodeNetwork);

                            // don't need to add NodeNetworks to managers - this will be done if it is visible automatically
                        }

                        // can't attach the NodeNetwork 
                        toReturn = nodeNetwork;
                    }
                    break;
                case "(SplineList)":
                    {
                        SplineList splineList = objectJustLoaded as SplineList;

                        if (shouldClone)
                        {
                            splineList = splineList.Clone();

                            elementRuntime.EntireSplineLists.Add(splineList.Name, splineList);

                            newElementRuntime.ReferencedFileRuntimeList.Add(splineList);

                            foreach (var spline in splineList)
                            {
                                spline.CalculateVelocities();
                                spline.CalculateAccelerations();
                            }

                            splineList.AddToManagers();
                            

                            splineList[0].UpdateShapes();
                        }

                        toReturn = splineList;

                    }
                    break;

                case "(Spline)":
                    {
                        SplineList splineList = objectJustLoaded as SplineList;
                        Spline spline = splineList.FirstOrDefault(item=>item.Name == objectName);

                        
                        if (shouldClone && spline != null)
                        {
                            spline = spline.Clone();
                            (toAddTo as SplineList).Add(spline);

                            // Eventually support layers?
                            //ShapeManager.AddToLayer(spline, layerToAddTo);
                        }
                        spline.CalculateVelocities();
                        spline.CalculateAccelerations();

                        toReturn = spline;
                    }
                    break;
            }


            if (toReturn != null && objectToLoad.AttachToContainer)
            {
                if (toReturn is PositionedObject)
                {
                    // If the object is already attached to something, that means it
                    // came from a file, so we don't want to re-attach it.
                    PositionedObject asPositionedObject = toReturn as PositionedObject;
                    if (asPositionedObject.Parent == null)
                    {
                        asPositionedObject.AttachTo(elementRuntime, true);
                    }
                }
                else if (toReturn is Scene)
                {
                    ((Scene)toReturn).AttachAllDetachedTo(elementRuntime, true);
                }
                else if (toReturn is ShapeCollection)
                {
                    ((ShapeCollection)toReturn).AttachAllDetachedTo(elementRuntime, true);
                }
            }


            return toReturn;
        }



        private static Layer GetLayerToAddTo(NamedObjectSave objectToLoad, Layer layerToPutOn, ElementRuntime elementRuntime)
        {
            Layer layerToAddTo = layerToPutOn;

            if (!string.IsNullOrEmpty(objectToLoad.LayerOn))
            {

                if (objectToLoad.LayerOn == "Under Everything (Engine Layer)")
                {
                    layerToAddTo = SpriteManager.UnderAllDrawnLayer;
                }
                else if(objectToLoad.LayerOn == "Top Layer (Engine Layer)")
                {
                    layerToAddTo = SpriteManager.TopLayer;
                }
                else
                {
                    ElementRuntime layerContainer = elementRuntime.GetContainedElementRuntime(objectToLoad.LayerOn);
                    if (layerContainer == null)
                    {
                        System.Windows.Forms.MessageBox.Show("Could not find a Layer by the name \"" + objectToLoad.LayerOn + "\" in the object " + objectToLoad, "Layer not found");
                    }
                    else
                    {
                        layerToAddTo = ((Layer)layerContainer.DirectObjectReference);
                    }
                }

            }
            return layerToAddTo;
        }




    }
}
