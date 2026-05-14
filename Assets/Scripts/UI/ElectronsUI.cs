using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ElectronsUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text elementNameText;
    public TMP_Text configText;
    public Transform orbitalContainer;
    public Button btnBack;

    [Header("Orbital Box Prefab")]
    public GameObject orbitalBoxPrefab; // We'll create this next

    void Start()
    {
        btnBack.onClick.AddListener(() => ARModeManager.Instance.ShowMenu());
    }

    public void ShowElectrons(string elementName, string electronConfig)
    {
        if (elementNameText != null) elementNameText.text = elementName;
        if (configText != null) configText.text = FormatConfig(electronConfig);
        GenerateOrbitalDiagram(electronConfig);
        gameObject.SetActive(true);
    }

    // Formats "1s2 2s2 2p6" into "1s² 2s² 2p⁶"
    // Only superscripts the trailing electron count, not the shell number prefix.
    string FormatConfig(string config)
    {
        if (string.IsNullOrEmpty(config)) return config;

        // Each token is like "1s2", "2p6", "3d10" etc.
        // Split by space, reformat each token, then rejoin.
        var tokens = config.Split(' ');
        var sb = new System.Text.StringBuilder();
        foreach (var token in tokens)
        {
            if (string.IsNullOrEmpty(token)) continue;
            if (sb.Length > 0) sb.Append(' ');

            // Find where the trailing digit run starts (the electron count).
            // e.g. "2p6" → prefix "2p", suffix "6"
            //       "3d10" → prefix "3d", suffix "10"
            int splitAt = token.Length;
            while (splitAt > 1 && char.IsDigit(token[splitAt - 1])) splitAt--;

            string prefix = token.Substring(0, splitAt);       // "2p"
            string count  = token.Substring(splitAt);           // "6"
            sb.Append(prefix);
            if (count.Length > 0) sb.Append("<sup>").Append(count).Append("</sup>");
        }
        return sb.ToString();
    }

    void GenerateOrbitalDiagram(string config)
    {
        // Clear previous
        foreach (Transform child in orbitalContainer)
            Destroy(child.gameObject);

        if (orbitalBoxPrefab == null)
        {
            Debug.LogWarning("OrbitalBoxPrefab not set!");
            return;
        }

        // Parse config string like "1s2 2s2 2p6 3s2 3p6"
        string[] orbitals = config.Split(' ');

        foreach (string orbital in orbitals)
        {
            if (string.IsNullOrEmpty(orbital)) continue;

            // Parse orbital name and electron count
            // e.g. "2p6" -> name="2p", count=6
            string name = "";
            int count = 0;

            int i = 0;
            while (i < orbital.Length && !char.IsDigit(orbital[i]) || i == 0)
            {
                if (char.IsDigit(orbital[i]) && i > 0) break;
                name += orbital[i];
                i++;
            }

            // Get the last character(s) as electron count
            string countStr = "";
            while (i < orbital.Length)
            {
                countStr += orbital[i];
                i++;
            }

            if (!int.TryParse(countStr, out count)) continue;

            // Determine max electrons for this orbital type
            int maxElectrons = GetMaxElectrons(name);

            // Create orbital box. The template is inactive so the clone starts
            // inactive too — explicitly activate it so it renders.
            GameObject box = Instantiate(orbitalBoxPrefab, orbitalContainer);
            box.SetActive(true);
            OrbitalBox orbitalBox = box.GetComponent<OrbitalBox>();
            if (orbitalBox != null)
                orbitalBox.Setup(name, count, maxElectrons);
        }
    }

    int GetMaxElectrons(string orbitalName)
    {
        if (orbitalName.Contains("s")) return 2;
        if (orbitalName.Contains("p")) return 6;
        if (orbitalName.Contains("d")) return 10;
        if (orbitalName.Contains("f")) return 14;
        return 2;
    }
}