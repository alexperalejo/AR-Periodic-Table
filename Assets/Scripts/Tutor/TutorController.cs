using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Tutor
{
    /// <summary>
    /// Brain of the LLM tutor. Self-bootstrapping — creates the panel and HUD button
    /// at runtime. No Inspector wiring needed.
    /// </summary>
    public class TutorController : MonoBehaviour
    {
        // ---- Bootstrap ----------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<TutorController>() != null) return;
            var go = new GameObject("[TutorController]");
            DontDestroyOnLoad(go);

            var ctrl = go.AddComponent<TutorController>();
            ctrl._llmClient   = go.AddComponent<LLMClient>();
            ctrl._tools       = go.AddComponent<TutorToolRegistry>();
        }

        // ---- Config / tuning --------------------------------------------------------
        private const int MaxToolRoundtrips = 3;
        private const string ModelId        = "qwen3.6-27b";
        private const string BaseUrl        = "https://ai.gonzalezerik.com";

        // ---- Runtime refs -----------------------------------------------------------
        private LLMClient        _llmClient;
        private TutorToolRegistry _tools;
        private TutorPanel       _panel;
        private Button           _tutorButton;

        private readonly List<ChatMessage> _history = new();
        private bool _busy;

        // ---- Lifecycle --------------------------------------------------------------

        private void Start()
        {
            if (_llmClient != null)
            {
                _llmClient.config.baseUrl  = BaseUrl;
                _llmClient.config.model    = ModelId;
                _llmClient.config.temperature = 0.6f;
            }

            // Build the tutor panel.
            var panelGo = TutorPrefabFactory.CreatePanel();
            panelGo.transform.SetParent(transform, false);
            _panel = panelGo.GetComponent<TutorPanel>();

            // Wire close button.
            var bindings = panelGo.GetComponent<TutorPanelBindings>();
            if (bindings.closeButton != null)
                bindings.closeButton.onClick.AddListener(() => _panel.Close());

            // Wire send button & input.
            if (bindings.sendButton != null)
                bindings.sendButton.onClick.AddListener(OnSendPressed);
            if (bindings.inputField != null)
                bindings.inputField.onSubmit.AddListener(_ => OnSendPressed());

            // If the AppShell is running, it owns the Tutor button; otherwise create our own.
            if (UI.AppShellController.Instance == null)
                CreateHudButton();
        }

        // ---- Public API for AppShellController ---------------------------------

        public void TogglePanel()
        {
            if (_panel == null) return;
            if (_panel.IsOpen) _panel.Close();
            else               _panel.Open();
        }

        public void NotifyUnread()
        {
            UI.AppShellController.Instance?.SetTutorBadge(true);
        }

        private void CreateHudButton()
        {
            // Find the HUD canvas by looking for a Canvas with the ARHudController.
            var hudController = FindAnyObjectByType<UI.ARHudController>();
            Canvas hudCanvas = hudController != null ? hudController.GetComponentInParent<Canvas>() : null;
            if (hudCanvas == null) hudCanvas = FindFirstObjectByType<Canvas>();
            if (hudCanvas == null) return;

            _tutorButton = CreateSquareButton(hudCanvas.transform, "TutorButton", "Tutor",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-440f, -40f), new Vector2(180f, 180f));

            _tutorButton.onClick.AddListener(OnTutorButtonPressed);
        }

        private void OnTutorButtonPressed()
        {
            if (_panel == null) return;
            if (_panel.IsOpen) _panel.Close();
            else
            {
                _panel.Open();
                UI.AppShellController.Instance?.SetTutorBadge(false);
            }
        }

        private void OnSendPressed()
        {
            if (_busy) return;
            var bindings = GetComponentInChildren<TutorPanelBindings>(true);
            if (bindings == null || bindings.inputField == null) return;

            string text = bindings.inputField.text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            bindings.inputField.text = string.Empty;
            _panel.AppendUserMessage(text);
            _history.Add(new ChatMessage { role = "user", content = text });

            StartCoroutine(RunTurn());
        }

        // ---- LLM turn coroutine -------------------------------------------------

        private IEnumerator RunTurn()
        {
            _busy = true;
            _panel.SetThinking(true);

            var messages = BuildMessages();
            var toolDefs = _tools != null ? _tools.GetContextualTools() : new List<ToolDef>();
            int roundtrips = 0;

            ChatResponse resp = null;
            string error = null;

            while (roundtrips <= MaxToolRoundtrips)
            {
                resp  = null;
                error = null;

                yield return _llmClient.Complete(messages, toolDefs,
                    r => resp  = r,
                    e => error = e);

                if (error != null)
                {
                    _panel.SetThinking(false);
                    _panel.AppendAssistantMessage($"*Error: {error}*");
                    _busy = false;
                    yield break;
                }

                var choice = resp.choices[0];

                if (choice.finish_reason == "tool_calls" && choice.message.tool_calls != null)
                {
                    // Append the assistant tool-call message to history.
                    messages.Add(choice.message);
                    _history.Add(choice.message);

                    // Execute each tool and append results.
                    foreach (var tc in choice.message.tool_calls)
                    {
                        string result = _tools != null
                            ? _tools.Dispatch(tc.function.name, tc.function.arguments)
                            : "{\"status\":\"error\",\"reason\":\"TutorToolRegistry not available\"}";

                        // Show a status line so the user can see what the tutor did.
                        _panel.AppendToolStatus($"{tc.function.name.Replace('_', ' ')}");

                        var toolMsg = new ChatMessage
                        {
                            role         = "tool",
                            tool_call_id = tc.id,
                            content      = result,
                        };
                        messages.Add(toolMsg);
                        _history.Add(toolMsg);
                    }

                    roundtrips++;
                    if (roundtrips > MaxToolRoundtrips)
                    {
                        var bailMsg = new ChatMessage { role = "assistant", content = "(stopped — too many tool calls in one turn)" };
                        _panel.SetThinking(false);
                        _panel.AppendAssistantMessage(bailMsg.content);
                        _history.Add(bailMsg);
                        _busy = false;
                        yield break;
                    }

                    // Feed results back for another completion.
                    continue;
                }

                // Normal text response.
                string reply = choice.message?.content ?? string.Empty;
                _history.Add(new ChatMessage { role = "assistant", content = reply });

                _panel.SetThinking(false);
                _panel.AppendAssistantMessage(reply);
                _busy = false;
                yield break;
            }
        }

        // ---- System prompt ------------------------------------------------------

        private List<ChatMessage> BuildMessages()
        {
            string state = AppStateProvider.Instance != null
                ? AppStateProvider.Instance.DescribeForPrompt()
                : "(state unavailable)";

            var system = new ChatMessage
            {
                role    = "system",
                content =
$@"You are a helpful chemistry tutor inside an AR periodic table app.
The student can grab element cubes in 3D, scan real-world objects with their phone camera to see what elements they're made of, and ask you questions.

Keep answers concise — usually 2-3 sentences. Use plain prose, not bullet points, unless the student asks for a list. Match the student's apparent level: simpler if they sound new, more technical if they don't.

You have tools available to highlight elements on the table, focus a specific element, clear highlights, show a Bohr model, compare two elements, or filter the table by category. Use them whenever a visual response would help — don't just describe what you'd show, show it.

Current app state:
{state}"
            };

            var all = new List<ChatMessage> { system };
            all.AddRange(_history);
            return all;
        }

        // ---- HUD button helper --------------------------------------------------

        private static Button CreateSquareButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            var go  = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.08f, 0.12f, 0.20f, 0.85f);

            var btn = go.AddComponent<Button>();

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 44f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;

            return btn;
        }

        // ---- Voice input hook (not implemented — plug in Whisper here) -----------
        // public void OnVoiceInputAvailable(string transcribedText) { }
    }
}
