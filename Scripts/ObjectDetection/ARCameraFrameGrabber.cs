// Assets/Scripts/ObjectDetection/ARCameraFrameGrabber.cs
using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace PeriodicAR.ObjectDetection
{
    /// <summary>
    /// Captures a single frame from the AR camera as a JPEG.
    ///
    /// We deliberately go through XRCpuImage rather than ScreenCapture so the
    /// detection model sees what the camera actually saw — without the AR
    /// overlay, the HUD, the placed periodic table, or any other rendered
    /// content blocking the real-world objects.
    ///
    /// Output is rotated to match the device orientation and downscaled to
    /// roughly 640px on the long edge before JPEG-encoding, which is plenty
    /// for a 320×320 detection model and keeps uploads small.
    /// </summary>
    public static class ARCameraFrameGrabber
    {
        // EfficientDet-Lite0 wants 320×320. Going to ~640 gives the resize step
        // some headroom and keeps small objects detectable, without wasting
        // bandwidth on a full 4K phone-camera frame.
        private const int TargetLongEdge = 640;
        private const int JpegQuality    = 80;

        public struct GrabResult
        {
            public bool   success;
            public byte[] jpegBytes;
            public int    width;
            public int    height;
            public string error;
        }

        /// <summary>
        /// Synchronously grab the latest CPU image. Must be called from the
        /// main thread (as part of an Update / coroutine / button callback).
        /// </summary>
        public static GrabResult TryGrabJpeg(ARCameraManager cameraManager)
        {
            if (cameraManager == null)
                return Fail("ARCameraManager is null");

            if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage cpuImage))
                return Fail("No CPU image available — camera not ready or AR session not running");

            try
            {
                // Convert to RGBA8 in a managed buffer.
                var convParams = new XRCpuImage.ConversionParams
                {
                    inputRect        = new RectInt(0, 0, cpuImage.width, cpuImage.height),
                    outputDimensions = new Vector2Int(cpuImage.width, cpuImage.height),
                    outputFormat     = TextureFormat.RGBA32,
                    transformation   = XRCpuImage.Transformation.None,
                };

                int size = cpuImage.GetConvertedDataSize(convParams);
                using var buffer = new NativeArray<byte>(size, Allocator.Temp);
                // XRCpuImage.Convert has a safe NativeArray<byte> overload, which
                // means we don't need to enable "Allow 'unsafe' Code" on the
                // project. The Convert call blocks until the conversion is done.
                cpuImage.Convert(convParams, buffer);

                // Wrap the converted bytes in a Texture2D so we can resize/rotate
                // and call EncodeToJPG. Using Apply(false, false) keeps the texture
                // CPU-readable.
                var raw = new Texture2D(cpuImage.width, cpuImage.height,
                                        TextureFormat.RGBA32, false);
                raw.LoadRawTextureData(buffer);
                raw.Apply(false, false);

                // Most AR camera images come out landscape-left of the device's
                // current orientation. Rotate/flip to match what the user sees on
                // screen so the bbox math we do later is consistent.
                Texture2D oriented = ApplyDisplayOrientation(raw);
                if (oriented != raw) UnityEngine.Object.Destroy(raw);

                Texture2D scaled = ResizeToLongEdge(oriented, TargetLongEdge);
                if (scaled != oriented) UnityEngine.Object.Destroy(oriented);

                byte[] jpeg = scaled.EncodeToJPG(JpegQuality);
                int outW = scaled.width;
                int outH = scaled.height;

                UnityEngine.Object.Destroy(scaled);

                return new GrabResult
                {
                    success   = true,
                    jpegBytes = jpeg,
                    width     = outW,
                    height    = outH,
                };
            }
            catch (Exception e)
            {
                return Fail($"Convert failed: {e.Message}");
            }
            finally
            {
                cpuImage.Dispose();
            }
        }

        // ---- helpers -----------------------------------------------------------

        private static Texture2D ApplyDisplayOrientation(Texture2D src)
        {
            // Map the device orientation to a clockwise rotation that aligns the
            // CPU-image axes (which come out as if the device were in
            // LandscapeLeft) with the user's current view.
            ScreenOrientation o = Screen.orientation;
            int rotateCwQuarterTurns = o switch
            {
                ScreenOrientation.Portrait           => 1, // 90 cw
                ScreenOrientation.PortraitUpsideDown => 3, // 270 cw
                ScreenOrientation.LandscapeLeft      => 0,
                ScreenOrientation.LandscapeRight     => 2, // 180
                _ => 1,
            };
            if (rotateCwQuarterTurns == 0) return src;
            return RotateClockwise(src, rotateCwQuarterTurns);
        }

        private static Texture2D RotateClockwise(Texture2D src, int quarterTurns)
        {
            quarterTurns = ((quarterTurns % 4) + 4) % 4;
            if (quarterTurns == 0) return src;

            Color32[] srcPix = src.GetPixels32();
            int sw = src.width, sh = src.height;

            int dw, dh;
            if (quarterTurns % 2 == 1) { dw = sh; dh = sw; }
            else                       { dw = sw; dh = sh; }

            var dst = new Color32[dw * dh];

            for (int y = 0; y < sh; y++)
            {
                for (int x = 0; x < sw; x++)
                {
                    int dx, dy;
                    switch (quarterTurns)
                    {
                        case 1: dx = sh - 1 - y; dy = x;             break;
                        case 2: dx = sw - 1 - x; dy = sh - 1 - y;    break;
                        default: dx = y;          dy = sw - 1 - x;   break; // 3
                    }
                    dst[dy * dw + dx] = srcPix[y * sw + x];
                }
            }

            var rotated = new Texture2D(dw, dh, TextureFormat.RGBA32, false);
            rotated.SetPixels32(dst);
            rotated.Apply(false, false);
            return rotated;
        }

        private static Texture2D ResizeToLongEdge(Texture2D src, int longEdge)
        {
            int sw = src.width, sh = src.height;
            int longest = Mathf.Max(sw, sh);
            if (longest <= longEdge) return src;

            float scale = (float)longEdge / longest;
            int dw = Mathf.Max(1, Mathf.RoundToInt(sw * scale));
            int dh = Mathf.Max(1, Mathf.RoundToInt(sh * scale));

            // Use a temporary RT so we get GPU-side bilinear filtering.
            var rt = RenderTexture.GetTemporary(dw, dh, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;
            try
            {
                Graphics.Blit(src, rt);
                RenderTexture.active = rt;
                var dst = new Texture2D(dw, dh, TextureFormat.RGBA32, false);
                dst.ReadPixels(new Rect(0, 0, dw, dh), 0, 0, false);
                dst.Apply(false, false);
                return dst;
            }
            finally
            {
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        private static GrabResult Fail(string err)
        {
            Debug.LogWarning($"[ARCameraFrameGrabber] {err}");
            return new GrabResult { success = false, error = err };
        }
    }
}
