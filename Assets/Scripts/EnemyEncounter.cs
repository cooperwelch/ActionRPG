using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyEncounter : MonoBehaviour
{
    public string subzoneName;
    public OverworldAudioManager audioManager;
    public TopDownMovement heroMovement;
    private EnemyGridMovement enemyMovement;

    private void Awake()
    {
        enemyMovement = gameObject.GetComponent<EnemyGridMovement>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("OverworldHero"))
        {
            audioManager.PlayEnemyEncounter();
            Transform movePoint = enemyMovement.movePoint;
            OverworldSubzoneContainer.AddEncounter(
                movePoint.position.x,
                movePoint.position.y - 1, // Offset player from last encounter so we don't auto-collide again.
                subzoneName,
                OverworldSubzoneContainer.PlayerDirection.Down
            );

            heroMovement.StopMovement();
            heroMovement.StopAnimation();
            enemyMovement.StopMovement();
            StartCoroutine(SubzoneTransition());
        }
    }

    private IEnumerator SubzoneTransition()
    {
        yield return new WaitForSeconds(2.0f);
        SceneManager.LoadScene(subzoneName);
    }
}
