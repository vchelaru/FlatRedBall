using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using FlatRedBall.Content.Scene;
using FlatRedBall;
using FlatRedBall.IO;

namespace InstructionEditor
{
    public class InstructionEditorPropertiesSave
    {
        #region Fields

        public Vector3 EditorCameraPosition;
        public CameraSave CameraBounds;

        public List<string> CurrentSpriteMembersWatching = new List<string>();
        public List<string> CurrentSpriteFrameMembersWatching = new List<string>();
        public List<string> CurrentPositionedModelMembersWatching = new List<string>();
        public List<string> CurrentTextMembersWatching = new List<string>();
        #endregion

        #region Methods

        #region Publi Static "From" Methods

        public static InstructionEditorPropertiesSave FromEditorData()
        {
            InstructionEditorPropertiesSave ieps = new InstructionEditorPropertiesSave();
            ieps.EditorCameraPosition = SpriteManager.Camera.Position;
            ieps.CameraBounds = CameraSave.FromCamera(EditorData.SceneCamera);

            ieps.CurrentSpriteMembersWatching = EditorData.CurrentSpriteMembersWatching;
            ieps.CurrentSpriteFrameMembersWatching = EditorData.CurrentSpriteFrameMembersWatching;
            ieps.CurrentPositionedModelMembersWatching = EditorData.CurrentPositionedModelMembersWatching;
            ieps.CurrentTextMembersWatching = EditorData.CurrentTextMembersWatching;

            return ieps;
        }



        public static InstructionEditorPropertiesSave FromFile(string fileName)
        {
            return FileManager.XmlDeserialize<InstructionEditorPropertiesSave>(fileName);
        }

        #endregion

        #region Public Methods

        public void ApplyToEditor()
        {
            SpriteManager.Camera.Position = EditorCameraPosition;
            CameraBounds.SetCamera(EditorData.SceneCamera);

            EditorData.CurrentPositionedModelMembersWatching.Clear();
            EditorData.CurrentSpriteFrameMembersWatching.Clear();
            EditorData.CurrentSpriteMembersWatching.Clear();
            EditorData.CurrentTextMembersWatching.Clear();

            EditorData.CurrentSpriteMembersWatching.AddRange(CurrentSpriteMembersWatching);
            EditorData.CurrentSpriteFrameMembersWatching.AddRange(CurrentSpriteFrameMembersWatching);
            EditorData.CurrentPositionedModelMembersWatching.AddRange(CurrentPositionedModelMembersWatching);
            EditorData.CurrentTextMembersWatching.AddRange(CurrentTextMembersWatching);
        }

        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);
        }

        #endregion

        #endregion

    }
}
