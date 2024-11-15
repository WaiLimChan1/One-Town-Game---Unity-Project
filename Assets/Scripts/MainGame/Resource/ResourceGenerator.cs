using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResourceGenerator : NetworkBehaviour
{
    [Header("ResourceGenerator Components")]
    protected AnimationController AnimationController;
    protected SpriteRenderer SpriteRenderer;

    public enum ResourceType { GOLD, WOOD, MEAT };
    public ResourceType resourceType;

    [Header("Building Sprites")]
    [SerializeField] protected Sprite Active;
    [SerializeField] protected Sprite Inactive;
    [SerializeField] protected Sprite Destroyed;

    [Header("Resource Generator's Remaining Resource")]
    [SerializeField] protected Image resourceBarBackground;
    [SerializeField] protected Image resourceBar;
    private float ShowResourceBarTime = 5.0f;
    [Networked] TickTimer ShowResourceBarTimer { get; set; }

    [SerializeField] public int maxResourceCount;
    [Networked] public int resourceCount { get; set; }

    public override void Spawned()
    {
        AnimationController = GetComponentInChildren<AnimationController>();
        SpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        resourceCount = maxResourceCount;
    }

    public virtual void LoseResource()
    {
        if (resourceCount <= 0) return;

        AnimationController.SetTrigger("Hurt");
        ShowResourceBarTimer = TickTimer.CreateFromSeconds(Runner, ShowResourceBarTime);
        resourceCount--;
        if (resourceCount < 0) resourceCount = 0;
    }

    public void InactivateResourceBar()
    {
        resourceBarBackground.gameObject.SetActive(false);
        resourceBar.gameObject.SetActive(false);
    }
    public void ActivateResourceBar()
    {
        resourceBarBackground.gameObject.SetActive(true);
        resourceBar.gameObject.SetActive(true);
    }

    public override void Render()
    {
        //Render sprite
        if (resourceCount > 0) SpriteRenderer.sprite = Active;
        else SpriteRenderer.sprite = Inactive;

        //Render HealthBar
        if (ShowResourceBarTimer.ExpiredOrNotRunning(Runner)) InactivateResourceBar();
        else
        {
            ActivateResourceBar();
            resourceBar.fillAmount = ((float) resourceCount) / maxResourceCount;
        }
    }
}
