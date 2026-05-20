using UnityEngine;

/// <summary>
/// Base class for all managed UI elements.
/// </summary>
public abstract class UserWidgetBase : MonoBehaviour
{
    public RectTransform RectTransform => (RectTransform)transform;

    protected WorldGenUIManager UIManager => WorldGenUIManager.Instance;
}
