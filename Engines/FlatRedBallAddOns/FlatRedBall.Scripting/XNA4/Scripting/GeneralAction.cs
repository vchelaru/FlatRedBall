using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Scripting;

namespace FlatRedBall.Scripting
{
	public class GeneralAction : IScriptAction
	{
		public double ScreenTimeExecuted { get; private set; }


		bool hasExecuted = false; 
		public string Name
		{
			get;
			set;
		}

		public Func<bool> IsCompleteFunction;

		public Action ActionToPerform;

		public GeneralAction()
		{
			IsCompleteFunction = DefaultIsCompleteFunction; 
		}

		public bool IsComplete()
		{
			return IsCompleteFunction();
		}

		public bool Execute()
		{
			ActionToPerform();
			hasExecuted = true;
			ScreenTimeExecuted = FlatRedBall.TimeManager.CurrentScreenTime;
			return true;
		}

		public GeneralAction Lasting(double durationInSeconds)
		{
			IsCompleteFunction = () => hasExecuted && TimeManager.CurrentScreenSecondsSince(ScreenTimeExecuted) > durationInSeconds;

			return this;
		}

		public override string ToString()
		{
			if (!string.IsNullOrEmpty(Name))
			{
				return Name;
			}
			else
			{
				return base.ToString();
			}
		}

		private bool DefaultIsCompleteFunction()
		{
			return hasExecuted; 
		}
	}
}
