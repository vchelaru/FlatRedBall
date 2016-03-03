using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using MasterInstaller.Components.InstallableComponents.DirectX;
using MasterInstaller.Components.InstallableComponents.FRBDK;
using MasterInstaller.Components.InstallableComponents.XNA3_1;
using MasterInstaller.Components.InstallableComponents.XNA4;
using MasterInstaller.Components.MainComponents.Completed;
using MasterInstaller.Components.MainComponents.CustomSetup;
using MasterInstaller.Components.MainComponents.Introduction;
using MasterInstaller.Components.SetupComponents.FrbdkSetup;

namespace MasterInstaller.Components
{
    public static class ComponentStorage
    {
        public static IntroductionComponent IntroductionComponent;
        public static CustomSetupComponent CustomSetupComponent;
        public static FrbdkSetupComponent FrbdkSetupComponent;
        public static DirectXComponent DirectXComponent;
        public static XNA3_1Component Xna3_1Component;
        public static XNA4Component Xna4Component;
        public static FrbdkComponent FrbdkComponent;
        public static FileAssociationComponent FileAssociationComponent;
        public static CompletedComponent CompletedComponent;

        public static void SetValue(string name, bool value)
        {
            if (Settings.ContainsKey(name))
            {
                Settings[name] = value;
            }else
            {
                Settings.Add(name, value);
            }
        }
        public static bool GetValue(string name)
        {
            if (!Settings.ContainsKey(name))
                return false;

            return Settings[name];
        }

        static List<InstallableComponentBase> installableComponents;
        public static List<InstallableComponentBase> GetInstallableComponents()
        {
            Initialize();

            if (installableComponents == null)
            {
                installableComponents = new List<InstallableComponentBase>();

                var myType = typeof(ComponentStorage);
                var fields = myType.GetFields(
                       BindingFlags.Public | BindingFlags.Static);
                foreach (var fieldInfo in fields)
                {
                    if (typeof(InstallableComponentBase).IsAssignableFrom(fieldInfo.FieldType))
                    {
                        var component = (InstallableComponentBase)fieldInfo.GetValue(null);

                        installableComponents.Add(component);
                    }
                }

            }
            return installableComponents;
        }

        public static SerializableDictionary<string, bool> Settings = 
            new SerializableDictionary<string, bool>();

        static bool initialized = false;
        static void Initialize()
        {
            if (!initialized)
            {
                initialized = true;
                FrbdkSetupComponent = new FrbdkSetupComponent();
                DirectXComponent = new DirectXComponent();
                Xna3_1Component = new XNA3_1Component();
                Xna4Component = new XNA4Component();
                FrbdkComponent = new FrbdkComponent();
            }
        }
    }
}
