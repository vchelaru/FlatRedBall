using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueSaveClasses
{
    public enum VersionRequirement
    {
        Any,
        NewerThan,
        EqualToOrNewerThan,
        EqualTo,
        EqualToOrOlderThan,
        OlderThan
    }

    public class PluginRequirement
    {
        public string Name { get; set; }

        // The Version class is not serializable, so we have to store the string instead
        public string Version { get; set; }

        public VersionRequirement VersionRequirement
        {
            get;
            set;
        } = VersionRequirement.EqualToOrNewerThan;


    }
}
