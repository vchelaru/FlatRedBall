using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlueViewOfficialPlugins.Scripting;
using ICSharpCode.NRefactory.CSharp;

namespace GlueView.Scripting
{
    public class PrimitiveOperationManager
    {
        // These are ordered from smallest
        // to biggest.  The order of the enum
        // can be used to 
        enum TypeEnumeration
        {
            Byte,
            Short,
            Int,
            Long,
            Float,
            Double,
            String
        }

        Dictionary<Type, TypeEnumeration> mTypeToEnumerationDictionary = new Dictionary<Type, TypeEnumeration>();


        static PrimitiveOperationManager mSelf;

        public static PrimitiveOperationManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new PrimitiveOperationManager();
                }
                return mSelf;
            }
        }


        public PrimitiveOperationManager()
        {
            mTypeToEnumerationDictionary = new Dictionary<Type, TypeEnumeration>();

            mTypeToEnumerationDictionary.Add(typeof(byte), TypeEnumeration.Byte);
            mTypeToEnumerationDictionary.Add(typeof(short), TypeEnumeration.Short);
            mTypeToEnumerationDictionary.Add(typeof(int), TypeEnumeration.Int);
            mTypeToEnumerationDictionary.Add(typeof(long), TypeEnumeration.Long);
            mTypeToEnumerationDictionary.Add(typeof(float), TypeEnumeration.Float);
            mTypeToEnumerationDictionary.Add(typeof(double), TypeEnumeration.Double);
            mTypeToEnumerationDictionary.Add(typeof(string), TypeEnumeration.String);
        }



        public object AddObjects(object left, object right)
        {
            Type typeToCastTo = GetTypeToCastTo(left, right);

            if (typeToCastTo == typeof(byte))
            {
                return (byte)Convert.ChangeType(left, typeof(byte))
                    +
                    (byte)Convert.ChangeType(right, typeof(byte));
            }
            else if (typeToCastTo == typeof(short))
            {
                return (short)Convert.ChangeType(left, typeof(short))
                    +
                    (short)Convert.ChangeType(right, typeof(short));
            }
            else if (typeToCastTo == typeof(int))
            {
                return (int)Convert.ChangeType(left, typeof(int))
                    +
                    (int)Convert.ChangeType(right, typeof(int));
            }
            else if (typeToCastTo == typeof(long))
            {
                return (long)Convert.ChangeType(left, typeof(long))
                    +
                    (long)Convert.ChangeType(right, typeof(long));
            }
            else if (typeToCastTo == typeof(float))
            {
                return (float)Convert.ChangeType(left, typeof(float))
                    +
                    (float)Convert.ChangeType(right, typeof(float));
            }
            else if (typeToCastTo == typeof(double))
            {
                return (double)Convert.ChangeType(left, typeof(double))
                    +
                    (double)Convert.ChangeType(right, typeof(double));
            }
            else if (typeToCastTo == typeof(string))
            {
                return left.ToString() + right.ToString();

            }
            else
            {
                throw new NotImplementedException();
            }
        }


        public object SubtractObjects(object left, object right)
        {
            Type typeToCastTo = GetTypeToCastTo(left, right);

            if (typeToCastTo == typeof(byte))
            {
                return (byte)Convert.ChangeType(left, typeof(byte))
                    -
                    (byte)Convert.ChangeType(right, typeof(byte));
            }
            else if (typeToCastTo == typeof(short))
            {
                return (short)Convert.ChangeType(left, typeof(short))
                    -
                    (short)Convert.ChangeType(right, typeof(short));
            }
            else if (typeToCastTo == typeof(int))
            {
                return (int)Convert.ChangeType(left, typeof(int))
                    -
                    (int)Convert.ChangeType(right, typeof(int));
            }
            else if (typeToCastTo == typeof(long))
            {
                return (long)Convert.ChangeType(left, typeof(long))
                    -
                    (long)Convert.ChangeType(right, typeof(long));
            }
            else if (typeToCastTo == typeof(float))
            {
                return (float)Convert.ChangeType(left, typeof(float))
                    -
                    (float)Convert.ChangeType(right, typeof(float));
            }
            else if (typeToCastTo == typeof(double))
            {
                return (double)Convert.ChangeType(left, typeof(double))
                    -
                    (double)Convert.ChangeType(right, typeof(double));
            }
            else
            {
                throw new NotImplementedException();
            }
        }


        public object MultiplyObjects(object left, object right)
        {
            Type typeToCastTo = GetTypeToCastTo(left, right);

            if (typeToCastTo == typeof(byte))
            {
                return (byte)Convert.ChangeType(left, typeof(byte))
                    *
                    (byte)Convert.ChangeType(right, typeof(byte));
            }
            else if (typeToCastTo == typeof(short))
            {
                return (short)Convert.ChangeType(left, typeof(short))
                    *
                    (short)Convert.ChangeType(right, typeof(short));
            }
            else if (typeToCastTo == typeof(int))
            {
                return (int)Convert.ChangeType(left, typeof(int))
                    *
                    (int)Convert.ChangeType(right, typeof(int));
            }
            else if (typeToCastTo == typeof(long))
            {
                return (long)Convert.ChangeType(left, typeof(long))
                    *
                    (long)Convert.ChangeType(right, typeof(long));
            }
            else if (typeToCastTo == typeof(float))
            {
                return (float)Convert.ChangeType(left, typeof(float))
                    *
                    (float)Convert.ChangeType(right, typeof(float));
            }
            else if (typeToCastTo == typeof(double))
            {
                return (double)Convert.ChangeType(left, typeof(double))
                    *
                    (double)Convert.ChangeType(right, typeof(double));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public object DivideObjects(object left, object right)
        {
            Type typeToCastTo = GetTypeToCastTo(left, right);

            if (typeToCastTo == typeof(byte))
            {
                return (byte)Convert.ChangeType(left, typeof(byte))
                    /
                    (byte)Convert.ChangeType(right, typeof(byte));
            }
            else if (typeToCastTo == typeof(short))
            {
                return (short)Convert.ChangeType(left, typeof(short))
                    /
                    (short)Convert.ChangeType(right, typeof(short));
            }
            else if (typeToCastTo == typeof(int))
            {
                return (int)Convert.ChangeType(left, typeof(int))
                    /
                    (int)Convert.ChangeType(right, typeof(int));
            }
            else if (typeToCastTo == typeof(long))
            {
                return (long)Convert.ChangeType(left, typeof(long))
                    /
                    (long)Convert.ChangeType(right, typeof(long));
            }
            else if (typeToCastTo == typeof(float))
            {
                return (float)Convert.ChangeType(left, typeof(float))
                    /
                    (float)Convert.ChangeType(right, typeof(float));
            }
            else if (typeToCastTo == typeof(double))
            {
                return (double)Convert.ChangeType(left, typeof(double))
                    /
                    (double)Convert.ChangeType(right, typeof(double));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public object MinObjects(object left, object right)
        {
            Type typeToCastTo = GetTypeToCastTo(left, right);

            if (typeToCastTo == typeof(byte))
            {
                return System.Math.Min((byte)Convert.ChangeType(left, typeof(byte))
                    ,
                    (byte)Convert.ChangeType(right, typeof(byte)));
            }
            else if (typeToCastTo == typeof(short))
            {
                return System.Math.Min((short)Convert.ChangeType(left, typeof(short))
                    ,
                    (short)Convert.ChangeType(right, typeof(short)));
            }
            else if (typeToCastTo == typeof(int))
            {
                return System.Math.Min((int)Convert.ChangeType(left, typeof(int))
                    ,
                    (int)Convert.ChangeType(right, typeof(int)));
            }
            else if (typeToCastTo == typeof(long))
            {
                return System.Math.Min((long)Convert.ChangeType(left, typeof(long))
                    ,
                    (long)Convert.ChangeType(right, typeof(long)));
            }
            else if (typeToCastTo == typeof(float))
            {
                return System.Math.Min((float)Convert.ChangeType(left, typeof(float))
                    ,
                    (float)Convert.ChangeType(right, typeof(float)));
            }
            else if (typeToCastTo == typeof(double))
            {
                return System.Math.Min((double)Convert.ChangeType(left, typeof(double))
                    ,
                    (double)Convert.ChangeType(right, typeof(double)));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public object MaxObjects(object left, object right)
        {
            Type typeToCastTo = GetTypeToCastTo(left, right);

            if (typeToCastTo == typeof(byte))
            {
                return System.Math.Max((byte)Convert.ChangeType(left, typeof(byte))
                    ,
                    (byte)Convert.ChangeType(right, typeof(byte)));
            }
            else if (typeToCastTo == typeof(short))
            {
                return System.Math.Max((short)Convert.ChangeType(left, typeof(short))
                    ,
                    (short)Convert.ChangeType(right, typeof(short)));
            }
            else if (typeToCastTo == typeof(int))
            {
                return System.Math.Max((int)Convert.ChangeType(left, typeof(int))
                    ,
                    (int)Convert.ChangeType(right, typeof(int)));
            }
            else if (typeToCastTo == typeof(long))
            {
                return System.Math.Max((long)Convert.ChangeType(left, typeof(long))
                    ,
                    (long)Convert.ChangeType(right, typeof(long)));
            }
            else if (typeToCastTo == typeof(float))
            {
                return System.Math.Max((float)Convert.ChangeType(left, typeof(float))
                    ,
                    (float)Convert.ChangeType(right, typeof(float)));
            }
            else if (typeToCastTo == typeof(double))
            {
                return System.Math.Max((double)Convert.ChangeType(left, typeof(double))
                    ,
                    (double)Convert.ChangeType(right, typeof(double)));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private Type GetTypeToCastTo(object left, object right)
        {
            Type leftType = left.GetType();
            Type rightType = right.GetType();

            int max = System.Math.Max(
                (int)mTypeToEnumerationDictionary[leftType],
                (int)mTypeToEnumerationDictionary[rightType]);

            TypeEnumeration maxEnumeration = (TypeEnumeration)max;

            Type typeToCastTo = TypeFromEnum(maxEnumeration);
            return typeToCastTo;
        }


        Type TypeFromEnum(TypeEnumeration enumeration)
        {
            foreach (KeyValuePair<Type, TypeEnumeration> kvp in mTypeToEnumerationDictionary)
            {
                if (kvp.Value == enumeration)
                {
                    return kvp.Key;
                }
            }
            return null;

        }

        internal object ApplyUnaryOperation(ICSharpCode.NRefactory.CSharp.UnaryOperatorType unaryOperatorType, object originalValue, ExpressionParseType referenceOrValue)
        {
            object valueToModify = originalValue;

            if (originalValue is IAssignableReference)
            {
                valueToModify = ((IAssignableReference)originalValue).CurrentValue;
            }
            object toReturn = null;


            if (valueToModify == null)
            {
                toReturn = null;
            }

            else if (valueToModify is float)
            {
                if (unaryOperatorType == UnaryOperatorType.Minus)
                {
                    toReturn = (float)valueToModify * -1;
                }
                else if (unaryOperatorType == UnaryOperatorType.PostIncrement)
                {
                    toReturn = (float)valueToModify + 1;
                }
                else if (unaryOperatorType == UnaryOperatorType.PostDecrement)
                {
                    toReturn = (float)valueToModify - 1;
                }
            }
            else if (valueToModify is int)
            {
                if (unaryOperatorType == UnaryOperatorType.Minus)
                {
                    toReturn = (int)valueToModify * -1;
                }
                else if (unaryOperatorType == UnaryOperatorType.PostIncrement)
                {
                    toReturn = (int)valueToModify + 1;
                }
                else if (unaryOperatorType == UnaryOperatorType.PostDecrement)
                {
                    toReturn = (int)valueToModify - 1;
                }
            }
            else if (valueToModify is long)
            {
                if (unaryOperatorType == UnaryOperatorType.Minus)
                {
                    toReturn = (long)valueToModify * -1;
                }
                else if (unaryOperatorType == UnaryOperatorType.PostIncrement)
                {
                    toReturn = (long)valueToModify + 1;
                }
                else if (unaryOperatorType == UnaryOperatorType.PostDecrement)
                {
                    toReturn = (long)valueToModify - 1;
                }
            }
            else if (valueToModify is double)
            {
                if (unaryOperatorType == ICSharpCode.NRefactory.CSharp.UnaryOperatorType.Minus)
                {
                    toReturn = (double)valueToModify * -1;
                }
                else if (unaryOperatorType == UnaryOperatorType.PostIncrement)
                {
                    toReturn = (double)valueToModify + 1;
                }
                else if (unaryOperatorType == UnaryOperatorType.PostDecrement)
                {
                    toReturn = (double)valueToModify - 1;
                }
            }
            else if (valueToModify is string)
            {

            }
            else if (valueToModify is bool)
            {
                if (unaryOperatorType == ICSharpCode.NRefactory.CSharp.UnaryOperatorType.Not)
                {
                    toReturn = !((bool)valueToModify);
                }
            }

            if (toReturn == null)
            {

                throw new NotImplementedException();
            }
            else
            {
                // If the operator is one that modifies the object (like ++), we want to apply it back
                if (referenceOrValue == ExpressionParseType.GetReference && originalValue is IAssignableReference)
                {
                    ((IAssignableReference)originalValue).CurrentValue = toReturn;
                    toReturn = originalValue;
                }


                return toReturn;
            }
        }

        internal bool LessThan(object left, object right)
        {
            Type typeToCastTo = GetTypeToCastTo(left, right);

            if (typeToCastTo == typeof(byte))
            {
                return (byte)Convert.ChangeType(left, typeof(byte))
                    <
                    (byte)Convert.ChangeType(right, typeof(byte));
            }
            else if (typeToCastTo == typeof(short))
            {
                return (short)Convert.ChangeType(left, typeof(short))
                    <
                    (short)Convert.ChangeType(right, typeof(short));
            }
            else if (typeToCastTo == typeof(int))
            {
                return (int)Convert.ChangeType(left, typeof(int))
                    <
                    (int)Convert.ChangeType(right, typeof(int));
            }
            else if (typeToCastTo == typeof(long))
            {
                return (long)Convert.ChangeType(left, typeof(long))
                    <
                    (long)Convert.ChangeType(right, typeof(long));
            }
            else if (typeToCastTo == typeof(float))
            {
                return (float)Convert.ChangeType(left, typeof(float))
                    <
                    (float)Convert.ChangeType(right, typeof(float));
            }
            else if (typeToCastTo == typeof(double))
            {
                return (double)Convert.ChangeType(left, typeof(double))
                    <
                    (double)Convert.ChangeType(right, typeof(double));
            }
            else
            {
                throw new NotImplementedException();
            }

        }

        internal bool GreaterThan(object left, object right)
        {
            return LessThan(right, left);
        }
    }
}
