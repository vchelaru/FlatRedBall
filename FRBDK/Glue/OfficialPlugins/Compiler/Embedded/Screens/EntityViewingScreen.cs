{CompilerDirectives}

using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Screens;
using GlueControl.Models;

namespace GlueControl.Screens
{
    class EntityViewingScreen : Screen
    {
        public IDestroyable CurrentEntity { get; set; }

        public static string GameElementTypeToCreate { get; set; }
        public static NamedObjectSave InstanceToSelect { get; set; }


        public EntityViewingScreen() : base(nameof(EntityViewingScreen))
        {

        }

        public override void Initialize(bool addToManagers)
        {
            base.Initialize(addToManagers);

            if(addToManagers)
            {
                AddToManagers();
            }

            BeforeCustomInitialize?.Invoke();
        }

        public override void AddToManagers()
        {
            base.AddToManagers();

            //var instance = ownerType.GetConstructor(new System.Type[0]).Invoke(new object[0]) as IDestroyable;
            var instance = GlueControl.InstanceLogic.Self.CreateEntity(EntityViewingScreen.GameElementTypeToCreate) as IDestroyable;
            CurrentEntity = instance;
            var instanceAsPositionedObject = (PositionedObject)instance;
            instanceAsPositionedObject.Velocity = Microsoft.Xna.Framework.Vector3.Zero;
            instanceAsPositionedObject.Acceleration = Microsoft.Xna.Framework.Vector3.Zero;

            GlueControl.Editing.EditingManager.Self.ElementEditingMode = GlueControl.Editing.ElementEditingMode.EditingEntity;

            Camera.Main.X = 0;
            Camera.Main.Y = 0;
            Camera.Main.Detach();

            GlueControl.Editing.EditingManager.Self.Select(InstanceToSelect);
        }

        public override void Destroy()
        {
            GlueControl.InstanceLogic.Self.DestroyDynamicallyAddedInstances();

            CurrentEntity?.Destroy();

            base.Destroy();
        }
    }
}
