using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using MasterInstaller.Components.InstallableComponents.DirectX;
using MasterInstaller.Components.InstallableComponents.DotNet4;
using MasterInstaller.Components.InstallableComponents.FRBDK;
using MasterInstaller.Components.InstallableComponents.XNA3_1;
using MasterInstaller.Components.InstallableComponents.XNA4;
using MasterInstaller.Components.MainComponents.BeginInstall;
using MasterInstaller.Components.MainComponents.Completed;
using MasterInstaller.Components.MainComponents.CustomSetup;
using MasterInstaller.Components.MainComponents.Introduction;
using MasterInstaller.Components.MainComponents.SetupType;
using MasterInstaller.Components.SetupComponents.FrbdkSetup;

namespace MasterInstaller.Components
{
    public static class ComponentStorage
    {
        public static IntroductionComponent IntroductionComponent = new IntroductionComponent();
        public static SetupTypeComponent SetupTypeComponent = new SetupTypeComponent();
        public static CustomSetupComponent CustomSetupComponent = new CustomSetupComponent();
        public static FrbdkSetupComponent FrbdkSetupComponent = new FrbdkSetupComponent();
        public static BeginInstallComponent BeginInstallComponent = new BeginInstallComponent();
        public static DotNet4Component DotNet4Component = new DotNet4Component();
        public static DirectXComponent DirectXComponent = new DirectXComponent();
        public static XNA3_1Component Xna3_1Component = new XNA3_1Component();
        public static XNA4Component Xna4Component = new XNA4Component();
        public static FrbdkComponent FrbdkComponent = new FrbdkComponent();
        public static FileAssociationComponent FileAssociationComponent = new FileAssociationComponent();
        public static CompletedComponent CompletedComponent = new CompletedComponent();

        public static void SetValue(string name, object value)
        {
            if (Settings.ContainsKey(name))
            {
                Settings[name] = value;
            }else
            {
                Settings.Add(name, value);
            }
        }
        public static T GetValue<T>(string name)
        {
            if (!Settings.ContainsKey(name))
                return default(T);

            return (T) Settings[name];
        }

        private static List<InstallableComponentBase> GetInstallableComponents()
        {
            var returnList = new List<InstallableComponentBase>();

            var myType = typeof(ComponentStorage);
            var fields = myType.GetFields(
                   BindingFlags.Public | BindingFlags.Static);
            foreach (var fieldInfo in fields)
            {
                if (typeof(InstallableComponentBase).IsAssignableFrom(fieldInfo.FieldType))
                {
                    returnList.Add((InstallableComponentBase) fieldInfo.GetValue(null));
                }
            }

            return returnList;
        }

        public static SerializableDictionary<string, object> Settings = new SerializableDictionary<string, object>();
        public static List<InstallableComponentBase> InstallableComponents = GetInstallableComponents();

        public static string Save()
        {
            var save = new ComponentStorageSave {Settings = Settings};

            var filename = Restarter.SavePath() + @"restart.xml";
            var s = new XmlSerializer(typeof (ComponentStorageSave));
            using (var tw = new StreamWriter(filename))
            {
                s.Serialize(tw, save);
            }

            return filename;
        }

        public static void Load(string filename)
        {
            var s = new XmlSerializer(typeof (ComponentStorageSave));
            using (var tw = new StreamReader(filename))
            {
                var result = (ComponentStorageSave) s.Deserialize(tw);
                Settings = result.Settings;
            }
        }
    }
}
