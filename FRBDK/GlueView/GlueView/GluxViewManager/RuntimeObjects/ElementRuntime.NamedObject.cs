using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Math;
using FlatRedBall.Graphics;
using System.Windows.Forms;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FlatRedBall.Glue.RuntimeObjects;
using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Math.Splines;
using FlatRedBall.Glue.RuntimeObjects.File;

namespace FlatRedBall.Glue
{
    public partial class ElementRuntime
    {
        private object CreateFlatRedBallTypeNos(NamedObjectSave namedObjectSave, 
            PositionedObjectList<ElementRuntime> listToPopulate, Layer layerToPutOn)
        {
            object returnObject = null;

            ElementRuntime newElementRuntime = null;

            switch (namedObjectSave.SourceClassType)
            {
                case "Layer":
                case "FlatRedBall.Graphics.Layer":
                    returnObject = CreateLayerObject(namedObjectSave, returnObject);
                    break;
                case "AxisAlignedRectangle":
                case "FlatRedBall.Math.Geometry.AxisAlignedRectangle":
                    AxisAlignedRectangle aaRectangle = ShapeManager.AddAxisAlignedRectangle();
                    if (layerToPutOn != null)
                    {
                        ShapeManager.AddToLayer(aaRectangle, layerToPutOn);
                    }
                    aaRectangle.Name = namedObjectSave.InstanceName;
                    returnObject = aaRectangle;
                    break;
                case "Camera":
                case "FlatRedBall.Camera":
                    if (namedObjectSave.IsNewCamera)
                    {
                        returnObject = null;
                    }
                    else
                    {
                        returnObject = SpriteManager.Camera;
                    }
                    break;
                case "Circle":
                case "FlatRedBall.Math.Geometry.Circle":
                    Circle circle = ShapeManager.AddCircle();
                    circle.Name = namedObjectSave.InstanceName;
                    if (layerToPutOn != null)
                    {
                        ShapeManager.AddToLayer(circle, layerToPutOn);
                    }
                    returnObject = circle;

                    break;
                case "Polygon":
                case "FlatRedBall.Math.Geometry.Polygon":
                    Polygon polygon = ShapeManager.AddPolygon();
                    polygon.Name = namedObjectSave.InstanceName;

                    if(layerToPutOn != null)
                    {
                        ShapeManager.AddToLayer(polygon, layerToPutOn);
                    }
                    returnObject = polygon;

                    break;
                case "Sprite":
                case "FlatRedBall.Sprite":
                    Sprite sprite = SpriteManager.AddSprite((Texture2D)null);
                    if (layerToPutOn != null)
                    {
                        SpriteManager.AddToLayer(sprite, layerToPutOn);
                    }
                    sprite.Name = namedObjectSave.InstanceName;
                    returnObject = sprite;
                    break;
                case "SpriteFrame":
                case "FlatRedBall.ManagedSpriteGroups.SpriteFrame":
                    SpriteFrame spriteFrame = SpriteManager.AddSpriteFrame(null, SpriteFrame.BorderSides.All);
                    if (layerToPutOn != null)
                    {
                        SpriteManager.AddToLayer(spriteFrame, layerToPutOn);
                    }
                    spriteFrame.Name = namedObjectSave.InstanceName;
                    returnObject = spriteFrame;
                    break;
                case "Text":
                case "FlatRedBall.Graphics.Text":
                    Text text = TextManager.AddText("");
                    if (layerToPutOn != null)
                    {
                        TextManager.AddToLayer(text, layerToPutOn);
                        text.SetPixelPerfectScale(layerToPutOn);
                    }
                    text.Name = namedObjectSave.InstanceName;
                    returnObject = text;
                    break;
                case "Scene":
                case "FlatRedBall.Scene":
                    Scene scene = new Scene();

                    scene.Name = namedObjectSave.InstanceName;
                    returnObject = scene;
                    break;
                default:
                    // do nothing - need to add more types?
                    break;
            }

            if (returnObject != null)
            {
                if (returnObject is IScalable)
                {
                    newElementRuntime = new ScalableElementRuntime();
                }
                else
                {
                    newElementRuntime = new ElementRuntime();
                }
                newElementRuntime.Initialize(null, layerToPutOn, namedObjectSave, CreationOptions.OnBeforeVariableSet, CreationOptions.OnAfterVariableSet);
                newElementRuntime.mDirectObjectReference = returnObject;

                if (returnObject is Camera && !namedObjectSave.IsNewCamera)
                {
                    SpriteManager.Camera.AttachTo(newElementRuntime, false);
                    SpriteManager.Camera.RelativePosition = Vector3.Zero;
                    newElementRuntime.Z = 40;
                    newElementRuntime.Name = namedObjectSave.InstanceName;
                }
                else if (returnObject is FlatRedBall.Utilities.INameable)
                {
                    newElementRuntime.Name = ((FlatRedBall.Utilities.INameable)returnObject).Name;
                }
                else
                {
                    object nameValueAsObject;
                    if (LateBinder.TryGetValueStatic(returnObject, "Name", out nameValueAsObject))
                    {

                        newElementRuntime.Name = (string)nameValueAsObject;

                    }
                }

                listToPopulate.Add(newElementRuntime);
            }

            return returnObject;
        }

