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
                    "System.Linq",
                    "System.Collections.Generic"
                },
                Methods = new Method[0],
                Properties = new[]
                {
                    GetProperty_CurrentGlueProject(),
                    GetProperty_CurrentElement(),
                    GetProperty_CurrentNamedObjectSave(),
                    GetProperty_CurrentNamedObjectSaves()
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

        private static Property GetProperty_CurrentGlueProject()
        {
            return new Property
            {
                Name = "CurrentGlueProject",
                ReturnType = "GlueProjectSave",
                GetSimpleBody = "ObjectFinder.Self.GlueProject"
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

        private static Property GetProperty_CurrentNamedObjectSaves()
        {
            return new Property
            {
                Name = "CurrentNamedObjectSaves",
                GetBody = "return Editing.EditingManager.Self.CurrentNamedObjects;",
                ReturnType = "IReadOnlyList<NamedObjectSave>",
                SetMethod = new PropertyMethod
                {
                    Name = "SetCurrentNamedObjectSaves",
                    Parameters = new[]
                    {
                        new Parameter
                            {
                                Type = "IReadOnlyList<NamedObjectSave>",
                                Name = "namedObjectSaves",
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