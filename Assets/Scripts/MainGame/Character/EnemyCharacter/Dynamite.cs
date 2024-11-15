using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dynamite : NetworkBehaviour
{
    [SerializeField] private DynamiteAnimationController DAC;
    [SerializeField] private Collider2D DynamiteHitBox;
    [SerializeField] private Collider2D ExplosionAttackBox;
    [SerializeField] private float moveSpeed = 5;
    [SerializeField] private float moveDrag = 1;
    [SerializeField] private float detonationTime = 2;
    [SerializeField] private float damage;

    [Networked] private TickTimer detonationTimeTimer { get; set; }
    [Networked] private bool DidHitSomething { get; set; }

    public override void Spawned()
    {
        detonationTimeTimer = TickTimer.CreateFromSeconds(Runner, detonationTime);
        DidHitSomething = false;
    }
    public void Flip(float rotationDegree)
    {
        if (rotationDegree < 90 && rotationDegree > -90) GetComponentInChildren<SpriteRenderer>().flipY = false;
        if (rotationDegree > 90 || rotationDegree < -90) GetComponentInChildren<SpriteRenderer>().flipY = true;
    }

    public void SetUp(float damage, float rotationDegree)
    {
        this.damage = damage;
        Flip(rotationDegree);
    }

    public override void FixedUpdateNetwork()
    {
        HitSomething();
        if (detonationTimeTimer.ExpiredOrNotRunning(Runner) == false && !DidHitSomething)
        {
            //Move
            transform.Translate(transform.right * moveSpeed * Runner.DeltaTime, Space.World);

            //Slow down the dynamite
            moveSpeed -= moveDrag * Runner.DeltaTime;
            if (moveSpeed < 0) moveSpeed = 0;
        }

        if (detonationTimeTimer.Expired(Runner) || DidHitSomething)
        {
            DAC.SetTrigger("Explosion");
        }
    }

    private void HitSomething()
    {
        //Hitting Ally Characters
        Collider2D[] colliders = Physics2D.OverlapBoxAll(DynamiteHitBox.bounds.center, DynamiteHitBox.bounds.size, 0, LayerMask.GetMask("AllyCharacter"));
        foreach (Collider2D collider in colliders)
        {
            Character allyCharacter = collider.GetComponent<Character>();
            if (allyCharacter != null && allyCharacter.healthNetworked > 0)
            {
                DidHitSomething = true;
                return;
            }
        }

        //Hitting Buildings
        colliders = Physics2D.OverlapBoxAll(DynamiteHitBox.bounds.center, DynamiteHitBox.bounds.size, 0, LayerMask.GetMask("Building"));
        foreach (Collider2D collider in colliders)
        {
            Building building = collider.GetComponent<Building>();
            if (building != null && building.healthNetworked > 0)
            {
                DidHitSomething = true;
                return;
            }
        }
    }

    public void AnimationTriggerAttack()
    {
        //Hitting Ally Characters
        Collider2D[] colliders = Physics2D.OverlapBoxAll(ExplosionAttackBox.bounds.center, ExplosionAttackBox.bounds.size, 0, LayerMask.GetMask("AllyCharacter"));
        foreach (Collider2D collider in colliders)
        {
            Character ally = collider.GetComponent<Character>();
            if (ally != null && ally.healthNetworked > 0)
            {
                ally.TakeDamageNetworked(damage);
            }
        }

        //Hitting Buildings
        colliders = Physics2D.OverlapBoxAll(ExplosionAttackBox.bounds.center, ExplosionAttackBox.bounds.size, 0, LayerMask.GetMask("Building"));
        foreach (Collider2D collider in colliders)
        {
            Building building = collider.GetComponent<Building>();
            if (building != null && building.healthNetworked > 0)
            {
                building.TakeDamageNetworked(damage);
            }
        }
    }

    public void TriggerDespawn()
    {
        Runner.Despawn(Object);
    }

}
