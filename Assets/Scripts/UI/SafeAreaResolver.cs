using UnityEngine;

namespace PeriodicAR.UI
{
    public static class SafeAreaResolver
    {
        public static float BottomInset()
        {
            var sa = Screen.safeArea;
            float screenH = Screen.height;
            float inset = sa.y; // pixels from the bottom of the screen to the safe area
            float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
            float ptInset = inset * (160f / dpi);
            return Mathf.Max(ptInset, Theme.SafeAreaBottom);
        }
    }
}
