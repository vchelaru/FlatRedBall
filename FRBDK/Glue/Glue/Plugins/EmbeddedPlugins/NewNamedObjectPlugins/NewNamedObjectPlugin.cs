using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;

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
            IElement element = EditorLogic.CurrentElement;

            if (element != null)
            {
                if(nos.SourceType == SourceType.FlatRedBallType)
                {
                    switch (nos.SourceClassType)
                    {
                        case "Sprite":
                            AdjustSprite(nos, element);
                            break;
                        case "Circle":
                            AdjustCircle(nos, element);
                            break;
                        case "SpriteFrame":
                            AdjustSpriteFrame(nos, element);
                            break;
                        case "AxisAlignedRectangle":
                            AdjustAxisAlignedRectangle(nos, element);
                            break;
                        case "Layer":
                            AdjustLayer(nos, element);
                            break;


                    }

                }
            }

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

                nos.SetPropertyValue("Radius", 16f);
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

                nos.SetPropertyValue("PixelSize", .5f);

            }
        }

        private void AdjustAxisAlignedRectangle(NamedObjectSave nos, IElement element)
        {
            if (Is2D(element))
            {
                nos.SetPropertyValue("Width", 32f);
                nos.SetPropertyValue("Height", 32f);
            }
        }

        private void AdjustSprite(NamedObjectSave nos, IElement element)
        {
            if (Is2D(element))
            {
                nos.SetPropertyValue("TextureScale", 1.0f);


            }
        }
    }
}
