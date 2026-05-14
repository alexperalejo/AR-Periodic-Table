using System.Collections;
using UnityEngine;

namespace PeriodicAR.UI
{
    [RequireComponent(typeof(StatusPillBindings))]
    public class StatusPill : MonoBehaviour
    {
        private StatusPillBindings _b;
        private Coroutine _hideRoutine;
        private Coroutine _fadeRoutine;

        private void Awake()
        {
            _b = GetComponent<StatusPillBindings>();
            if (_b.canvasGroup != null)
            {
                _b.canvasGroup.alpha = 0f;
                _b.canvasGroup.interactable = false;
                _b.canvasGroup.blocksRaycasts = false;
            }
            if (_b.dismissButton != null)
                _b.dismissButton.onClick.AddListener(Clear);
        }

        public void Show(string text, float autoHideSec = 3f)
        {
            if (_b.messageText != null) _b.messageText.text = text;

            if (_hideRoutine != null) StopCoroutine(_hideRoutine);
            if (_fadeRoutine  != null) StopCoroutine(_fadeRoutine);

            _fadeRoutine = StartCoroutine(Fade(1f, Theme.AnimFast));
            if (autoHideSec > 0f)
                _hideRoutine = StartCoroutine(AutoHide(autoHideSec));

            if (_b.canvasGroup != null)
            {
                _b.canvasGroup.interactable   = true;
                _b.canvasGroup.blocksRaycasts = true;
            }
        }

        public void Clear()
        {
            if (_hideRoutine != null) StopCoroutine(_hideRoutine);
            if (_fadeRoutine  != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(Fade(0f, Theme.AnimFast, () =>
            {
                if (_b.canvasGroup != null)
                {
                    _b.canvasGroup.interactable   = false;
                    _b.canvasGroup.blocksRaycasts = false;
                }
            }));
        }

        private IEnumerator AutoHide(float delay)
        {
            yield return new WaitForSeconds(delay);
            Clear();
        }

        private IEnumerator Fade(float target, float duration, System.Action onComplete = null)
        {
            if (_b.canvasGroup == null) yield break;
            float start = _b.canvasGroup.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _b.canvasGroup.alpha = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            _b.canvasGroup.alpha = target;
            onComplete?.Invoke();
        }
    }
}
