#if ANDROID || IOS
#define REQUIRES_PRIMARY_THREAD_LOADING
#endif

using Color = Microsoft.Xna.Framework.Color;

// Generated Usings

namespace FlatRedBallAddOns.Screens
{
	public partial class ScreenTemplate
	{
		// Generated Fields

		public ScreenTemplate()
			: base("ScreenTemplate")
		{
		}

        public override void Initialize(bool addToManagers)
        {
			// Generated Initialize

        }
        
// Generated AddToManagers


		public override void Activity(bool firstTimeCalled)
		{
			// Generated Activity


				// After Custom Activity
				
            
		}

		public override void Destroy()
		{
			// Generated Destroy

			base.Destroy();

			CustomDestroy();

		}

		// Generated Methods


	}
}
