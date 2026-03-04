using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;

namespace OfficialPlugins.VariableDisplay
{
    static partial class NamedObjectVariableShowingLogic
    {
        /// <summary>
        /// Determines if a variable should be ignored (not displayed) by the variable plugin.
        /// </summary>
        /// <param name="name">The name of the variable which may be ignored.</param>
        /// <param name="instance">The NamedObjectSave owning the variable.</param>
        /// <param name="ati">The Asset Type Info for the NamedObjectSave.</param>
        /// <returns>Whether to skip the variable.</returns>
        private static bool GetIfShouldBeSkipped(string name, NamedObjectSave instance, AssetTypeInfo ati)
        {
            ///////////////////Early Out////////////////////////
            if (string.IsNullOrEmpty(name))
            {
                return true;
            }

            //////////////////End Early Out//////////////////////

            if (ati != null)
            {
                if (ati.IsPositionedObject)
                {
                    if (name.EndsWith("Velocity") || name.EndsWith("Acceleration") || name.StartsWith("Relative") ||
                        name == "ParentBone" || name == "KeepTrackOfReal" || name == "Drag"

                        )
                    {
                        return true;
                    }

                }

                if (ati == AvailableAssetTypes.CommonAtis.AxisAlignedRectangle)
                {
                    return name == "ScaleX" || name == "ScaleY" || name == "Top" || name == "Bottom" ||
                        name == "Left" || name == "Right";
                }

                if (ati == AvailableAssetTypes.CommonAtis.Text)
                {
                    return
                        name == "AlphaRate" || name == "RedRate" || name == "GreenRate" || name == "BlueRate" ||
                        name == "ScaleVelocity" || name == "SpacingVelocity" ||
                        name == "ScaleXVelocity" || name == "ScaleYVelocity" ||
                        // These used to be the standard way to size text, but now we just
                        // use "TextureScale"
                        // Note that these can still be exposed - see the Text
                        name == "Scale" || name == "Spacing" || name == "NewLineDistance"

                        ;
                }

                if (ati == AvailableAssetTypes.CommonAtis.Camera)
                {
                    return
                        name == "AspectRatio" || name == "DestinationRectangle" || name == "CameraModelCullMode";

                }

                if (ati == AvailableAssetTypes.CommonAtis.Polygon)
                {
                    return
                        name == "RotationX" || name == "RotationY" || name == "Points";
                }


                if (ati == AvailableAssetTypes.CommonAtis.Layer)
                {
                    return
                        name == "LayerCameraSettings";
                }

                if (ati == AvailableAssetTypes.CommonAtis.Sprite)
                {
                    return
                        name == "AlphaRate" || name == "RedRate" || name == "GreenRate" || name == "BlueRate" ||
                        name == "RelativeTop" || name == "RelativeBottom" ||
                        name == "RelativeLeft" || name == "RelativeRight" ||
                        name == "TimeCreated" || name == "TimeIntoAnimation" ||
                        name == "ScaleX" || name == "ScaleY" ||
                        name == "CurrentChainIndex" ||
                        name == "Top" || name == "Bottom" || name == "Left" || name == "Right" ||
                        name == "PixelSize" ||
                        name == "LeftTextureCoordinate" || name == "RightTextureCoordinate" ||
                        name == "BottomTextureCoordinate" || name == "TopTextureCoordinate" ||
                        name == "ScaleXVelocity" || name == "ScaleYVelocity" ||
                        name == "TextureFilter"
                        ;

                }
            }


            EntitySave? nosEntity = instance.SourceType == SourceType.Entity
                ? ObjectFinder.Self.GetEntitySave(instance.SourceClassType)
                : null;

            var variableInNos = nosEntity?.GetCustomVariableRecursively(name);
            CustomVariable? baseNos = variableInNos != null
                ? ObjectFinder.Self.GetBaseCustomVariable(variableInNos)
                : null;

            var isSharedStatic = baseNos?.IsShared == true;

            /////////////////Early Out///////////////////////////
            if (isSharedStatic)
            {
                return true;
            }


            return false;
        }
    }
}