        private object CreateLayerObject(NamedObjectSave namedObjectSave, object returnObject)
        {
            Layer newLayer = SpriteManager.AddLayer();
            // SpriteManager.MoveToFront(newLayer); 
            newLayer.Name = namedObjectSave.InstanceName;
            if (namedObjectSave.Is2D)
            {
                newLayer.UsePixelCoordinates();
                if (namedObjectSave.DestinationRectangle.HasValue)
                {
                    var rectangle = namedObjectSave.DestinationRectangle.Value;

                    if (namedObjectSave.LayerCoordinateUnit == LayerCoordinateUnit.Pixel)
                    {
                        newLayer.LayerCameraSettings.LeftDestination = rectangle.X;
                        newLayer.LayerCameraSettings.RightDestination = rectangle.X + rectangle.Width;

                        newLayer.LayerCameraSettings.TopDestination = rectangle.Y;
                        newLayer.LayerCameraSettings.BottomDestination = rectangle.Y + rectangle.Height;

                        newLayer.LayerCameraSettings.OrthogonalWidth = newLayer.LayerCameraSettings.RightDestination -
                            newLayer.LayerCameraSettings.LeftDestination;

                        newLayer.LayerCameraSettings.OrthogonalHeight = newLayer.LayerCameraSettings.BottomDestination -
                            newLayer.LayerCameraSettings.TopDestination;
                    }
                    else if (namedObjectSave.LayerCoordinateUnit == LayerCoordinateUnit.Percent)
                    {
                        newLayer.LayerCameraSettings.LeftDestination = FlatRedBall.Math.MathFunctions.RoundToInt(rectangle.X * SpriteManager.Camera.DestinationRectangle.Width * .01f);
                        newLayer.LayerCameraSettings.RightDestination = FlatRedBall.Math.MathFunctions.RoundToInt((rectangle.X + rectangle.Width) * SpriteManager.Camera.DestinationRectangle.Width * .01f);

                        newLayer.LayerCameraSettings.TopDestination = FlatRedBall.Math.MathFunctions.RoundToInt(rectangle.Y * SpriteManager.Camera.DestinationRectangle.Height * .01f);
                        newLayer.LayerCameraSettings.BottomDestination = FlatRedBall.Math.MathFunctions.RoundToInt((rectangle.Y + rectangle.Height) * SpriteManager.Camera.DestinationRectangle.Height * .01f);

                        newLayer.LayerCameraSettings.OrthogonalWidth = SpriteManager.Camera.OrthogonalWidth * (rectangle.Width) / 100.0f;

                        newLayer.LayerCameraSettings.OrthogonalHeight = SpriteManager.Camera.OrthogonalHeight * (rectangle.Height) / 100.0f;

                    }
                    else
                    {
                        throw new NotImplementedException();
                    }





                    if (newLayer.LayerCameraSettings.RightDestination > SpriteManager.Camera.DestinationRectangle.Width)
                    {
                        MessageBox.Show("The Layer named " + namedObjectSave.InstanceName +
                            " has a right-side of " + newLayer.LayerCameraSettings.RightDestination +
                            " but the resolution width is only " + SpriteManager.Camera.DestinationRectangle.Width +
                            ". Setting the Layer's Right to the Camera's width.  This may cause crashes in your game, so you will want to change this value",
                            "Layer out of screen");
                        newLayer.LayerCameraSettings.RightDestination = SpriteManager.Camera.DestinationRectangle.Width ;
                    }
                    if (newLayer.LayerCameraSettings.BottomDestination > SpriteManager.Camera.DestinationRectangle.Bottom)
                    {
                        MessageBox.Show("The Layer named " + namedObjectSave.InstanceName +
                            " has a bottom-side of " + newLayer.LayerCameraSettings.BottomDestination +
                            " but the resolution height is only " + SpriteManager.Camera.DestinationRectangle.Height +
                            ". Setting the Layer's Bottom to the Camera's height.  This may cause crashes in your game, so you will want to change this value",
                            "Layer out of screen");
                        newLayer.LayerCameraSettings.BottomDestination = SpriteManager.Camera.DestinationRectangle.Bottom;

                    }
                }

                if (namedObjectSave.LayerCoordinateType == LayerCoordinateType.MatchCamera)
                {

                    if (namedObjectSave.DestinationRectangle.HasValue)
                    {
                        var rectangle = namedObjectSave.DestinationRectangle.Value;

                        float ratioX = 0;
                        float ratioY = 0;

                        if (namedObjectSave.LayerCoordinateUnit == LayerCoordinateUnit.Pixel)
                        {
                            ratioX = rectangle.Width / (float)SpriteManager.Camera.DestinationRectangle.Width;
                            ratioY = rectangle.Height / (float)SpriteManager.Camera.DestinationRectangle.Height;
                        }
                        else
                        {
                            ratioX = rectangle.Width / 100.0f;
                            ratioY = rectangle.Height / 100.0f;
                        }

                        newLayer.LayerCameraSettings.OrthogonalWidth = SpriteManager.Camera.OrthogonalWidth * ratioX;
                        newLayer.LayerCameraSettings.OrthogonalHeight = SpriteManager.Camera.OrthogonalHeight * ratioY;
                    }
                    else
                    {
                        newLayer.LayerCameraSettings.OrthogonalWidth = SpriteManager.Camera.OrthogonalWidth;
                        newLayer.LayerCameraSettings.OrthogonalHeight = SpriteManager.Camera.OrthogonalHeight;
                    }
                }
            }

