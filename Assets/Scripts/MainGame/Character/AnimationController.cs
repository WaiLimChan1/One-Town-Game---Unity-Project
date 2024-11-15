using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    private Character character;
    private void AnimationTriggerAttack() 
    {
        try
        {
            character.AnimationTriggerAttack();
        }
        catch (Exception ex)
        {
            // Handle the exception
            Console.WriteLine($"An exception occurred: {ex.Message}");
        }
    }


    private Animator Animator;

    private void Awake()
    {
        character = GetComponentInParent<Character>();
        Animator = GetComponent<Animator>();
    }

    public void SetTrigger(string Trigger) { Animator.SetTrigger(Trigger); }

    public int GetAnimatorStatus() { return Animator.GetInteger("Status"); }
    public bool AnimationFinished() { return Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.99f; }
    public void Flip(bool isFacingLeft) { GetComponent<SpriteRenderer>().flipX = isFacingLeft; }
    public void ChangeAnimation(int status) { Animator.SetInteger("Status", status); }
    public void GoToAnimationLastFrame() { Animator.Play(Animator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 0.98f); }
    public void RestartAnimation() { Animator.Play(Animator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 0); }
}

