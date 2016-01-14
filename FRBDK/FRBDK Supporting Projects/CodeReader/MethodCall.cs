using System;
using System.Collections;

namespace CodeReader
{
	/// <summary>
	/// Summary description for MethodCall.
	/// </summary>
	public struct MethodCall
	{
		public string methodCalled;
		public ArrayList arguments;

		public void Fill(string code)
		{
			arguments = new ArrayList();
			methodCalled = code.Substring(0, code.IndexOf('('));

			string argumentList = code.Substring(code.IndexOf('(') + 1, code.LastIndexOf(')') - code.IndexOf('(') - 1);

			if(argumentList.IndexOf('(') != -1)
			{
				int level = 0;
				ArrayList splits = new ArrayList();


				for(int i = 0; i < argumentList.Length; i++)
				{
					if(level == 0 && argumentList[i] == ',')
						splits.Add(i);
					if(argumentList[i] == '(')
						level++;
					if(argumentList[i] == ')')
						level--;
				}

				if(splits.Count == 0)
				{
					arguments.Add(argumentList);
				}
				else
				{
				

				}
			}
			else
			{
				string[] sa = argumentList.Split(',');
				foreach(string s in sa)
					arguments.Add(s.Trim());
			}


		}
	
	
		public void RemoveArgumentDoubleQuotes()
		{
			for(int i = 0; i < arguments.Count; i++)
			{
				string s = arguments[i] as String;
				s = s.Replace("\"", "");
				arguments[i] = s;
			}
		}
	}
}
