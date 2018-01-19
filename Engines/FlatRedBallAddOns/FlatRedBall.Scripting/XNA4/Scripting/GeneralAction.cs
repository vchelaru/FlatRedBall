using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Scripting;

namespace FlatRedBall.Scripting
{
	public class GeneralAction : IScriptAction
	{
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
			return true;
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
