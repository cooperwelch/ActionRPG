using TMPro;
using UnityEngine;

public class MainMenuOptionRow : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private bool enabledOption = true;
    [SerializeField] private Color restingColor = Color.white;
    [SerializeField] private Color focusedColor = Color.yellow;
    [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    public bool IsEnabled => enabledOption;
    public RectTransform RectTransform => transform as RectTransform;
    public RectTransform LabelRectTransform => label != null ? label.rectTransform : RectTransform;

    public void Refresh(bool focused, bool disabled)
    {
        if (label == null)
        {
            return;
        }

        if (disabled || !enabledOption)
        {
            label.color = disabledColor;
            return;
        }

        label.color = focused ? focusedColor : restingColor;
    }
}
