using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

/*
 *	Class for specifying Z-Ordering for dynamically created UI
 *	so they should consistently be rendered in the correct order.
 *	
 *	As unity relies on a hierarchy to determine Z-Ordering, this 
 *	will be handled by the UI Manager via a leaf/node tree where
 *	the desired z-order is a "layer" of the tree, maybe two
 *	parameters for a hard "layer" vs ordering within that layer.
 */
/// <summary>
/// ScriptableObject storing UI type -> z-order mappings.
/// Create via:
/// Assets/Create/UI/UI Z Order Config
/// </summary>
[CreateAssetMenu(
    fileName = "UIZOrderConfig",
    menuName = "UI/UI Z Order Config"
)]
public class WorldGenUIConfig : ScriptableObject
{
	[SerializeField]
    private List<Entry> entries = new();

    private Dictionary<Type, int> lookup;

    [Serializable]
    public class Entry
    {
        [Tooltip("MonoBehaviour type representing this UI element.")]
        [SerializeField]
        private MonoScript script;

        [Tooltip("Lower values render in front.")]
        public int zOrder;

        public Type GetUIType()
        {
            Type type = script != null ? script.GetClass() : null;

			// Only allow UIElement-derived types
			if (type == null || !typeof(UserWidgetBase).IsAssignableFrom(type))
				return null;

			return type;
        }
    }

    private void BuildLookup()
    {
        lookup = new Dictionary<Type, int>();

        foreach (var entry in entries)
        {
            Type type = entry.GetUIType();

            if (type == null)
                continue;

            lookup[type] = entry.zOrder;
        }
    }

    public int GetZOrder(Type type)
    {
        if (lookup == null)
            BuildLookup();

        return lookup.TryGetValue(type, out int value) ? value : 0;
    }

    public int GetZOrder<T>() where T : UserWidgetBase
    {
        return GetZOrder(typeof(T));
    }
}
