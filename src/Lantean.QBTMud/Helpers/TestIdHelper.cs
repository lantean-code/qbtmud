namespace Lantean.QBTMud.Helpers
{
    public static class TestIdHelper
    {
        private static bool _useTestIds = false;

        public static string? For(string id)
        {
            if (_useTestIds)
            {
                return id;
            }

            return null;
        }

        internal static void EnableTestIds()
        {
            _useTestIds = true;
        }
    }
}
