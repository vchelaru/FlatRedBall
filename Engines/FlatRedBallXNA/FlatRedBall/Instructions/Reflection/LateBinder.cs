#if IOS || ANDROID
#define NO_CODE_EMIT
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
#if !NO_CODE_EMIT
using System.Reflection.Emit;
#endif

#if WpfDataUi
namespace WpfDataUi

#else
namespace FlatRedBall.Instructions.Reflection
#endif
{
    public abstract class LateBinder
    {
        static Dictionary<Type, LateBinder> mLateBinders = new Dictionary<Type, LateBinder>();

        /// <summary>
        /// Forces the plain-reflection implementation even where dynamic code is available. Exists so tests can
        /// exercise the Native AOT code path on the JIT.
        /// </summary>
        internal static bool ForceReflection;

        /// <summary>
        /// Whether get/set delegates can be emitted with DynamicMethod. False under Native AOT (and other runtimes
        /// where RuntimeFeature.IsDynamicCodeSupported is false), where LateBinder uses plain reflection instead.
        /// </summary>
        internal static bool CanEmitCode
        {
            get
            {
#if NO_CODE_EMIT
                return false;
#elif NET6_0_OR_GREATER
                return RuntimeFeature.IsDynamicCodeSupported && !ForceReflection;
#else
                return !ForceReflection;
#endif
            }
        }

        /// <summary>
        /// The members LateBinder reads and writes by name. Native AOT and trimming keep these on any type used with
        /// LateBinder when the type is known statically.
        /// </summary>
        internal const DynamicallyAccessedMemberTypes AccessedMembers =
            DynamicallyAccessedMemberTypes.PublicFields |
            DynamicallyAccessedMemberTypes.NonPublicFields |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.NonPublicProperties;

        /// <summary>
        /// Returns the LateBinder for the argument type. Under Native AOT or trimming, the fields and properties
        /// of the type must be kept: pass a type known at compile time, or root the type's members (for example with
        /// DynamicDependency). Value types additionally need LateBinder&lt;T&gt; instantiated at compile time
        /// under Native AOT.
        /// </summary>
        [UnconditionalSuppressMessage("AOT", "IL3050",
            Justification = "Reference types share the canonical LateBinder<__Canon> code, so any reference type works " +
            "under Native AOT. Value types are reported below when their instantiation is not available.")]
        public static LateBinder GetInstance([DynamicallyAccessedMembers(AccessedMembers)] Type type)
        {
            if (!mLateBinders.ContainsKey(type))
            {
                Type t;
                try
                {
                    t = typeof(LateBinder<>).MakeGenericType(
                        type);
                }
                catch (NotSupportedException e) when (type.IsValueType)
                {
                    throw new NotSupportedException(
                        $"LateBinder cannot be created for the value type {type} because LateBinder<{type.Name}> was not " +
                        $"compiled into this app (Native AOT cannot create generic instantiations over value types at runtime). " +
                        $"Reference LateBinder<{type.Name}> somewhere in code, or use a class.", e);
                }
                object obj = Activator.CreateInstance(t);

                mLateBinders.Add(type, obj as LateBinder);
            }
            return mLateBinders[type];

        }

        /// <summary>
        /// Returns the value of the variable obtained by name. This named "Static"
        /// because it is a static version of the GetValue variable on LateBinderinstances.
        /// This can be used to obtain values without needing to first create an instance of a late binder.
        /// </summary>
        /// <param name="target">The object from which to get a value.</param>
        /// <param name="name">The variable name.</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("Looks up members by name on the runtime type of the target. Under trimming or Native AOT the target's fields and properties must be preserved, for example by annotating or rooting the type.")]
        public static object GetValueStatic(object target, string name)
        {
            return GetInstance(target.GetType()).GetValue(target, name);
        }

        [RequiresUnreferencedCode("Looks up members by name on the runtime type of the target. Under trimming or Native AOT the target's fields and properties must be preserved, for example by annotating or rooting the type.")]
        public static bool SetValueStatic(object target, string name, object value)
        {
            return GetInstance(target.GetType()).SetValue(target, name, value);
        }

        [RequiresUnreferencedCode("Looks up members by name on the runtime type of the target. Under trimming or Native AOT the target's fields and properties must be preserved, for example by annotating or rooting the type.")]
        public static bool TryGetValueStatic(object target, string name, out object result)
        {
            return GetInstance(target.GetType()).TryGetValue(target, name, out result);
        }

        public abstract object GetValue(object target, string name);
        public abstract bool IsReadOnly(string name);
        public abstract bool IsWriteOnly(string name);
        public abstract bool TryGetValue(object target, string name, out object result);
        public abstract bool SetValue(object target, string name, object value);
    }



    /// <summary>
    /// Provides a simple interface to late bind a class.
    /// </summary>
    /// <remarks>The first time you attempt to get or set a property, it will dynamically generate the get and/or set 
    /// methods and cache them internally.  Subsequent gets uses the dynamic methods without having to query the type's 
    /// meta data.</remarks>
    public sealed class LateBinder<[DynamicallyAccessedMembers(LateBinder.AccessedMembers)] T> : LateBinder
    {
        internal class SetPropertyInfo
        {
            public SetHandler Delegate;
            public PropertyInfo PropertyInfo;

            internal void Invoke(object source, object value)
            {
#if DEBUG
                if(value != null)
                {
                    var valueType = value.GetType();
                    var propertyType = PropertyInfo.PropertyType; 
                    var isAssignable = propertyType.IsAssignableFrom(valueType);
                    if(!isAssignable)
                    {
                        var message =
                            $"Attempting to set property {PropertyInfo.Name} of type {propertyType} to value {value} with type {value.GetType()}";
                        throw new InvalidOperationException(message);
                    }
                }
#endif
                Delegate(source, value);
            }
        }
        #region Fields

        HashSet<string> mFieldsSet = new HashSet<string>();
        HashSet<string> mPropertieSet = new HashSet<string>();

        [DynamicallyAccessedMembers(LateBinder.AccessedMembers)]
        private Type mType;
        private Dictionary<string, GetHandler> mPropertyGet;
        private Dictionary<string, SetPropertyInfo> mPropertySet;

        private Dictionary<Type, List<string>> mFields;

        private T mTarget = default(T);

        private static LateBinder<T> _instance;

        #endregion

        #region Properties

        public static LateBinder<T> Instance
        {
            get { return _instance; }
        }


        /// <summary>
        /// The instance that this binder operates on by default
        /// </summary>
        /// <remarks>This can be overridden by the caller explicitly passing a target to the indexer</remarks>
        public T Target
        {
            get { return mTarget; }
            set { mTarget = value; }
        }

        /// <summary>
        /// Gets or Sets the supplied property on the contained <seealso cref="Instance"/>
        /// </summary>
        /// <exception cref="InvalidOperationException">Throws if the contained Instance is null.</exception>
        public object this[string propertyName]
        {
            get
            {
                ValidateInstance();
                return this[mTarget, propertyName];
            }
            set
            {
                ValidateInstance();
                this[mTarget, propertyName] = value;
            }
        }

        /// <summary>
        /// Gets or Sets the supplied property on the supplied target
        /// </summary>
        public object this[T target, string propertyName]
        {
            get
            {
                ValidateGetter(ref propertyName);
                return mPropertyGet[propertyName](target);
            }
            set
            {
                ValidateSetter(ref propertyName);
                mPropertySet[propertyName].Invoke(target, value);
            }
        }


        #endregion

        #region Methods

        #region Constructors

        static LateBinder()
        {
            _instance = new LateBinder<T>();
        }


        public LateBinder(T instance)
            : this()
        {
            mTarget = instance;
        }

        public LateBinder()
        {
            mType = typeof(T);
            mPropertyGet = new Dictionary<string, GetHandler>();
            mPropertySet = new Dictionary<string, SetPropertyInfo>();

            mFields = new Dictionary<Type, List<string>>();
        }
        #endregion

        #endregion

        #region Public Methods

        public override object GetValue(object target, string name)
        {
            if (mFieldsSet.Contains(name))
            {
                return GetField(target, name);
            }
            else if (mPropertieSet.Contains(name))
            {
                return GetProperty(target, name);
            }
            else
            {
                GetFieldRecursive(mType, name, out FieldInfo fieldInfo, out Type throwaway);
                if (fieldInfo != null)
                {
                    mFieldsSet.Add(name);
                    return GetField(target, name);
                }
                else
                {
                    mPropertieSet.Add(name);
                    return GetProperty(target, name);
                }
            }
        }

        private void GetFieldRecursive([DynamicallyAccessedMembers(AccessedMembers)] Type type, string fieldName, out FieldInfo field, out Type ownerType)
        {
            field = type.GetField(fieldName, mGetFieldBindingFlags);
            ownerType = null;

            if(field != null)
            {
                ownerType = type;
            }
            else if(field == null && type.BaseType != null)
            {
                GetFieldRecursive(GetBaseType(type), fieldName, out field, out ownerType);
            }
        }

        // The annotation on T keeps the inherited public and protected members of its base types, but
        // Type.BaseType itself carries no annotation, so the base-type walks go through here.
        [return: DynamicallyAccessedMembers(AccessedMembers)]
        [UnconditionalSuppressMessage("Trimming", "IL2073",
            Justification = "The annotation on T keeps the members of its base types.")]
        private static Type GetBaseType([DynamicallyAccessedMembers(AccessedMembers)] Type type) => type.BaseType;

        public override bool IsReadOnly(string name)
        {
            FieldInfo fieldInfo;
            PropertyInfo propertyInfo;

            fieldInfo = mType.GetField(name);

            if (fieldInfo != null)
            {
                return false; // need to adjust this eventually...assuming false now
            }
            else
            {
                propertyInfo = mType.GetProperty(name);
                if (propertyInfo != null)
                {
                    return propertyInfo.CanWrite == false;
                }
            }

            return false;
        }

        public override bool IsWriteOnly(string name)
        {
            FieldInfo fieldInfo;
            PropertyInfo propertyInfo;

            fieldInfo = mType.GetField(name);

            if (fieldInfo != null)
            {
                return false; // need to adjust this eventually...assuming false now
            }
            else
            {
                propertyInfo = mType.GetProperty(name);
                if (propertyInfo != null)
                {
                    return propertyInfo.CanRead == false;
                }
            }

            return false;

        }

        public override bool TryGetValue(object target, string name, out object result)
        {
            if (mFieldsSet.Contains(name))
            {
                result = GetField(target, name);
                return true;
            }
            else if (mPropertieSet.Contains(name))
            {
                result = GetProperty(target, name);
                return true;
            }
            else
            {
                if (mType.GetField(name, mGetFieldBindingFlags) != null)
                {
                    mFieldsSet.Add(name);
                    result = GetField(target, name);
                    return true;
                }
                else if (mType.GetProperty(name) != null)
                {
                    mPropertieSet.Add(name);
                    result = GetProperty(target, name);
                    return true;
                }
                else
                {
                    result = null;
                    return false;
                }

            }
        }

        public override bool SetValue(object target, string name, object value)
        {
            var wasSet = false;
            if (mFieldsSet.Contains(name))
            {
                // do nothing currently
                SetField(target, name, value);
                wasSet = true;
            }
            else if (mPropertieSet.Contains(name))
            {
                SetProperty(target, name, value);
                wasSet = true;
            }
            else
            {
                try
                {
                    if (mType.GetField(name, mGetFieldBindingFlags) != null)
                    {
                        mFieldsSet.Add(name);

                        SetField(target, name, value);
                        wasSet = true;
                    }
                    else
                    {
                        mPropertieSet.Add(name);
                        SetProperty(target, name, value);
                        wasSet = true;
                    }
                }
                catch(InvalidCastException innerException)
                {
                    throw new InvalidOperationException(
                        $"Error setting {name} to {value} (value type {value?.GetType()})", innerException);
                }
            }

            return wasSet;
        }

        private void SetField(object target, string name, object value)
        {



#if NO_CODE_EMIT
            
            // If a field is 
            // set to a struct
            // like Vector3, then
            // the value that is set
            // will be boxed if using
            // SetValue.  On the PC we
            // use SetValueDirect, but that
            // isn't available on the CompactFramework.
            // I've asked a question about it here:
            // http://stackoverflow.com/questions/11698172/how-to-set-a-field-of-a-struct-type-on-an-object-in-windows-phone-7
            FieldInfo fieldInfo = target.GetType().GetField(
                name);

            fieldInfo.SetValue(target, value);

#else
            FieldInfo fieldInfo = mType.GetField(
                name);

#if DEBUG
            try
            {
#endif



                fieldInfo.SetValueDirect(
                    __makeref(target), value);


#if DEBUG
            }
            catch (Exception e)
            {
                if(fieldInfo == null)
                {
                    throw new Exception("Could nto find field by the name " + name );
                }
                else
                {

                    throw new Exception("Error trying to set field " + name + " which is of type " + fieldInfo.FieldType + ".\nTrying to set to " + value + " of type " + value.GetType());
                }
            }
#endif


#endif
        }


        /// <summary>
        /// Sets the supplied property on the supplied target
        /// </summary>
        /// <typeparam name="K">the type of the value</typeparam>
        public void SetProperty<K>(object target, string propertyName, K value)
        {
#if NO_CODE_EMIT

            // find out if this is a property or field
            Type type = typeof(T);

            PropertyInfo propertyInfo = type.GetProperty(propertyName);

            if (propertyInfo != null)
            {
                propertyInfo.SetValue(target, value, null);
            }

            else
            {
                FieldInfo fieldInfo = type.GetField(propertyName);

                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(target, value);
                }
                else
                {
                    throw new ArgumentException("Cannot find property or field with the name " + propertyName);
                }

            }


#else
            ValidateSetter(ref propertyName);

            if (mPropertySet.ContainsKey(propertyName))
            {
                try
                {
                    mPropertySet[propertyName].Invoke(target, value);
                }
                catch (System.AccessViolationException ex)
                {
                    throw new Exception($"Could not set {propertyName} to {value} with type {value?.GetType()}, probably because the types do not match", ex);
                }
            }
            else
            {
                // This is probably not a property so see if it is a field.

                FieldInfo fieldInfo = mType.GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

                if (fieldInfo == null)
                {
                    string errorMessage =
                        $"LateBinder could not find a field or property by the name of {propertyName}" +
                        $" in the class {mType}.  Check the name of the property to verify if it is correct.";
                    throw new System.MemberAccessException(errorMessage);
                }
                else
                {

                    fieldInfo.SetValue(target, value);
                }
            }
#endif
        }

        static BindingFlags mGetFieldBindingFlags = BindingFlags.GetField | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | 
            BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        public object GetField(object target, string fieldName)
        {


            if (target == null)
            {
                FieldInfo fieldInfo = mType.GetField(fieldName, mGetFieldBindingFlags);
                return fieldInfo.GetValue(null);

            }
            else
            {
                // use this to get the recurisve type in case it's defined by base
                GetFieldRecursive(mType, fieldName, out FieldInfo fieldInfo, out Type fieldOwner);

                return fieldInfo.GetValue(target);
            }
        }

        public ReturnType GetField<ReturnType>(T target, string propertyName)
        {
            return (ReturnType)GetField(target, propertyName);
        }

        /// <summary>
        /// Gets  the supplied property on the supplied target
        /// </summary>
        /// <typeparam name="K">The type of the property being returned</typeparam>
        public K GetProperty<K>(T target, string propertyName)
        {
            return (K)GetProperty(target, propertyName);
        }

        public object GetProperty(object target, string propertyName)
        {
#if NO_CODE_EMIT
            // SLOW, but still works
            return GetPropertyThroughReflection(target, propertyName);
#else
            // June 11, 2011
            // Turns out that
            // getters for value
            // types don't work properly.
            // I found this out by trying to
            // get the X value on a System.Drawing.Rectangle
            // which was 0, but it kept returning a value of
            // 2 billion.  Checking for value types and using 
            // regular reflection fixes this problem.
            if (target == null || typeof(T).IsValueType)
            {
                // SLOW, but still works
                return GetPropertyThroughReflection(target, propertyName);
            }
            else
            {
                ValidateGetter(ref propertyName);

                GetHandler getHandler = mPropertyGet[propertyName];

                // getter may throw an exception.  We don't want the grid to blow up in that case, so we'll return null:
                try
                {
                    return getHandler(target);
                }
                catch
                {
                    return null;
                }
            }
#endif


        }

        private static object GetPropertyThroughReflection(object target, string propertyName)
        {
            PropertyInfo pi = typeof(T).GetProperty(propertyName, mGetterBindingFlags);

            if (pi == null)
            {
                string message = "Could not find the property " + propertyName + "\n\nAvailableProperties:\n\n";
                PropertyInfo[] properties = typeof(T).GetProperties(mGetterBindingFlags);

                foreach (PropertyInfo containedProperty in properties)
                {
                    message += containedProperty.Name + "\n";

                }

                throw new InvalidOperationException(message);
            }

            return pi.GetValue(target, null);
        }

        #endregion

        #region Private Helpers
        private void ValidateInstance()
        {
            if (mTarget == null)
            {
                throw new InvalidOperationException("Instance property must not be null");
            }
        }
        private void ValidateSetter(ref string propertyName)
        {
            if (!mPropertySet.ContainsKey(propertyName))
            {
                BindingFlags bindingFlags =
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.Static;
                PropertyInfo propertyInfo = mType.GetProperty(propertyName, bindingFlags);

                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    var setInfo = new SetPropertyInfo();
                    setInfo.Delegate = DynamicMethodCompiler.CreateSetHandler(mType, propertyInfo);
                    setInfo.PropertyInfo = propertyInfo;
                    mPropertySet.Add(propertyName, setInfo);
                }

            }
        }

        static BindingFlags mGetterBindingFlags = 
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.FlattenHierarchy;                

        private void ValidateGetter(ref string propertyName)
        {
            if (!mPropertyGet.ContainsKey(propertyName))
            {
                Type type = mType;
                PropertyInfo propertyInfo = mType.GetProperty(propertyName, mGetterBindingFlags);

                var baseType = GetBaseType(mType);
                while(baseType != null && propertyInfo == null)
                {
                    propertyInfo = baseType.GetProperty(propertyName, mGetterBindingFlags);
                    if(propertyInfo != null)
                    {
                        type = baseType;
                    }
                    else
                    {
                        baseType = GetBaseType(baseType);
                    }
                }
                if (propertyInfo != null)
                {

                    mPropertyGet[propertyName] = DynamicMethodCompiler.CreateGetHandler(type, propertyInfo);
                }
            }
        }
