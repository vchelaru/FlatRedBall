#define IncludeSetVariable
#define SupportsEditMode
#define HasGum

using GlueControl;
using GlueControl.Dtos;
using GlueControl.Models;
using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace GlueControl.Managers
{

   internal partial class GluxCommands
   {
       public async Task CopyNamedObjectIntoElement(NamedObjectSave nos, GlueElement nosOwner, GlueElement targetElement, bool performSaveAndGenerateCode = true, bool updateUi = true)
       {
           var currentMethod = typeof(GluxCommands).GetMethod("CopyNamedObjectIntoElement", new Type[] {typeof(NamedObjectSave), typeof(GlueElement), typeof(GlueElement), typeof(bool), typeof(bool) });
           var parameters = new Dictionary<string, GlueCallsClassGenerationManager.GlueParameters>
           {
               { "nos", new GlueCallsClassGenerationManager.GlueParameters { Value = nos, Dependencies = new Dictionary<string, object> { { "nosOwner", nosOwner } }  } },
               { "performSaveAndGenerateCode", new GlueCallsClassGenerationManager.GlueParameters { Value = performSaveAndGenerateCode } },
               { "updateUi", new GlueCallsClassGenerationManager.GlueParameters { Value = updateUi } },
           };
           await GlueCallsClassGenerationManager.ConvertToMethodCallToGame(currentMethod, parameters, new GlueCallsClassGenerationManager.CallMethodParameters
           {
              EchoToGame = false
           });
      }

       public async Task <List<GeneralResponse<NamedObjectSave>>> CopyNamedObjectListIntoElement(List<NamedObjectSave> nosList, GlueElement nosOwner, GlueElement targetElement, bool performSaveAndGenerateCode = true, bool updateUi = true)
       {
           var currentMethod = typeof(GluxCommands).GetMethod("CopyNamedObjectListIntoElement", new Type[] {typeof(List<NamedObjectSave>), typeof(GlueElement), typeof(GlueElement), typeof(bool), typeof(bool) });
           var parameters = new Dictionary<string, GlueCallsClassGenerationManager.GlueParameters>
           {
               { "nosList", new GlueCallsClassGenerationManager.GlueParameters { Value = nosList, Dependencies = new Dictionary<string, object> { { "nosOwner", nosOwner } }  } },
               { "targetElement", new GlueCallsClassGenerationManager.GlueParameters { Value = targetElement } },
               { "performSaveAndGenerateCode", new GlueCallsClassGenerationManager.GlueParameters { Value = performSaveAndGenerateCode } },
               { "updateUi", new GlueCallsClassGenerationManager.GlueParameters { Value = updateUi } },
           };
           return (List<GeneralResponse<NamedObjectSave>>) await GlueCallsClassGenerationManager.ConvertToMethodCallToGame(currentMethod, parameters, new GlueCallsClassGenerationManager.CallMethodParameters
           {
              EchoToGame = false
           });
      }

       public async Task SetVariableOnList(List<NosVariableAssignment> nosVariableAssignments, GlueElement nosOwner, bool performSaveAndGenerateCode = true, bool updateUi = true, bool recordUndo = true, bool echoToGame = false)
       {
           var currentMethod = typeof(GluxCommands).GetMethod("SetVariableOnList", new Type[] {typeof(List<NosVariableAssignment>), typeof(GlueElement), typeof(bool), typeof(bool), typeof(bool), typeof(bool)  });
           var parameters = new Dictionary<string, GlueCallsClassGenerationManager.GlueParameters>
           {
               { "nosVariableAssignments", new GlueCallsClassGenerationManager.GlueParameters { Value = nosVariableAssignments, Dependencies = new Dictionary<string, object> { { "nosOwner", nosOwner } }  } },
               { "performSaveAndGenerateCode", new GlueCallsClassGenerationManager.GlueParameters { Value = performSaveAndGenerateCode } },
               { "updateUi", new GlueCallsClassGenerationManager.GlueParameters { Value = updateUi } },
               { "recordUndo", new GlueCallsClassGenerationManager.GlueParameters { Value = recordUndo } },
           };
           await GlueCallsClassGenerationManager.ConvertToMethodCallToGame(currentMethod, parameters, new GlueCallsClassGenerationManager.CallMethodParameters
           {
              EchoToGame = echoToGame
           });
      }

       public async Task SetVariableOn(NamedObjectSave nos, GlueElement nosOwner, string memberName, object value, bool performSaveAndGenerateCode = true, bool updateUi = true, bool recordUndo = true, bool echoToGame = false)
       {
           var currentMethod = typeof(GluxCommands).GetMethod("SetVariableOn", new Type[] {typeof(NamedObjectSave), typeof(GlueElement), typeof(string), typeof(object), typeof(bool), typeof(bool), typeof(bool), typeof(bool)  });
           var parameters = new Dictionary<string, GlueCallsClassGenerationManager.GlueParameters>
           {
               { "nos", new GlueCallsClassGenerationManager.GlueParameters { Value = nos, Dependencies = new Dictionary<string, object> { { "nosOwner", nosOwner } }  } },
               { "memberName", new GlueCallsClassGenerationManager.GlueParameters { Value = memberName } },
               { "value", new GlueCallsClassGenerationManager.GlueParameters { Value = value } },
               { "performSaveAndGenerateCode", new GlueCallsClassGenerationManager.GlueParameters { Value = performSaveAndGenerateCode } },
               { "updateUi", new GlueCallsClassGenerationManager.GlueParameters { Value = updateUi } },
               { "recordUndo", new GlueCallsClassGenerationManager.GlueParameters { Value = recordUndo } },
           };
           await GlueCallsClassGenerationManager.ConvertToMethodCallToGame(currentMethod, parameters, new GlueCallsClassGenerationManager.CallMethodParameters
           {
              EchoToGame = echoToGame
           });
      }

       public async Task SaveGlux(TaskExecutionPreference taskExecutionPreference = TaskExecutionPreference.Asap)
       {
           var currentMethod = typeof(GluxCommands).GetMethod("SaveGlux", new Type[] {typeof(TaskExecutionPreference) });
           var parameters = new Dictionary<string, GlueCallsClassGenerationManager.GlueParameters>
           {
               { "taskExecutionPreference", new GlueCallsClassGenerationManager.GlueParameters { Value = taskExecutionPreference } },
           };
           await GlueCallsClassGenerationManager.ConvertToMethodCallToGame(currentMethod, parameters, new GlueCallsClassGenerationManager.CallMethodParameters
           {
              EchoToGame = false
           });
      }

   }
}
