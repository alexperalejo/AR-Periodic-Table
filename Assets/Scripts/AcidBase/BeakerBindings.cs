using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.AcidBase
{
    public class BeakerBindings : MonoBehaviour
    {
        public Image            liquidImage;
        public TextMeshProUGUI  phDisplay;
        public TextMeshProUGUI  substanceLabel;
        public TextMeshProUGUI  descriptionText;
        public Slider           phSlider;
        public Dropdown         substanceDropdown;
        public Dropdown         indicatorDropdown;
        public Button           neutralizeButton;
        public Button           clearButton;
        public Image            phBar;          // horizontal color gradient bar
    }
}
