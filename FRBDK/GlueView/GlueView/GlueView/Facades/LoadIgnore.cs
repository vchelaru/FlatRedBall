using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall;

namespace GlueView.Facades
{
	public class LoadIgnore
	{
		/// <summary>
		/// The time the Ignore was created
		/// </summary>
		public double CreationTime
		{
			get;
			private set;
		}

		public LoadIgnore()
		{
			CreationTime = TimeManager.CurrentTime;
		}
	}
}
