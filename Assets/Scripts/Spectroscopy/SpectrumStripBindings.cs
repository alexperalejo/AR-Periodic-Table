using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Spectroscopy
{
    public class SpectrumStripBindings : MonoBehaviour
    {
        public RectTransform  stripRoot;
        public RectTransform  linesContainer;
        public Image          background;
        public TMPro.TextMeshProUGUI modeLabel;
        public Button         emissionBtn;
        public Button         absorptionBtn;
        public Button         starsBtn;
        public RectTransform  linePopup;
        public TMPro.TextMeshProUGUI popupText;
        public Button         popupClose;
        public RectTransform  starOverlay;
    }
}
