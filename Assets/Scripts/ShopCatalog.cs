using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShopListing
{
    public ItemDefinition item;
    [Tooltip("-1 means infinite stock.")]
    public int stockLimit = -1;
}

[CreateAssetMenu(fileName = "ShopCatalog", menuName = "ScriptableObjects/Items/Shop Catalog")]
public class ShopCatalog : ScriptableObject
{
    public string shopId;
    public List<ShopListing> listings = new List<ShopListing>();
}
