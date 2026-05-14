using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.UI
{
    public static class AppShellPrefabFactory
    {
        public static GameObject CreateShell(out ActionWheel wheel,
                                             out StatusPill  pill,
                                             out Button      tutorButton,
                                             out Image       tutorBadge)
        {
            // ---- Root canvas -------------------------------------------------------
            var canvasGo = new GameObject("[AppShell]", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            float bottom = SafeAreaResolver.BottomInset();

            // ---- Backdrop (tap outside to close) ----------------------------------
            var backdropGo = new GameObject("Backdrop", typeof(RectTransform));
            backdropGo.transform.SetParent(canvasGo.transform, false);
            var brt = backdropGo.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            var bimg = backdropGo.AddComponent<Image>();
            bimg.color = Color.clear;
            bimg.raycastTarget = true;
            var backdropBtn = backdropGo.AddComponent<Button>();
            backdropGo.SetActive(false);

            // ---- Wheel root -------------------------------------------------------
            var wheelRootGo = new GameObject("WheelRoot", typeof(RectTransform));
            wheelRootGo.transform.SetParent(canvasGo.transform, false);
            var wrt = wheelRootGo.GetComponent<RectTransform>();
            wrt.anchorMin = wrt.anchorMax = new Vector2(0.35f, 0f);
            wrt.pivot     = new Vector2(0.5f, 0f);
            wrt.anchoredPosition = new Vector2(0f, bottom);
            wrt.sizeDelta = new Vector2(420f, 200f);

            // Pills container (expands upward above FAB).
            var pillsRootGo = new GameObject("PillsRoot", typeof(RectTransform));
            pillsRootGo.transform.SetParent(wheelRootGo.transform, false);
            var prt = pillsRootGo.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 0f);
            prt.anchorMax = new Vector2(1f, 0f);
            prt.pivot     = new Vector2(0.5f, 0f);
            prt.anchoredPosition = new Vector2(0f, Theme.FabSize + Theme.WheelGap);
            prt.sizeDelta = new Vector2(0f, 110f);
            var pillsCG = pillsRootGo.AddComponent<CanvasGroup>();

            // Row 0.
            var row0 = MakePillRow(pillsRootGo.transform, 0f);
            // Row 1 (below row0 logically, offset upward).
            var row1 = MakePillRow(pillsRootGo.transform, Theme.PillHeight + Theme.WheelGap);

            // Page indicator row.
            var pageRowGo = new GameObject("PageDots", typeof(RectTransform));
            pageRowGo.transform.SetParent(pillsRootGo.transform, false);
            var pgrt = pageRowGo.GetComponent<RectTransform>();
            pgrt.anchorMin = new Vector2(0.5f, 0f);
            pgrt.anchorMax = new Vector2(0.5f, 0f);
            pgrt.pivot     = new Vector2(0.5f, 1f);
            pgrt.anchoredPosition = new Vector2(0f, -(Theme.WheelGap * 0.5f));
            pgrt.sizeDelta = new Vector2(60f, 14f);
            var pgLayout = pageRowGo.AddComponent<HorizontalLayoutGroup>();
            pgLayout.spacing          = 6f;
            pgLayout.childAlignment   = TextAnchor.MiddleCenter;
            pgLayout.childControlWidth  = false;
            pgLayout.childControlHeight = false;

            // FAB button.
            var fabGo = new GameObject("FAB", typeof(RectTransform));
            fabGo.transform.SetParent(wheelRootGo.transform, false);
            var frt = fabGo.GetComponent<RectTransform>();
            frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0f);
            frt.pivot     = new Vector2(0.5f, 0f);
            frt.anchoredPosition = Vector2.zero;
            frt.sizeDelta = new Vector2(Theme.FabSize, Theme.FabSize);

            var fabImg = fabGo.AddComponent<Image>();
            fabImg.color = Theme.SurfaceElevated;
            var fabBtn = fabGo.AddComponent<Button>();

            var fabLabelGo = new GameObject("Label", typeof(RectTransform));
            fabLabelGo.transform.SetParent(fabGo.transform, false);
            SetFill(fabLabelGo.GetComponent<RectTransform>());
            var fabTmp = fabLabelGo.AddComponent<TextMeshProUGUI>();
            fabTmp.text      = "+";
            fabTmp.fontSize  = Theme.TypeSizeXL;
            fabTmp.alignment = TextAlignmentOptions.Center;
            fabTmp.color     = Theme.OnSurface;

            // Wheel bindings + ActionWheel component.
            var bindings = wheelRootGo.AddComponent<ActionWheelBindings>();
            bindings.fabButton       = fabBtn;
            bindings.fabLabel        = fabTmp;
            bindings.pillsCanvasGroup = pillsCG;
            bindings.pillsRoot       = prt;
            bindings.pillRow0        = row0;
            bindings.pillRow1        = row1;
            bindings.pageIndicatorRow = pageRowGo;
            bindings.backdropButton  = backdropBtn;

            wheel = wheelRootGo.AddComponent<ActionWheel>();

            // ---- Tutor button (bottom-right) ----------------------------------------
            var tutorGo = new GameObject("TutorButton", typeof(RectTransform));
            tutorGo.transform.SetParent(canvasGo.transform, false);
            var trt = tutorGo.GetComponent<RectTransform>();
            trt.anchorMin = trt.anchorMax = new Vector2(1f, 0f);
            trt.pivot     = new Vector2(1f, 0f);
            trt.anchoredPosition = new Vector2(-bottom, bottom);
            trt.sizeDelta = new Vector2(Theme.FabSize, Theme.FabSize);

            var tutorImg = tutorGo.AddComponent<Image>();
            tutorImg.color = Theme.SurfaceElevated;
            tutorButton = tutorGo.AddComponent<Button>();

            var tutorLabelGo = new GameObject("Label", typeof(RectTransform));
            tutorLabelGo.transform.SetParent(tutorGo.transform, false);
            SetFill(tutorLabelGo.GetComponent<RectTransform>());
            var tutorTmp = tutorLabelGo.AddComponent<TextMeshProUGUI>();
            tutorTmp.text      = "T";
            tutorTmp.fontSize  = Theme.TypeSizeL;
            tutorTmp.alignment = TextAlignmentOptions.Center;
            tutorTmp.color     = Theme.OnSurface;

            // Badge dot (top-right corner of Tutor button).
            var badgeGo = new GameObject("Badge", typeof(RectTransform));
            badgeGo.transform.SetParent(tutorGo.transform, false);
            var badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(1f, 1f);
            badgeRt.pivot     = new Vector2(1f, 1f);
            badgeRt.anchoredPosition = Vector2.zero;
            badgeRt.sizeDelta = new Vector2(12f, 12f);
            tutorBadge = badgeGo.AddComponent<Image>();
            tutorBadge.color = Theme.Accent;
            badgeGo.SetActive(false);

            // ---- Status pill (bottom-left, above wheel) ----------------------------
            var pillGo = new GameObject("StatusPill", typeof(RectTransform));
            pillGo.transform.SetParent(canvasGo.transform, false);
            var srt2 = pillGo.GetComponent<RectTransform>();
            srt2.anchorMin = srt2.anchorMax = new Vector2(0f, 0f);
            srt2.pivot     = new Vector2(0f, 0f);
            srt2.anchoredPosition = new Vector2(bottom, bottom + Theme.FabSize + Theme.WheelGap * 2f);
            srt2.sizeDelta = new Vector2(320f, Theme.PillHeight);

            var pillImg = pillGo.AddComponent<Image>();
            pillImg.color = Theme.SurfaceGlass;
            var pillCG = pillGo.AddComponent<CanvasGroup>();
            var pillBtn = pillGo.AddComponent<Button>();

            var pillTextGo = new GameObject("Text", typeof(RectTransform));
            pillTextGo.transform.SetParent(pillGo.transform, false);
            SetFill(pillTextGo.GetComponent<RectTransform>(), 8f);
            var pillTmp = pillTextGo.AddComponent<TextMeshProUGUI>();
            pillTmp.text      = string.Empty;
            pillTmp.fontSize  = Theme.TypeSizeS;
            pillTmp.alignment = TextAlignmentOptions.MidlineLeft;
            pillTmp.color     = Theme.OnSurface;

            var pillBindings = pillGo.AddComponent<StatusPillBindings>();
            pillBindings.canvasGroup   = pillCG;
            pillBindings.messageText   = pillTmp;
            pillBindings.background    = pillImg;
            pillBindings.dismissButton = pillBtn;

            pill = pillGo.AddComponent<StatusPill>();

            return canvasGo;
        }

        private static RectTransform MakePillRow(Transform parent, float yOffset)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, yOffset);
            rt.sizeDelta = new Vector2(0f, Theme.PillHeight);

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing          = Theme.WheelGap;
            layout.childAlignment   = TextAnchor.MiddleCenter;
            layout.childControlWidth  = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth  = false;
            layout.childForceExpandHeight = false;

            return rt;
        }

        private static void SetFill(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, 0f);
            rt.offsetMax = new Vector2(-inset, 0f);
        }
    }
}
