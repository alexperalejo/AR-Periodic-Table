using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Quiz
{
    /// <summary>
    /// Contextual quiz system. Generates questions based on current AppStateProvider state
    /// and sends them to the Qwen LLM for evaluation, or uses local pre-made questions.
    /// </summary>
    public class QuizController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<QuizController>() != null) return;
            var go = new GameObject("[QuizController]");
            DontDestroyOnLoad(go);
            go.AddComponent<QuizController>();
        }

        public static QuizController Instance { get; private set; }

        private GameObject      _panel;
        private TextMeshProUGUI _questionText;
        private TextMeshProUGUI _feedbackText;
        private TextMeshProUGUI _scoreText;
        private Button[]        _choiceBtns;
        private Button          _nextBtn;

        private QuizQuestion    _currentQuestion;
        private bool            _answered;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            UI.AppShellController.Instance?.RegisterWheelItem(new UI.WheelItem
            {
                id       = "quiz",
                label    = "Quiz",
                iconName = "?",
                page     = 2,
                onTap    = TogglePanel,
            });
        }

        public void TogglePanel()
        {
            if (_panel == null) BuildPanel();
            bool open = !_panel.activeSelf;
            _panel.SetActive(open);
            if (open) PresentContextualQuestion();
        }

        public void PresentContextualQuestion()
        {
            var q = QuizPromptBuilder.BuildQuestion();
            PresentQuestion(q);
        }

        public void PresentQuestion(QuizQuestion q)
        {
            _currentQuestion = q;
            _answered = false;

            if (_panel == null) BuildPanel();
            _panel.SetActive(true);

            if (_questionText) _questionText.text = q.question;
            if (_feedbackText) _feedbackText.text = "";

            for (int i = 0; i < _choiceBtns.Length; i++)
            {
                if (i < q.choices.Count)
                {
                    _choiceBtns[i].gameObject.SetActive(true);
                    int captured = i;
                    var lbl = _choiceBtns[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (lbl) lbl.text = q.choices[i];
                    _choiceBtns[i].onClick.RemoveAllListeners();
                    _choiceBtns[i].onClick.AddListener(() => OnChoiceSelected(captured));
                    SetButtonColor(_choiceBtns[i], UI.Theme.SurfaceGlass);
                }
                else
                {
                    _choiceBtns[i].gameObject.SetActive(false);
                }
            }

            if (_nextBtn) _nextBtn.gameObject.SetActive(false);
            UpdateScore();
        }

        private void OnChoiceSelected(int idx)
        {
            if (_answered) return;
            _answered = true;

            bool correct = _currentQuestion != null && idx == _currentQuestion.correctIndex;

            if (correct)
            {
                QuizScoreTracker.RecordCorrect();
                SetButtonColor(_choiceBtns[idx], new Color(0.1f, 0.6f, 0.2f));
                if (_feedbackText)
                    _feedbackText.text = $"✓ Correct! {_currentQuestion?.explanation}";
                UI.AppShellController.Instance?.ShowStatus("Correct! +1", 2f);
            }
            else
            {
                QuizScoreTracker.RecordWrong();
                SetButtonColor(_choiceBtns[idx], new Color(0.6f, 0.1f, 0.1f));
                if (_currentQuestion != null && _currentQuestion.correctIndex < _choiceBtns.Length)
                    SetButtonColor(_choiceBtns[_currentQuestion.correctIndex], new Color(0.1f, 0.5f, 0.1f));
                if (_feedbackText)
                    _feedbackText.text = $"✗ The answer is: {_currentQuestion?.choices?[_currentQuestion?.correctIndex ?? 0]}. {_currentQuestion?.explanation}";
            }

            if (_nextBtn) _nextBtn.gameObject.SetActive(true);
            UpdateScore();
        }

        private void UpdateScore()
        {
            if (_scoreText)
                _scoreText.text = $"Score: {QuizScoreTracker.Correct}/{QuizScoreTracker.Attempted}  Streak: {QuizScoreTracker.Streak}";
        }

        private static void SetButtonColor(Button btn, Color c)
        {
            var img = btn.GetComponent<Image>();
            if (img) img.color = c;
        }

        private void BuildPanel()
        {
            var cv = FindFirstObjectByType<Canvas>();
            if (cv == null) return;

            float bot = UI.SafeAreaResolver.BottomInset();
            _panel = new GameObject("QuizPanel", typeof(RectTransform));
            _panel.transform.SetParent(cv.transform, false);
            var rt = _panel.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, bot + UI.Theme.FabSize + 70f);
            rt.sizeDelta = new Vector2(340f, 520f);

            _panel.AddComponent<Image>().color = UI.Theme.SurfaceElevated;
            var vlg = _panel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(10, 10, 10, 10); vlg.spacing = 6f;
            _panel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Header
            var hRow = NewRow(_panel.transform, 36f);
            AddLabel(hRow.transform, "Quick Quiz", UI.Theme.TypeSizeM, UI.Theme.OnSurface, bold: true);
            var closeBtn = AddButton(hRow.transform, "✕", UI.Theme.SurfaceGlass, UI.Theme.OnSurface, 36f);
            closeBtn.onClick.AddListener(TogglePanel);

            _scoreText    = AddLabel(_panel.transform, "Score: 0/0  Streak: 0", UI.Theme.TypeSizeS, UI.Theme.OnSurfaceDim, height: 24f);
            _questionText = AddLabel(_panel.transform, "", UI.Theme.TypeSizeS + 1f, UI.Theme.OnSurface, height: 80f, wrap: true);

            _choiceBtns = new Button[4];
            for (int i = 0; i < 4; i++)
                _choiceBtns[i] = AddButton(_panel.transform, "", UI.Theme.SurfaceGlass, UI.Theme.OnSurface, height: 50f);

            _feedbackText = AddLabel(_panel.transform, "", UI.Theme.TypeSizeS, UI.Theme.OnSurface, height: 70f, wrap: true);

            _nextBtn = AddButton(_panel.transform, "Next Question", UI.Theme.Accent, UI.Theme.OnSurface, height: 44f);
            _nextBtn.onClick.AddListener(PresentContextualQuestion);
            _nextBtn.gameObject.SetActive(false);

            _panel.SetActive(false);
        }

        // ---- helpers ---------------------------------------------------------------

        private static GameObject NewRow(Transform parent, float h)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = false; hlg.spacing = 6f;
            go.AddComponent<LayoutElement>().preferredHeight = h;
            return go;
        }

        private static TextMeshProUGUI AddLabel(Transform parent, string text, float size, Color color,
            bool bold = false, float height = 28f, bool wrap = false)
        {
            var go = new GameObject("L", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            tmp.alignment = wrap ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Center;
            if (bold) tmp.fontStyle = FontStyles.Bold;
            if (wrap) tmp.enableWordWrapping = true;
            return tmp;
        }

        private static Button AddButton(Transform parent, string label, Color bg, Color labelColor,
            float width = -1f, float height = 44f)
        {
            var go = new GameObject(string.IsNullOrEmpty(label) ? "Btn" : label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            if (width > 0) le.preferredWidth = width; else le.flexibleWidth = 1f;
            le.preferredHeight = height;
            go.AddComponent<Image>().color = bg;
            var btn = go.AddComponent<Button>();
            var lGo = new GameObject("L", typeof(RectTransform));
            lGo.transform.SetParent(go.transform, false);
            var lrt = lGo.GetComponent<RectTransform>(); lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = new Vector2(4, 2); lrt.offsetMax = new Vector2(-4, -2);
            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = UI.Theme.TypeSizeS; tmp.alignment = TextAlignmentOptions.Center; tmp.color = labelColor;
            tmp.enableWordWrapping = true;
            return btn;
        }
    }

    public class QuizQuestion
    {
        public string       question;
        public List<string> choices      = new();
        public int          correctIndex;
        public string       explanation;
    }
}
