using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MapCellDebugHighlight : MonoBehaviour
{
	public MapDisplay mapDisplay;

	public Material material;

    [SerializeField] private GraphicRaycaster raycaster;
    [SerializeField] private EventSystem eventSystem;

	    // UV-space polygon
    public List<Vector2> polygon;

    [Header("Raycast")]
    [SerializeField] private Camera targetCamera;

    [Header("Texture Info")]
    [SerializeField] private Texture2D sourceTexture;

	bool bActive = false;

    private void OnEnable()
    {
        WorldEvents.OnWorldMapGenerationFinished += HandleWorldGenerated;
    }

    private void OnDisable()
    {
        WorldEvents.OnWorldMapGenerationFinished -= HandleWorldGenerated;
    }

    private void HandleWorldGenerated()
    {
        Debug.Log("PolygonPicker received world generation finished event.");

        bActive = true;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        polygon = new List<Vector2>();

		if (material == null)
		{
			material = mapDisplay.MapUIMaterial;
			Vector4[] verts = new Vector4[32];
			material.SetVectorArray("_Polygon", verts); 
		}

		targetCamera = Camera.main;
    }

	void OnDestroy()
	{
		if (material != null)
		{
			material.SetInt("_VertexCount", 0);

		}
	}

    // Update is called once per frame
    void Update()
    {
		if (!bActive)
			return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryPickPolygon();
        }      
    }

    private void TryPickPolygon()
    {
		polygon.Clear();

		PointerEventData pointerData = new PointerEventData(eventSystem);

        pointerData.position = Mouse.current.position.ReadValue();

        List<RaycastResult> results = new List<RaycastResult>();

        raycaster.Raycast(pointerData, results);

        foreach (RaycastResult result in results)
        {
            Debug.Log("UI Hit: " + result.gameObject.name);

            RawImage rawImage = result.gameObject.GetComponent<RawImage>();

            if (rawImage != null)
            {
                Debug.Log("Clicked RawImage!");

				// UV coordinate on mesh
				Vector2 uv; 
				MapUtils.ScreenToUV(rawImage, result.screenPosition, targetCamera, out uv);

				Debug.Log($"UV: {uv}");

				sourceTexture = mapDisplay.CurrentDisplayTex;

				// Convert UV -> pixel coordinates
				Vector2Int pixelCoord = UVToPixel(uv, sourceTexture);

				Debug.Log($"Pixel: {pixelCoord}");

				// TODO:
				// Replace with your actual polygon lookup implementation
				VCell cell = GetNearestPolygon(pixelCoord);

				if (cell != null)
				{
					Debug.Log($"Selected cell: {cell.ToString()}");

					for (int i = 0; i < cell.Vertices.Length; i++) 
					{
						polygon.Add(MapUtils.PixelToUV(cell.Vertices[i].Coords, sourceTexture));
					}

					HighlightCell();
				}
            }
        }
    }

	private void HighlightCell()
	{
		if (material == null)
			return;

        material.SetInt("_VertexCount", polygon.Count);

        Vector4[] verts = new Vector4[32];

        for (int i = 0; i < polygon.Count; i++)
        {
            verts[i] = new Vector4(
                polygon[i].x,
                polygon[i].y,
                0,
                0
            );
        }

        material.SetVectorArray("_Polygon", verts);  
	}

    private Vector2Int UVToPixel(Vector2 uv, Texture2D tex)
    {
        int x = Mathf.FloorToInt(uv.x * tex.width);
        int y = Mathf.FloorToInt(uv.y * tex.height);

        // Clamp in case UVs land exactly on edge
        x = Mathf.Clamp(x, 0, tex.width - 1);
        y = Mathf.Clamp(y, 0, tex.height - 1);

        return new Vector2Int(x, y);
    }

    // =========================================================
    // Stub for future implementation
    // =========================================================
    private VCell GetNearestPolygon(Vector2Int pixelCoord)
    {
		if (WorldGenerator.WorldGeneratorInstance != null)
		{
			VCell cell = WorldGenerator.WorldGeneratorInstance.GetCellFromUV(pixelCoord);			
			return cell;
		}
        // You will implement this later.
        // Expected behavior:
        // - Search your polygon dataset
        // - Find nearest polygon to pixel coordinate
        // - Return polygon

        return null;
    }
}
