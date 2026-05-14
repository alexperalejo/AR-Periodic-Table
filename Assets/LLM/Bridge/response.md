**Bridge error:** Unexpected character encountered while parsing value: c. Path 'type', line 1, position 6.

```
  at Newtonsoft.Json.JsonTextReader.ParseValue () [0x002b3] in <761cf2a144514d2291a678c334d49e9b>:0 
  at Newtonsoft.Json.JsonTextReader.Read () [0x0004c] in <761cf2a144514d2291a678c334d49e9b>:0 
  at Newtonsoft.Json.Linq.JContainer.ReadContentFrom (Newtonsoft.Json.JsonReader r, Newtonsoft.Json.Linq.JsonLoadSettings settings) [0x001f2] in <761cf2a144514d2291a678c334d49e9b>:0 
  at Newtonsoft.Json.Linq.JContainer.ReadTokenFrom (Newtonsoft.Json.JsonReader reader, Newtonsoft.Json.Linq.JsonLoadSettings options) [0x00030] in <761cf2a144514d2291a678c334d49e9b>:0 
  at Newtonsoft.Json.Linq.JObject.Load (Newtonsoft.Json.JsonReader reader, Newtonsoft.Json.Linq.JsonLoadSettings settings) [0x0006a] in <761cf2a144514d2291a678c334d49e9b>:0 
  at Newtonsoft.Json.Linq.JObject.Parse (System.String json, Newtonsoft.Json.Linq.JsonLoadSettings settings) [0x0000c] in <761cf2a144514d2291a678c334d49e9b>:0 
  at Newtonsoft.Json.Linq.JObject.Parse (System.String json) [0x00000] in <761cf2a144514d2291a678c334d49e9b>:0 
  at PeriodicAR.LLMBridge.UnityBridge.Poll () [0x000ba] in C:\Users\erikg\Documents\AR-Periodic-Table-dev\Assets\LLM\Editor\UnityBridge.cs:64 
```