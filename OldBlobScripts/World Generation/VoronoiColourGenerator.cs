using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using TriangleNet.Geometry;
using TriangleNet.Tools;

public static class Poly
{
    public static bool ContainsPoint(Vector2[] polyPoints, Vector2 p)
    {
        var j = polyPoints.Length - 1;
        var inside = false;
        for (int i = 0; i < polyPoints.Length; j = i++)
        {
            var pi = polyPoints[i];
            var pj = polyPoints[j];
            if (((pi.y <= p.y && p.y < pj.y) || (pj.y <= p.y && p.y < pi.y)) &&
                (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y) + pi.x))
                inside = !inside;
        }
        return inside;
    }
}

/*
 * VoronoiColourGenerator maintains a list of layered VoronoiMapGenerators
 * 
 * VoronoiMapGenerators need to be added which will automatically generate
 * the colours array for each map layer.
 * 
 * Goal is that given a very dense voronoi map as a base layer, "combine"
 * the cells together to form different terrain types (i.e water) 
 * based on a combination of the height map and the cell sites on upper
 * layers comprising of "less dense" i.e "larger" cells.
 * 
 */
public class VoronoiColourGenerator
{
    public struct ColourLayer
    {
        public Color[] baseColours;
        public Color[] extraColours;
        public int usedIndex;

        public ColourLayer(int bcSize, int ecSize)
        {
            baseColours = new Color[bcSize];
            extraColours = new Color[ecSize];
            usedIndex = 0;
        }
    }
    // Generally when colouring a world map, our
    // map will be divided into water or land tiles.
    // applying noise or other kinds of terrein
    // is a future problem for future me.
    public List<VoronoiMapGenerator> layeredMapGenerators = new List<VoronoiMapGenerator>();

    VoronoiMapGenerator baseLayer = null;

    public int Width = 0;
    public int Height = 0;

    public List<ColourLayer> cellColourLayers = new List<ColourLayer>();
    public Color[] cellColours;
    public Color[] vMapPixels;

    private Color thisColour;

    // Colour ranges for HSV
    public int maxHue = 360;
    public int minHue = 0;

    public int maxSat = 100;
    public int minSat = 60;

    public int maxBright = 100;
    public int minBright = 60;

    public int hueOffset = 0;

    public bool shuffleColours = true;

    public float minHeight = 0.2f;

    public float[,] heightMap;

    public VoronoiColourGenerator(int Width, int Height)
    {
        this.Width = Width;
        this.Height = Height;
    }

    public void GenerateCellColours()
    {
        // Set Colours based on layering
        SetVCellColoursByMultiHeightThreshold(baseLayer);
    }

    public void GenerateCellColours(HashSet<TileBlob> blobs)
    {
        // Set Colours based on blobs
        SetVCellColoursByCellColours(blobs);
    }

    public void GenerateCombinedLayeredMap()
    {
        ColourizeCells(new List<Vertex>(baseLayer.mesh.Vertices), baseLayer);
        PaintVoronoiCellBorders(1);
    }

    public void InitializeVoronoiCombinedCellColours()
    {
        if (baseLayer == null || baseLayer.voronoi == null)
        {
            UnityEngine.Debug.Log("Error, voronoi diagram is null");
            return;
        }

        // Colour map for the final map texture.
        vMapPixels = new Color[Width * Height];

        // Cell colours array with layering determined
        cellColours = new Color[baseLayer.voronoi.Faces.Count];

        // some default colours
        cellColours = cellColourLayers[0].baseColours;

    }

    /*
     * Insert new map generator layers, order matters and determined by user.
     * 
     * If no base layer exists set it to first map layer.
     * 
     * Automatically generate available colours for each layer.
     * 
     */
    public void SetVoronoiMapGenerators(VoronoiMapGenerator mapGen)
    {
        layeredMapGenerators.Add(mapGen);

        if (baseLayer == null && layeredMapGenerators.Count > 0)
        {
            baseLayer = layeredMapGenerators[0];
        }

        // generate a number of colours equal to the number of estimated cells
        // and a number of spares equal to the total number of squares divided
        // by 10. For ~1000 land sites/cells this is approx. 100, for 100 this
        // is 10 spares.
        int totalFaces = mapGen.voronoi.Faces.Count;
        // clamp spares to 50.
        int spares = Mathf.Max(50, totalFaces / 5);

        ColourLayer colourLayer = new ColourLayer(totalFaces, spares);
        List<Color> generatedColours =
            ShuffleColoursToList(GenerateHSVColoursFromVMap(mapGen, totalFaces + spares));

        // main colours
        colourLayer.baseColours = generatedColours.Take(mapGen.voronoi.Faces.Count).ToArray();
        // spares
        colourLayer.extraColours = generatedColours.Skip(mapGen.voronoi.Faces.Count).ToArray();


        cellColourLayers.Add(colourLayer);
    }

