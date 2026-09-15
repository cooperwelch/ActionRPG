using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotRow : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Sprite unselectedBackgroundSprite;
    [SerializeField] private Sprite selectedBackgroundSprite;
    [SerializeField] private Sprite emptyIcon;
    [SerializeField] private Color occupiedColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(0.5f, 0.5f, 0.5f, 1f);

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
            if (titleText != null)
            {
                titleText.text = "Empty";
                titleText.color = emptyColor;
            }

            SetIcon(emptyIcon);
            return;
        }

        if (titleText != null)
        {
            titleText.text = item.displayName;
            titleText.color = occupiedColor;
        }

        SetIcon(item.icon);
    }

    private void SetIcon(Sprite sprite)
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.enabled = sprite != null;
        if (sprite != null)
        {
            iconImage.sprite = sprite;
        }
    }
}
