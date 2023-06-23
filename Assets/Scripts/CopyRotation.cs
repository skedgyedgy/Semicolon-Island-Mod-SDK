using UnityEngine;

public class CopyRotation : MonoBehaviour {
    [SerializeField]
    private Transform copyFrom;

    private void LateUpdate () {
        transform.rotation = copyFrom.rotation;
    }
}