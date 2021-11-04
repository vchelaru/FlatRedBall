using FlatRedBall.Glue.SaveClasses;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.Compiler.Dtos
{
    #region UpdateCurrentElementDto (base class for updating element to game)
    public class UpdateCurrentElementDto
    {
        public ScreenSave ScreenSave { get; set; }
        public EntitySave EntitySave { get; set; }

        [JsonIgnore]
        public GlueElement GlueElement => (GlueElement)ScreenSave ?? EntitySave;

    }
    #endregion

    #region RemoveObjectDto
    public class RemoveObjectDto : UpdateCurrentElementDto
    {
        public string ElementNameGlue { get; set; }
        public string ObjectName { get; set; }
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
    #endregion

    #region SetEditMode
    class SetEditMode
    {
        public bool IsInEditMode { get; set; }
    }
    #endregion

    #region SelectObjectDto
    class SelectObjectDto : UpdateCurrentElementDto
    {
        public NamedObjectSave NamedObject { get; set; }
        public string ElementNameGlue { get; set; }
        public string StateName { get; set; }
        public string StateCategoryName { get; set; }
        public bool BringIntoFocus { get; set; }

    }
    #endregion

    #region GlueVariableSetData
    public enum AssignOrRecordOnly
    {
        Assign,
        RecordOnly
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
    }
    #endregion

    #region GlueVariableSetDataResponse
    public class GlueVariableSetDataResponse
    {
        public string Exception { get; set; }
        public bool WasVariableAssigned { get; set; }
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

    #region AddObjectDto
    public class AddObjectDto : NamedObjectSave
    {
        public string CopyOriginalName { get; set; }
        public string ElementNameGame { get; set; }
    }
    #endregion

    #region AddObjectDtoResponse
    public class AddObjectDtoResponse
    {
        public bool WasObjectCreated { get; set; }
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
    }
    #endregion

    #region MoveObjectToContainerDtoResponse
    public class MoveObjectToContainerDtoResponse
    {
        public bool WasObjectMoved { get; set; }
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
        public string StrippedFileName { get; set; }
    }
    #endregion

    #region SetBorderlessDto
    public class SetBorderlessDto
    {
        public bool IsBorderless { get; set; }
    }
    #endregion

    #region GlueViewSettingsDto
    public class GlueViewSettingsDto
    {
        public bool ShowScreenBoundsWhenViewingEntities { get; set; }
        public decimal GridSize { get; set; }
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
}
