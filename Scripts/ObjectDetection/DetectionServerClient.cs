// Assets/Scripts/ObjectDetection/DetectionServerClient.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace PeriodicAR.ObjectDetection
{
    /// <summary>
    /// POSTs a JPEG to the Coral detection server and yields a parsed
    /// DetectionResponse. Designed to be called from a coroutine.
    /// </summary>
    public class DetectionServerClient
    {
        public string ServerUrl { get; set; } = "https://coral-detect.example.com/detect";
        public int    MaxResults { get; set; } = 5;
        public float  ScoreThreshold { get; set; } = 0.4f;
        public int    TimeoutSeconds { get; set; } = 10;

        // Optional Cloudflare Access service-token headers. Leave both empty
        // to skip — no auth, only your tunnel's public hostname rules apply.
        public string CfAccessClientId { get; set; }
        public string CfAccessClientSecret { get; set; }

        public struct Result
        {
            public bool   success;
            public string error;
            public DetectionResponse response;
        }

        public IEnumerator Send(byte[] jpegBytes, Action<Result> onComplete)
        {
            if (jpegBytes == null || jpegBytes.Length == 0)
            {
                onComplete?.Invoke(new Result { success = false, error = "Empty JPEG payload" });
                yield break;
            }

            string url = $"{ServerUrl}?max={MaxResults}&threshold={ScoreThreshold:F2}";

            var form = new WWWForm();
            form.AddBinaryData("image", jpegBytes, "frame.jpg", "image/jpeg");

            using var req = UnityWebRequest.Post(url, form);
            req.timeout = TimeoutSeconds;
            if (!string.IsNullOrEmpty(CfAccessClientId))
                req.SetRequestHeader("CF-Access-Client-Id", CfAccessClientId);
            if (!string.IsNullOrEmpty(CfAccessClientSecret))
                req.SetRequestHeader("CF-Access-Client-Secret", CfAccessClientSecret);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(new Result
                {
                    success = false,
                    error   = $"HTTP {req.responseCode}: {req.error}",
                });
                yield break;
            }

            DetectionResponse parsed = null;
            try
            {
                parsed = JsonUtility.FromJson<DetectionResponse>(req.downloadHandler.text);
            }
            catch (Exception e)
            {
                onComplete?.Invoke(new Result { success = false, error = $"Parse: {e.Message}" });
                yield break;
            }

            if (parsed == null)
            {
                onComplete?.Invoke(new Result { success = false, error = "Empty JSON response" });
                yield break;
            }

            onComplete?.Invoke(new Result { success = true, response = parsed });
        }
    }
}
