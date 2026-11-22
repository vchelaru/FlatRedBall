using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.Content.Aseprite;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OfficialPlugins.AnimationChainPlugin.Errors
{
    internal class AnimationChainErrorReporter : ErrorReporterBase
    {
        public override ErrorViewModel[] GetAllErrors()
        {
            var errors = new List<ErrorViewModel>();

            var project = GlueState.Self.CurrentGlueProject;

            // Many objects reference the same few animation files; read each once for the whole scan.
            var animationNamesByFile = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            void AddBadReferencesFrom(GlueElement glueElement)
            {
                foreach(var namedObject in glueElement.AllNamedObjects)
                {
                    if(AnimationReferenceErrorViewModel.GetIfHasError(glueElement, namedObject, out FilePath filePath, out string animationName,
                        animationNamesByFile))
                    {
                        var error = new AnimationReferenceErrorViewModel(filePath, namedObject, animationName);
                        errors.Add(error);
                    }
                }
            }

            foreach(var screen in project.Screens)
            {
                AddBadReferencesFrom(screen);
            }
            foreach(var entity in project.Entities)
            {
                AddBadReferencesFrom(entity);
            }

            return errors.ToArray();
        }
    }
}
