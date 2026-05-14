using System.Collections.Generic;
using PeriodicAR.AR;
using UnityEngine;

namespace PeriodicAR.UI
{
    /// <summary>
    /// Provides the built-in "Place" wheel item that wraps TapToPlace.
    /// All other items are registered by their feature controllers.
    /// </summary>
    public static class WheelRegistry
    {
        public static List<WheelItem> GetBuiltinItems()
        {
            return new List<WheelItem>
            {
                new WheelItem
                {
                    id       = "place",
                    label    = "Place",
                    iconName = "⊞",
                    page     = 0,
                    onTap    = OnPlaceTapped,
                },
            };
        }

        private static void OnPlaceTapped()
        {
            var placer = Object.FindAnyObjectByType<TapToPlace>();
            if (placer == null) return;
            switch (placer.CurrentState)
            {
                case TapToPlace.State.Idle:
                    placer.ArmPlacement();
                    AppShellController.Instance?.ShowStatus("Tap a plane to place the table");
                    break;
                case TapToPlace.State.Armed:
                    placer.CancelPlacement();
                    break;
                case TapToPlace.State.Placed:
                    placer.RemoveTable();
                    AppShellController.Instance?.ShowStatus("Table removed");
                    break;
            }
        }
    }
}
