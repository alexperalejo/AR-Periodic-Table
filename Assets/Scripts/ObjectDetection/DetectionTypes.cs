using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PeriodicAR.ObjectDetection
{
    [Serializable]
    public class Detection
    {
        public string label;
        public float  score;
        public float[] bbox; // [xmin, ymin, xmax, ymax], normalized 0..1, origin top-left
    }

    [Serializable]
    public class DetectionResponse
    {
        public int   width;
        public int   height;
        public Detection[] detections;
        public float inference_ms;
    }

    [Serializable]
    public class ObjectComposition
    {
        public string[] primary;
        public string[] trace;
        public string   note;
    }

    public class ObjectElementsCatalog
    {
        [JsonProperty("objects")]
        public Dictionary<string, ObjectComposition> Objects { get; set; }
    }
}
