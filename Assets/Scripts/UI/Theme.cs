using UnityEngine;

namespace PeriodicAR.UI
{
    public static class Theme
    {
        // Surfaces
        public static readonly Color SurfaceGlass    = new(0.08f, 0.10f, 0.14f, 0.78f);
        public static readonly Color SurfaceElevated = new(0.12f, 0.14f, 0.18f, 0.92f);
        public static readonly Color OnSurface       = new(0.95f, 0.97f, 1.00f, 1.00f);
        public static readonly Color OnSurfaceDim    = new(0.95f, 0.97f, 1.00f, 0.60f);

        // Accent
        public static readonly Color Accent          = new(0.30f, 0.78f, 1.00f, 1.00f);
        public static readonly Color AccentMuted     = new(0.30f, 0.78f, 1.00f, 0.30f);

        // Semantic
        public static readonly Color Danger          = new(0.95f, 0.35f, 0.35f, 1.00f);
        public static readonly Color Success         = new(0.40f, 0.85f, 0.55f, 1.00f);
        public static readonly Color Warning         = new(0.98f, 0.78f, 0.30f, 1.00f);

        // Geometry
        public const float CornerRadius    = 16f;
        public const float PillHeight      = 44f;
        public const float FabSize         = 64f;
        public const float WheelGap        = 8f;
        public const float SafeAreaBottom  = 32f;

        // Type scale
        public const float TypeSizeS  = 14f;
        public const float TypeSizeM  = 18f;
        public const float TypeSizeL  = 24f;
        public const float TypeSizeXL = 32f;

        // Motion durations (seconds)
        public const float AnimFast = 0.15f;
        public const float AnimMed  = 0.30f;
        public const float AnimSlow = 0.60f;
    }
}
