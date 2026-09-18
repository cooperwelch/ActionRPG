using UnityEngine;

[CreateAssetMenu(fileName = "ThrowingAxe", menuName = "ScriptableObjects/Weapon/Secondary/ThrowingAxe")]
public class ThrowingAxe : SecondaryWeapon
{
    public GameObject projectilePrefab;
    public float throwSpeed = 15f;
    public float launchAngle = 35f;
    public Vector2 spawnOffset = new Vector2(0.5f, 0f);
    public float crouchYOffset = -0.5f;

    public override void Execute(Transform player, Vector2 direction, bool isCrouching)
    {
        float yOffset = spawnOffset.y + (isCrouching ? crouchYOffset : 0f);
        Vector2 spawnPos = (Vector2)player.position + new Vector2(spawnOffset.x * direction.x, yOffset);
        GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        float radians = launchAngle * Mathf.Deg2Rad;
        Vector2 velocity = new Vector2(
            Mathf.Cos(radians) * throwSpeed * direction.x,
            Mathf.Sin(radians) * throwSpeed
        );

        projectile.GetComponent<Projectile>().Initialize(
            attackDamage,
            velocity,
            canDamageEnemies,
            canMineOre,
            canBreakBreakables
        );
    }
}
