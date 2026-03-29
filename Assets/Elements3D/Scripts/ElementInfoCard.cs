//Ties tile tap -> Show info card + load element

using TMPro;
using UnityEngine;

public class ElementInfoCard : MonoBehaviour
{
    [Header("Text Fields")]
    public TMP_Text titleText;
    public TMP_Text symbolText;
    public TMP_Text atomicNumberText;
    public TMP_Text atomicMassText;
    public TMP_Text groupText;
    public TMP_Text densityText;
    public TMP_Text meltingPointText;
    public TMP_Text boilingPointText;
    public TMP_Text electronConfigText;

    [Header("Bohr Model")]
    public ElementLoader elementLoader;

    public void ShowCard(
     string elementName,
     string symbol,
     int atomicNumber,
     string atomicMass,
     string group,
     string density,
     string meltingPoint,
     string boilingPoint,
     string electronConfig
 )
    {
        titleText.text = elementName;
        symbolText.text = "Symbol: " + symbol;
        atomicNumberText.text = "Atomic Number: " + atomicNumber;
        atomicMassText.text = "Atomic Mass: " + atomicMass;
        groupText.text = "Group: " + group;
        densityText.text = "Density: " + density;
        meltingPointText.text = "Melting Point: " + meltingPoint;
        boilingPointText.text = "Boiling Point: " + boilingPoint;
        electronConfigText.text = "Electron Config: " + electronConfig;

        gameObject.SetActive(true);
    }

    public void HideCard()
    {
        gameObject.SetActive(false);
    }
}