            returnObject = newLayer;
            return returnObject;
        }

        private void SetInstanceVariablesOnNamedObjects()
        {
            for (int i = 0; i < mContainedElements.Count; i++)
            {
                ElementRuntime elementRuntime = mContainedElements[i];

                SetVariablesOnElementRuntime(elementRuntime);
            }

            for (int i = 0; i < this.mElementsInList.Count; i++)
            {
                ElementRuntime elementRuntime = mElementsInList[i];
                SetVariablesOnElementRuntime(elementRuntime);
            }


        }

        private void SetVariablesOnElementRuntime(ElementRuntime elementRuntime)
        {
            object objectToSet = elementRuntime.mDirectObjectReference;

            if (objectToSet == null)
            {
                objectToSet = elementRuntime;
            }

            // If the IElement
            // that contains the
            // elementRuntime has
            // a base IElement, then
            // the elementRuntime's NamedObjectSave
            // may be defined in a base object, and the
            // derived classes may provide a chain of overriden
            // variables.
            string name = elementRuntime.mAssociatedNamedObjectSave.InstanceName;

            IElement element = this.AssociatedIElement;
            List<NamedObjectSave> nosList = new List<NamedObjectSave>();
            do
            {
                NamedObjectSave nos = element.GetNamedObject(name);

                if (nos != null && (nos.DefinedByBase || nos == elementRuntime.mAssociatedNamedObjectSave))
                {
                    nosList.Add(nos);
                }

                element = ObjectFinder.Self.GetIElement(element.BaseElement);

            } while (element != null);


            // Start with the most derived, so do a reverse for loop
            for (int i = nosList.Count - 1; i > -1; i--)
            {
                SetVariablesForNamedObject(objectToSet, nosList[i]);
            }
        }

