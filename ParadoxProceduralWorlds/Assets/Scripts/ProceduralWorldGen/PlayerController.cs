using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/*
	Technically this is for using the Procedural World Generator
 */

/// <summary>
/// Simple 2D orthographic camera controller.
///
/// Features:
/// - WASD / Arrow key panning
/// - Middle mouse drag panning
/// - Mouse wheel zoom
/// - Camera clamped to a Canvas RectTransform
///
/// Assumptions:
/// - Orthographic camera
/// - Canvas is Screen Space - Overlay OR Screen Space - Camera
/// - Canvas contains a RectTransform defining the playable/viewable area
/// </summary>
[RequireComponent(typeof(Camera))]
public class PlayerController : MonoBehaviour
{
	[Header("References")]
	[SerializeField]
	private Camera m_MainCamera;

	[SerializeField]private Canvas m_MainCanvas;
	[SerializeField]private RectTransform m_MainCanvasBounds;

    [Header("Movement")]
    [SerializeField] private float keyboardPanSpeed = 10f;
    [SerializeField] private float mousePanSpeed = 1f;

	[Header("Edge Pan")]
	[SerializeField] private float edgePanSpeed = 10f;
	[SerializeField] private float edgePanThreshold = 20f; // pixels from screen edge
	[SerializeField] private bool enableEdgePan = true;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 2f;
    [SerializeField] private float maxZoom = 20f;

    [Header("Bounds")]
    [Tooltip(
        "How far outside the canvas bounds the camera center may move.\n" +
        "0 = camera center can reach the exact edge.\n" +
        "0.5 = half a screen outside.\n" +
        "1 = one full screen outside."
    )]
    [SerializeField]
    [Range(0f, 2f)]
    private float screenPaddingPercent = 0f;

	[Header("Recentering")]
	[SerializeField] private float recenterSpeed = 5f;

	private bool isRecentering;
	private Vector3 recenterTarget;

    [Header("Hotkeys")]
    [Tooltip("Re-center the camera on the bounds center.")]
    [SerializeField] private Key recenterKey = Key.Space;

    private Vector3 lastMousePosition;

    private void Reset()
    {
        m_MainCamera = GetComponent<Camera>();
    }

    private void Awake()
    {
        if (m_MainCamera == null)
		{
			m_MainCamera = GetComponent<Camera>();
		}

        m_MainCamera.orthographic = true;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_MainCamera = GetComponent<Camera>();

		if (m_MainCanvas == null )
		{
			m_MainCanvas = FindFirstObjectByType<Canvas>();
		}
    }

    // Update is called once per frame
    void Update()
    {
        HandleKeyboardPan();
        HandleMousePan();
		HandleEdgePan();
        HandleZoom();
		HandleHotkeys();
		HandleRecentering();

        ClampToBounds();        
    }

    /// <summary>
    /// Handle miscellaneous keyboard hotkeys.
    /// </summary>
    private void HandleHotkeys()
    {
        if (Keyboard.current[recenterKey].wasPressedThisFrame)
        {
            RecenterCameraLerped();
        }
    }

	private void HandleRecentering()
	{
		if (!isRecentering)
			return;

		transform.position = Vector3.Lerp
		(
			transform.position,
			recenterTarget,
			recenterSpeed * Time.deltaTime
		);

		// Stop once sufficiently close
		if (Vector3.Distance(transform.position, recenterTarget) < 0.01f)
		{
			transform.position = recenterTarget;
			isRecentering = false;
		}
	}

    /// <summary>
    /// Moves the camera to the center of the bounds canvas.
    /// </summary>
    public void RecenterCameraInstant()
    {
        if (m_MainCanvasBounds == null)
            return;

        Vector3[] corners = new Vector3[4];
        m_MainCanvasBounds.GetWorldCorners(corners);

        Vector3 center = (corners[0] + corners[2]) * 0.5f;

        transform.position = new Vector3(
            center.x,
            center.y,
            transform.position.z
        );
    }

