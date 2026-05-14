using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Equilibrium
{
    /// <summary>
    /// Simple bar chart showing reactant vs product concentrations.
    /// </summary>
    public class ConcentrationGraph : MonoBehaviour
    {
        private Image _reactantBar;
        private Image _productBar;
        private TMPro.TextMeshProUGUI _reactantLabel;
        private TMPro.TextMeshProUGUI _productLabel;

        public void Build(Transform parent)
        {
            var go = new GameObject("ConcGraph", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 80f);
            go.AddComponent<LayoutElement>().preferredHeight = 80f;
            go.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.3f);

            var row = new GameObject("Bars", typeof(RectTransform));
            row.transform.SetParent(go.transform, false);
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.05f, 0.1f);
            rowRt.anchorMax = new Vector2(0.95f, 0.9f);
            rowRt.offsetMin = rowRt.offsetMax = Vector2.zero;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.LowerCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = false;
            hlg.spacing = 10f;

            (_reactantBar, _reactantLabel) = CreateBarColumn(row.transform, "Reactants", new Color(0.4f, 0.6f, 1f));
            (_productBar,  _productLabel)  = CreateBarColumn(row.transform, "Products",  new Color(0.4f, 1f, 0.5f));
        }

        public void UpdateBars(float reactantFraction, float productFraction)
        {
            SetBarHeight(_reactantBar, reactantFraction);
            SetBarHeight(_productBar,  productFraction);
        }

        private static (Image bar, TMPro.TextMeshProUGUI label) CreateBarColumn(Transform parent, string name, Color color)
        {
            var col = new GameObject(name, typeof(RectTransform));
            col.transform.SetParent(parent, false);
            col.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var colVlg = col.AddComponent<VerticalLayoutGroup>();
            colVlg.childAlignment = TextAnchor.LowerCenter;
            colVlg.childForceExpandWidth = true;
            colVlg.childForceExpandHeight = false;
            colVlg.spacing = 2f;

            var barGo = new GameObject("Bar", typeof(RectTransform));
            barGo.transform.SetParent(col.transform, false);
            var le = barGo.AddComponent<LayoutElement>();
            le.preferredWidth = 30f; le.preferredHeight = 30f;
            var img = barGo.AddComponent<Image>();
            img.color = color;
            img.type  = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Vertical;
            img.fillAmount = 0.5f;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(col.transform, false);
            labelGo.AddComponent<LayoutElement>().preferredHeight = 16f;
            var tmp = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = name; tmp.fontSize = 9f;
            tmp.color = UI.Theme.OnSurfaceDim; tmp.alignment = TMPro.TextAlignmentOptions.Center;

            return (img, tmp);
        }

        private static void SetBarHeight(Image bar, float fraction)
        {
            if (bar) bar.fillAmount = Mathf.Clamp01(fraction);
        }
    }
}