        private void SetVariablesForNamedObject(object newlyCreatedElementRuntime, NamedObjectSave n)
        {

            if (newlyCreatedElementRuntime != null)
            {
                ElementRuntime containedElementRuntime = GetContainedElementRuntime(n);

                Type typeOfNewObject = newlyCreatedElementRuntime.GetType();

                // As of June 24, 2012
                // States are set before
                // CustomVariables:
                if (!string.IsNullOrEmpty(n.CurrentState))
                {
                    containedElementRuntime.SetState(n.CurrentState, false);
                }


                // If it's null, this means it's a default value so don't set anything               
                foreach (CustomVariableInNamedObject cvino in n.InstructionSaves.Where(cvino=>cvino.Value != null))
                {
                    // If there isn't a TypedMember for the variable, that means Glue won't generate code for it, so we shouln't be applying it.
                    if (!ShouldApplyVariableOnRuntime(cvino, n))
                    {

                        continue;
                    }

                    try
                    {
                        // We used to execute 
                        // the Instructions right
                        // on the element runtime itself
                        // but I think it's best if we use
                        // the CustomVariable code so that we
                        // get all the benefit of CustomVariables
                        // like loading of files, and calling events.
                        //cvino.ToInstruction(newlyCreatedElementRuntime).Execute();

                        // Update May 25, 2012
                        // We used to get the ElementRuntime
                        // for the NamedObjectSave and have it
                        // set the CustomVariable - however this
                        // doesn't work because it tries to access
                        // files within its own scope, instead of files
                        // that belong to "this".
                        //CustomVariable customVariable = new CustomVariable();
                        //customVariable.Type = cvino.Type;
                        ////customVariable.SourceObject = n.InstanceName;
                        ////customVariable.SourceObjectProperty = cvino.Member;
                        //customVariable.DefaultValue = cvino.Value;
                        //customVariable.Name = cvino.Member;
                        //containedElementRuntime.SetCustomVariable(customVariable, customVariable.DefaultValue, false);

                        CustomVariable customVariable = new CustomVariable();
                        customVariable.Type = cvino.Type;
                        customVariable.SourceObject = n.InstanceName;
                        customVariable.SourceObjectProperty = cvino.Member;
                        customVariable.DefaultValue = cvino.Value;
                        customVariable.Name = cvino.Member;
                        if (cvino.Value is string && customVariable.GetIsFile())
                        {
                            // We want to load the file at this level and pass the result down:
                            ReferencedFileSave rfs = GetReferencedFileFromName(cvino.Value);
                            object fileRuntime = null;

                            if (rfs == null)
                            {
                                fileRuntime = null;
                            }
                            else
                            {
                                fileRuntime = LoadReferencedFileSave(rfs, true, this.AssociatedIElement);
                            }

                            customVariable.DefaultValue = fileRuntime;
                        }
                        if (customVariable.DefaultValue is float && customVariable.SourceObjectProperty == "Z" &&
                            newlyCreatedElementRuntime is PositionedObject && ((PositionedObject)newlyCreatedElementRuntime).Parent == Camera.Main)
                        {
                            float value = (float)customVariable.DefaultValue - 40;

                            customVariable.DefaultValue = value;

                        }
                        SetCustomVariable(customVariable, mAssociatedIElement, customVariable.DefaultValue, false);
                        
                    }
                    catch(Exception e)
                    {
                        int m = 3;
                        m++;
                        // for now, do nothing
                    }

                }
            }
        }

