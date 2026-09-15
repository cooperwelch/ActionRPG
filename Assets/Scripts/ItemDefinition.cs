using UnityEngine;

public enum ItemEffectType
{
    Heal
}

[CreateAssetMenu(fileName = "ItemDefinition", menuName = "ScriptableObjects/Items/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public string displayName;
    [TextArea]
    public string description;
    public Sprite icon;
    public int cost;
    [TextArea]
    public string unaffordableMessage = "Not enough denarius to purchase.";
    [TextArea]
    public string inventoryFullMessage = "Inventory is full.";
    public ItemEffectType effectType = ItemEffectType.Heal;
    public int healAmount;

    public string GetBlockedPurchaseMessage()
    {
        PlayerStats.Initialize();
        if (PlayerStats.Denarius < cost)
        {
            return unaffordableMessage;
        }

        if (!PlayerStats.HasEmptySlot())
        {
            return inventoryFullMessage;
        }

        return string.Empty;
    }

    public int Execute()
    {
        switch (effectType)
        {
            case ItemEffectType.Heal:
                return PlayerStats.Heal(healAmount);
            default:
                return 0;
        }
    }
}
