using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.IO;
using System.IO;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
#if GLUE
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Parsing;
#endif

namespace FlatRedBall.Glue
{
	public static class BehaviorManager
	{
		#region Fields

		static string mBehaviorTemplateCode;

		#endregion

		#region Properties

		public static string BehaviorFolder
		{
			get
			{
				return FileManager.UserApplicationDataForThisApplication + "behaviors/";
			}
		}

		#endregion

        #region Methods

#if GLUE
        //public static void Initialize()
        //{
        //    mBehaviorTemplateCode = Resources.Resource1.BehaviorTemplate;

        //}
#endif
		public static bool DoesBehaviorExist(string behaviorName)
		{
            string fileName = BehaviorFolder + behaviorName + ".cs";

			return File.Exists(fileName);
		}

		public static List<string> GetAllBehaviors()
		{
			List<string> files = FileManager.GetAllFilesInDirectory(
				BehaviorFolder, "cs");

			for (int i = 0; i < files.Count; i++)
			{
				files[i] = FileManager.RemovePath(FileManager.RemoveExtension(files[i]));
			}

			return files;
		}

		public static ICodeBlock GetMethodCallForBehavior(BehaviorSave behavior, IBehaviorContainer container)
		{
			return GetMethodCallForBehavior(behavior.Name, container);
		}

		public static ICodeBlock GetMethodCallForBehavior(string behaviorName, IBehaviorContainer container)
		{
		    ICodeBlock codeBlock = new CodeDocument();

            string returnString = GetRawBehaviorMethodHeader(behaviorName);

            if (returnString.StartsWith("//"))
            {
                codeBlock.Line(returnString);
                return codeBlock;
            }
            else
            {

                returnString = returnString.Trim();
                returnString = returnString.Replace("public ", "");
                returnString = returnString.Replace("private ", "");
                returnString = returnString.Replace("protected ", "");
                returnString = returnString.Replace("internal ", "");
                returnString = returnString.Replace("override ", "");
                returnString = returnString.Replace("virtual ", "");
                returnString = returnString.Replace("static ", "");

                // The first word is going to be the return value
                returnString = returnString.Substring(returnString.IndexOf(' ') + 1);


				List<BehaviorRequirement> requirements = 
					BehaviorManager.GetBehaviorRequirementsForBehavior(behaviorName);

				if (requirements.Count != 0)
				{
					// Let's clear out the argments and fill them with the objects that fulfill the requirements
					int start = returnString.IndexOf('(') + 1;
					int indexOfClose = returnString.IndexOf(')');

					string argumentList = returnString.Substring(start, indexOfClose - start) ;

					returnString = returnString.Replace(argumentList, "");

					argumentList = "";

					for (int i = 0; i < requirements.Count; i++)
					{
						//string requirementFulfiller = 
						string requirementFulfiller = container.GetFulfillerName(requirements[i]);

						argumentList += requirementFulfiller;

						if (i != requirements.Count - 1)
						{
							argumentList += ", ";
						}

					}
					returnString = returnString.Insert(start, argumentList);


				}

                codeBlock.Line(returnString + ";");
                return codeBlock;
            }
		}

        private static string GetRawBehaviorMethodHeader(string behaviorName)
        {
            string behaviorContents = GetBehaviorContents(behaviorName);

            if (behaviorContents.StartsWith("//"))
            {
                return behaviorContents;
            }
            else
            {
                int indexToStartAt = behaviorContents.IndexOf(behaviorName);

                if (indexToStartAt == -1)
                {
#if GLUE
                    System.Windows.Forms.MessageBox.Show("Could not find the function " + behaviorName + " in the behavior.");
#endif
                    return "";
                }
                else
                {

                    indexToStartAt = behaviorContents.LastIndexOf("\n", indexToStartAt) + 1;

                    int indexToEnd = behaviorContents.IndexOfAny(
                        new char[] { '\r', '\n' }, indexToStartAt + 1);

                    string returnString = behaviorContents.Substring(indexToStartAt, indexToEnd - indexToStartAt);
                    return returnString;
                }
            }
        }

		public static List<IBehaviorContainer> GetAllContainersReferencingBehavior(string behaviorName)
		{
			List<IBehaviorContainer> containers = new List<IBehaviorContainer>();

            //for (int i = 0; i < ObjectFinder.Self.GlueProject.Entities.Count; i++)
            //{
            //    EntitySave entitySave = ObjectFinder.Self.GlueProject.Entities[i];



            //    if (entitySave.ContainsBehavior(behaviorName))
            //    {
            //        containers.Add(entitySave);
            //    }
            //}

            //for (int i = 0; i < ObjectFinder.Self.GlueProject.Screens.Count; i++)
            //{
            //    ScreenSave screenSave = ObjectFinder.Self.GlueProject.Screens[i];

            //    if (screenSave.ContainsBehavior(behaviorName))
            //    {
            //        containers.Add(screenSave);
            //    }
            //}

			return containers;
		}

