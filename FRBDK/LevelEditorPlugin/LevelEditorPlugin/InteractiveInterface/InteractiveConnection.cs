namespace InteractiveInterface
{
    public static class InteractiveConnection
    {
        public static bool Initialized()
        {
            if (Callback != null)
                return true;

            return false;
        }
        public static IInteractiveInterface Callback;
    }
}
