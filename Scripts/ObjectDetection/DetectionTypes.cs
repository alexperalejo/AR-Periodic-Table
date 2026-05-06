// Assets/Scripts/ObjectDetection/DetectionTypes.cs
using System;
using System.Collections.Generic;

namespace PeriodicAR.ObjectDetection
{
    /// <summary>
    /// One detection returned by the Coral server. bbox is normalized 0..1 in
    /// image space (origin top-left).
    /// </summary>
    [Serializable]
    public class Detection
    {
        public string label;
        public float  score;
        public float[] bbox; // [xmin, ymin, xmax, ymax] normalized 0..1

        public float CenterX => (bbox != null && bbox.Length == 4) ? 0.5f * (bbox[0] + bbox[2]) : 0.5f;
        public float CenterY => (bbox != null && bbox.Length == 4) ? 0.5f * (bbox[1] + bbox[3]) : 0.5f;
    }

    /// <summary>
    /// Top-level response from POST /detect.
    /// </summary>
    [Serializable]
    public class DetectionResponse
    {
        public int width;
        public int height;
        public Detection[] detections;
        public float inference_ms;
    }

    /// <summary>
    /// Composition info for a single COCO label, parsed from
    /// Resources/ObjectElements.json.
    /// </summary>
    [Serializable]
    public class ObjectComposition
    {
        public string[] primary;
        public string[] trace;
        public string   note;
    }

    /// <summary>
    /// Lookup table loaded once at startup. Wraps the JSON's nested 'objects'
    /// dictionary in something Unity's JsonUtility can deserialize via the
    /// helper in ObjectElementsCatalog.
    /// </summary>
    public class ObjectElementsCatalog
    {
        private readonly Dictionary<string, ObjectComposition> _byLabel;

        public ObjectElementsCatalog(Dictionary<string, ObjectComposition> byLabel)
        {
            _byLabel = byLabel ?? new Dictionary<string, ObjectComposition>();
        }

        public bool TryGet(string label, out ObjectComposition composition)
        {
            return _byLabel.TryGetValue(label, out composition);
        }

        public int Count => _byLabel.Count;
    }
}
