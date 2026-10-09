using FarmFuryRampage.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryRampage.Run
{
    /// <summary>Phase 1 placeholder HUD, uGUI built in code. The real HUD (GDD section 10) arrives in Phase 2.</summary>
    public sealed class GreyboxHud : MonoBehaviour
    {
        static readonly Vector2 ReferenceResolution = new(1080f, 2340f);
        const int TopFontSize = 44;
        const int MessageFontSize = 72;
        const int HintFontSize = 36;

        Text top;
        Text message;
        Text hint;

        void Awake()
        {
            var canvasGo = new GameObject("GreyboxHud", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            top = MakeText(canvasGo.transform, font, TopFontSize, TextAnchor.UpperCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), -60f);
            message = MakeText(canvasGo.transform, font, MessageFontSize, TextAnchor.MiddleCenter, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), 0f);
            hint = MakeText(canvasGo.transform, font, HintFontSize, TextAnchor.LowerCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), 60f);
            hint.text = "Drag (or A/D) to steer · R restart · N next level";
        }

        public void Render(RunSim sim, string levelName)
        {
            top.text = $"{levelName}   {Mathf.RoundToInt(sim.Progress * 100f)}%   Scrap {sim.Scrap}   Herd {sim.HerdCount}";
            message.text = sim.Phase switch
            {
                RunPhase.Won => $"CLEARED!\nHerd {sim.HerdCount} · Kills {sim.Kills}\nTap for next level",
                RunPhase.Failed => "HERD LOST\nTap to retry",
                _ => string.Empty,
            };
        }

        static Text MakeText(Transform parent, Font font, int size, TextAnchor anchor, Vector2 anchorMin, Vector2 anchorMax, float y)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, anchorMin.y);
            rect.sizeDelta = new Vector2(-80f, size * 4f);
            rect.anchoredPosition = new Vector2(0f, y);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
