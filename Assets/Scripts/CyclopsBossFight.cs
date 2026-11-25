using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cinemachine;
using Language.Lua;
using UnityEngine;

public class CyclopsBossFight : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private BoxCollider2D[] colliders;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform virtualCameraBossFightTarget;
    [SerializeField] private CyclopsBossStateMachine stateMachine;
    [SerializeField] private SubzoneHUD subzoneHUD;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Cyclops cyclops;
    [SerializeField] private CameraShake cameraShake;
    private BoxCollider2D _bossFightStartTrigger;

    private bool moveCam = false;
    private bool playerNeedsUnfreeze = false;
    private bool reset = false;
    private Transform originalVirtualCameraFollow;
    private bool moveCamBackToPlayer = false;

    private void Awake()
    {
        DisableColliders();
        _bossFightStartTrigger = GetComponent<BoxCollider2D>();
        originalVirtualCameraFollow = virtualCamera.Follow;

        if (PlayerStats.IsCyclopsDefeated)
        {
            ApplyDefeatedState();
        }
    }

    private void ApplyDefeatedState()
    {
        _bossFightStartTrigger.enabled = false;
        DisableColliders();
        if (cyclops != null)
        {
            cyclops.gameObject.SetActive(false);
        }
    }

    private void DisableColliders()
    {
        foreach (BoxCollider2D collider in colliders)
        {
            collider.enabled = false;
        }
    }

    private void EnableColliders()
    {
        foreach (BoxCollider2D collider in colliders)
        {
            collider.enabled = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (playerNeedsUnfreeze)
        {
            _bossFightStartTrigger.enabled = false;
            playerNeedsUnfreeze = false;
            EnableColliders();
            playerMovement.AllowMovement();
        }
        if (moveCam)
        {
            virtualCamera.transform.position = Vector3.MoveTowards(
                virtualCamera.transform.position,
                virtualCameraBossFightTarget.position,
                Time.deltaTime * 7f
            );
            
            if (virtualCamera.transform.position.x >= virtualCameraBossFightTarget.position.x)
            {
                moveCam = false;
                StartCoroutine(StartFight());
            }
        }

        if (stateMachine.bossState == CyclopsBossStateMachine.BossState.Dead)
        {
            if (!reset)
            {
                reset = true;
                subzoneHUD.FillPlayerHealthMeter();
                DisableColliders();
                moveCamBackToPlayer = true;
                playerMovement.StopForDialogue();
            }
        }

        if (moveCamBackToPlayer)
        {
            Vector3 followTarget = playerTransform.position +
                virtualCamera.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset;
            virtualCamera.transform.position = Vector3.MoveTowards(
                virtualCamera.transform.position,
                followTarget,
                Time.deltaTime * 7f
            );

            if (Mathf.Abs(virtualCamera.transform.position.x - followTarget.x) < 0.2f)
            {
                moveCamBackToPlayer = false;
                RetargetVirtualCamera();
                playerMovement.AllowMovement();
            }
        }
    }

    private IEnumerator StartFight()
    {
        yield return new WaitForSeconds(1f);
        cameraShake.ShakeCamera(2f, 4f);
        cyclops.Roar();
        yield return new WaitForSeconds(1.0f);
        subzoneHUD.FillBossHealthMeter();
        yield return new WaitForSeconds(2.2f);
        playerNeedsUnfreeze = true;
        stateMachine.Run();
    }

    private void UntargetVirtualCamera()
    {
        virtualCamera.Follow = null;
        moveCam = true;
    }

    private void RetargetVirtualCamera()
    {
        virtualCamera.PreviousStateIsValid = false;
        virtualCamera.Follow = originalVirtualCameraFollow;
        moveCam = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") == true)
        {
            playerMovement.StopForDialogue();
            UntargetVirtualCamera();
        }
    }
}
