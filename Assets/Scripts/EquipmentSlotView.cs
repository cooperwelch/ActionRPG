using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlotView : MonoBehaviour
{
    [SerializeField] private Image frameImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite unselectedFrame;
    [SerializeField] private Sprite selectedFrame;
    [SerializeField] private Sprite equippedFrame;
    [SerializeField] private Sprite emptyIcon;

    public RectTransform RectTransform => transform as RectTransform;

    public void SetEquippedFrame(Sprite frame)
    {
        if (frame != null)
        {
            equippedFrame = frame;
        }
    }

    public void Bind(Sprite weaponIcon, bool equipped, bool focused)
    {
        if (frameImage != null)
        {
            if (equipped && equippedFrame != null)
            {
                frameImage.sprite = equippedFrame;
            }
            else
            {
                Sprite frame = focused ? selectedFrame : unselectedFrame;
                if (frame != null)
                {
                    frameImage.sprite = frame;
                }
            }
        }

        if (weaponIcon == null)
        {
            SetIcon(emptyIcon);
            return;
        }

        SetIcon(weaponIcon);
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
