using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.UI
{
    public class ActionWheelBindings : MonoBehaviour
    {
        public Button        fabButton;
        public TMP_Text      fabLabel;
        public CanvasGroup   pillsCanvasGroup;
        public RectTransform pillsRoot;
        public RectTransform pillRow0;
        public RectTransform pillRow1;
        public GameObject    pageIndicatorRow;
        public List<Image>   pageDots = new();
        public Button        backdropButton;
    }
}
