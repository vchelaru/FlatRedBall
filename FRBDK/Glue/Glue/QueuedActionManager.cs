using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue
{
    /// <summary>
    /// Stores information about actions to perform - such as reloading the project, saving out .csproj files, and regenerating code files.
    /// </summary>
    public class QueuedActionManager
    {
        QueuedActionManager mSelf;

        public QueuedActionManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new QueuedActionManager();
                }
                return mSelf;
            }
        }

        bool ShouldSaveCodeProjects
        {
            get;
            set;
        }

        bool ShouldSaveGlux
        {
            get;
            set;
        }

        bool ShouldReloadGlux
        {
            get;
            set;
        }

        List<IElement> ElementsToGenerate
        {
            get;
            set;
        }

        List<ReferencedFileSave> CsvsToRegenerate
        {
            get;
            set;
        }

        List<ReferencedFileSave> FilesToUpdateProjectMembershipOn
        {
            get;
            set;
        }
    }
}
