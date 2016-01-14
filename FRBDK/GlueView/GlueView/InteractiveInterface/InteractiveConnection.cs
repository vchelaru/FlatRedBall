using System.Threading.Tasks;
namespace InteractiveInterface
{
    public static class InteractiveConnection
    {
        public static void Initialize()
        {
            Task.Factory.StartNew(() =>
            {
                var callback = Callback;
                try
                {
                    callback.SelectNamedObjectSave(null, null);
                }
                catch
                {
                    // do we care?
                }
            });
            // do nothing?
        }


        public static bool Initialized()
        {
            if (Callback != null)
                return true;

            return false;
        }
        public static IInteractiveInterface Callback;
    }
}
