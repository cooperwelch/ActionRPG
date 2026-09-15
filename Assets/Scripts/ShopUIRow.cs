using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUIRow : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Sprite unselectedBackgroundSprite;
    [SerializeField] private Sprite selectedBackgroundSprite;
    [SerializeField] private Color affordableCostColor = Color.white;
    [SerializeField] private Color unaffordableCostColor = new Color32(0x42, 0x3F, 0x73, 255);

    public RectTransform RectTransform => transform as RectTransform;

    public void Bind(ItemDefinition item, bool focused)
    {
        if (backgroundImage != null)
        {
            Sprite sprite = focused ? selectedBackgroundSprite : unselectedBackgroundSprite;
            if (sprite != null)
            {
                backgroundImage.sprite = sprite;
            }
        }

        if (item == null)
        {
            return;
        }

        if (titleText != null)
        {
            titleText.text = item.displayName;
        }

        if (costText != null)
        {
            costText.text = item.cost.ToString("D2");
            costText.color = PlayerStats.Denarius >= item.cost ? affordableCostColor : unaffordableCostColor;
        }

        if (iconImage != null)
        {
            iconImage.enabled = item.icon != null;
            if (item.icon != null)
            {
                iconImage.sprite = item.icon;
            }
        }
    }

    public void SetFocused(bool focused)
    {
        if (backgroundImage == null)
        {
            return;
        }

        Sprite sprite = focused ? selectedBackgroundSprite : unselectedBackgroundSprite;
        if (sprite != null)
        {
            backgroundImage.sprite = sprite;
        }
    }
}
