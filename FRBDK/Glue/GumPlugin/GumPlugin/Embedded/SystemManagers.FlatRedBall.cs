using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RenderingLibrary
{
    public partial class SystemManagers : FlatRedBall.Managers.IManager
    {

        public void Update()
        {
            
        }

        public void UpdateDependencies()
        {
            this.TextManager.RenderTextTextures();
        }
    }
}