    /*
     * Given a voronoi map generator layer and a x,y coordinate find the corresponding
     * voronoi cell id from that layer.
     * 
     */
    private int FindCellIDFromCoord(VoronoiMapGenerator mapGen, Vector2 coord)
    {
        TriangleNet.Topology.Triangle tri = 
            (TriangleNet.Topology.Triangle)mapGen.triangleQuadTree.Query(coord.x, coord.y);

        int closestID = 0;
        float closestDst = float.MaxValue;
        foreach (Vertex tri_vert in tri.vertices)
        {
            Vector2 mSite = new Vector2(coord.x, coord.y);
            Vector2 vert2Check = new Vector2((float)tri_vert.X, (float)tri_vert.Y);
            float dst = (mSite - vert2Check).sqrMagnitude;

            if (dst < closestDst)
            {
                closestDst = dst;
                closestID = tri_vert.ID;
            }
        }

        return closestID;
    }

    public int GetLayerFromHue(float hue)
    {
        int m_hue = Mathf.FloorToInt(hue * 360);
        for (int i = 0; i < layeredMapGenerators.Count; i++)
        {
            if (m_hue >= layeredMapGenerators[i].minHue && m_hue <= layeredMapGenerators[i].maxHue)
            {
                return i;
            }
        }

        Debug.Log("not poggers");
        return -1;
    }

    /*
     * If height at x,y is < someThreshold then find the corresponding Cell ID
     * on the higher layers in order to "combine" multiple cells together and
     * colour them the same way.
     * 
     * Suppose Layer 1 at coord (64, 64) is 0.1f with threshold < 0.25f; this 
     * would be coloured to the corresponding hue for the Layer 1 colour layer 
     * (instead of what it is for the base layer). 
     * 
     * However if layer 2 has threshold 0.15f instead the colour for that colour
     * index changes to the layer 2 cell colour value at that site location.
     * 
     * This will hopefully combine "deeper" tiles into larger cells (ocean),
     * relative to "sea" tiles relative to land tiles and so on.
     * 
     */
    private void SetVCellColoursByMultiHeightThreshold(VoronoiMapGenerator mapGen)
    {
        int totalPoints = mapGen.voronoi.Faces.Count;

        for (int i = 0; i < totalPoints; i++)
        {
            int Ecks = Mathf.RoundToInt((float)mapGen.voronoi.Faces[i].generator.X);
            int Why = Mathf.RoundToInt((float)mapGen.voronoi.Faces[i].generator.Y);

            if (heightMap != null)
            {

                for (int j = 1; j < layeredMapGenerators.Count; j++)
                {
                    if (heightMap[Ecks, Why] < layeredMapGenerators[j].minHeight)
                    {
                        // Find the ID of the cell in the given above layer, given the coordinates
                        // of the site of the current cell on the main layer.
                        int currentCellID = 
                                FindCellIDFromCoord(layeredMapGenerators[j], new Vector2(Ecks, Why));

                        // debug colour if something goes wrong
                        Color m_colour = new Color(0, 0, 0);
                        if (currentCellID < cellColourLayers[j].baseColours.Length)
                        {
                            m_colour = cellColourLayers[j].baseColours[currentCellID];
                        }

                        cellColours[i] = m_colour;
                    }
                }
            }
        }
    }

    private void SetVCellColoursByCellColours(HashSet<TileBlob> blobs)
    {
        foreach (TileBlob blob in blobs)
        {
            foreach (VoronoiCell cell in blob.GetChildren())
            {
                cellColours[cell.SiteCoord.ID] = cell.GraphData.cellColour;
            }
        }
    }

