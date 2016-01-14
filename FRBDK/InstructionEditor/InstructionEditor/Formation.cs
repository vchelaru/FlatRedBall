using System;
using FlatRedBall;
using FlatRedBall.Instructions;
using InstructionEditor.Collections;

namespace InstructionEditor
{
	/// <summary>
	/// Summary description for Formation.
	/// </summary>
	public class Formation
	{
		
		public InstructionList ia;

		public FormationArray fa;

		public Formation()
		{
			ia = new InstructionList();
			fa = new FormationArray();

		}


		public void SubtractShortestTimeFromAll()
		{
			double shortest = double.MaxValue;

			foreach(Instruction i in ia)
				if(i.TimeToExecute < shortest)
					shortest = i.TimeToExecute;

			foreach(Instruction i in ia)
				i.TimeToExecute -= shortest;

		}
	}
}
