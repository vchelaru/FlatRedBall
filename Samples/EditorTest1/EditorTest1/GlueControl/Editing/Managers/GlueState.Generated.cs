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
using System.Linq;
using System.Collections.Generic;

namespace GlueControl.Managers
{

   internal partial class GlueState
   {
       public static GlueState Self { get; }
       static GlueState() => Self = new GlueState();
public GlueProjectSave CurrentGlueProject
{
    get
    {
        return ObjectFinder.Self.GlueProject;
    }
}

public GlueElement CurrentElement
{
    get
    {
        return Editing.EditingManager.Self.CurrentGlueElement;
    }
}

public NamedObjectSave CurrentNamedObjectSave
{
    get
    {
        return Editing.EditingManager.Self.CurrentNamedObjects.FirstOrDefault();
    }
}

       public async Task<object> SetCurrentNamedObjectSave(NamedObjectSave namedObjectSave, GlueElement nosOwner)
      {
          var parameter = new GlueCallsClassGenerationManager.GlueParameters { Value = namedObjectSave
, Dependencies = new Dictionary<string, object> {{ "nosOwner", nosOwner }}};


          return await GlueCallsClassGenerationManager.ConvertToPropertyCallToGame(nameof(CurrentNamedObjectSave), typeof(NamedObjectSave), parameter, new GlueCallsClassGenerationManager.CallPropertyParameters
          {
              ReturnToPropertyType = false
          });
      }
public IReadOnlyList<NamedObjectSave> CurrentNamedObjectSaves
{
    get
    {
        return Editing.EditingManager.Self.CurrentNamedObjects;
    }
}

       public async Task<object> SetCurrentNamedObjectSaves(IReadOnlyList<NamedObjectSave> namedObjectSaves, GlueElement nosOwner)
      {
          var parameter = new GlueCallsClassGenerationManager.GlueParameters { Value = namedObjectSaves
, Dependencies = new Dictionary<string, object> {{ "nosOwner", nosOwner }}};


          return await GlueCallsClassGenerationManager.ConvertToPropertyCallToGame(nameof(CurrentNamedObjectSaves), typeof(IReadOnlyList<NamedObjectSave>), parameter, new GlueCallsClassGenerationManager.CallPropertyParameters
          {
              ReturnToPropertyType = false
          });
      }
public int? SelectedSubIndex
{
    get;
    set;
}

       public async Task<object> SetSelectedSubIndex(int? index)
      {
          var parameter = new GlueCallsClassGenerationManager.GlueParameters { Value = index
};


          return await GlueCallsClassGenerationManager.ConvertToPropertyCallToGame(nameof(SelectedSubIndex), typeof(int?), parameter, new GlueCallsClassGenerationManager.CallPropertyParameters
          {
              ReturnToPropertyType = false
          });
      }
   }
}