    private void SetVCellColoursByHeightThreshold(VoronoiMapGenerator mapGen)
    {
        int totalPoints = mapGen.voronoi.Faces.Count;

        cellColours = ShuffleColoursToArray(GenerateHSVColours(totalPoints), totalPoints);

        for (int i = 0; i < totalPoints; i++)
        {
            int Ecks = Mathf.RoundToInt((float)mapGen.voronoi.Faces[i].generator.X);
            int Why = Mathf.RoundToInt((float)mapGen.voronoi.Faces[i].generator.Y);

            if (heightMap != null)
            {
                if (heightMap[Ecks, Why] < minHeight)
                {
                    Color m_colour = new Color(1, 1, 1, 1);
                    cellColours[i] = m_colour;
                }
            }
        }
    }

    List<Color> GenerateHSVColoursFromVMap(VoronoiMapGenerator mapGen, int maxColours)
    {
        int totalPoints = maxColours;

        List<Color> colors = new List<Color>();

        int maxHues = totalPoints;

        (int, int) hueRange = (mapGen.minHue, mapGen.maxHue);
        (int, int) satRange = (mapGen.minSat, mapGen.maxSat);
        (int, int) valRange = (mapGen.minBright, mapGen.maxBright);

        // I recommend introducing some randomness in the generation so you don't end up with a TOO obviously generated result
        float hueJitter = 0.05f;
        float satJitter = 0.05f;
        float valJitter = 0.05f;

        int satCount = 10; // Number of saturation bands to generate.  Must be greater than 1 to avoid a div/0 error.
        int valCount = 10; // Number of value bands to generate.  Must be greater than 1 to avoid a div/0 error.

        // This will generate 1000 colors (10*10*10).  To set (approximate) number of colors directly, use cubic root of n for each.
        int hueCount = 10; // Number of hues to generate. Must be greater than 1 to avoid a div/0 error.

        // making hueCount closer to the number of sites
        // will be slightly more if there is a remainder.
        hueCount = Mathf.CeilToInt((float)totalPoints / (valCount * satCount));

        if (hueCount < 10) hueCount = 10;


        for (int i = 0; i < hueCount; i++)
        {
            for (int j = 0; j < satCount; j++)
            {
                for (int k = 0; k < valCount; k++)
                {
                    var hue = Mathf.Lerp(hueRange.Item1, hueRange.Item2, ((float)i / (hueCount - 1)) + Random.Range(-hueJitter, hueJitter));
                    var sat = Mathf.Lerp(satRange.Item1, satRange.Item2, ((float)j / (satCount - 1)) + Random.Range(-satJitter, satJitter));
                    // This gives a fully linear distribution, however people don't see brightness linearly.  
                    // Recommend using gamma scaling instead.  This is left as an exercise to the reader :)
                    var val =
                        Mathf.Lerp(valRange.Item1,
                                   valRange.Item2,
                                   ((float)k / (valCount - 1)) + Random.Range(-valJitter, valJitter));

                    int hue_rounded = Mathf.FloorToInt(hue);
                    float hue_rounder = val % 1;
                    hue = Utils.Mod(hue_rounded + hueOffset, 360) + hue_rounder;
                    colors.Add(Color.HSVToRGB(hue / 360, sat / 100, val / 100));

                }
            }
        }

        List<Color> temp = new List<Color>();
        float amt = (float)colors.Count / totalPoints;
        for (int i = 0; i < totalPoints; i++)
        {
            int index = Mathf.RoundToInt(i * amt);
            temp.Add(colors[index]);
        }

        return temp;
    }

