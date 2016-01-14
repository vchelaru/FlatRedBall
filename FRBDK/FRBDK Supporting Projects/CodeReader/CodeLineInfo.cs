using System;
using System.IO;
using System.Collections;

namespace CodeReader
{
	/// <summary>
	/// Summary description for CodeLineInfo.
	/// </summary>
	public struct CodeLineInfo
	{
		#region members

		public string objectCallingMethod;

		/// <summary>
		/// Returns the property being assigned to separated by periods.
		/// </summary>
		/// <remarks>
		/// For example, FRB.Collections.SpriteArray.x would return a string array with
		/// four items:  FRB, Collections, SpriteArray, and x;
		/// </remarks>
		public string[] assignmentLValue;
		public string[] assignmentRValue;
		public MethodCall methodCall;

		#region XML Docs
		/// <summary>
		/// The type in a declaration.
		/// </summary>
		/// <remarks>
		/// The type would be int in the code "int i;" 
		/// or "SpriteArray sa = new SpriteArray();
		/// </remarks>
		#endregion
		public string declarationType;

		#region XML Docs
		/// <summary>
		/// The variable declared in a declaration;
		/// </summary>
		/// <remarks>
		/// The instance would be i in the code "int i;" or sa in the code
		/// "SpriteArray sa = new SpriteArray();"</remarks>
		#endregion
		public string declarationInstance;

		public string constructorCall;

		public bool assignment;
		#endregion


		public void Decode(string codeToFill)
		{

			// remove tabs at the beginning of the line

			codeToFill = codeToFill.Trim();

			while(codeToFill != "" && codeToFill[0] == '\t')
				codeToFill = codeToFill.Remove(0, 1);



			StringReader sr = new StringReader(codeToFill);

			Initialize();

			string[] itemsInCode = codeToFill.Split(' ');

			/*
			 * Is a comment or #region/#endregionlike:
			 * // comment that should be ignored
			 */
			if(codeToFill == "" || codeToFill.IndexOf("//") == 0 || codeToFill[0] == '#')
				return;
			/*
			 * Simply declaring an object as follows:
			 * int m;
			 */
			if(itemsInCode.Length == 2 && itemsInCode[0].IndexOf('(') == -1)
			{
				declarationType = itemsInCode[0];

				declarationInstance = itemsInCode[1].Substring(0, itemsInCode[1].Length - 1);
			}
				/*
				 * Declaring an object and instantiating it:
				 * SomeObject so = new SomeObject(argument);
				 */
			else if(itemsInCode.Length > 4 && itemsInCode[2] == "=" && itemsInCode[3] == "new")
			{
				declarationType = itemsInCode[0];
				declarationInstance = itemsInCode[1];

				string methodCallText = itemsInCode[4];
				for(int i = 5; i < itemsInCode.Length; i++)
					methodCallText += itemsInCode[i];

				methodCall.Fill( methodCallText);
				assignment = true;
			}

				/*
				 * Declaring a new object but calling a method to get the instance:
				 * SomeObject so = AddObjectMethod(argument);
				 */
			else if(itemsInCode.Length > 3 && itemsInCode[2] == "=")
			{
				declarationType = itemsInCode[0];

				declarationInstance = itemsInCode[1];

				string methodCallText = itemsInCode[3];
				
				for(int i = 4; i < itemsInCode.Length; i++)
					methodCallText += itemsInCode[i];
				methodCall.Fill( methodCallText);
				assignment = true;
			}
				/*
				 * Assinging a property or variable
				 * someObject.someProperty = someValue;
				 * 
				 */
			else if(itemsInCode.Length > 2 && (itemsInCode[1] == "=" || itemsInCode[2] == "="))
			{
				this.assignmentLValue = itemsInCode[0].Split('.');



				int indexOfEqual = 0;

				for(int i = 0; i < itemsInCode.Length; i++)
					if(itemsInCode[i] == "=")
					{
						indexOfEqual = i;
						break;
					}

				assignmentRValue = new string[ itemsInCode.Length - indexOfEqual - 1];

				for(int i = indexOfEqual + 1; i < itemsInCode.Length; i++)
				{
					assignmentRValue[i - indexOfEqual - 1] = itemsInCode[i];
				}
				assignment = true;
			}
				/*
				 * calling some method
				 * someObject.someMethod(arg1, arg2);
				 * 
				 */
			else if(codeToFill.IndexOf('(') != -1)
			{
				int periodBeforeArguments = codeToFill.Substring(0, codeToFill.IndexOf('(') + 1).LastIndexOf('.');

				objectCallingMethod = codeToFill.Substring(0, periodBeforeArguments);

				string argumentList = codeToFill.Substring( periodBeforeArguments + 1, codeToFill.Length - 2 - periodBeforeArguments);

				methodCall.Fill( argumentList );
			}

		}


		private void Initialize()
		{
			assignmentLValue = null;
			assignmentRValue = null;

			objectCallingMethod = declarationType = declarationInstance = "";
			assignment = false;
		}


		public float parseFloat(string s)
		{
			s = s.Replace(";", "");
			s = s.Replace("f", "");
			return float.Parse(s);

		}

		public int parseInt(string s)
		{
			return int.Parse(s.Replace(";", ""));
		}

		public bool parseBool(string s)
		{
			return bool.Parse(s.Replace(";", ""));
		}
	}
}