		private static string GetBehaviorContents(string behaviorName)
		{
		    ICodeBlock codeBlock = new CodeDocument();
            string fileName = BehaviorFolder + behaviorName + ".cs";

            if (File.Exists(fileName))
            {

                string behaviorContents = FileManager.FromFileText(fileName
                    );

                codeBlock.Line(behaviorContents);
            }
            else
            {
                codeBlock.Line("// There is an invalid behavior reference to behavior " + behaviorName);
            }

		    return codeBlock.ToString();
		}

		public static string GetBehaviorCodeFrom(BehaviorSave behavior)
		{
			return GetBehaviorCodeFrom(behavior.Name);
		}

		public static string GetBehaviorCodeFrom(string behaviorName)
		{
            string fileName = BehaviorFolder + behaviorName + ".cs";

            if (!File.Exists(fileName))
            {
                return "// Could not find the behavior " + behaviorName;
            }
            else
            {

                string behaviorContents = FileManager.FromFileText(
                    fileName);

                int indexToStartAt = behaviorContents.IndexOf(behaviorName);

                indexToStartAt = behaviorContents.LastIndexOf("\n", indexToStartAt) + 1;

                int indexToEnd = behaviorContents.LastIndexOf("}");
                indexToEnd = behaviorContents.LastIndexOf("}", indexToEnd - 1);

                string returnString = behaviorContents.Substring(indexToStartAt, indexToEnd - indexToStartAt);

                return returnString;
            }
		}

		public static List<BehaviorRequirement> GetBehaviorRequirementsForElement(IBehaviorContainer behaviorContainer)
		{
			List<BehaviorRequirement> requirements = new List<BehaviorRequirement>();

			for (int i = 0; i < behaviorContainer.Behaviors.Count; i++)
			{
				requirements.AddRange(GetBehaviorRequirementsForBehavior(behaviorContainer.Behaviors[i]));
			}

			return requirements;
		}

		public static List<BehaviorRequirement> GetBehaviorRequirementsForBehavior(BehaviorSave behavior)
		{
			return GetBehaviorRequirementsForBehavior(behavior.Name);
		}

		static char[] splitChars = new[] { ',' };
        public static List<BehaviorRequirement> GetBehaviorRequirementsForBehavior(string behaviorName)
        {
            List<BehaviorRequirement> behaviorRequirements = new List<BehaviorRequirement>();

			if (BehaviorManager.DoesBehaviorExist(behaviorName))
			{

				string header = GetRawBehaviorMethodHeader(behaviorName);

				string argumentList = header.Substring(header.IndexOf('('));

				argumentList = argumentList.Replace("(", "").Replace(")", "");
				argumentList = argumentList.Trim();

				if (!string.IsNullOrEmpty(argumentList))
				{
					string[] arguments = argumentList.Split(splitChars);

					for (int i = 0; i < arguments.Length; i++)
					{
						arguments[i] = arguments[i].Trim();
						BehaviorRequirement requirement = new BehaviorRequirement(arguments[i]);
						requirement.OwningBehavior = behaviorName;
						behaviorRequirements.Add(requirement);
					}

				}

			}
			return behaviorRequirements;

        }

        public static void CreateNewBehavior(string behaviorName)
        {
            string directory = BehaviorFolder;

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string behaviorFileName = directory + behaviorName + ".cs";

            if (File.Exists(behaviorFileName))
            {
#if GLUE
                System.Windows.Forms.MessageBox.Show("The behavior " + behaviorName + " already exists.");
#endif
            }
            else
            {
                // Create a brand new file with the name.
                string newBehaviorContents = mBehaviorTemplateCode.Replace("METHOD_NAME", behaviorName);

                FileManager.SaveText(newBehaviorContents,
                    behaviorFileName);


            }
        }

        #endregion			  

#if GLUE
		internal static void UpdateBehavior(string changedBehavior)
		{
			// Find all objects that reference this behavior.
			List<IBehaviorContainer> allBehaviors = GetAllContainersReferencingBehavior(
				FileManager.RemovePath(FileManager.RemoveExtension(changedBehavior)));

			foreach (IBehaviorContainer container in allBehaviors)
			{
                CodeWriter.GenerateCode(container as IElement);
			}
		}
#endif
        
    }
}