    List<Color> GenerateHSVColours(int totalPoints)
    {
        List<Color> colors = new List<Color>();

        int maxHues = totalPoints;

        (int, int) hueRange = (minHue, maxHue);
        (int, int) satRange = (minSat, maxSat);
        (int, int) valRange = (minBright, maxBright);

        // I recommend introducing some randomness in the generation so you don't end up with a TOO obviously generated result
        float hueJitter = 0.05f;
        float satJitter = 0.05f;
        float valJitter = 0.05f;

        int satCount = 10; // Number of saturation bands to generate.  Must be greater than 1 to avoid a div/0 error.
        int valCount = 10; // Number of value bands to generate.  Must be greater than 1 to avoid a div/0 error.

        // This will generate 1000 colors (10*10*10).  To set (approximate) number of colors directly, use cubic root of n for each.
        int hueCount = 10; // Number of hues to generate. Must be greater than 1 to avoid a div/0 error.

        // making hueCount closer to the number of sites
        // will be slightly more if there is a remainder.
        hueCount = Mathf.CeilToInt((float)totalPoints / (valCount * satCount));

        if (hueCount < 10) hueCount = 10;


        for (int i = 0; i < hueCount; i++)
        {
            for (int j = 0; j < satCount; j++)
            {
                for (int k = 0; k < valCount; k++)
                {
                    var hue = Mathf.Lerp(hueRange.Item1, hueRange.Item2, ((float)i / (hueCount - 1)) + Random.Range(-hueJitter, hueJitter));
                    var sat = Mathf.Lerp(satRange.Item1, satRange.Item2, ((float)j / (satCount - 1)) + Random.Range(-satJitter, satJitter));
                    // This gives a fully linear distribution, however people don't see brightness linearly.  
                    // Recommend using gamma scaling instead.  This is left as an exercise to the reader :)
                    var val =
                        Mathf.Lerp(valRange.Item1,
                                   valRange.Item2,
                                   ((float)k / (valCount - 1)) + Random.Range(-valJitter, valJitter));

                    int hue_rounded = Mathf.FloorToInt(hue);
                    float hue_rounder = val % 1;
                    hue = Utils.Mod(hue_rounded + hueOffset, 360) + hue_rounder;
                    colors.Add(Color.HSVToRGB(hue / 360, sat / 100, val / 100));
                }
            }
        }

        List<Color> temp = new List<Color>();
        float amt = (float)colors.Count / totalPoints;
        for (int i = 0; i < totalPoints; i++)
        {
            int index = Mathf.RoundToInt(i * amt);
            temp.Add(colors[index]);
        }

        return temp;
    }

    private Color[] ShuffleColoursToArray(List<Color> orderedColours, int maxSize)
    {
        orderedColours.Shuffle();
        return orderedColours.Take(maxSize).ToArray();
    }

    private List<Color> ShuffleColoursToList(List<Color> orderedColours)
    {
        orderedColours.Shuffle();
        return orderedColours;
    }

    public void ColourizeCells(List<Vertex> mapSites, VoronoiMapGenerator mapGen)
    {     

        int VCellCount = mapGen.voronoi.Faces.Count;

        Debug.Log("mapSites: " + mapSites.Count);

        foreach (var face in mapGen.voronoi.Faces)
        {
            // get the ID of the current face
            int currentVSiteID = face.ID;

            thisColour = cellColours[currentVSiteID];

            int x = Mathf.RoundToInt((float)face.generator.X);
            int y = Mathf.RoundToInt((float)face.generator.Y);

            // scanline flood fill
            ConvexScanlineFloodfill(face, x, y, true, true);
        }
    }

    void PaintVoronoiCellBorders(float lineWidth)
    {
        VoronoiMapGenerator map = layeredMapGenerators.FirstOrDefault();
        if (map != null)
        {
            foreach (TriangleNet.Topology.DCEL.Face face in map.voronoi.Faces)
            {
                List<TriangleNet.Topology.DCEL.HalfEdge> edges =
                   new List<TriangleNet.Topology.DCEL.HalfEdge>();

                int currentFaceEcks = Mathf.RoundToInt((float)face.generator.X);
                int currentFaceWhy = Mathf.RoundToInt((float)face.generator.Y);

                int firstEdgeID = face.Edge.ID;
                var tempEdge = face.Edge;

                List<Vector2Int> points = new List<Vector2Int>();

                do
                {
                    edges.Add(tempEdge);

                    tempEdge = tempEdge.Next;
                }
                while (tempEdge != null && firstEdgeID != tempEdge.ID);

                foreach (var edge in edges)
                {                    
                    Vector2Int p0 = new Vector2Int(
                        Mathf.RoundToInt((float)edge.Origin.X), Mathf.RoundToInt((float)edge.Origin.Y));

                    points.Add(p0);                    
                }


                if (currentFaceEcks == 0 || currentFaceWhy == 0 ||
                    currentFaceEcks == Width - 1 || currentFaceWhy == Height - 1)
                {
                    Vector2Int p0 = new Vector2Int(
                        Mathf.RoundToInt((float)edges[edges.Count - 1].Twin.Origin.X), 
                        Mathf.RoundToInt((float)edges[edges.Count - 1].Twin.Origin.Y));

                    points.Add(p0);

                }

                if ((currentFaceEcks == 0 && currentFaceWhy == 0) ||
                    (currentFaceEcks == Width - 1 && currentFaceWhy == Height - 1) ||
                    (currentFaceEcks == 0 && currentFaceWhy == Height - 1) ||
                    (currentFaceEcks == Width - 1 && currentFaceWhy == 0))
                {
                    Vector2Int p0 = new Vector2Int(currentFaceEcks, currentFaceWhy);

                    points.Add(p0);
                }

                for (int i = 0; i < points.Count - 1; i++)
                {
                    DrawLine(points[i], points[i+1], Color.black, vMapPixels, lineWidth);
                }

                DrawLine(points[points.Count - 1], points[0], Color.black, vMapPixels, lineWidth);
            }
        }
    }

