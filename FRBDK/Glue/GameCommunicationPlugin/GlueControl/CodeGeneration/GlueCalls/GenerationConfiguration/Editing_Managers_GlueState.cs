using System;

namespace GameCommunicationPlugin.GlueControl.CodeGeneration.GlueCalls.GenerationConfiguration
{
    internal class Editing_Managers_GlueState
    {
        internal static GenerationOptions GetGenerationOptions()
        {
            return new GenerationOptions
            {
                Name = "GlueState",
                BaseClass = "GlueCommandsStateBase",
                Namespace = "GlueControl.Managers",
                Defines = new[] { "IncludeSetVariable", "SupportsEditMode", "HasGum" },
                Usings = new[] {
                    "System.Linq"
                },
                Methods = new Method[0],
                Properties = new[]
                {
                    GetProperty_CurrentElement(),
                    GetProperty_CurrentNamedObjectSave()
                },
                AddStaticSelfReference = true
            };
        }

        private static Property GetProperty_CurrentElement()
        {
            return new Property
            {
                Name = "CurrentElement",
                ReturnType = "GlueElement",
                GetSimpleBody = "Editing.EditingManager.Self.CurrentGlueElement"
            };
        }

        private static Property GetProperty_CurrentNamedObjectSave()
        {
            return new Property
            {
                Name = "CurrentNamedObjectSave",
                GetBody = "return Editing.EditingManager.Self.CurrentNamedObjects.FirstOrDefault();",
                ReturnType = "NamedObjectSave",
                SetMethod = new PropertyMethod
                {
                    Name = "SetCurrentNamedObjectSave",
                    Parameters = new[]
                    {
                        new Parameter
                            {
                                Type = "NamedObjectSave",
                                Name = "namedObjectSave",
                                IsParameterUsedByGlue = true,
                                Dependencies = new [] { "nosOwner" }
                            },
                            new Parameter
                            {
                                Type = "GlueElement",
                                Name = "nosOwner",

                            }
                    }
                }
            };
        }
    }
}