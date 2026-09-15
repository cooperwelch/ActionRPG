using System.Collections;
using UnityEngine;

public class ShopInteraction : MonoBehaviour
{
    [SerializeField] private ShopCatalog catalog;

    public void OnConversationEnd(Transform actor)
    {
        if (catalog == null || GameplayUI.IsBlocking)
        {
            return;
        }

        StartCoroutine(OpenAfterDialogue());
    }

    private IEnumerator OpenAfterDialogue()
    {
        yield return null;
        if (GameplayUI.IsBlocking)
        {
            yield break;
        }

        ShopUIController shopUI = FindObjectOfType<ShopUIController>();
        if (shopUI != null)
        {
            shopUI.Open(catalog);
        }
    }
}