    public Texture2D SaveTexture()
    {        
        return SaveVoronoiToTexture();
    }

    Texture2D SaveVoronoiToTexture()
    {
        Texture2D tex = new Texture2D(Width, Height);

        tex.SetPixels(vMapPixels);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();

        string filename = "VoronoiColourGenerator_Output";

        // Encode texture into PNG        
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/Temp/" + filename + ".png", bytes);
        UnityEngine.Debug.Log(bytes.Length / 1024 + "Kb was saved as: " + 
            Application.dataPath + "/Temp/" + filename + ".png");

        return tex;
    }

    private void ConvexScanlineFloodfill(TriangleNet.Topology.DCEL.Face face,
                                            int x, int y, 
                                            bool goUp, bool goDown)
    {
        int minEcks = x;
        int maxEcks = x;

        while (minEcks > 0 && ShouldBeThisColour(face, minEcks - 1, y))
        {
            minEcks--;
        }

        while (maxEcks < Width - 1 && ShouldBeThisColour(face, maxEcks + 1, y))
        {
            maxEcks++;
        }

        int firstPixel = y * Width + minEcks;
        int lastPixel = y * Width + maxEcks;

        for (int pix = firstPixel; pix <= lastPixel; pix++)
        {
            vMapPixels[pix] = thisColour;
        }

        if (goUp && y > 0)
        {
            int nextY = y - 1;
            for (x = minEcks; x <= maxEcks; x++)
            {
                if (ShouldBeThisColour(face, x, nextY))
                {
                    ConvexScanlineFloodfill(face, x, nextY, true, false);
                    // once you've found a point 'up' you can stop looking because it'll all be 
                    // filled by the scanline,
                    // because the shape is convex.
                    break;
                }
            }
        }

        if (goDown && y < Height - 1)
        {
            int NextY = y + 1;
            for (x = minEcks; x <= maxEcks; x++)
            {
                if (ShouldBeThisColour(face, x, NextY))
                {
                    ConvexScanlineFloodfill(face, x, NextY, false, true);
                    return;
                }
            }
        }
    }

    public bool IsPointInPolygon(Point p, List<Point> polygon)
    {
        double minX = polygon[0].X;
        double maxX = polygon[0].X;
        double minY = polygon[0].Y;
        double maxY = polygon[0].Y;
        for (int i = 1; i < polygon.Count; i++)
        {
            Point q = polygon[i];
            minX = Mathf.Min((float)q.X, (float)minX);
            maxX = Mathf.Max((float)q.X, (float)maxX);
            minY = Mathf.Min((float)q.Y, (float)minY);
            maxY = Mathf.Max((float)q.Y, (float)maxY);
        }

        if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY)
        {
            return false;
        }

