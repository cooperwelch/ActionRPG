using UnityEngine;

public enum ItemEffectType
{
    Heal,
    AddOre
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
    public int oreAmount;

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
            case ItemEffectType.AddOre:
                if (oreAmount <= 0)
                {
                    return 0;
                }

                PlayerStats.AddOre(oreAmount);
                return oreAmount;
            default:
                return 0;
        }
    }
}
