using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System.Linq;

namespace OfficialPlugins.SpritePlugin.Errors
{
    internal class SpriteMissingTextureErrorViewModel : ErrorViewModel
    {
        NamedObjectSave namedObject;

        public override string UniqueId => Details;

        public SpriteMissingTextureErrorViewModel(NamedObjectSave namedObject)
        {
            this.namedObject = namedObject;

            Details = $"{namedObject} sets texture coordinates (LeftTexturePixel/RightTexturePixel/TopTexturePixel/BottomTexturePixel) " +
                $"but does not have a Texture assigned. This will throw an exception at runtime.";
        }

        public override bool GetIfIsFixed()
        {
            var owner = ObjectFinder.Self.GetElementContaining(namedObject);

            if (owner == null || owner.AllNamedObjects.Contains(namedObject) == false)
            {
                return true;
            }

            return SpriteMissingTextureErrorReporter.GetIfHasError(namedObject, owner) == false;
        }

        public override void HandleDoubleClick()
        {
            GlueState.Self.CurrentNamedObjectSave = namedObject;
            GlueCommands.Self.DialogCommands.FocusTab("Properties");
        }
    }
}
