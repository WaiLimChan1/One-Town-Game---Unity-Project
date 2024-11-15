using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Building : NetworkBehaviour, IBeforeUpdate
{
    [Header("Building Components")]
    protected AnimationController AnimationController;
    protected SpriteRenderer SpriteRenderer;
    [SerializeField] protected Collider2D standingHitBox;
    [SerializeField] protected Collider2D destroyedHitBox;

    [Header("Building Sprites")]
    [SerializeField] protected Sprite Standing;
    [SerializeField] protected Sprite Construction;
    [SerializeField] protected Sprite Destroyed;

    [Header("Building Healthbar")]
    [SerializeField] protected Image HealthBarBackground;
    [SerializeField] protected Image HealthBar;
    private float ShowHealthBarTime = 5.0f;
    TickTimer ShowHealthBarTimer;

    [Header("Building Variables")]
    [SerializeField] public float maxHealth;
    [Networked] public float healthNetworked { get; set; }

    public override void Spawned()
    {
        AnimationController = GetComponentInChildren<AnimationController>();
        SpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        healthNetworked = maxHealth;
    }

    void IBeforeUpdate.BeforeUpdate()
    {
        if (healthNetworked > 0)
        {
            standingHitBox.enabled = true;
            destroyedHitBox.enabled = false;
        }
        else
        {
            standingHitBox.enabled = false;
            destroyedHitBox.enabled = true;
        }
    }

    public virtual void TakeDamageNetworked(float damage)
    {
        if (healthNetworked <= 0) return;
            
        AnimationController.SetTrigger("Hurt");
        ShowHealthBarTimer = TickTimer.CreateFromSeconds(Runner, ShowHealthBarTime);
        healthNetworked -= damage;
        if (healthNetworked < 0) healthNetworked = 0;
    }

    public virtual void HealNetworked(float healAmount)
    {
        if (healthNetworked <= 0) return;

        AnimationController.SetTrigger("Heal");
        ShowHealthBarTimer = TickTimer.CreateFromSeconds(Runner, ShowHealthBarTime);
        healthNetworked += healAmount;
        if (healthNetworked > maxHealth) healthNetworked = maxHealth;
    }

    public void InactivateHealthBar()
    {
        HealthBarBackground.gameObject.SetActive(false);
        HealthBar.gameObject.SetActive(false);
    }
    public void ActivateHealthBar()
    {
        HealthBarBackground.gameObject.SetActive(true);
        HealthBar.gameObject.SetActive(true);
    }

    public override void Render()
    {
        //Render sprite
        if (healthNetworked > 0) SpriteRenderer.sprite = Standing;
        else SpriteRenderer.sprite = Destroyed;

        //Render HealthBar
        if (ShowHealthBarTimer.ExpiredOrNotRunning(Runner) || healthNetworked <= 0) InactivateHealthBar();
        else
        {
            ActivateHealthBar();
            HealthBar.fillAmount = healthNetworked / maxHealth;
        }
    }
}
