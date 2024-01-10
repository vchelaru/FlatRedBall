using System;
using System.Collections.Generic;
using System.Text;
using GameCommunicationPlugin.GlueControl.CodeGeneration.GlueCalls;

namespace GameCommunicationPlugin.GlueControl.CodeGeneration.GlueCalls.GenerationConfiguration
{
    internal class Editing_Managers_GluxCommands
    {
        internal static GenerationOptions GetGenerationOptions()
        {
            return new GenerationOptions
            {
                Name = "GluxCommands",
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
                                IsParameterUsedByGlue = true,
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
                                IsParameterUsedByGlue = true,
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
                                IsParameterUsedByGlue = true
                            },
                            new Parameter
                            {
                                Type = "bool",
                                Name = "updateUi",
                                DefaultValue = "true",
                                IsParameterUsedByGlue = true
                            },
                            new Parameter
                            {
                                Type = "bool",
                                Name = "recordUndo",
                                DefaultValue = "true",
                                IsParameterUsedByGlue = true
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
                                IsParameterUsedByGlue = true,
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
                                IsParameterUsedByGlue = true,

                            },
                            new Parameter
                            {
                                Type = "object",
                                Name = "value",
                                IsParameterUsedByGlue = true,

                            },
                            new Parameter {
                                Type = "bool",
                                Name = "performSaveAndGenerateCode",
                                DefaultValue = "true",
                                IsParameterUsedByGlue = true
                            },
                            new Parameter
                            {
                                Type = "bool",
                                Name = "updateUi",
                                DefaultValue = "true",
                                IsParameterUsedByGlue = true
                            },
                            new Parameter
                            {
                                Type = "bool",
                                Name = "recordUndo",
                                DefaultValue = "true",
                                IsParameterUsedByGlue = true
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
                                IsParameterUsedByGlue = true,
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
                                 IsParameterUsedByGlue = true
                             },
                             new Parameter
                             {
                                 Type = "bool",
                                 Name = "updateUi",
                                 DefaultValue = "true",
                                 IsParameterUsedByGlue = true
                             }
                        }
            };
        }

        private static Method GetMethod_CopyNamedObjectListIntoElement()
        {
            return new Method
            {
                Name = "CopyNamedObjectListIntoElement",
                ReturnType = "List<GeneralResponse<NamedObjectSave>>",
                Parameters = new[]
                        {
                            new Parameter
                            {
                                Type = "List<NamedObjectSave>",
                                Name = "nosList",
                                IsParameterUsedByGlue = true,
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
                                 Name = "targetElement",
                                 IsParameterUsedByGlue= true,
                             },
                             new Parameter
                             {
                                 Type = "bool",
                                 Name = "performSaveAndGenerateCode",
                                 DefaultValue = "true",
                                 IsParameterUsedByGlue = true
                             },
                             new Parameter
                             {
                                 Type = "bool",
                                 Name = "updateUi",
                                 DefaultValue = "true",
                                 IsParameterUsedByGlue = true
                             }
                        }
            };
        }
    }
}
