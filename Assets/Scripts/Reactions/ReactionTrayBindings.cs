using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Reactions
{
    public class ReactionTrayBindings : MonoBehaviour
    {
        [HideInInspector] public TMP_Text slotALabel;
        [HideInInspector] public TMP_Text slotBLabel;
        [HideInInspector] public Button   slotAButton;
        [HideInInspector] public Button   slotBButton;
        [HideInInspector] public Button   igniteButton;
        [HideInInspector] public Button   closeButton;
        [HideInInspector] public TMP_Text resultText;
        [HideInInspector] public Button   skipButton;
        [HideInInspector] public GameObject panelRoot;
    }
}
