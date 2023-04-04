using FlatRedBall.Glue.SaveClasses;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommunicationPlugin.GlueControl.Dtos
{
    #region UpdateCurrentElementDto (base class for updating element to game)
    public class NamedObjectWithElementName
    {
        public string GlueElementName { get; set; }
        public string ContainerName { get; set; }
        public NamedObjectSave NamedObjectSave { get; set; }
    }

    public class UpdateCurrentElementDto
    {
        public ScreenSave ScreenSave { get; set; }
        public EntitySave EntitySave { get; set; }

        [JsonIgnore]
        public GlueElement GlueElement => (GlueElement)ScreenSave ?? EntitySave;

        public List<NamedObjectWithElementName> NamedObjectsToUpdate { get; set; } = new List<NamedObjectWithElementName>();
    }

    #endregion

    #region RemoveObjectDto
    public class RemoveObjectDto : UpdateCurrentElementDto
    {
        public string ElementNameGlue { get; set; }
        public List<string> ObjectNames { get; set; } = new List<string>();

        public override string ToString()
        {
            string toReturn = $"Remove {ElementNameGlue}.";

            foreach (var name in ObjectNames)
            {
                toReturn += name + ",";
            }
            return toReturn;
        }
    }
    #endregion

    #region SetVariableDto
    public class SetVariableDto
    {
        public string InstanceOwner { get; set; }

        public string ObjectName { get; set; }
        public string VariableName { get; set; }
        public object VariableValue { get; set; }
        public string Type { get; set; }
    }

    public class SetVariableDtoList
    {
        public List<SetVariableDto> SetVariableList { get; set; } = new List<SetVariableDto>();
    }

    #endregion

    #region SetEditMode
    class SetEditMode
    {
        public bool IsInEditMode { get; set; }
        public string AbsoluteGlueProjectFilePath { get; set; }
    }
    #endregion

    #region SelectObjectDto
    class SelectObjectDto : UpdateCurrentElementDto
    {
        public List<NamedObjectSave> NamedObjects { get; set; } = new List<NamedObjectSave>();

        public string ElementNameGlue { get; set; }

        // if the user selects an element which is abstract, then we need to fallback
        public string BackupElementNameGlue { get; set; }

        public string StateName { get; set; }
        public string StateCategoryName { get; set; }
        public bool BringIntoFocus { get; set; }

    }
    #endregion

    #region SelectPrevious/Next 

    class SelectPreviousDto { }
    class SelectNextDto { }

    #endregion

    #region GoToDefinitionDto

    class GoToDefinitionDto {}

    #endregion

    #region GlueVariableSetData
    public enum AssignOrRecordOnly
    {
        Assign,
        RecordOnly
    }

    public class GlueVariableSetDataList : UpdateCurrentElementDto
    {
        public List<GlueVariableSetData> Data { get; set; } = new List<GlueVariableSetData>();
    }

    public class GlueVariableSetData : UpdateCurrentElementDto
    {
        public AssignOrRecordOnly AssignOrRecordOnly { get; set; }
        /// <summary>
        /// The owner of the NamedObjectSave, which is either the current screen or the current entity save
        /// </summary>
        public string InstanceOwnerGameType { get; set; }
        public string VariableName { get; set; }
        public string VariableValue { get; set; }
        public string Type { get; set; }
        public bool IsState { get; set; }

        public override string ToString() => $"{VariableName}={VariableValue}";
    }
    #endregion

    #region GlueVariableSetDataResponse

    public class GlueVariableSetDataResponseList
    {
        public List<GlueVariableSetDataResponse> Data { get; set; } = new List<GlueVariableSetDataResponse>();
    }

    public class GlueVariableSetDataResponse
    {
        public string Exception { get; set; }
        public bool WasVariableAssigned { get; set; }
    }

    #endregion

    #region CurrentDisplayInfoDto

    public class CurrentDisplayInfoDto
    {
        public decimal ZoomPercentage { get; set; }
        public int DestinationRectangleWidth { get; set; }
        public int DestinationRectangleHeight { get; set; }

        public float OrthogonalWidth { get; set; }
        public float OrthogonalHeight { get; set; }
    }

    #endregion

    #region GetCameraPosition
    public class GetCameraPosition
    {
        // no members I think...
    }
    #endregion

    #region GetCameraPositionResponse
    public class GetCameraPositionResponse
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
    #endregion

    #region GetCameraSave
    public class GetCameraSave
    {
        // no members
    }
    #endregion

    #region ChangeZoomDto

    public enum PlusOrMinus
    {
        Plus, Minus
    }

    public class ChangeZoomDto
    {
        public PlusOrMinus PlusOrMinus { get; set; }
    }

    #endregion

    #region AddObjectDto

    public class AddObjectDtoList : UpdateCurrentElementDto
    {
        public List<AddObjectDto> Data { get; set; } = new List<AddObjectDto>();
    }

    public class AddObjectDto : UpdateCurrentElementDto
    {
        public NamedObjectSave NamedObjectSave { get; set; }
        public string CopyOriginalName { get; set; }
        public string ElementNameGame { get; set; }
        public bool SelectNewObject { get; set; }

        public override string ToString()
        {
            return $"Add NOS {NamedObjectSave.InstanceName} ({NamedObjectSave.SourceClassType})";
        }
    }
    #endregion

    #region AddObjectDtoResponse
    public class AddObjectDtoResponse
    {
        public bool WasObjectCreated { get; set; }
    }

    public class AddObjectDtoListResponse
    {
        public List<AddObjectDtoResponse> Data { get; set; } = new List<AddObjectDtoResponse>();
    }

    #endregion

    #region AddVariableDto
    public class AddVariableDto
    {
        public CustomVariable CustomVariable { get; set; }
        public string ElementGameType { get; set; }
    }
    #endregion

    #region MoveObjectToContainerDto
    public class MoveObjectToContainerDto
    {
        public string ElementName { get; set; }
        public string ObjectName { get; set; }
        public string ContainerName { get; set; }

        public override string ToString()
        {
            return $"Move {ElementName}.{ObjectName} to container {ContainerName}";
        }
    }

    public class MoveObjectToContainerListDto
    {
        public List<MoveObjectToContainerDto> Changes { get; set; } = new List<MoveObjectToContainerDto>();
    }

    #endregion

    #region MoveObjectToContainerDtoResponse
    public class MoveObjectToContainerDtoResponse
    {
        public int NumberSuccessfullyMoved { get; set; }
        public int NumberFailedToMoved { get; set; }

    }
    #endregion

    #region RemoveObjectDtoResponse
    public class RemoveObjectDtoResponse
    {
        public bool WasObjectRemoved { get; set; }
        public bool DidScreenMatch { get; set; }
    }
    #endregion

    #region SetCameraPositionDto
    public class SetCameraPositionDto
    {
        public Microsoft.Xna.Framework.Vector3 Position { get; set; }
    }
    #endregion

    #region Set Camera Setup and related

    public class SetCameraSetupDto
    {
        public bool IsGenerateCameraDisplayCodeEnabled { get; set; }
        public float Scale { get; set; }
        public float ScaleGum { get; set; }
        public bool Is2D { get; set; }
        public int ResolutionWidth { get; set; }
        public int ResolutionHeight { get; set; }
        public decimal? AspectRatio { get; set; }
        public bool AllowWindowResizing { get; set; }
        public bool IsFullScreen { get; set; }
        public ResizeBehavior ResizeBehavior { get; set; }
        public ResizeBehavior ResizeBehaviorGum { get; set; }
        public WidthOrHeight DominantInternalCoordinates { get; set; }
        public Microsoft.Xna.Framework.Graphics.TextureFilter TextureFilter { get; set; }
    }

    public class SetCameraAspectRatioDto
    {
        public decimal? AspectRatio { get; set; }
    }

    #endregion

    #region CreateNewEntityDto
    public class CreateNewEntityDto
    {
        public EntitySave EntitySave { get; set; }
    }
    #endregion

    #region CreateNewStateDto
    public class CreateNewStateDto
    {
        public StateSave StateSave { get; set; }
        public string CategoryName { get; set; }
        public string ElementNameGame { get; set; }
    }
    #endregion

    #region ChangeStateVariableDto
    public class ChangeStateVariableDto
    {
        public StateSave StateSave { get; set; }
        public string CategoryName { get; set; }
        public string ElementNameGame { get; set; }
        public string VariableName { get; set; }
    }
    #endregion

    #region RestartScreenDto 
    public class RestartScreenDto 
    {
        public bool ShowSelectionBump { get; set; } = true;
        public bool ReloadGlobalContent { get; set; }
    }
    #endregion

    #region ReloadGlobalContentDto
    public class ReloadGlobalContentDto
    {
        public string StrippedGlobalContentFileName { get; set; }
    }
    #endregion

    #region TogglePauseDto
    public class TogglePauseDto { }
    #endregion

    #region AdvanceOneFrameDto
    public class AdvanceOneFrameDto { }
    #endregion

    #region SetSpeedDto
    public class SetSpeedDto
    {
        public int SpeedPercentage { get; set; }
    }
    #endregion

    #region ModifyCollisionDto
    public class ModifyCollisionDto
    {
        public string TileShapeCollection { get; set; }
        public List<Vector2> AddedPositions { get; set; }
        public List<Vector2> RemovedPositions { get; set; }
        public bool RequestRestart { get; set; }
    }
    #endregion

    #region ForceReloadFileDto
    public class ForceReloadFileDto
    {
        public bool LoadInGlobalContent { get; set; }
        public List<string> ElementsContainingFile { get; set; }

        public bool IsLocalizationDatabase { get; set; }

        public string StrippedFileName { get; set; }
        public string FileRelativeToProject { get; set; }
    }
    #endregion

    #region SetBorderlessDto
    public class SetBorderlessDto
    {
        public bool IsBorderless { get; set; }
    }
    #endregion

    public class ForceGameResolution
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }

    #region GlueViewSettingsDto
    public class GlueViewSettingsDto
    {
        public bool ShowScreenBoundsWhenViewingEntities { get; set; }

        public bool ShowGrid { get; set; }
        public decimal GridSize { get; set; }

        public bool SetBackgroundColor { get; set; }
        public int BackgroundRed { get; set; }
        public int BackgroundGreen { get; set; }
        public int BackgroundBlue { get; set; }

        public bool EnableSnapping { get; set; }
        public decimal SnapSize { get; set; }
        public decimal PolygonPointSnapSize { get; set; }
    }
    #endregion

    #region GeneralCommandResponse
    public class GeneralCommandResponse
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; }
    }
    #endregion

    #region GetCommandsDto
    public class GetCommandsDto
    {

    }

    public class GetCommandsDtoResponse
    {
        public List<string> Commands { get; set; } = new List<string>();
    }
    #endregion

    #region Glue/XXXX/CommandsDto

    public class TypedParameter
    {
        public string Type { get; set; }
        public object Value { get; set; }

        public static TypedParameter FromValue(object value)
        {
            var toReturn = new TypedParameter();
            toReturn.Type = GetFriendlyName(value?.GetType());
            toReturn.Value = value;
            return toReturn;
        }

        static string GetFriendlyName(Type type)
        {
            string friendlyName = type?.Name;
            if (type?.IsGenericType == true)
            {
                int iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0)
                {
                    friendlyName = friendlyName.Remove(iBacktick);
                }
                friendlyName += "<";
                Type[] typeParameters = type.GetGenericArguments();
                for (int i = 0; i < typeParameters.Length; ++i)
                {
                    string typeParamName = GetFriendlyName(typeParameters[i]);
                    friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
                }
                friendlyName += ">";
            }

            return friendlyName;
        }
    }

    public class GlueElementReference
    {
        public string ElementNameGlue { get; set; }

        public static GlueElementReference From(GlueElement element)
        {
            var toReturn = new GlueElementReference();

            toReturn.ElementNameGlue = element.Name;

            return toReturn;
        }
    }

    public class NamedObjectSaveReference
    {
        public GlueElementReference GlueElementReference { get; set; }
        public string NamedObjectName { get; set; }
    }

    public class NosReferenceVariableAssignment
    {
        public NamedObjectSaveReference NamedObjectSave;
        public string VariableName;
        public TypedParameter Value;
    }



    public class FacadeCommandBase : RespondableDto
    {
        public string Method { get; set; }
        public string GetPropertyName { get; set; }
        public string SetPropertyName { get; set; }
        public List<object> Parameters { get; set; } = new List<object>();
        public Dictionary<string, string> CorrectTypeForParameters = new Dictionary<string, string>();

        public override string ToString()
        {
            string toReturn = GetType().Name + " " + (Method ?? GetPropertyName ?? SetPropertyName);

            foreach(var param in Parameters)
            {
                toReturn += " " + SummaryFor(param);
            }

            return toReturn;
        }

        string SummaryFor(object type)
        {
            if(type is Newtonsoft.Json.Linq.JObject jobject)
            {
                var elementReference = jobject.ToObject<GlueElementReference>();
                if(elementReference.ElementNameGlue != null)
                {
                    return $"{elementReference.ElementNameGlue}";
                }

                var nosReference = jobject.ToObject<NamedObjectSaveReference>();
                if(nosReference.GlueElementReference?.ElementNameGlue != null)
                {
                    return $"{nosReference.GlueElementReference.ElementNameGlue}.{nosReference.NamedObjectName}";
                }

                var typedParameter = jobject.ToObject<TypedParameter>();
                if(typedParameter.Type != null)
                {
                    return $"{typedParameter.Type} {typedParameter.Value}";
                }
            }

            return type?.ToString();
        }
    }

    public class GlueCommandDto : FacadeCommandBase { }
    public class GluxCommandDto : FacadeCommandBase 
    {
        public bool EchoToGame { get; set; } = false;
    }
    public class GlueStateDto : FacadeCommandBase { }
    public class GenerateCodeCommandDto : FacadeCommandBase { }
    public class RefreshCommandDto : FacadeCommandBase { }
    #endregion

    #region Base DTOs/Utilities

    public class ResponseWithContentDto : RespondableDto
    {
        public string Content { get; set; }
    }

    public class RespondableDto
    {
        public int Id { get; set; }
        public int OriginalDtoId { get; set; }
    }


    #endregion
}
