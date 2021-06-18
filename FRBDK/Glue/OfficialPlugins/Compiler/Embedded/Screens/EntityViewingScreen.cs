{CompilerDirectives}

using FlatRedBall.Graphics;
using FlatRedBall.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace {ProjectNamespace}.Screens
{
    class EntityViewingScreen : Screen
    {
        public IDestroyable CurrentEntity { get; set; }

        public EntityViewingScreen() : base(nameof(EntityViewingScreen))
        {

        }

        public override void Destroy()
        {
            CurrentEntity?.Destroy();

            base.Destroy();
        }

    }
}
