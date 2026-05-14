using System;
using System.Collections;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace PeriodicAR.ObjectDetection
{
    [Serializable]
    public class DetectionClientConfig
    {
        public string serverUrl            = "https://tpu.gonzalezerik.com/detect";
        public string cfAccessClientId     = "";
        public string cfAccessClientSecret = "";
        public float  threshold            = 0.4f;
        public int    maxResults           = 5;
    }

    public class DetectionServerClient : MonoBehaviour
    {
        public DetectionClientConfig config = new DetectionClientConfig();

        public IEnumerator PostFrame(
            byte[]                      jpegBytes,
            Action<DetectionResponse>   onDone,
            Action<string>              onError)
        {
            string url = $"{config.serverUrl}?max={config.maxResults}&threshold={config.threshold:F2}";

            var form = new WWWForm();
            form.AddBinaryData("image", jpegBytes, "frame.jpg", "image/jpeg");

            using var req = UnityWebRequest.Post(url, form);
            req.timeout = 10;

            if (!string.IsNullOrEmpty(config.cfAccessClientId))
                req.SetRequestHeader("CF-Access-Client-Id", config.cfAccessClientId);
            if (!string.IsNullOrEmpty(config.cfAccessClientSecret))
                req.SetRequestHeader("CF-Access-Client-Secret", config.cfAccessClientSecret);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"HTTP {req.responseCode}: {req.error}");
                yield break;
            }

            string raw = req.downloadHandler.text;
            try
            {
                var resp = JsonConvert.DeserializeObject<DetectionResponse>(raw);
                if (resp == null)
                    onError?.Invoke("Server returned an empty or null response.");
                else
                    onDone?.Invoke(resp);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DetectionServerClient] Raw response: {raw}");
                onError?.Invoke($"JSON parse error: {ex.Message}");
            }
        }
    }
}
