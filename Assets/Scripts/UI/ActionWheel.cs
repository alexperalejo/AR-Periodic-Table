using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PeriodicAR.UI
{
    [RequireComponent(typeof(ActionWheelBindings))]
    public class ActionWheel : MonoBehaviour
    {
        private ActionWheelBindings _b;
        private List<WheelItem> _items = new();
        private bool _expanded;
        private int  _currentPage;
        private int  _totalPages;
        private string _pinnedId;

        private Coroutine _expandAnim;

        private const float LongPressDuration = 0.5f;
        private const int   MaxPerPage = 6;
        private const float PillWidth  = 96f;

        private void Awake()
        {
            _b = GetComponent<ActionWheelBindings>();
            if (_b.fabButton != null)
                _b.fabButton.onClick.AddListener(OnFabClicked);
            if (_b.backdropButton != null)
                _b.backdropButton.onClick.AddListener(Collapse);
            SetPillsVisible(false, instant: true);
        }

        // ---- Public API ---------------------------------------------------------

        public void SetItems(List<WheelItem> items)
        {
            _items = items ?? new List<WheelItem>();
            _totalPages = Mathf.Max(1, Mathf.CeilToInt(_items.Count / (float)MaxPerPage));
            _currentPage = 0;
            RebuildPills();
            RebuildPageDots();
        }

        public void SetPinnedId(string id)
        {
            _pinnedId = id;
            RefreshFabLabel();
        }

        public void Expand()
        {
            if (_expanded) return;
            _expanded = true;
            if (_expandAnim != null) StopCoroutine(_expandAnim);
            _expandAnim = StartCoroutine(AnimatePills(1f));
            if (_b.backdropButton != null) _b.backdropButton.gameObject.SetActive(true);
            UpdateFabLabel(true);
        }

        public void Collapse()
        {
            if (!_expanded) return;
            _expanded = false;
            if (_expandAnim != null) StopCoroutine(_expandAnim);
            _expandAnim = StartCoroutine(AnimatePills(0f));
            if (_b.backdropButton != null) _b.backdropButton.gameObject.SetActive(false);
            UpdateFabLabel(false);
        }

        // ---- FAB ----------------------------------------------------------------

        private void OnFabClicked()
        {
            if (_expanded)
            {
                // If a shortcut is pinned, fire it and collapse.
                var pinned = FindPinnedItem();
                if (pinned != null)
                {
                    pinned.onTap?.Invoke();
                    Collapse();
                    return;
                }
                Collapse();
                return;
            }
            // Short-tap while collapsed with pinned item.
            var p = FindPinnedItem();
            if (p != null)
            {
                p.onTap?.Invoke();
                return;
            }
            Expand();
        }

        private WheelItem FindPinnedItem()
        {
            if (string.IsNullOrEmpty(_pinnedId)) return null;
            return _items.Find(x => x.id == _pinnedId);
        }

        private void UpdateFabLabel(bool open)
        {
            if (_b.fabLabel == null) return;
            _b.fabLabel.text = open ? "×" : (FindPinnedItem() != null ? FindPinnedItem().iconName : "+");
        }

        private void RefreshFabLabel()
        {
            if (_b.fabLabel == null) return;
            var p = FindPinnedItem();
            _b.fabLabel.text = _expanded ? "×" : (p != null ? p.iconName : "+");
        }

        // ---- Pills --------------------------------------------------------------

        private void RebuildPills()
        {
            ClearChildren(_b.pillRow0);
            ClearChildren(_b.pillRow1);
            if (_b.pillRow0 == null || _b.pillRow1 == null) return;

            int start = _currentPage * MaxPerPage;
            int end   = Mathf.Min(start + MaxPerPage, _items.Count);

            int row0Count = Mathf.Min(end - start, 3);
            int row1Count = (end - start) - row0Count;

            for (int i = start; i < start + row0Count; i++) CreatePill(_b.pillRow0, _items[i]);
            for (int i = start + row0Count; i < end; i++) CreatePill(_b.pillRow1, _items[i]);
        }

        private void CreatePill(RectTransform parent, WheelItem item)
        {
            var go = new GameObject(item.id, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(PillWidth, Theme.PillHeight);

            var img = go.AddComponent<Image>();
            img.color = Theme.SurfaceGlass;

            var btn = go.AddComponent<Button>();
            var capturedItem = item;
            btn.onClick.AddListener(() => OnPillTapped(capturedItem));

            // Add long-press handler via EventTrigger.
            AddLongPressHandler(go, capturedItem);

            // Label.
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(4, 0); lrt.offsetMax = new Vector2(-4, 0);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = item.label;
            tmp.fontSize  = Theme.TypeSizeS;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Theme.OnSurface;

            // Dim if not available.
            if (item.isAvailable != null && !item.isAvailable())
                img.color = new Color(Theme.SurfaceGlass.r, Theme.SurfaceGlass.g, Theme.SurfaceGlass.b, 0.4f);
        }

        private void AddLongPressHandler(GameObject go, WheelItem item)
        {
            var et = go.AddComponent<EventTrigger>();
            Coroutine longPressRoutine = null;

            var downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            downEntry.callback.AddListener(_ =>
            {
                longPressRoutine = StartCoroutine(LongPressTimer(item));
            });
            et.triggers.Add(downEntry);

            var upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            upEntry.callback.AddListener(_ =>
            {
                if (longPressRoutine != null) StopCoroutine(longPressRoutine);
            });
            et.triggers.Add(upEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ =>
            {
                if (longPressRoutine != null) StopCoroutine(longPressRoutine);
            });
            et.triggers.Add(exitEntry);
        }

        private IEnumerator LongPressTimer(WheelItem item)
        {
            yield return new WaitForSeconds(LongPressDuration);
            PinItem(item);
        }

        private void PinItem(WheelItem item)
        {
            _pinnedId = item.id;
            RefreshFabLabel();
            AppShellController.Instance?.ShowStatus($"Pinned: {item.label}", 2f);
        }

        private void OnPillTapped(WheelItem item)
        {
            item.onTap?.Invoke();
            Collapse();
            AppShellController.Instance?.ShowStatus(item.label, 2f);
        }

        // ---- Page dots ----------------------------------------------------------

        private void RebuildPageDots()
        {
            if (_b.pageIndicatorRow == null) return;
            foreach (Transform c in _b.pageIndicatorRow.transform) Destroy(c.gameObject);
            _b.pageDots.Clear();

            for (int i = 0; i < _totalPages; i++)
            {
                int captured = i;
                var dotGo = new GameObject($"Dot{i}", typeof(RectTransform));
                dotGo.transform.SetParent(_b.pageIndicatorRow.transform, false);
                var dotRt = dotGo.GetComponent<RectTransform>();
                dotRt.sizeDelta = new Vector2(10f, 10f);
                var img = dotGo.AddComponent<Image>();
                img.color = i == _currentPage ? Theme.Accent : Theme.OnSurfaceDim;
                _b.pageDots.Add(img);

                var btn = dotGo.AddComponent<Button>();
                btn.onClick.AddListener(() => SetPage(captured));
            }
        }

        private void SetPage(int page)
        {
            _currentPage = Mathf.Clamp(page, 0, _totalPages - 1);
            RebuildPills();
            for (int i = 0; i < _b.pageDots.Count; i++)
                _b.pageDots[i].color = i == _currentPage ? Theme.Accent : Theme.OnSurfaceDim;
        }

        // ---- Animation ----------------------------------------------------------

        private IEnumerator AnimatePills(float target)
        {
            if (_b.pillsCanvasGroup == null) yield break;
            float start = _b.pillsCanvasGroup.alpha;
            float t = 0f;
            float dur = Theme.AnimMed;
            while (t < dur)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(start, target, t / dur);
                _b.pillsCanvasGroup.alpha = a;
                if (_b.pillsRoot != null)
                    _b.pillsRoot.localScale = Vector3.one * Mathf.Lerp(start < target ? 0.85f : 1f, target < 1f ? 0.85f : 1f, t / dur);
                yield return null;
            }
            _b.pillsCanvasGroup.alpha = target;
            if (_b.pillsRoot != null)
                _b.pillsRoot.localScale = Vector3.one * (target > 0f ? 1f : 0.85f);
            SetPillsVisible(target > 0f);
        }

        private void SetPillsVisible(bool visible, bool instant = false)
        {
            if (_b.pillsCanvasGroup == null) return;
            if (instant) _b.pillsCanvasGroup.alpha = visible ? 1f : 0f;
            _b.pillsCanvasGroup.interactable   = visible;
            _b.pillsCanvasGroup.blocksRaycasts = visible;
            if (_b.pageIndicatorRow != null) _b.pageIndicatorRow.SetActive(visible);
        }

        private static void ClearChildren(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
        }
    }
}
