using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace PeriodicAR.Tutor
{
    [Serializable]
    public class LLMClientConfig
    {
        public string baseUrl        = "https://ai.gonzalezerik.com";
        public string model          = "qwen3.6-27b";
        public float  temperature    = 0.6f;
        public int    timeoutSeconds = 60;
    }

    public class LLMClient : MonoBehaviour
    {
        public LLMClientConfig config = new LLMClientConfig();

        public IEnumerator Complete(
            List<ChatMessage>     messages,
            List<ToolDef>         tools,
            Action<ChatResponse>  onDone,
            Action<string>        onError)
        {
            var request = new ChatRequest
            {
                model       = config.model,
                messages    = messages.ToArray(),
                tools       = (tools != null && tools.Count > 0) ? tools.ToArray() : null,
                temperature = config.temperature,
                stream      = false,
            };

            string json = JsonConvert.SerializeObject(request, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
            });

            string url = $"{config.baseUrl}/v1/chat/completions";
            using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout         = config.timeoutSeconds;

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"HTTP {req.responseCode}: {req.error}");
                yield break;
            }

            string raw = req.downloadHandler.text;
            try
            {
                var resp = JsonConvert.DeserializeObject<ChatResponse>(raw);
                if (resp == null || resp.choices == null || resp.choices.Length == 0)
                    onError?.Invoke("LLM returned empty response.");
                else
                    onDone?.Invoke(resp);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMClient] Raw response: {raw}");
                onError?.Invoke($"JSON parse error: {ex.Message}");
            }
        }
    }
}
