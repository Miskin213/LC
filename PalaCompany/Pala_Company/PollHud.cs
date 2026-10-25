using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PalaCompany
{
    internal enum HudAnchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    internal sealed class PollHud
    {
        private readonly Action<string> _logError;
        private GameObject _widgetObject;
        private TextMeshProUGUI _text;
        private RectTransform _textRect;
        private Image[] _borderImages;
        private Sprite _borderSprite;
        private bool _loggedError;

        internal PollHud(Action<string> logError)
        {
            _logError = logError;
        }

        internal void Update(
            PollHudState state,
            bool enabled,
            HudAnchor anchor,
            float offsetX,
            float offsetY,
            float fontSize,
            float opacity,
            bool showBorder,
            float borderThickness,
            string borderColor,
            float boxWidth,
            float padding,
            bool showLatestVote)
        {
            try
            {
                if (!enabled || state == null || state.Phase == PollHudPhase.Hidden)
                {
                    if (_widgetObject != null)
                        _widgetObject.SetActive(false);
                    return;
                }

                var hud = HUDManager.Instance;
                if (hud == null || hud.HUDContainer == null)
                    return;

                if (_widgetObject == null
                    || _text == null
                    || _widgetObject.transform.parent != hud.HUDContainer.transform)
                    CreateWidget(hud);

                ApplyLayout(anchor, offsetX, offsetY);
                float safeBoxWidth = Mathf.Max(120f, boxWidth);
                float safeBorderThickness = Mathf.Max(0f, borderThickness);
                float safePadding = Mathf.Clamp(
                    padding + (showBorder ? safeBorderThickness : 0f),
                    0f,
                    safeBoxWidth / 2f - 1f);
                float safeFontSize = Mathf.Max(8f, fontSize);
                _text.fontSize = safeFontSize;
                _text.fontSizeMin = Mathf.Min(12f, safeFontSize);
                _text.fontSizeMax = safeFontSize;
                _text.enableAutoSizing = true;
                _text.enableWordWrapping = true;
                _textRect.offsetMin = new Vector2(safePadding, safePadding);
                _textRect.offsetMax = new Vector2(-safePadding, -safePadding);
                _text.text = BuildText(state, showLatestVote);

                float innerWidth = Mathf.Max(1f, safeBoxWidth - safePadding * 2f);
                float textHeight = _text.GetPreferredValues(_text.text, innerWidth, 0f).y;
                var widgetRect = (RectTransform)_widgetObject.transform;
                widgetRect.sizeDelta = new Vector2(
                    safeBoxWidth,
                    Mathf.Max(safeFontSize, textHeight) + safePadding * 2f);

                Color parsedBorderColor;
                if (!ColorUtility.TryParseHtmlString(borderColor, out parsedBorderColor))
                    parsedBorderColor = Color.white;
                parsedBorderColor.a *= Mathf.Clamp01(opacity);
                for (int index = 0; index < _borderImages.Length; index++)
                {
                    _borderImages[index].color = parsedBorderColor;
                    _borderImages[index].gameObject.SetActive(showBorder && safeBorderThickness > 0f);
                }
                SetBorderThickness(safeBorderThickness);

                var textColor = _text.color;
                textColor.a = Mathf.Clamp01(opacity);
                _text.color = textColor;
                _widgetObject.SetActive(true);
            }
            catch (Exception exception)
            {
                if (!_loggedError)
                {
                    _logError("Could not update the poll HUD widget: " + exception);
                    _loggedError = true;
                }
            }
        }

        private void CreateWidget(HUDManager hud)
        {
            if (hud.tipsPanelHeader == null || hud.tipsPanelHeader.font == null)
                throw new InvalidOperationException("HUDManager tip panel font is unavailable.");

            if (_widgetObject != null)
                UnityEngine.Object.Destroy(_widgetObject);

            _widgetObject = new GameObject("PalaCompanyPollWidget", typeof(RectTransform));
            _widgetObject.transform.SetParent(hud.HUDContainer.transform, false);
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(_widgetObject.transform, false);
            _text = textObject.GetComponent<TextMeshProUGUI>();
            _text.font = hud.tipsPanelHeader.font;
            _text.alignment = TextAlignmentOptions.TopLeft;
            _text.enableWordWrapping = true;
            _text.enableAutoSizing = true;
            _text.richText = true;
            _text.raycastTarget = false;

            _textRect = _text.rectTransform;
            _textRect.anchorMin = Vector2.zero;
            _textRect.anchorMax = Vector2.one;
            _textRect.offsetMin = Vector2.zero;
            _textRect.offsetMax = Vector2.zero;

            if (_borderSprite == null)
            {
                var pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                pixel.SetPixel(0, 0, Color.white);
                pixel.Apply();
                _borderSprite = Sprite.Create(pixel, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            }

            _borderImages = new Image[4];
            for (int index = 0; index < _borderImages.Length; index++)
            {
                var borderObject = new GameObject(
                    "Border" + index,
                    typeof(RectTransform),
                    typeof(Image));
                borderObject.transform.SetParent(_widgetObject.transform, false);
                var borderRect = (RectTransform)borderObject.transform;
                var borderImage = borderObject.GetComponent<Image>();
                borderImage.sprite = _borderSprite;
                borderImage.raycastTarget = false;
                _borderImages[index] = borderImage;
                ConfigureBorderRect(borderRect, index);
            }
        }

        private void ApplyLayout(HudAnchor anchor, float offsetX, float offsetY)
        {
            Vector2 normalizedAnchor;
            switch (anchor)
            {
                case HudAnchor.TopLeft:
                    normalizedAnchor = new Vector2(0f, 1f);
                    _text.alignment = TextAlignmentOptions.TopLeft;
                    break;
                case HudAnchor.BottomLeft:
                    normalizedAnchor = new Vector2(0f, 0f);
                    _text.alignment = TextAlignmentOptions.BottomLeft;
                    break;
                case HudAnchor.BottomRight:
                    normalizedAnchor = new Vector2(1f, 0f);
                    _text.alignment = TextAlignmentOptions.BottomRight;
                    break;
                default:
                    normalizedAnchor = new Vector2(1f, 1f);
                    _text.alignment = TextAlignmentOptions.TopRight;
                    break;
            }

            var rect = (RectTransform)_widgetObject.transform;
            rect.anchorMin = normalizedAnchor;
            rect.anchorMax = normalizedAnchor;
            rect.pivot = normalizedAnchor;
            float x = normalizedAnchor.x == 0f ? offsetX : -offsetX;
            float y = normalizedAnchor.y == 0f ? -offsetY : offsetY;
            rect.anchoredPosition = new Vector2(x, y);
        }

        private void SetBorderThickness(float thickness)
        {
            for (int index = 0; index < _borderImages.Length; index++)
            {
                var borderRect = (RectTransform)_borderImages[index].transform;
                borderRect.sizeDelta = index < 2
                    ? new Vector2(0f, thickness)
                    : new Vector2(thickness, 0f);
            }
        }

        private static void ConfigureBorderRect(RectTransform rect, int index)
        {
            if (index == 0)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
            }
            else if (index == 1)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
            }
            else if (index == 2)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
            }
            else
            {
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(1f, 0.5f);
            }

            rect.anchoredPosition = Vector2.zero;
        }

        private static string BuildText(PollHudState state, bool showLatestVote)
        {
            if (state.Phase == PollHudPhase.Result)
                return state.ResultText ?? "";

            int total = state.Votes1 + state.Votes2;
            int percent1 = total > 0 ? Mathf.RoundToInt(state.Votes1 * 100f / total) : 0;
            int percent2 = total > 0 ? Mathf.RoundToInt(state.Votes2 * 100f / total) : 0;
            string option1 = Loc.T(
                "poll.hud.option",
                Loc.T("poll.hud.command1"),
                state.Option1,
                state.Votes1,
                percent1);
            string option2 = Loc.T(
                "poll.hud.option",
                Loc.T("poll.hud.command2"),
                state.Option2,
                state.Votes2,
                percent2);

            if (state.Votes1 > state.Votes2)
                option1 = Loc.T("poll.hud.option.leading", option1);
            else if (state.Votes2 > state.Votes1)
                option2 = Loc.T("poll.hud.option.leading", option2);

            string text = Loc.T("poll.hud.title", Mathf.CeilToInt(state.SecondsLeft))
                + "\n" + option1
                + "\n" + option2;
            if (showLatestVote)
            {
                string latestVote = !string.IsNullOrEmpty(state.LatestVoteUsername)
                    && !string.IsNullOrEmpty(state.LatestVoteOption)
                        ? Loc.T("poll.hud.latest", state.LatestVoteUsername, state.LatestVoteOption)
                        : Loc.T("poll.hud.latest.none");
                text += "\n" + latestVote;
            }
            return text;
        }

    }
}
