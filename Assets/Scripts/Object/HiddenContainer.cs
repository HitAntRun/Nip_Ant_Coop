using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HiddenContainer : MonoBehaviour
{
    [SerializeField] private string containerId;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;
    [SerializeField] private Transform antAnchor;
    [SerializeField] private Collider clickCollider;

    public string Id => containerId;
    public Transform AntAnchor => antAnchor != null ? antAnchor : transform;
    public bool IsOpen { get; private set; }

    private Termite hidden;

    public void ResetState()
    {
        IsOpen = false;
        hidden = null;
        if (body) body.sprite = closedSprite;
        if (clickCollider) clickCollider.enabled = true;
    }

    public void SetHidden(Termite t)
    {
        hidden = t;
        t.gameObject.SetActive(false);
    }

    public void Open()
    {
        if (IsOpen) return;
        if (StageFlow.instance != null && StageFlow.instance.IsEnded) return;

        IsOpen = true;
        if (body) body.sprite = openSprite;

        if (hidden != null)
        {
            hidden.gameObject.SetActive(true);
        }
    }

    public void Close()
    {
        if(!IsOpen) return;
        if (StageFlow.instance != null && StageFlow.instance.IsEnded) return;
        if (hidden != null && hidden.IsDying) return;

        IsOpen = false;
        if (body) body.sprite = closedSprite;
        
        if(hidden != null) hidden.gameObject.SetActive(false);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }
}
