using UnityEngine;

public class ObjectBackFlip : MonoBehaviour
{
    [SerializeField] private HiddenContainer container;
    [SerializeField] private SpriteRenderer[] targets;

    [SerializeField] private bool[] flipWhenFlat   = new bool[4];
    [SerializeField] private bool[] flipWhenRolled = new bool[4];

    [SerializeField] private bool logDirection;

    private Transform cam;

    void Awake()
    {
        if (container == null) container = GetComponent<HiddenContainer>();
    }

    void Start() => cam = Camera.main.transform;

    void LateUpdate()
    {
        if (cam == null) return;

        Vector3 f = transform.forward; f.y = 0f;
        Vector3 c = cam.forward;       c.y = 0f;

        float angle = Vector3.SignedAngle(c, f, Vector3.up);
        int dir = ((Mathf.RoundToInt(angle / 90f) % 4) + 4) % 4;
        
        bool rolled = container != null && container.IsOpen;
        bool flip = rolled ? flipWhenRolled[dir] : flipWhenFlat[dir];

        foreach (var sr in targets)
            if (sr) sr.flipX = flip;
    }
}