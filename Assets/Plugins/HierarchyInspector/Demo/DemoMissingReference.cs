using UnityEngine;

/// <summary>
/// Demo MonoBehaviour with a public Transform reference. Used in the demo scene
/// to show the Missing Reference Indicator: when the field is left null, the
/// hierarchy row gets the indicator marker.
/// </summary>
[AddComponentMenu("Hierarchy Decorator/Demo/Missing Reference Target")]
public class DemoMissingReference : MonoBehaviour
{
    [Tooltip("Intentionally left null in the demo scene to surface the missing-reference indicator.")]
    public Transform target;
}