#endregion

#region Contained Classes
        internal delegate object GetHandler(object source);
        internal delegate void SetHandler(object source, object value);

        /// <summary>
        /// provides helper functions for late binding a class
        /// </summary>
        /// <remarks>
        /// Class found here:
        /// http://www.codeproject.com/useritems/Dynamic_Code_Generation.asp
        /// </remarks>
        internal sealed class DynamicMethodCompiler
        {
            // DynamicMethodCompiler
            private DynamicMethodCompiler() { }

            // The reflection fallbacks below keep the exceptions the emitted delegates produce: a setter or getter that
            // throws surfaces its own exception (not TargetInvocationException), and a value of the wrong type is an
            // InvalidCastException, which callers such as the Tiled entity instantiator catch to explain the mismatch.
            private static object GetThroughReflection(PropertyInfo propertyInfo, object source)
            {
                try
                {
                    return propertyInfo.GetValue(source, null);
                }
                catch (TargetInvocationException e) when (e.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                    throw;
                }
            }

            private static void SetThroughReflection(PropertyInfo propertyInfo, object source, object value)
            {
                try
                {
                    propertyInfo.SetValue(source, value, null);
                }
                catch (TargetInvocationException e) when (e.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                }
                catch (ArgumentException e)
                {
                    throw new InvalidCastException(e.Message, e);
                }
            }

            // CreateGetDelegate
            [UnconditionalSuppressMessage("AOT", "IL3050",
                Justification = "Code is only emitted when CanEmitCode is true, which includes RuntimeFeature.IsDynamicCodeSupported.")]
            internal static GetHandler CreateGetHandler(Type type, PropertyInfo propertyInfo)
            {
                if (!CanEmitCode)
                {
                    // Native AOT and other runtimes without dynamic code: plain reflection.
                    return source => GetThroughReflection(propertyInfo, source);
                }

#if !NO_CODE_EMIT
                MethodInfo getMethodInfo = propertyInfo.GetGetMethod(true);
                DynamicMethod dynamicGet = CreateGetDynamicMethod(type);
                ILGenerator getGenerator = dynamicGet.GetILGenerator();

                getGenerator.Emit(OpCodes.Ldarg_0);
                getGenerator.Emit(OpCodes.Call, getMethodInfo);
                BoxIfNeeded(getMethodInfo.ReturnType, getGenerator);
                getGenerator.Emit(OpCodes.Ret);

                var getHandler = (GetHandler)dynamicGet.CreateDelegate(typeof(GetHandler));
                return getHandler;
#else
                throw new NotSupportedException();
#endif
            }

            // CreateSetDelegate
            [UnconditionalSuppressMessage("AOT", "IL3050",
                Justification = "Code is only emitted when CanEmitCode is true, which includes RuntimeFeature.IsDynamicCodeSupported.")]
            internal static SetHandler CreateSetHandler(Type type, PropertyInfo propertyInfo)
            {
                if (!CanEmitCode)
                {
                    // Native AOT and other runtimes without dynamic code: plain reflection.
                    return (source, value) => SetThroughReflection(propertyInfo, source, value);
                }

#if !NO_CODE_EMIT
                MethodInfo setMethodInfo = propertyInfo.GetSetMethod(true);

                DynamicMethod dynamicSet = CreateSetDynamicMethod(type);
                ILGenerator setGenerator = dynamicSet.GetILGenerator();

                setGenerator.Emit(OpCodes.Ldarg_0);
                setGenerator.Emit(OpCodes.Ldarg_1);
                UnboxIfNeeded(setMethodInfo.GetParameters()[0].ParameterType, setGenerator);
                setGenerator.Emit(OpCodes.Call, setMethodInfo);
                setGenerator.Emit(OpCodes.Ret);

                return (SetHandler)dynamicSet.CreateDelegate(typeof(SetHandler));
#else
                throw new NotSupportedException();
#endif
            }

#if !NO_CODE_EMIT
            // CreateGetDynamicMethod
#if NET7_0_OR_GREATER
            [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
#endif
            private static DynamicMethod CreateGetDynamicMethod(Type type)
            {
                return new DynamicMethod("DynamicGet", typeof(object), new Type[] { typeof(object) }, type, true);
            }

            // CreateSetDynamicMethod
#if NET7_0_OR_GREATER
            [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
#endif
            private static DynamicMethod CreateSetDynamicMethod(Type type)
            {
                return new DynamicMethod("DynamicSet", typeof(void), new Type[] { typeof(object), typeof(object) }, type, true);
            }

            // BoxIfNeeded
            private static void BoxIfNeeded(Type type, ILGenerator generator)
            {
                if (type.IsValueType)
                {
                    generator.Emit(OpCodes.Box, type);
                }
            }

            // UnboxIfNeeded
            private static void UnboxIfNeeded(Type type, ILGenerator generator)
            {
                if (type.IsValueType)
                {
                    generator.Emit(OpCodes.Unbox_Any, type);
                }
            }
#endif
        }
#endregion
    }
}
