using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeaSerpentBossController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SeaSerpentBossMotor motor;
    [SerializeField] private SeaSerpentBossAttackController attacks;
    [SerializeField] private SeaSerpentBossHealth health;
    [SerializeField] private SeaSerpentBossAnimatorBridge animatorBridge;

    [Header("Arena Anchors")]
    [SerializeField] private Transform leftAnchor;
    [SerializeField] private Transform rightAnchor;

    [Header("Timing")]
    [SerializeField] private float idleDuration = 2.0f;

    [Header("Attack Selection")]
    [Range(0f, 1f)]
    [SerializeField] private float spitChance = 0.5f;

    private ISeaSerpentBossState currentState;

    private bool isRunning = false;

    public SeaSerpentBossMotor Motor => motor;
    public SeaSerpentBossAttackController Attacks => attacks;
    public SeaSerpentBossHealth Health => health;
    public SeaSerpentBossAnimatorBridge AnimatorBridge => animatorBridge;

    public Transform LeftAnchor => leftAnchor;
    public Transform RightAnchor => rightAnchor;
    public float IdleDuration => idleDuration;
    public float SpitChance => spitChance;

    private void Awake()
    {
        if (motor == null) motor = GetComponent<SeaSerpentBossMotor>();
        if (attacks == null) attacks = GetComponent<SeaSerpentBossAttackController>();
        if (health == null) health = GetComponent<SeaSerpentBossHealth>();
        if (animatorBridge == null) animatorBridge = GetComponentInChildren<SeaSerpentBossAnimatorBridge>();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDied += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDied -= HandleDeath;
        }
    }

    private void Update()
    {
        if (!isRunning) return;

        motor.Tick();
        currentState?.Tick();
    }

    public void StartBoss()
    {
        if (isRunning) return;

        isRunning = true;
        health.IsVulnerable = true;

        ChangeState(new SeaSerpentBossIdleState(this));
    }

    public void ChangeState(ISeaSerpentBossState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    private void HandleDeath()
    {
        ChangeState(new SeaSerpentBossDeadState(this));
    }

    public Vector3 GetRandomSideTarget()
    {
        if (Random.value < 0.5f)
        {
            return leftAnchor.position;
        }

        return rightAnchor.position;
    }
}

public class SeaSerpentBossIdleState : ISeaSerpentBossState
{
    private readonly SeaSerpentBossController boss;
    private float timer;

    public SeaSerpentBossIdleState(SeaSerpentBossController boss)
    {
        this.boss = boss;
    }

    public void Enter()
    {
        timer = boss.IdleDuration;
        boss.Motor.StartIdle();

        Debug.Log("Boss: Idle");
    }

    public void Tick()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            if (Random.value < boss.SpitChance)
            {
                boss.ChangeState(new SeaSerpentBossSpitState(boss));
                return;
            }

            Vector3 target = boss.GetRandomSideTarget();
            boss.ChangeState(new SeaSerpentBossMoveState(boss, target));
        }
    }

    public void Exit()
    {
        boss.Motor.StopIdle();
    }
}

public class SeaSerpentBossMoveState : ISeaSerpentBossState
{
    private readonly SeaSerpentBossController boss;
    private readonly Vector3 target;

    private float delayTimer = 0.5f;
    private bool hasArrived;

    public SeaSerpentBossMoveState(SeaSerpentBossController boss, Vector3 target)
    {
        this.boss = boss;
        this.target = target;
    }

    public void Enter()
    {
        boss.Motor.MoveTo(target);
        delayTimer = 0.5f;
        hasArrived = false;

        Debug.Log("Boss: Move");
    }

    public void Tick()
    {
        if (!hasArrived)
        {
            if (boss.Motor.IsAtTarget())
            {
                hasArrived = true;
            } else
            {
                return;
            }
        }

        delayTimer -= Time.deltaTime;
        if (delayTimer <= 0f)
        {
            boss.ChangeState(new SeaSerpentBossAttackState(boss));
        }
    }

    public void Exit()
    {
    }
}

public class SeaSerpentBossAttackState : ISeaSerpentBossState
{
    private readonly SeaSerpentBossController boss;
    private bool finished;

    public SeaSerpentBossAttackState(SeaSerpentBossController boss)
    {
        this.boss = boss;
    }

    public void Enter()
    {
        finished = false;
        boss.Attacks.StartBiteAttack(OnAttackComplete);
        Debug.Log("Boss: Bite Attack");
    }

    public void Tick()
    {
        if (finished)
        {
            boss.ChangeState(new SeaSerpentBossIdleState(boss));
        }
    }

    public void Exit()
    {
        boss.Attacks.CancelAttack();
    }

    private void OnAttackComplete()
    {
        finished = true;
    }
}

// Moves to one end of the ship and spits a volley of acid across the deck.
public class SeaSerpentBossSpitState : ISeaSerpentBossState
{
    private readonly SeaSerpentBossController boss;
    private bool finished;

    public SeaSerpentBossSpitState(SeaSerpentBossController boss)
    {
        this.boss = boss;
    }

    public void Enter()
    {
        finished = false;
        boss.Attacks.StartSpitAttack(OnAttackComplete);
        Debug.Log("Boss: Spit Attack");
    }

    public void Tick()
    {
        if (finished)
        {
            boss.ChangeState(new SeaSerpentBossIdleState(boss));
        }
    }

    public void Exit()
    {
        boss.Attacks.CancelAttack();
    }

    private void OnAttackComplete()
    {
        finished = true;
    }
}

public class SeaSerpentBossDeadState : ISeaSerpentBossState
{
    private readonly SeaSerpentBossController boss;

    public SeaSerpentBossDeadState(SeaSerpentBossController boss)
    {
        this.boss = boss;
    }

    public void Enter()
    {
        boss.Motor.StopIdle();
        boss.AnimatorBridge.SetBiteHitboxActive(false);
        Debug.Log("Boss: Dead");
    }

    public void Tick()
    {
    }

    public void Exit()
    {
    }
}