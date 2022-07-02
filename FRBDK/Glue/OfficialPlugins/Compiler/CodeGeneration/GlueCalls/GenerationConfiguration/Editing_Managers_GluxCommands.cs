using System;
using System.Collections.Generic;
using System.Text;
using OfficialPlugins.Compiler.CodeGeneration.GlueCalls;

namespace OfficialPlugins.Compiler.CodeGeneration.GlueCalls.GenerationConfiguration
{
    internal class Editing_Managers_GluxCommands
    {
        internal static GenerationOptions GetGenerationOptions()
        {
            return new GenerationOptions
            {
                Name = "GluxCommandsTest",
                BaseClass = "GlueCommandsStateBase",
                Namespace = "GlueControl.Managers",
                Defines = new[] { "IncludeSetVariable", "SupportsEditMode", "HasGum" },
                Methods = new[]
                {
                    GetMethod_CopyNamedObjectIntoElement(),
                    GetMethod_CopyNamedObjectListIntoElement(),
                    GetMethod_SetVariableOnList(),
                    GetMethod_SetVariableOn(),
                    GetMethod_SaveGlux()
                }
            };
        }

        private static Method GetMethod_SaveGlux()
        {
            return new Method
            {
                Name = "SaveGlux",
                Parameters = new[]
                        {
                            new Parameter
                            {
                                Type = "TaskExecutionPreference",
                                Name = "taskExecutionPreference",
                                GlueParameterOrder = 1,
                                DefaultValue = "TaskExecutionPreference.Asap"
                            }
                        }
            };
        }

        private static Method GetMethod_SetVariableOnList()
        {
            return new Method
            {
                Name = "SetVariableOnList",
                Parameters = new[]
                        {
                            new Parameter
                            {
                                Type = "List<NosVariableAssignment>",
                                Name = "nosVariableAssignments",
                                GlueParameterOrder = 1,
                                Dependencies = new [] { "nosOwner" }
                            },
                            new Parameter
                            {
                                Type = "GlueElement",
                                Name = "nosOwner",

                            },
                            new Parameter {
                                Type = "bool",
                                Name = "performSaveAndGenerateCode",
                                DefaultValue = "true",
                                GlueParameterOrder = 2
                            },
                            new Parameter
                            {
                                Type = "bool",
                                Name = "updateUI",
                                DefaultValue = "true",
                                GlueParameterOrder = 3
                            }
                        },
                AddEchoToGame = true
            };
        }

        private static Method GetMethod_SetVariableOn()
        {
            return new Method
            {
                Name = "SetVariableOn",
                Parameters = new[]
                        {
                            new Parameter
                            {
                                Type = "NamedObjectSave",
                                Name = "nos",
                                GlueParameterOrder = 1,
                                Dependencies = new [] { "nosOwner" }
                            },
                            new Parameter
                            {
                                Type = "GlueElement",
                                Name = "nosOwner",

                            },
                            new Parameter
                            {
                                Type = "string",
                                Name = "memberName",

                            },
                            new Parameter
                            {
                                Type = "object",
                                Name = "value",

                            },
                            new Parameter {
                                Type = "bool",
                                Name = "performSaveAndGenerateCode",
                                DefaultValue = "true",
                                GlueParameterOrder = 2
                            },
                            new Parameter
                            {
                                Type = "bool",
                                Name = "updateUI",
                                DefaultValue = "true",
                                GlueParameterOrder = 3
                            }
                        },
                AddEchoToGame = true
            };
        }

        private static Method GetMethod_CopyNamedObjectIntoElement()
        {
            return new Method
            {
                Name = "CopyNamedObjectIntoElement",
                Parameters = new[]
                        {
                            new Parameter
                            {
                                Type = "NamedObjectSave",
                                Name = "nos",
                                GlueParameterOrder = 1,
                                Dependencies = new [] { "nosOwner" }
                            },
                             new Parameter
                             {
                                 Type = "GlueElement",
                                 Name = "nosOwner"
                             },
                             new Parameter
                             {
                                 Type = "GlueElement",
                                 Name = "targetElement"
                             },
                             new Parameter
                             {
                                 Type = "bool",
                                 Name = "performSaveAndGenerateCode",
                                 DefaultValue = "true",
                                 GlueParameterOrder = 2
                             },
                             new Parameter
                             {
                                 Type = "bool",
                                 Name = "updateUi",
                                 DefaultValue = "true",
                                 GlueParameterOrder = 3
                             }
                        }
            };
        }

        private static Method GetMethod_CopyNamedObjectListIntoElement()
        {
            return new Method
            {
                Name = "CopyNamedObjectIntoElement",
                Parameters = new[]
                        {
                            new Parameter
                            {
                                Type = "List<NamedObjectSave>",
                                Name = "nosList",
                                GlueParameterOrder = 1,
                                Dependencies = new [] { "nosOwner" }
                            },
                             new Parameter
                             {
                                 Type = "GlueElement",
                                 Name = "nosOwner"
                             },
                             new Parameter
                             {
                                 Type = "GlueElement",
                                 Name = "targetElement"
                             },
                             new Parameter
                             {
                                 Type = "bool",
                                 Name = "performSaveAndGenerateCode",
                                 DefaultValue = "true",
                                 GlueParameterOrder = 2
                             },
                             new Parameter
                             {
                                 Type = "bool",
                                 Name = "updateUi",
                                 DefaultValue = "true",
                                 GlueParameterOrder = 3
                             }
                        }
            };
        }
    }
}
