using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.IO;
using System.IO;

namespace FlatRedBall.Glue.SaveClasses
{
    #region Enums

    public enum CallType
	{
		AutomaticActivity,
		AutomaticInitialize,
		Manual
    }

    public enum FileLocation
    {
        ProjectPrimarySharedSecondary,
        SharedPrimaryProjectSecondary,
        ProjectOnly,
        SharedOnly
    }

    #endregion


    #region BehaviorRequirement

    public struct BehaviorRequirement
    {
        public string OwningBehavior;
        public string Name;
        public string Type;

        public BehaviorRequirement(string entireRequirementLine)
        {
            int spaceIndex = entireRequirementLine.IndexOf(' ');

            Type = entireRequirementLine.Substring(0, spaceIndex);

            int typeStartIndex = spaceIndex + 1;
            Name = entireRequirementLine.Substring(typeStartIndex, entireRequirementLine.Length - typeStartIndex);
            OwningBehavior = null;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} in {2}", Type, Name, OwningBehavior);
        }
    }
    #endregion

    public class BehaviorSave
    {
        #region Properties

        public CallType CallType
		{
			get;
			set;
		}

		[ReadOnlyAttribute(true)]
		public string Name
		{
			get;
			set;
		}

		public BehaviorSave Clone()
		{
			return (BehaviorSave)this.MemberwiseClone();
		}

        public FileLocation FileLocation
        {
            get;
            set;
        }

        #endregion



    }
}