	public void RecenterCameraLerped()
	{
		if (m_MainCanvasBounds == null)
			return;

		Vector3[] corners = new Vector3[4];
		m_MainCanvasBounds.GetWorldCorners(corners);

		Vector3 center = (corners[0] + corners[2]) * 0.5f;

		recenterTarget = new Vector3(
			center.x,
			center.y,
			transform.position.z
		);

		isRecentering = true;
	}

    private void HandleKeyboardPan()
    {

        Vector2 input = Vector2.zero;

		bool wasPanned = false;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
		{
			wasPanned = true;
			input.y += 1f;
		}            

        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
		{
			wasPanned = true;
            input.y -= 1f;
		}

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
		{
			wasPanned = true;
            input.x -= 1f;
		}

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
		{
			wasPanned = true;
            input.x += 1f;
		}

		if (wasPanned) 
		{
			isRecentering = false;
		}

        Vector3 movement = (Vector3)input.normalized
            * keyboardPanSpeed
            * m_MainCamera.orthographicSize
            * Time.deltaTime;

        transform.position += movement;
    }

    private void HandleMousePan()
    {
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            lastMousePosition = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.middleButton.isPressed)
        {
			isRecentering = false;

            Vector3 currentMousePosition = Mouse.current.position.ReadValue();
            Vector3 delta = currentMousePosition - lastMousePosition;

            // Convert screen movement into world movement
            Vector3 worldDelta = m_MainCamera.ScreenToWorldPoint(Vector3.zero)
                               - m_MainCamera.ScreenToWorldPoint(delta);

            transform.position += worldDelta * mousePanSpeed;

            lastMousePosition = currentMousePosition;
        }
    }

	private void HandleEdgePan()
	{
		if (!enableEdgePan)
			return;

		Vector2 mousePos = Mouse.current.position.ReadValue();
		Vector2 screenSize = new Vector2(Screen.width, Screen.height);

		Vector2 direction = Vector2.zero;

		// Left
		if (mousePos.x <= edgePanThreshold)
			direction.x -= 1f;

		// Right
		if (mousePos.x >= screenSize.x - edgePanThreshold)
			direction.x += 1f;

		// Down
		if (mousePos.y <= edgePanThreshold)
			direction.y -= 1f;

		// Up
		if (mousePos.y >= screenSize.y - edgePanThreshold)
			direction.y += 1f;

		if (direction == Vector2.zero)
			return;

		Vector3 movement = (Vector3)direction.normalized
			* edgePanSpeed
			* m_MainCamera.orthographicSize
			* Time.deltaTime;

		transform.position += movement;
	}

    private void HandleZoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) < 0.01f)
            return;

		isRecentering = false;

        m_MainCamera.orthographicSize -= scroll * zoomSpeed * Time.deltaTime;
        m_MainCamera.orthographicSize = Mathf.Clamp(
            m_MainCamera.orthographicSize,
            minZoom,
            maxZoom
        );
    }

    private void ClampToBounds()
    {
        if (m_MainCanvasBounds == null)
            return;

        Vector3[] corners = new Vector3[4];
		m_MainCanvasBounds.GetWorldCorners(corners);

        float left   = corners[0].x;
        float bottom = corners[0].y;
        float right  = corners[2].x;
        float top    = corners[2].y;

        float camHeight = m_MainCamera.orthographicSize;
        float camWidth = camHeight * m_MainCamera.aspect;

        // Extra movement allowance based on visible screen size
        float horizontalPadding = camWidth * screenPaddingPercent;
        float verticalPadding = camHeight * screenPaddingPercent;

        float minX = left - horizontalPadding;
        float maxX = right + horizontalPadding;

        float minY = bottom - verticalPadding;
        float maxY = top + verticalPadding;

        Vector3 pos = transform.position;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        transform.position = pos;
    }
}
