using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Torch : EnemyCharacter
{
    [SerializeField] protected string[] AttackNames = { "Right", "Left", "Down", "Up" };
    [SerializeField] protected BoxCollider2D[] AttackBoxes;

    protected override void EndAnimation()
    {
        if (statusNetworked == CharacterStatus.ATTACK_SIDE_1 ||
            statusNetworked == CharacterStatus.ATTACK_DOWN_1 ||
            statusNetworked == CharacterStatus.ATTACK_UP_1)
            if (AnimationController.AnimationFinished())
                statusNetworked = CharacterStatus.IDLE;
    }

    protected override void DetermineStatus()
    {
        EndAnimation();
        if (statusNetworked == CharacterStatus.ATTACK_SIDE_1 ||
            statusNetworked == CharacterStatus.ATTACK_DOWN_1 ||
            statusNetworked == CharacterStatus.ATTACK_UP_1) 
            return;

        if (target != null)
        {
            //Calculate target distance differently for AllyCharacter and building
            float targetDistance = 0;

            //Calculate target distance for AllyCharacter
            AllyCharacter ally = target.GetComponent<AllyCharacter>();
            if (ally != null)
                targetDistance = Vector3.Distance(transform.position, target.transform.position);

            //Calculate target distance for building
            Building building = target.GetComponent<Building>();
            if (building != null)
            {
                Vector2 origin = transform.position;
                Vector2 direction = movementDirectionNetworked;
                RaycastHit2D hit = Physics2D.Raycast(origin, direction, SearchRadius, LayerMask.GetMask("Building"));
                if (hit.collider != null && hit.collider.gameObject == target) targetDistance = hit.distance + AttackRange / 2;
                else targetDistance = SearchRadius;
            }

            //Determine status based on target distance
            if (targetDistance > AttackRange)
                statusNetworked = CharacterStatus.RUN;
            else if (targetDistance <= AttackRange && AttackCoolDownTimer.ExpiredOrNotRunning(Runner))
            {
                if (facingDirectionDegreeNetworked > 45 && facingDirectionDegreeNetworked < 135)
                {
                    statusNetworked = CharacterStatus.ATTACK_UP_1;
                    AttackCoolDownTimer = TickTimer.CreateFromSeconds(Runner, AttackCoolDown);
                }
                else if (facingDirectionDegreeNetworked > -135 && facingDirectionDegreeNetworked < -45)
                {
                    statusNetworked = CharacterStatus.ATTACK_DOWN_1;
                    AttackCoolDownTimer = TickTimer.CreateFromSeconds(Runner, AttackCoolDown);
                }
                else
                {
                    statusNetworked = CharacterStatus.ATTACK_SIDE_1;
                    AttackCoolDownTimer = TickTimer.CreateFromSeconds(Runner, AttackCoolDown);
                }
            }
            else statusNetworked = CharacterStatus.IDLE;
        }
        else //Move towards TownHall
        {
            movementDirectionNetworked = (EnemyCharacter.TownHallPosition - transform.position).normalized;
            facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;
            statusNetworked = CharacterStatus.RUN;
        }
    }

    public override void AnimationTriggerAttack()
    {
        int index = 0;
        if (statusNetworked == CharacterStatus.ATTACK_SIDE_1)
        {
            if (!spriteRenderer.flipX) index = 0;
            if (spriteRenderer.flipX) index = 1;
        }
        else if (statusNetworked == CharacterStatus.ATTACK_DOWN_1) index = 2;
        else if (statusNetworked == CharacterStatus.ATTACK_UP_1) index = 3;

        BoxCollider2D attackBox = AttackBoxes[index];

        //Hitting Ally Character
        Collider2D[] colliders = Physics2D.OverlapBoxAll(attackBox.bounds.center, attackBox.bounds.size, 0, LayerMask.GetMask("AllyCharacter"));
        foreach (Collider2D collider in colliders)
        {
            Character allyCharacter = collider.GetComponent<Character>();
            if (allyCharacter != null && allyCharacter.healthNetworked > 0) allyCharacter.TakeDamageNetworked(damage);
        }

        //Hitting Buildings
        colliders = Physics2D.OverlapBoxAll(attackBox.bounds.center, attackBox.bounds.size, 0, LayerMask.GetMask("Building"));
        foreach (Collider2D collider in colliders)
        {
            Building building = collider.GetComponent<Building>();
            if (building != null && building.healthNetworked > 0) building.TakeDamageNetworked(damage);
        }
    }
}