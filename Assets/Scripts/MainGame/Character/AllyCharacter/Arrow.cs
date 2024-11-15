using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Arrow : NetworkBehaviour
{
    [SerializeField] private Collider2D AttackBox;
    [SerializeField] private float moveSpeed = 20;
    [SerializeField] private float lifeTime = 2;
    [SerializeField] private float damage;

    [Networked] private TickTimer lifeTimeTimer { get; set; }
    [Networked] private bool DidHitSomething { get; set; }

    public override void Spawned()
    {
        lifeTimeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
        DidHitSomething = false;
    }

    public void SetUp(float damage)
    {
        this.damage = damage;
    }

    public override void FixedUpdateNetwork()
    {
        HitSomething();
        if (lifeTimeTimer.ExpiredOrNotRunning(Runner) == false && !DidHitSomething)
        {
            transform.Translate(transform.right * moveSpeed * Runner.DeltaTime, Space.World);
        }

        if (lifeTimeTimer.Expired(Runner) || DidHitSomething)
        {
            Runner.Despawn(Object);
        }
    }

    private void HitSomething()
    {
        Collider2D[] colliders = Physics2D.OverlapBoxAll(AttackBox.bounds.center, AttackBox.bounds.size, 0, LayerMask.GetMask("EnemyCharacter"));
        foreach (Collider2D collider in colliders)
        {
            Character enemy = collider.GetComponent<Character>();
            if (enemy != null && enemy.healthNetworked > 0)
            {
                enemy.TakeDamageNetworked(damage);
                DidHitSomething = true;
                break;
            }
        }
    }
}
