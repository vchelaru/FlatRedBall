using System;
using System.Collections.Generic;
using System.Text;

namespace GlueControl.Models
{
    public class ReferencedFileSave
    {
        public string Name
        {
            get; set;
        }

        public List<PropertySave> Properties
        {
            get;
            set;
        } = new List<PropertySave>();

        public bool IsSharedStatic
        {
            get;
            set;
        }

        public bool LoadedOnlyWhenReferenced
        {
            get
            {
                return Properties.GetValue<bool>(nameof(LoadedOnlyWhenReferenced));
            }
            set
            {
                Properties.SetValue(nameof(LoadedOnlyWhenReferenced), value);
            }
        }


        public bool IsCreatedByWildcard { get; set; }


    }
}