        private bool ShouldApplyVariableOnRuntime(CustomVariableInNamedObject cvino, NamedObjectSave n)
        {
            bool shouldApply = 
                // December 16, 2012 Victor Chelaru
                // I think that even if the NOS isn't
                // a Entity, the TypedMembers should still
                // have all properties shouldn't they?  It looks
                // like they do according to my tests...
                //n.SourceType != SourceType.Entity ||
                n.TypedMembers.FirstOrDefault(member => member.MemberName == cvino.Member) != null;

            if(!shouldApply)
            {
                // Check for special cases - variables whihch can't be exposed, but which should still be aplied:
                if(cvino.Member == "Points" && n.SourceType == SourceType.FlatRedBallType && n.ClassType == "Polygon")
                {
                    shouldApply = true;
                }
            }

            return shouldApply;
        }

        private object LoadFileObject(NamedObjectSave objectToLoad, IElement elementSave, Layer layerToPutOn,
            PositionedObjectList<ElementRuntime> listToPopulate)
        {
            string extension = FileManager.GetExtension(objectToLoad.SourceFile).ToLower();
            object returnObject = null;

            /////////////////////////////////EARLY OUT!//////////////////////////////////
            if (objectToLoad.SetByDerived)
            {
                return null;
            }
            //////////////////////////////END EARLY OUT//////////////////////////////////

            returnObject = CreateObjectBasedOnExtension(objectToLoad, elementSave, layerToPutOn, listToPopulate, extension);

            return returnObject;
        }

        private LoadedFile CreateObjectBasedOnExtension(NamedObjectSave objectToLoad, IElement elementSave, Layer layerToPutOn, PositionedObjectList<ElementRuntime> listToPopulate, string extension)
        {
            LoadedFile returnObject = null;
            if(!string.IsNullOrEmpty(objectToLoad.SourceFile))
            {
                returnObject = NamedObjectManager.LoadObjectForNos(objectToLoad, elementSave, layerToPutOn, listToPopulate, this);
                //ReferencedFileSave rfs = elementSave.GetReferencedFileSaveRecursively(
                //    objectToLoad.SourceFile);

                //if(rfs != null)
                //{
                //    returnObject = LoadReferencedFileSave(rfs, true, elementSave);
                //}
            }

            //switch (extension)
            //{
            //    case "scnx":
            //        returnObject = NamedObjectManager.LoadObjectForNos<Scene>(objectToLoad, elementSave, layerToPutOn, listToPopulate, this);
            //        break;

            //    case "shcx":
            //        returnObject = NamedObjectManager.LoadObjectForNos<ShapeCollection>(objectToLoad, elementSave, layerToPutOn, listToPopulate, this);
            //        break;
            //    case "nntx":
            //        //returnObject = NamedObjectManager.LoadNodeNetworkObject(objectToLoad, elementSave, layerToPutOn, listToPopulate, entireFileOnly);
            //        break;
            //    case "emix":
            //        returnObject = NamedObjectManager.LoadObjectForNos<EmitterList>(objectToLoad, elementSave, layerToPutOn, listToPopulate, this);
            //        break;
            //    case "splx":
            //        returnObject = NamedObjectManager.LoadObjectForNos<SplineList>(objectToLoad, elementSave, layerToPutOn, listToPopulate, this);
            //        break;
            //    default:
            //        // todo - loop through the custom object creators here:
            //        break;
            //}
            return returnObject;
        }

