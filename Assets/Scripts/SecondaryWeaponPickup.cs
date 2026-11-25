using UnityEngine;

public class SecondaryWeaponPickup : MonoBehaviour
{
    public SecondaryWeapon weapon;

    private void Awake()
    {

        if (weapon != null && PlayerStats.SecondaryWeapons.Contains(weapon.name))
        {
            transform.root.gameObject.SetActive(false);
        }
    }
}
