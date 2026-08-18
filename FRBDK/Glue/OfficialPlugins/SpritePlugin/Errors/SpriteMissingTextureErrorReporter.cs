using FlatRedBall;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System.Collections.Generic;

namespace OfficialPlugins.SpritePlugin.Errors
{
    internal class SpriteMissingTextureErrorReporter : ErrorReporterBase
    {
        public override ErrorViewModel[] GetAllErrors()
        {
            var errors = new List<ErrorViewModel>();

            var project = GlueState.Self.CurrentGlueProject;

            void AddErrorsFrom(GlueElement owner)
            {
                foreach (var namedObject in owner.AllNamedObjects)
                {
                    if (GetIfHasError(namedObject, owner))
                    {
                        errors.Add(new SpriteMissingTextureErrorViewModel(namedObject));
                    }
                }
            }

            foreach (var screen in project.Screens)
            {
                AddErrorsFrom(screen);
            }
            foreach (var entity in project.Entities)
            {
                AddErrorsFrom(entity);
            }

            return errors.ToArray();
        }

        public static bool GetIfHasError(NamedObjectSave namedObject, GlueElement owner)
        {
            if (namedObject.GetAssetTypeInfo() != AvailableAssetTypes.CommonAtis.Sprite)
            {
                return false;
            }

            var hasTextureCoordinateSet =
                namedObject.GetCustomVariable(nameof(Sprite.LeftTexturePixel))?.Value != null ||
                namedObject.GetCustomVariable(nameof(Sprite.RightTexturePixel))?.Value != null ||
                namedObject.GetCustomVariable(nameof(Sprite.TopTexturePixel))?.Value != null ||
                namedObject.GetCustomVariable(nameof(Sprite.BottomTexturePixel))?.Value != null;

            if (!hasTextureCoordinateSet)
            {
                return false;
            }

            var textureValue = ObjectFinder.Self.GetValueRecursively(
                namedObject, owner, nameof(Sprite.Texture)) as string;

            return string.IsNullOrEmpty(textureValue);
        }
    }
}
