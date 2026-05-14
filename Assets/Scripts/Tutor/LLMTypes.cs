using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PeriodicAR.Tutor
{
    [Serializable]
    public class ChatMessage
    {
        public string role;      // "system" | "user" | "assistant" | "tool"
        public string content;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ToolCall[] tool_calls;  // only on assistant messages

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string tool_call_id;    // only on tool messages
    }

    [Serializable]
    public class ToolCall
    {
        public string       id;
        public string       type;      // "function"
        public FunctionCall function;
    }

    [Serializable]
    public class FunctionCall
    {
        public string name;
        public string arguments; // JSON string — parse with JsonConvert.DeserializeObject
    }

    [Serializable]
    public class ToolDef
    {
        public string      type;       // "function"
        public FunctionDef function;
    }

    [Serializable]
    public class FunctionDef
    {
        public string  name;
        public string  description;
        public JObject parameters;    // raw JSON schema object
    }

    [Serializable]
    public class ChatRequest
    {
        public string        model;
        public ChatMessage[] messages;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ToolDef[] tools;

        public float temperature = 0.6f;
        public bool  stream      = false;
    }

    [Serializable]
    public class ChatChoice
    {
        public ChatMessage message;
        public string      finish_reason;
    }

    [Serializable]
    public class ChatResponse
    {
        public ChatChoice[] choices;
    }
}
