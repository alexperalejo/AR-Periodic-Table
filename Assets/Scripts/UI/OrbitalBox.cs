using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrbitalBox : MonoBehaviour
{
    public TMP_Text orbitalLabel;  // e.g. "2p"
    public TMP_Text electronsText; // shows arrows like "↑↓ ↑↓ ↑↓"

    public void Setup(string orbitalName, int electronCount, int maxElectrons)
    {
        if (orbitalLabel != null)
            orbitalLabel.text = orbitalName;

        if (electronsText != null)
            electronsText.text = BuildArrows(electronCount, maxElectrons);
    }

    string BuildArrows(int count, int max)
    {
        int numBoxes = max / 2; // number of sub-boxes
        string result = "";

        for (int i = 0; i < numBoxes; i++)
        {
            int electronsInBox = 0;
            if (count > i) electronsInBox++;       // first electron (spin up)
            if (count > numBoxes + i) electronsInBox++; // second electron (spin down, Hund's rule)

            if (electronsInBox == 0)
                result += "[  ]";
            else if (electronsInBox == 1)
                result += "[↑ ]";
            else
                result += "[↑↓]";

            if (i < numBoxes - 1) result += " ";
        }

        return result;
    }
}