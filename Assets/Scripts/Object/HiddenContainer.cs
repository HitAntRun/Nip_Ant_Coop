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
    
    [Header("Pop")]
    [SerializeField] private float popHeight = 0.5f;
    [SerializeField] private float popDuration = 0.25f;
    private Coroutine popRoutine;
    
    public string Id => containerId;
    public Transform AntAnchor => antAnchor != null ? antAnchor : transform;
    public bool IsOpen { get; private set; }

    private Termite hidden;

    public void ResetState()
    {
        IsOpen = false;
        hidden = null;
        if (body) body.sprite = closedSprite;
        StopPop();
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
            if (popRoutine != null) StopCoroutine(popRoutine);
            popRoutine = StartCoroutine(Pop(hidden.transform));
        }
    }

    public void Close()
    {
        if(!IsOpen) return;
        if (StageFlow.instance != null && StageFlow.instance.IsEnded) return;
        if (hidden != null && hidden.IsDying) return;

        IsOpen = false;
        if (body) body.sprite = closedSprite;
        
        StopPop();
        if(hidden != null) hidden.gameObject.SetActive(false);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }
    
    private IEnumerator Pop(Transform ant)
    {
        ant.localPosition = Vector3.zero;
        float t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / popDuration) * Mathf.PI);
            ant.localPosition = Vector3.up * popHeight * k;
            yield return null;
        }
        ant.localPosition = Vector3.zero;
        popRoutine = null;
    }
    
    private void StopPop()
    {
        if (popRoutine != null) { StopCoroutine(popRoutine); popRoutine = null; }
        if (hidden != null) hidden.transform.localPosition = Vector3.zero;
    }
}