        private void CreateNamedObjectElementRuntime(IElement elementSave, Layer layerProvidedByContainer, List<NamedObjectSave> namedObjectSaveList,
            PositionedObjectList<ElementRuntime> listToPopulate, PositionedObject parentElementRuntime)
        {
            
            foreach (NamedObjectSave n in namedObjectSaveList)
            {
                Object newObject = null;

                if (ShouldElementRuntimeBeCreatedForNos(n, elementSave))
                {
                    Layer layerToPutOn = GetLayerForNos(layerProvidedByContainer, n);

                    switch (n.SourceType)
                    {

                        case SourceType.File:

                            newObject = LoadFileObject(n, elementSave, layerToPutOn, listToPopulate);
                            
                            break;


                        case SourceType.Entity:

                            newObject = LoadEntityObject(n, layerToPutOn, listToPopulate);

                            break;


                        case SourceType.FlatRedBallType:
                            newObject = CreateFlatRedBallTypeNos(n, listToPopulate, layerToPutOn);
                            break;
                    }
                }
                if (newObject != null && newObject is PositionedObject)
                {
                    PositionedObject attachTo;
                    float parentZToSet;
                    GetWhatToAttachToForNewNos(parentElementRuntime, n, newObject, out attachTo, out parentZToSet);

                    if (attachTo != null)
                    {
                        Vector3 oldPosition = attachTo.Position;
                        Matrix oldRotationMatrix = attachTo.RotationMatrix;

                        attachTo.Position = new Vector3();



                        attachTo.Z = parentZToSet;
                        attachTo.RotationMatrix = Matrix.Identity;

                        ((PositionedObject)newObject).AttachTo(attachTo, true);

                        attachTo.Position = oldPosition;
                        attachTo.RotationMatrix = oldRotationMatrix;
                    }
                }
            }
        }

        private Graphics.Layer GetLayerForNos(Layer layerProvidedByContainer, NamedObjectSave n)
        {
            Layer layerToPutOn = layerProvidedByContainer;
            // If the NOS specifies its own Layer, handle that:

            if (!string.IsNullOrEmpty(n.LayerOn))
            {

                if (n.LayerOn == "Under Everything (Engine Layer)")
                {
                    layerToPutOn = SpriteManager.UnderAllDrawnLayer;
                }
                else if (n.LayerOn == "Top Layer (Engine Layer)")
                {
                    layerToPutOn = SpriteManager.TopLayer;
                }
            
                else
                {
                    ElementRuntime layerContainer = GetContainedElementRuntime(n.LayerOn);
                    if (layerContainer != null)
                    {
                        layerToPutOn = ((Layer)layerContainer.mDirectObjectReference);
                    }
                }
            }
            return layerToPutOn;
        }

        private static void GetWhatToAttachToForNewNos(PositionedObject parentElementRuntime, NamedObjectSave n, Object newObject, out PositionedObject attachTo, out float parentZToSet)
        {
            attachTo = null;
            parentZToSet = 0;
            if (n.AttachToCamera &&
                // AttachToCamera is not allowed on objects within an Entity
                // This can lead to confusing behavior.  Technically the NOS itself
                // could have AttachToCamera set to true if Glue has messed up somehow
                // or if the user has manually edited the .glux.  However, even if this
                // is the case, we don't want to perform an attachment.
                (parentElementRuntime is ElementRuntime == false ||
                    ((ElementRuntime)parentElementRuntime).mAssociatedIElement is EntitySave == false))
            {
                parentZToSet = 40;
                attachTo = SpriteManager.Camera;
            }
            else if (n.AttachToContainer)
            {
                if (parentElementRuntime is ElementRuntime == false ||
                    ((ElementRuntime)parentElementRuntime).mAssociatedIElement is ScreenSave == false)
                {
                    attachTo = parentElementRuntime;
                }

                if (newObject is PositionedObject)
                {
                    // It's already attached to something else in its files
                    if (((PositionedObject)newObject).Parent != null)
                    {
                        attachTo = null;
                    }
                }
            }
        }


    }
}
