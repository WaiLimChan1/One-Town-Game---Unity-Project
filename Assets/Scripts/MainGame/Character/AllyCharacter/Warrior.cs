using Fusion;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Windows;

public class Warrior : AllyCharacter
{
    [SerializeField] protected string[] AttackNames = { "Right", "Left", "Down", "Up" };
    [SerializeField] protected BoxCollider2D[] AttackBoxes;

    public override void EndAnimation()
    {
        if (statusNetworked != CharacterStatus.IDLE && statusNetworked != CharacterStatus.RUN)
            if (AnimationController.AnimationFinished())
                statusNetworked = CharacterStatus.IDLE;
    }

    public void DetermineAttackStatusNetworked()
    {
        if (!AttackCoolDownTimer.ExpiredOrNotRunning(Runner)) return;

        int randNum = Random.Range(0, 2);
        if (facingDirectionDegreeNetworked > 45 && facingDirectionDegreeNetworked < 135)
        {
            if (randNum == 0) statusNetworked = CharacterStatus.ATTACK_UP_1;
            else statusNetworked = CharacterStatus.ATTACK_UP_2;
            AttackCoolDownTimer = TickTimer.CreateFromSeconds(Runner, AttackCoolDown);
        }
        else if (facingDirectionDegreeNetworked > -135 && facingDirectionDegreeNetworked < -45)
        {
            if (randNum == 0) statusNetworked = CharacterStatus.ATTACK_DOWN_1;
            else statusNetworked = CharacterStatus.ATTACK_DOWN_2;
            AttackCoolDownTimer = TickTimer.CreateFromSeconds(Runner, AttackCoolDown);
        }
        else
        {
            if (randNum == 0) statusNetworked = CharacterStatus.ATTACK_SIDE_1;
            else statusNetworked = CharacterStatus.ATTACK_SIDE_2;
            AttackCoolDownTimer = TickTimer.CreateFromSeconds(Runner, AttackCoolDown);
        }
    }

    protected override void DetermineStatusNetworked(PlayerData input, NetworkButtons buttonsPrev)
    {
        EndAnimation();

        if (statusNetworked == CharacterStatus.IDLE || statusNetworked == CharacterStatus.RUN)
        {
            statusNetworked = CharacterStatus.IDLE;
            if (input.movementDirection.magnitude > 0) statusNetworked = CharacterStatus.RUN;

            var pressed = input.NetworkButtons.GetPressed(buttonsPrev);
            if (pressed.WasPressed(buttonsPrev, NetworkedPlayer.PlayerInputButtons.LeftMouseButton))
            {
                DetermineAttackStatusNetworked();
            }
        }
    }

    //---------------------------------------------------------------------------------------------
    //Character Take Command Input
    public override void InfluencedByPatrolCommand()
    {
        base.InfluencedByPatrolCommand();
        if (statusNetworked != CharacterStatus.IDLE && statusNetworked != CharacterStatus.RUN) return;

        if (enemyTarget != null)
        {
            float targetDistance = Vector3.Distance(transform.position, enemyTarget.transform.position);
            if (targetDistance > AttackRange) statusNetworked = CharacterStatus.RUN;
            else if (targetDistance <= AttackRange && AttackCoolDownTimer.ExpiredOrNotRunning(Runner)) DetermineAttackStatusNetworked();
            else statusNetworked = CharacterStatus.IDLE;
        }
        else //No target, then just patrol the town
        {
            Patrol();
        }
        
    }

    public override void InfluencedByFollowCommand()
    {
        base.InfluencedByFollowCommand();
        if (statusNetworked != CharacterStatus.IDLE && statusNetworked != CharacterStatus.RUN) return;

        //Handle Nearby Enemies
        if (enemyTarget != null)
        {
            movementDirectionNetworked = (enemyTarget.transform.position - transform.position).normalized;
            facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;

            float targetDistance = Vector3.Distance(transform.position, enemyTarget.transform.position);
            if (targetDistance > AttackRange && targetDistance < AttackRange * 3)
            {
                statusNetworked = CharacterStatus.RUN;
                return;
            }
            if (targetDistance <= AttackRange && AttackCoolDownTimer.ExpiredOrNotRunning(Runner))
            {
                DetermineAttackStatusNetworked();
                return;
            }
        }

        //Follow Target
        if (followTarget != null)
        {
            movementDirectionNetworked = (followTarget.transform.position - transform.position).normalized;
            facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;

            statusNetworked = CharacterStatus.RUN;

            float followTargetDistance = Vector3.Distance(transform.position, followTarget.transform.position);
            if (followTargetDistance < StopFollowDistance) statusNetworked = CharacterStatus.IDLE;
            if (followTargetDistance < MoveOutOfWayDistance)
            {
                statusNetworked = CharacterStatus.RUN;
                movementDirectionNetworked *= -1;
            }
        }
        else
            statusNetworked = CharacterStatus.IDLE;
    }

    public override void InfluencedByHoldPositionCommand()
    {
        base.InfluencedByHoldPositionCommand();
        if (statusNetworked != CharacterStatus.IDLE && statusNetworked != CharacterStatus.RUN) return;

        //Handle Nearby Enemies
        if (enemyTarget != null)
        {
            movementDirectionNetworked = (enemyTarget.transform.position - transform.position).normalized;
            facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;

            float targetDistance = Vector3.Distance(transform.position, enemyTarget.transform.position);
            if (targetDistance > AttackRange && targetDistance < AttackRange * 2)
            {
                statusNetworked = CharacterStatus.RUN;
                return;
            }
            if (targetDistance <= AttackRange && AttackCoolDownTimer.ExpiredOrNotRunning(Runner))
            {
                DetermineAttackStatusNetworked();
                return;
            }
        }

        //Move To Hold Position
        movementDirectionNetworked = (holdPosition - transform.position).normalized;
        facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;

        if (statusNetworked == CharacterStatus.IDLE || statusNetworked == CharacterStatus.RUN)
        {
            statusNetworked = CharacterStatus.RUN;

            float holdPositionDistance = Vector3.Distance(transform.position, holdPosition);
            if (holdPositionDistance < StopMoveHoldDistance) statusNetworked = CharacterStatus.IDLE;
        }

    }
    //---------------------------------------------------------------------------------------------


    public override void AnimationTriggerAttack()
    {
        int index = 0;
        if (statusNetworked == CharacterStatus.ATTACK_SIDE_1 || statusNetworked == CharacterStatus.ATTACK_SIDE_2)
        {
            if (!spriteRenderer.flipX) index = 0;
            if (spriteRenderer.flipX) index = 1;
        }
        else if (statusNetworked == CharacterStatus.ATTACK_DOWN_1 || statusNetworked == CharacterStatus.ATTACK_DOWN_2) index = 2;
        else if (statusNetworked == CharacterStatus.ATTACK_UP_1 || statusNetworked == CharacterStatus.ATTACK_UP_2) index = 3;

        BoxCollider2D attackBox = AttackBoxes[index];

        Collider2D[] colliders = Physics2D.OverlapBoxAll(attackBox.bounds.center, attackBox.bounds.size, 0, LayerMask.GetMask("EnemyCharacter"));
        foreach (Collider2D collider in colliders)
        {
            Character enemy = collider.GetComponent<Character>();
            if (enemy != null && enemy.healthNetworked > 0) enemy.TakeDamageNetworked(damage);
        }
    }
}
