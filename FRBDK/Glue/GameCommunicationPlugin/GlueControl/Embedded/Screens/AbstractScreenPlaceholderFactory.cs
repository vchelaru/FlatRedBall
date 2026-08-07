{CompilerDirectives}

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace GlueControl.Screens
{
    // Selecting an abstract Screen with no concrete derived Screen (e.g. a base GameScreen with a
    // SetByDerived object and nothing deriving from it - see FRB issue #2006) has no real Type to
    // instantiate. SetByDerived fields on the base are just default(T), so any concrete subclass of
    // the abstract Screen is enough to view/edit it. Rather than have Glue codegen a throwaway derived
    // Screen file whose lifetime has to be tracked (added, then removed once a real derived Screen
    // exists), synthesize an empty subclass at runtime - it costs nothing to recreate and there's
    // nothing to clean up.
    static class AbstractScreenPlaceholderFactory
    {
        static readonly ConcurrentDictionary<Type, Type> PlaceholderTypes = new ConcurrentDictionary<Type, Type>();

        static readonly ModuleBuilder ModuleBuilder = AssemblyBuilder
            .DefineDynamicAssembly(new AssemblyName("GlueControl.EditorPlaceholders"), AssemblyBuilderAccess.Run)
            .DefineDynamicModule("GlueControl.EditorPlaceholders");

        public static Type GetOrCreate(Type abstractScreenType) =>
            PlaceholderTypes.GetOrAdd(abstractScreenType, CreatePlaceholderType);

        static Type CreatePlaceholderType(Type abstractScreenType)
        {
            var typeBuilder = ModuleBuilder.DefineType(
                abstractScreenType.FullName + "_EditorPlaceholder",
                TypeAttributes.Public | TypeAttributes.Class,
                abstractScreenType);

            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            return typeBuilder.CreateType();
        }
    }
}
