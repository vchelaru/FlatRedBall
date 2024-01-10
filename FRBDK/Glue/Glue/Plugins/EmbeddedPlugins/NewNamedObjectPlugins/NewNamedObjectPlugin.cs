using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using EditorObjects.IoC;
using FlatRedBall.Glue.SetVariable;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.NewNamedObjectPlugins
{
    [Export(typeof(PluginBase))]
    public class NewNamedObjectPlugin : EmbeddedPlugin
    {
        public override void StartUp()
        {
            this.ReactToNewObjectHandler += AdjustNewNamedObject;
        }

        void AdjustNewNamedObject(NamedObjectSave nos)
        {
            //ExportedImplementations.GlueState.Self.CurrentElement;
            var element = ObjectFinder.Self.GetElementContaining(nos) ?? GlueState.Self.CurrentElement;

            if (element != null)
            {
                if(nos.SourceType == SourceType.FlatRedBallType)
                {
                    var ati = nos.GetAssetTypeInfo();
                    if(ati == AvailableAssetTypes.CommonAtis.Sprite)
                    {
                        AdjustSprite(nos, element);
                    }
                    else if(ati == AvailableAssetTypes.CommonAtis.Circle)
                    {
                        AdjustCircle(nos, element);
                    }
                    else if(nos.GetAssetTypeInfo()?.FriendlyName == "SpriteFrame")
                    {
                        AdjustSpriteFrame(nos, element);
                    }
                    else if(ati == AvailableAssetTypes.CommonAtis.AxisAlignedRectangle)
                    {
                        AdjustAxisAlignedRectangle(nos, element);
                    }
                    else if(ati == AvailableAssetTypes.CommonAtis.Layer)
                    {
                        AdjustLayer(nos, element);
                    }
                    else if(ati == AvailableAssetTypes.CommonAtis.ShapeCollection)
                    {
                        AdjustShapeCollection(nos, element);
                    }
                    else if(ati == AvailableAssetTypes.CommonAtis.PositionedObjectList)
                    {
                        AdjustPositionedObjectList(nos, element);
                    }
                }
            }

        }

        private static void AdjustPositionedObjectList(NamedObjectSave nos, IElement element)
        {
            nos.ExposedInDerived = true;
            Container.Get<NamedObjectSetVariableLogic>().ReactToNamedObjectChangedValue(nameof(nos.ExposedInDerived), false,
                namedObjectSave: nos);
        }

        private void AdjustShapeCollection(NamedObjectSave nos, IElement element)
        {
            nos.ExposedInDerived = true;
            Container.Get<NamedObjectSetVariableLogic>().ReactToNamedObjectChangedValue(nameof(nos.ExposedInDerived), false,
                namedObjectSave: nos);
        }

        private void AdjustLayer(NamedObjectSave nos, IElement element)
        {
            if (Is2D(element))
            {
                nos.Is2D = true;
            }
        }

        private void AdjustCircle(NamedObjectSave nos, IElement element)
        {
            if (Is2D(element))
            {
                if (nos.GetCustomVariable("Radius")?.Value == null)
                {
                    nos.SetVariable("Radius", 16f);
                }
            }
        }

        private bool Is2D(IElement element)
        {
            return (element is EntitySave && (element as EntitySave).Is2D) ||
                ObjectFinder.Self.GlueProject.In2D;
        }

        private void AdjustSpriteFrame(NamedObjectSave nos, IElement element)
        {

            if (Is2D(element))
            {
                nos.SetVariable("PixelSize", .5f);
            }
        }

        private void AdjustAxisAlignedRectangle(NamedObjectSave nos, IElement element)
        {
            if (Is2D(element))
            {
                // Don't overwrite it if it already has a value (like from a game)
                if(nos.GetCustomVariable("Width")?.Value == null)
                {
                    nos.SetVariable("Width", 32f);
                }

                if (nos.GetCustomVariable("Height")?.Value == null)
                {
                    nos.SetVariable("Height", 32f);
                }
            }
        }

        private void AdjustSprite(NamedObjectSave nos, IElement element)
        {
            if (Is2D(element))
            {
                if (nos.GetCustomVariable("TextureScale")?.Value == null)
                {
                    nos.SetVariable("TextureScale", 1.0f);
                }


            }
        }
    }
}
