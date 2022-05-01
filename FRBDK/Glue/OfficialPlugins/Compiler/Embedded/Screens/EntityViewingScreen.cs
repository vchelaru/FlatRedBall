{CompilerDirectives}

using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Screens;
using GlueControl.Models;
using System;
using Microsoft.Xna.Framework;

namespace GlueControl.Screens
{
    class EntityViewingScreen : Screen
    {
        #region Fields/properties

        public IDestroyable CurrentEntity { get; set; }

        public static string GameElementTypeToCreate { get; set; }
        public static NamedObjectSave InstanceToSelect { get; set; }

        public static bool ShowScreenBounds { get; set; }

        bool isViewingAbstractEntity;

        #endregion

        public EntityViewingScreen() : base(nameof(EntityViewingScreen))
        {

        }

        public override void Initialize(bool addToManagers)
        {
            base.Initialize(addToManagers);

            if (addToManagers)
            {
                AddToManagers();
            }

            BeforeCustomInitialize?.Invoke();
        }

        public override void ActivityEditMode()
        {
            base.ActivityEditMode();

            var camera = Camera.Main;
            if (isViewingAbstractEntity)
            {
                Editing.EditorVisuals.Text(
                    $"The entity {EntityViewingScreen.GameElementTypeToCreate} is abstract so it cannot be previewed.\nSelect a derived entity type to view it.",
                    new Vector3(camera.X, camera.Y, 0));
            }

            try
            {
                foreach (var item in FlatRedBall.SpriteManager.ManagedPositionedObjects)
                {
                    if (item is FlatRedBall.Entities.IEntity entity)
                    {
                        entity.ActivityEditMode();
                    }
                }
                base.ActivityEditMode();
            }
            catch (Exception e)
            {
                Editing.EditorVisuals.Text(
                    $"Error in edit mode for entity {CurrentEntity?.GetType().Name}\n{e}\n{e.InnerException}",
                    new Vector3(camera.X, camera.Y, 0));
            }

            if (ShowScreenBounds)
            {
                var width = CameraSetup.Data.ResolutionWidth;
                var height = CameraSetup.Data.ResolutionHeight;
                Editing.EditorVisuals.Rectangle(width, height, Vector3.Zero);
            }
        }

        public override void AddToManagers()
        {
            base.AddToManagers();

            if (!string.IsNullOrEmpty(EntityViewingScreen.GameElementTypeToCreate))
            {
                var entityType = this.GetType().Assembly.GetType(EntityViewingScreen.GameElementTypeToCreate);
                isViewingAbstractEntity = entityType?.IsAbstract == true;

                GlueControl.Editing.EditingManager.Self.ElementEditingMode = GlueControl.Editing.ElementEditingMode.EditingEntity;

                Camera.Main.X = 0;
                Camera.Main.Y = 0;
                Camera.Main.Detach();

                if (!isViewingAbstractEntity)
                {

                    //var instance = ownerType.GetConstructor(new System.Type[0]).Invoke(new object[0]) as IDestroyable;
                    var instance = GlueControl.InstanceLogic.Self.CreateEntity(EntityViewingScreen.GameElementTypeToCreate) as IDestroyable;
                    CurrentEntity = instance;
                    var instanceAsPositionedObject = (PositionedObject)instance;
                    instanceAsPositionedObject.Velocity = Microsoft.Xna.Framework.Vector3.Zero;
                    instanceAsPositionedObject.Acceleration = Microsoft.Xna.Framework.Vector3.Zero;


                    GlueControl.Editing.EditingManager.Self.Select(InstanceToSelect);
                }
            }
        }

        public override void Destroy()
        {
            GlueControl.InstanceLogic.Self.DestroyDynamicallyAddedInstances();

            CurrentEntity?.Destroy();

            foreach (var factory in {ProjectNamespace}.Performance.FactoryManager.GetAllFactories())
            {
                factory.Destroy();
            }

            base.Destroy();
        }
    }
}
