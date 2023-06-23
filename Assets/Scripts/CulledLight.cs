using UnityEngine;

[RequireComponent (typeof (Light))]
public class CulledLight : MonoBehaviour {
    private new Light light;

    [SerializeField]
    private float maxDistance = 50;

    private void Awake () {
        light = GetComponent<Light> ();
    }

    private void LateUpdate () {
        float scaledMaxDistance = QualitySettings.lodBias * maxDistance;
        light.enabled = (transform.position - Camera.main.transform.position).sqrMagnitude < scaledMaxDistance * scaledMaxDistance;
    }
}