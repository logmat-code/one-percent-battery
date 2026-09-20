namespace UGSSpace
{
    public static class DefineSymbolsAux
    {

        public static bool Is_UNITY_ANDROID
        {
            get
            {
#if UNITY_ANDROID
                return true;
#else
                return false;
#endif
            }
        }

        public static bool Is_UNITY_IOS
        {
            get
            {
#if UNITY_IOS
                return true;
#else
                return false;
#endif
            }
        }
    }
}
