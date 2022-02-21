using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Managers
{
    public class IconManager : Singleton<IconManager>
    {
        public Image CopyIcon
        {
            get
            {
                return Resources.Resource1.copyIcon;
            }
        }
    }
}
