using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public class ElementDatabase : MonoBehaviour
{
    public static ElementDatabase Instance { get; private set; }

    public Dictionary<int, ElementData> ByNumber { get; private set; } = new();
    public Dictionary<string, ElementData> BySymbol { get; private set; } = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    void Load()
    {
        // StreamingAssets path works on Android, but needs UnityWebRequest there.
        // We'll handle both safely:
        StartCoroutine(LoadFromStreamingAssets());
    }

    System.Collections.IEnumerator LoadFromStreamingAssets()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "PeriodicTableJSON.json");

#if UNITY_ANDROID && !UNITY_EDITOR
        using var www = UnityEngine.Networking.UnityWebRequest.Get(path);
        yield return www.SendWebRequest();
        if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to load periodic table JSON: " + www.error);
            yield break;
        }
        string json = www.downloadHandler.text;
#else
        if (!File.Exists(path))
        {
            Debug.LogError("PeriodicTableJSON.json not found at: " + path);
            yield break;
        }
        string json = File.ReadAllText(path);
#endif

        var root = JsonConvert.DeserializeObject<PeriodicTableRoot>(json);
        if (root?.elements == null)
        {
            Debug.LogError("JSON parsed but elements was null.");
            yield break;
        }

        ByNumber.Clear();
        BySymbol.Clear();

        foreach (var e in root.elements)
        {
            ByNumber[e.number] = e;
            if (!string.IsNullOrEmpty(e.symbol))
                BySymbol[e.symbol] = e;
        }

        Debug.Log($"Loaded {root.elements.Count} elements.");
    }
}