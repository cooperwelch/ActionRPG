using UnityEngine;

public class CraftingHammerPickup : MonoBehaviour
{
    private void Awake()
    {

        if (PlayerStats.HasCraftingHammer)
        {
            transform.root.gameObject.SetActive(false);
        }
    }
}
