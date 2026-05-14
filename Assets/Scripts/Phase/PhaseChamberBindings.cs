using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Phase
{
    public class PhaseChamberBindings : MonoBehaviour
    {
        public RectTransform    simContainer;
        public TextMeshProUGUI  phaseLabel;
        public TextMeshProUGUI  tempLabel;
        public TextMeshProUGUI  pressureLabel;
        public Slider           tempSlider;
        public Slider           pressureSlider;
        public Dropdown         substanceDropdown;
        public TextMeshProUGUI  annotationText;
        public Button           closeButton;
    }
}
