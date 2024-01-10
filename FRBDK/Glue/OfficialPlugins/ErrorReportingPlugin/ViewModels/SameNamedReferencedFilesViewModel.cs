using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.ErrorReportingPlugin.ViewModels
{
    internal class SameNamedReferencedFilesViewModel : ErrorViewModel
    {
        ReferencedFileSave firstRfs;
        string firstRfsPropertyName;
        ReferencedFileSave secondRfs;
        string secondRfsPropertyName;

        GlueElement firstContainer;
        GlueElement secondContainer;

        public override string UniqueId => Details;

        public SameNamedReferencedFilesViewModel(ReferencedFileSave first, ReferencedFileSave second)
        {
            firstRfs = first;
            firstContainer = ObjectFinder.Self.GetElementContaining(first);
            firstRfsPropertyName = firstRfs.GetInstanceName();

            secondRfs = second;
            secondContainer = ObjectFinder.Self.GetElementContaining(second);
            secondRfsPropertyName = secondRfs.GetInstanceName();

            var details = $"The files {first} and {second} generate to the same property name which will cause compile errors.";
            if(firstRfs.IncludeDirectoryRelativeToContainer == false && secondRfs.IncludeDirectoryRelativeToContainer == false)
            {
                // If they're the same name, we should tell them that the best way to fix it is remove one of the files and create a new one with a new name
                if(first.Name == second.Name)
                {
                    if ((first.IsCreatedByWildcard && second.IsCreatedByWildcard == false) ||
                        (second.IsCreatedByWildcard && first.IsCreatedByWildcard == false))
                    {
                        details += "\nOne of the files is added through wildcard, the other is explicitly added which results in the file being added twice. Consider removing the explicitly-added file.";
                    }
                    else
                    {
                        details += "\nConsider creating a new file with a different name, or removing one of the files from the FRB Editor";
                    }

                }
                else if(first.IsCreatedByWildcard || second.IsCreatedByWildcard)
                {
                    details += "\nConsider changing the wildcard file to have IncludeDirectoryRelativeToContainer set to true";
                }
                else
                {
                    details += "\nConsider setting IncludeDirectoryRelativeToContainer to true on one of the files";
                }
            }
            Details = details;

        }

        public override void HandleDoubleClick() =>
            GlueState.Self.CurrentReferencedFileSave = firstRfs;

        public override bool GetIfIsFixed()
        {
            if(firstRfs.GetInstanceName() != firstRfsPropertyName)
            {
                return true;
            }

            if(firstRfs.LoadedAtRuntime == false)
            {
                return true;
            }

            if(!IsContained(firstRfs, firstContainer))
            {
                return true;
            }

            if(secondRfs.GetInstanceName() != secondRfsPropertyName)
            {
                return true;
            }

            if (secondRfs.LoadedAtRuntime == false)
            {
                return true;
            }

            if(!IsContained(secondRfs, secondContainer))
            {
                return true;
            }


            return false;
        }

        private bool IsContained(ReferencedFileSave rfs, GlueElement container)
        {
            if(container != null)
            {
                return container.ReferencedFiles.Contains(rfs);
            }
            else
            {
                return GlueState.Self.CurrentGlueProject.GlobalFiles.Contains(rfs);
            }
        }
    }
}
