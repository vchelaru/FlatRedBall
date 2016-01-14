using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Serialization.Formatters;

namespace RemotingHelper
{
    public static class RemotingServer
    {
        public static void SetupPort(int port)
        {
            var serverFormatter = new SoapServerFormatterSinkProvider {TypeFilterLevel = TypeFilterLevel.Full};

            var dictionary = new ListDictionary {{"port", port}};

            ChannelServices.RegisterChannel(new HttpChannel(dictionary, null, serverFormatter), false);
        }

        public static void SetupInterface<T>()
        {
            var typeInfo =
                new WellKnownServiceTypeEntry(typeof(T),
                     GetName(typeof(T)) + ".rem", WellKnownObjectMode.Singleton);

            RemotingConfiguration.RegisterWellKnownServiceType(typeInfo);
        }

        internal static string GetName(MemberInfo source)
        {
            var info = source;
            var attributes = info.GetCustomAttributes(true);

            return attributes.Length > 0 ? ((DescriptionAttribute)attributes[0]).Description : source.Name;
        }
    }
}
