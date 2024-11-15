using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class DynamiteAnimationController : MonoBehaviour
{
    private Dynamite dynamite;
    private Animator Animator;

    private void AnimationTriggerAttack() { dynamite.AnimationTriggerAttack(); }
    private void AnimationTriggerDespawn() { dynamite.TriggerDespawn(); }

    private void Awake()
    {
        dynamite = GetComponentInParent<Dynamite>();
        Animator = GetComponent<Animator>();
    }

    public void SetTrigger(string Trigger) { Animator.SetTrigger(Trigger); }
}
