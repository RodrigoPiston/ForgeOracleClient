using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ForgeOracle.Plugin.UI
{
    /// <summary>Lets the panel be dragged by its title; reports the final offset so it can be saved.</summary>
    internal sealed class PanelDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform? Target { get; set; }

        public Func<bool>? Enabled { get; set; }

        public Action<Vector2>? Dropped { get; set; }

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Target == null || (Enabled != null && !Enabled()))
            {
                return;
            }

            Canvas? canvas = Target.GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
            Target.anchoredPosition += eventData.delta / Mathf.Max(0.01f, scale);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (Target != null && (Enabled == null || Enabled()))
            {
                Dropped?.Invoke(Target.anchoredPosition);
            }
        }
    }
}
