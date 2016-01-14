namespace System
{
    public static class StringExtensionMethods
    {
        public static int Compare(string first, string second, bool ignoreCase)
        {
            if (ignoreCase)
            {
                return string.Compare(first, second, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return string.Compare(first, second);
            }
        }
    }
}