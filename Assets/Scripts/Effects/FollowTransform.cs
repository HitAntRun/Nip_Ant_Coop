using UnityEngine;

public class FollowTransform : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private bool followRotation = true;

    private Vector3 localOffset;
    private Quaternion localRot;

    void Start()
    {
        if (target == null) return;
        localOffset = target.InverseTransformPoint(transform.position);
        localRot    = Quaternion.Inverse(target.rotation) * transform.rotation;
    }

    void LateUpdate()
    {
        if (target == null) return;
        transform.position = target.TransformPoint(localOffset);
        if (followRotation) transform.rotation = target.rotation * localRot;
    }
}