        // https://wrf.ecse.rpi.edu/Research/Short_Notes/pnpoly.html
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            bool step1 = (polygon[i].Y > p.Y) != (polygon[j].Y > p.Y);
            bool step2 = 
                p.X < (polygon[j].X - polygon[i].X) * (p.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X;

            if (step1 && step2)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private bool ShouldBeThisColour(TriangleNet.Topology.DCEL.Face face, int x, int y)
    {
        List<Vector2> points = new List<Vector2>();

        List<TriangleNet.Topology.DCEL.HalfEdge> edges = 
            new List<TriangleNet.Topology.DCEL.HalfEdge>();

        int currentFaceEcks = Mathf.RoundToInt((float)face.generator.X);
        int currentFaceWhy = Mathf.RoundToInt((float)face.generator.Y);

        int firstEdgeID = face.Edge.ID;
        var tempEdge = face.Edge;

        do
        {
            edges.Add(tempEdge);

            tempEdge = tempEdge.Next;
        }
        while (tempEdge != null && firstEdgeID != tempEdge.ID);

        foreach (var edge in edges)
        {
            points.Add(new Vector2((float)edge.Origin.X, (float)edge.Origin.Y));
        }

        
        if (currentFaceEcks == 0 || currentFaceWhy == 0 ||
            currentFaceEcks == Width - 1 || currentFaceWhy == Height - 1)
        {
            points.Add(new Vector2((float)edges[edges.Count - 1].Twin.Origin.X,
                                   (float)edges[edges.Count - 1].Twin.Origin.Y));
        }

        if ((currentFaceEcks == 0 && currentFaceWhy == 0) ||
            (currentFaceEcks == Width - 1 && currentFaceWhy == Height - 1) ||
            (currentFaceEcks == 0 && currentFaceWhy == Height - 1) ||
            (currentFaceEcks == Width - 1 && currentFaceWhy == 0))
        {
            points.Add(new Vector2(currentFaceEcks, currentFaceWhy));
        }

        return IsInsidePolygon(points.ToArray(), new Vector2(x, y));
    }

    public static float DistancePointLine2D(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        return (ProjectPointLine2D(point, lineStart, lineEnd) - point).magnitude;
    }
    public static Vector2 ProjectPointLine2D(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 rhs = point - lineStart;
        Vector2 vector2 = lineEnd - lineStart;
        float magnitude = vector2.magnitude;
        Vector2 lhs = vector2;
        if (magnitude > 1E-06f)
        {
            lhs = (Vector2)(lhs / magnitude);
        }
        float num2 = Mathf.Clamp(Vector2.Dot(lhs, rhs), 0f, magnitude);
        return (lineStart + ((Vector2)(lhs * num2)));
    }


    public static float ClosestDistanceToPolygon(Vector2[] verts, Vector2 point)
    {
        int nvert = verts.Length;
        int i, j = 0;
        float minDistance = Mathf.Infinity;
        for (i = 0, j = nvert - 1; i < nvert; j = i++)
        {
            float distance = DistancePointLine2D(point, verts[i], verts[j]);
            minDistance = Mathf.Min(minDistance, distance);
        }

        return minDistance;
    }

    public static bool IsInsidePolygon(Vector2[] vertices, Vector2 checkPoint, float margin = 0.01f)
    {
        if (ClosestDistanceToPolygon(vertices, checkPoint) < margin)
        {
            return true;
        }

        float[] vertX = new float[vertices.Length];
        float[] vertY = new float[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertX[i] = vertices[i].x;
            vertY[i] = vertices[i].y;
        }

        return IsInsidePolygon(vertices.Length, vertX, vertY, checkPoint.x, checkPoint.y);
    }

    public static bool IsInsidePolygon(int nvert, float[] vertx, float[] verty, float testx, float testy)
    {
        bool c = false;
        int i, j = 0;
        for (i = 0, j = nvert - 1; i < nvert; j = i++)
        {
            if ((((verty[i] <= testy) && (testy < verty[j])) ||
                 ((verty[j] <= testy) && (testy < verty[i]))) &&
                 (testx < (vertx[j] - vertx[i]) * (testy - verty[i]) / (verty[j] - verty[i]) + vertx[i]))
            {
                c = !c;
            }                
        }
        return c;
    }

    void DrawLine(Vector2Int pointA, Vector2Int pointB, Color colour, Color[] pixelArray, float thickness)
    {
        List<Vector2Int> line = supercover_line(pointA, pointB);
        foreach (Vector2Int c in line)
        {
            DrawCircle(c, Mathf.FloorToInt(thickness / 2), pixelArray, colour);
        }        
    }

    bool IsInMapRange(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    void DrawCircle(Vector2Int c, int r, Color[] pixelArray, Color colour)
    {
        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                if (x * x + y * y <= r * r)
                {
                    int drawX = c.x + x;
                    int drawY = c.y + y;
                    if (IsInMapRange(drawX, drawY))
                    {
                        //y * Width + minEcks
                        pixelArray[drawY * Width + drawX] = colour;
                    }
                }
            }
        }
    }

