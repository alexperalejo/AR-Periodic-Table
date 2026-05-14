using System;

namespace PeriodicAR.UI
{
    public class WheelItem
    {
        public string id;
        public string label;
        public string iconName;
        public int page;
        public Action onTap;
        public Func<bool> isAvailable;
    }
}
