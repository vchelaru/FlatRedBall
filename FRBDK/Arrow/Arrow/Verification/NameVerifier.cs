using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Arrow.Managers;
using FlatRedBall.Arrow.DataTypes;
using FlatRedBall.Instructions.Reflection;

namespace FlatRedBall.Arrow.Verification
{
    public class NameVerifier : Singleton<NameVerifier>
    {
        public bool IsInstanceNameValid(string name, out string whyIsntValid)
        {
            whyIsntValid = null;
            if (string.IsNullOrEmpty(name))
            {
                whyIsntValid = "Name cannot be empty";
            }

            return string.IsNullOrEmpty(whyIsntValid);

        }


        public bool IsInstanceNameValid(string name, out string whyIsntValid, object instanceSave, ArrowElementSave container)
        {
            whyIsntValid = null;
            foreach (var instanceObject in container.AllInstances)
            {
                if (instanceObject != instanceSave && LateBinder.GetValueStatic(instanceObject, "Name") as string == name)
                {
                    whyIsntValid = "The name " + name + " is already being used by another object";
                    break;
                }
            }

            if (string.IsNullOrEmpty(whyIsntValid))
            {
                IsInstanceNameValid(name, out whyIsntValid);
            }

            return string.IsNullOrEmpty(whyIsntValid);
        }

    }
}
