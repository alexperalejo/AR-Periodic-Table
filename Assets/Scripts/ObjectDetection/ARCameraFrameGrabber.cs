using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace PeriodicAR.ObjectDetection
{
    public class ARCameraFrameGrabber : MonoBehaviour
    {
        [SerializeField] private ARCameraManager _cameraManager;

        private void Awake()
        {
            if (_cameraManager == null)
                _cameraManager = FindFirstObjectByType<ARCameraManager>();
        }

        /// <summary>
        /// Captures the latest AR camera frame as a JPEG byte array.
        /// Call onDone with the bytes, or onError with a human-readable message.
        /// </summary>
        public IEnumerator CaptureJpeg(Action<byte[]> onDone, Action<string> onError)
        {
            if (_cameraManager == null)
            {
                _cameraManager = FindFirstObjectByType<ARCameraManager>();
                if (_cameraManager == null)
                {
                    onError?.Invoke("ARCameraManager not found in scene");
                    yield break;
                }
            }

            if (!_cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                onError?.Invoke("No CPU image available — AR session may not have started yet");
                yield break;
            }

            var convParams = new XRCpuImage.ConversionParams
            {
                inputRect        = new RectInt(0, 0, image.width, image.height),
                outputDimensions = new Vector2Int(image.width, image.height),
                outputFormat     = TextureFormat.RGB24,
                transformation   = XRCpuImage.Transformation.MirrorY,
            };

            var asyncConv = image.ConvertAsync(convParams);
            image.Dispose(); // dispose immediately — conversion keeps internal reference

            while (!asyncConv.status.IsDone())
                yield return null;

            if (asyncConv.status != XRCpuImage.AsyncConversionStatus.Ready)
            {
                asyncConv.Dispose();
                onError?.Invoke($"Image conversion failed: {asyncConv.status}");
                yield break;
            }

            var tex = new Texture2D(convParams.outputDimensions.x, convParams.outputDimensions.y,
                                    TextureFormat.RGB24, false);
            tex.LoadRawTextureData(asyncConv.GetData<byte>());
            tex.Apply();
            asyncConv.Dispose();

            byte[] jpeg = tex.EncodeToJPG(85);
            Destroy(tex);

            onDone?.Invoke(jpeg);
        }
    }
}