    int diagonal_distance(Vector2Int p0, Vector2Int p1)
    {
        var dx = p1.x - p0.x;
        var dy = p1.y - p0.y;
        return System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy));
    }

    Vector2Int round_point(Vector2 p)
    {
        return new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
    }

    float lerp(int start, int end, float t)
    {
        return start + t * (end - start);
    }

    Vector2Int lerp_point(Vector2Int p0, Vector2Int p1, float t)
    {
        return new Vector2Int(Mathf.RoundToInt(lerp(p0.x, p1.x, t)),
                              Mathf.RoundToInt(lerp(p0.y, p1.y, t)));
    }

    List<Vector2Int> GetLine2(Vector2Int from, Vector2Int to)
    {
        List<Vector2Int> points = new List<Vector2Int>();
        var N = diagonal_distance(from, to);
        for (int step = 0; step <= N; step++)
        {
            float t = (N == 0) ? 0.0f : (float)step / N;
            points.Add(round_point(lerp_point(from, to, t)));
        }
        return points;
    }

    List<Vector2Int> GetGridWalkLine(Vector2Int p0, Vector2Int p1)
    {
        var dx = p1.x - p0.x;
        var dy = p1.y - p0.y;
        var nx = System.Math.Abs(dx);
        var ny = System.Math.Abs(dy);

        var sign_x = dx > 0 ? 1 : -1; 
        var sign_y = dy > 0 ? 1 : -1;

        Vector2Int p = new Vector2Int(p0.x, p0.y);

        List<Vector2Int> points = new List<Vector2Int>();
        points.Add(p);
        
        for (int ix = 0, iy = 0; ix < nx || iy < ny;)
        {
            if ((0.5f + ix) / nx < (0.5f + iy) / ny)
            {
                // next step is horizontal
                p.x += sign_x;
                ix++;
            }
            else
            {
                // next step is vertical
                p.y += sign_y;
                iy++;
            }
            points.Add(new Vector2Int(p.x, p.y));
        }
        return points;
    }

    List<Vector2Int> supercover_line(Vector2Int p0, Vector2Int p1)
    {
        var dx = p1.x - p0.x;
        var dy = p1.y - p0.y;
        var nx = System.Math.Abs(dx);
        var ny = System.Math.Abs(dy);

        var sign_x = dx > 0 ? 1 : -1;
        var sign_y = dy > 0 ? 1 : -1;

        Vector2Int p = new Vector2Int(p0.x, p0.y);

        List<Vector2Int> points = new List<Vector2Int>();
        points.Add(p);

        for (int ix = 0, iy = 0; ix < nx || iy < ny;)
        {
            //if (Mathf.Approximately((0.5f + ix) / nx, (0.5f + iy) / ny))
            if (((0.5f + ix) / nx) == ((0.5f + iy) / ny))
            {
                // next step is diagonal
                p.x += sign_x;
                p.y += sign_y;
                ix++;
                iy++;
            }
            else if ((0.5f + ix) / nx < (0.5f + iy) / ny)
            {
                // next step is horizontal
                p.x += sign_x;
                ix++;
            }
            else
            {
                // next step is vertical
                p.y += sign_y;
                iy++;
            }
            points.Add(new Vector2Int(p.x, p.y));
        }
        return points;
    }

    List<Vector2Int> GetLine(Vector2Int from, Vector2Int to)
    {
        List<Vector2Int> line = new List<Vector2Int>();

        int x = from.x;
        int y = from.y;

        int dx = to.x - from.x;
        int dy = to.y - from.y;

        bool inverted = false;
        int step = System.Math.Sign(dx);
        int gradientStep = System.Math.Sign(dy);

        int longest = Mathf.Abs(dx);
        int shortest = Mathf.Abs(dy);

        if (longest < shortest)
        {
            inverted = true;
            longest = Mathf.Abs(dy);
            shortest = Mathf.Abs(dx);

            step = System.Math.Sign(dy);
            gradientStep = System.Math.Sign(dx);
        }

        int gradientAccumulation = Mathf.RoundToInt((float)longest / 2);
        for (int i = 0; i < longest; i++)
        {
            line.Add(new Vector2Int(x, y));

            if (inverted)
            {
                y += step;
            }
            else
            {
                x += step;
            }

            gradientAccumulation += shortest;
            if (gradientAccumulation > longest)
            {
                if (inverted)
                {
                    x += gradientStep;
                }
                else
                {
                    y += gradientStep;
                }
                gradientAccumulation -= longest;
            }
        }

        return line;
    }
}
