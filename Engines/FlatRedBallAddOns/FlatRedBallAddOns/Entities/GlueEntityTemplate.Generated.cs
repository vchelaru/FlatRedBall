#if ANDROID || IOS
#define REQUIRES_PRIMARY_THREAD_LOADING
#endif

using Color = Microsoft.Xna.Framework.Color;

namespace FlatRedBallAddOns.Entities
{
	public partial class GlueEntityTemplate
	{
        // This is made static so that static lazy-loaded content can access it.
        public static string ContentManagerName
        {
            get;
            set;
        }

		// Generated Fields

        public GlueEntityTemplate()
            : this(FlatRedBall.Screens.ScreenManager.CurrentScreen.ContentManagerName, true)
        {

        }

        public GlueEntityTemplate(string contentManagerName) :
            this(contentManagerName, true)
        {
        }


        public GlueEntityTemplate(string contentManagerName, bool addToManagers) :
			base()
		{
			// Don't delete this:
            ContentManagerName = contentManagerName;
            InitializeEntity(addToManagers);

		}

		protected virtual void InitializeEntity(bool addToManagers)
		{
			// Generated Initialize


		}

// Generated AddToManagers


    }
	
	
	// Extra classes
	
}
