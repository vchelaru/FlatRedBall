using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall;
using FlatRedBall.Glue;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Content.Instructions;
using FlatRedBall.IO;
using FlatRedBall.Instructions.Reflection;

namespace GlueView.Facades
{
	public class GlueProjectSaveCommands
	{
        List<string> mPositionVariables = new List<string>();

		public GlueProjectSaveCommands()
		{
            mPositionVariables.Add("X");
            mPositionVariables.Add("Y");
		}

		/// <summary>
		/// Saves the variables to the glux file.
		/// If a variable does not exist, it will be created
		/// Tells GlueView to ignore the next Glue Save
		/// </summary>
		public void SaveElement(ElementRuntime elementRuntime, List<string> variables)
		{
			if (elementRuntime != null)
			{
				foreach (string s in variables)
				{
					UpdateIElementVariable(elementRuntime, s);
				}

				//Save
				SaveGlux();

			}
		}

		/// <summary>
		/// Saves the GlueViewState.Self.CurrentGlueProject
		/// </summary>
		public void SaveGlux()
		{
			if (GlueViewState.Self.CurrentGlueProject != null)
			{
				int numberOfTries = 0;
				const int maxNumberOfTries = 5;
				bool hasBeenAdded = false;

				//ObjectFinder.GlueProject = null;
				while (numberOfTries < maxNumberOfTries)
				{
					try
					{
						FileManager.XmlSerialize(GlueViewState.Self.CurrentGlueProject, GlueViewState.Self.CurrentGlueProjectFile);
                        GluxManager.IgnoreNextReload();
                        //GluxManager.IgnoreNextSelection(); // I think we don't want to ignore this too.

						break;
					}
					catch (Exception e)
					{
						System.Threading.Thread.Sleep(25);
						numberOfTries++;
					}
				}
			}
		}

        public void UpdateIElementVariables(ElementRuntime elementRuntime, List<string> variables)
        {
            foreach (string variable in variables)
            {
                UpdateIElementVariable(elementRuntime, variable);
            }
        }

		private void UpdateIElementVariable(ElementRuntime elementRuntime, string variableToUpdate)
		{
			IElement currentIElement = elementRuntime.AssociatedIElement;

			if (currentIElement != null)
			{
				bool exists = currentIElement.GetCustomVariable(variableToUpdate) != null;

				if (!exists)
				{

                    CustomVariable variable = new CustomVariable();
                    variable.Type = "float";
                    variable.Name = variableToUpdate;
                    currentIElement.CustomVariables.Add(variable);
					variable.DefaultValue = 0.0f;

					//Update the Save so you can see the newly added variables
					elementRuntime.AssociatedNamedObjectSave.UpdateCustomProperties();
				}
			}


            bool isFlatRedBallType = elementRuntime.AssociatedNamedObjectSave.SourceType == SourceType.FlatRedBallType;
            bool wasFound = false;
            //Update the saves to the new position

            object objectToGetValueFrom = elementRuntime;
            if(elementRuntime.DirectObjectReference != null)
            {
                objectToGetValueFrom = elementRuntime.DirectObjectReference;
            }

            try
            {
                foreach (InstructionSave instruction in elementRuntime.AssociatedNamedObjectSave.InstructionSaves)
                {
                    if (instruction.Member == variableToUpdate)
                    {
                        Type objectType = objectToGetValueFrom.GetType();
                        instruction.Value = (float)LateBinder.GetInstance(objectType).GetValue(objectToGetValueFrom, variableToUpdate);
                        wasFound = true;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                int m = 3;

            }



            if (!wasFound)
            {
                CustomVariableInNamedObject newVariable = new CustomVariableInNamedObject();
                newVariable.Member = variableToUpdate;

                Type objectType = objectToGetValueFrom.GetType();
                newVariable.Value = (float)LateBinder.GetInstance(objectType).GetValue(objectToGetValueFrom, variableToUpdate);


                newVariable.Type = "float";// assume this for now, change later if necessary
                elementRuntime.AssociatedNamedObjectSave.InstructionSaves.Add(newVariable);
                wasFound = true;
            }
		}
	}
}
