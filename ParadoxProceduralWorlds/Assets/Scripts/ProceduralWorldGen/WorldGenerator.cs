using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    // [todo] noise settings, move to controller later
    public int Height = 512;
    public int Width = 512;
    public int Seed = 1000;
    public int Octaves = 4;
    public float Frequency = 100;
    public float Lacunarity = 2.15f;
    public float Persistence = 0.5f;
    public Vector2 Offsets = new Vector2(0,0);

    // Start is called before the first frame update
    void Start()
    {
        // get height map
    }

    public void GenerateWorld()
    {

    }

    void GetHeightMap()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
