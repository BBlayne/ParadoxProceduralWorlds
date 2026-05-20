using System;
using System.Collections.Generic;
using UnityEngine;

/*
 using UnityEngine;

public class InventoryWindow : UserWidgetBase
{
}
using UnityEngine;

public class TooltipUI : UserWidgetBase
{
}

UIZOrderConfig
 ├── TooltipUI       -> 0
 ├── InventoryWindow -> 50
 ├── PauseMenu       -> 100
 └── BackgroundFX    -> 1000

uiManager.RegisterUI(myInventoryWindow);
uiManager.RegisterUI(myTooltip);
 */


public class WorldGenUIManager : MonoBehaviour
{
	private static WorldGenUIManager m_Instance = null;
	public static WorldGenUIManager Instance
	{
		get { 
			if (m_Instance == null)
			{
				m_Instance = new WorldGenUIManager();
			}
			return m_Instance; 
		}
	}

	void Awake()
	{
		if (m_Instance == null)
		{
			m_Instance = this;
		}
		else if (m_Instance != this)
		{
			Destroy(this);
		}

		DontDestroyOnLoad(this);
	}

    [Header("Root canvas viewport")]
    [SerializeField]
	private static RectTransform m_ViewportRoot;

	[Header("Persistent z-order database")]
    [SerializeField]
    private WorldGenUIConfig m_UIConfig;

	private static Dictionary<Type, List<IUserInterface>> m_Widgets;
	public void Init()
	{
		m_Widgets = new Dictionary<Type, List<IUserInterface>>();
		// Any setup that happens here
	}

	public void SetUIConfig(WorldGenUIConfig config)
	{
		m_UIConfig = config;
	}

	public static void RegisterUserInterface<T>(T instance) where T : class, IUserInterface
	{

		if (!m_Widgets.ContainsKey(typeof(T)))
		{
			m_Widgets[typeof(T)] = new List<IUserInterface>();
			m_Widgets[typeof(T)].Add(instance);
		}
	}

    /// <summary>
    /// Parents a UI element under the viewport and inserts it into the hierarchy
    /// based on zOrder. Lower zOrder values appear in front of higher values.
    /// </summary>
    /// <param name="viewport">Root viewport transform under the canvas.</param>
    /// <param name="uiElement">The UI element to insert.</param>
    /// <param name="zOrder">Desired z-order. Smaller = more in front.</param>
    public void InsertByZOrder(RectTransform viewport, RectTransform uiElement, int zOrder)
    {
        // Parent first while preserving local layout data
        uiElement.SetParent(viewport, false);

        int insertIndex = viewport.childCount - 1;

        // Find first child with a larger z-order
        for (int i = 0; i < viewport.childCount; i++)
        {
            Transform child = m_ViewportRoot.GetChild(i);

            // Skip ourselves
            if (child == uiElement)
                continue;

            UserWidgetBase existingUI = child.GetComponent<UserWidgetBase>();

            if (existingUI == null)
            {
                continue;
            }

			int existingOrder = m_UIConfig.GetZOrder(existingUI.GetType());

            // Larger values go further back
            if (existingOrder > zOrder)
            {
                insertIndex = i;
                break;
            }
        }

        uiElement.SetSiblingIndex(insertIndex);
    }

	private void AddUserInterfaceToViewport(RectTransform widget, int ZOrder)
	{
		InsertByZOrder(m_ViewportRoot, widget, ZOrder);
	}

	public void OnSceneFinishedLoading()
	{
		// display stuff
	}

	void OnDebugMapCellClicked()
	{
		// Display the Debug Menu
		// 
	}
}
