using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugWorldDisplay : MonoBehaviour
{
    public bool DebugMode = false;

    // Stuff
    // World Gen Stuff
    // Voronoi Stuff

    // Start is called before the first frame update
    void Start()
    {
        if (DebugMode)
        {
            // Debug Stuff
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnDrawGizmos()
    {
        //DrawVoronoiCenters();
        //DrawVoronoiVertices();
        //MarkBridgeCells();
        //DrawAllVoronoiEdges();
        //DrawVoronoiEdges(1008);

        //DrawDelaneyConnections();
        //DrawBlobCenters(0.1f);
        //DrawBlobConnections(1.5f);

        //DrawCellEnemyConnections();
        //DrawCellFriendConnections();
    }

    /*
    private void CreateCellIDTextDisplays(TriangleNet.Mesh mesh)
    {


        foreach (Vertex vertex in mesh.Vertices)
        {
            string sID = vertex.ID + "";
            string name = "textDisplay(" + sID + ")";

            int x = Mathf.RoundToInt((float)vertex.X);
            int y = Mathf.RoundToInt((float)vertex.Y);

            string coords = "(" + x + ", " + y + ")";
            GameObject textGO = new GameObject("textDisplay" + vertex.ID);
            MeshRenderer meshRend = textGO.AddComponent<MeshRenderer>();
            Vector3 pos = new Vector3((float)vertex.X, 5, (float)vertex.Y);
            textGO.transform.position = pos;
            textGO.transform.rotation = Quaternion.Euler(90, 0, 0);

            TextMesh tm = textGO.AddComponent<TextMesh>();
            tm.text = sID + ": " + coords;
            tm.color = Color.red;
        }
    }
    */

    /*
    private void DrawCellEnemyConnections()
    {
        if (blobs.Count <= 0) return;

        Gizmos.color = Color.red;
        foreach (TileBlob blob in blobs)
        {
            foreach (VoronoiCell cell in blob.GetChildren())
            {
                foreach (VoronoiCell enemy in cell.GraphData.enemies)
                {
                    Vector3 p0 = new Vector3((float)cell.SiteCoord.X, 0.0f, (float)cell.SiteCoord.Y);
                    Vector3 p1 = new Vector3((float)enemy.SiteCoord.X, 0.0f, (float)enemy.SiteCoord.Y);

                    DrawLine(p0, p1, 1.0f);
                }
            }
        }
    }

    private void DrawCellFriendConnections()
    {
        if (blobs.Count <= 0) return;

        Gizmos.color = Color.green;
        foreach (TileBlob blob in blobs)
        {
            foreach (VoronoiCell cell in blob.GetChildren())
            {
                foreach (VoronoiCell friend in cell.GraphData.friends)
                {
                    Vector3 p0 = new Vector3((float)cell.SiteCoord.X, 0.0f, (float)cell.SiteCoord.Y);
                    Vector3 p1 = new Vector3((float)friend.SiteCoord.X, 0.0f, (float)friend.SiteCoord.Y);

                    DrawLine(p0, p1, 1.0f);
                }
            }
        }
    }

    private void MarkBridgeCells()
    {
        if (nodes.Count <= 0) return;

        if (voronoi == null || voronoi.Vertices.Count <= 0) return;

        foreach (VoronoiCell node in nodes)
        {
            if (node.GraphData.isBranch)
            {
                var face = voronoi.Faces[node.SiteCoord.ID];
                List<TriangleNet.Topology.DCEL.HalfEdge> edges =
                    new List<TriangleNet.Topology.DCEL.HalfEdge>();

                int firstEdgeID = face.Edge.ID;
                var tempEdge = face.Edge;

                do
                {
                    Vector3 pos1 = new Vector3();
                    Vector3 pos2 = new Vector3();

                    int x1 = Mathf.RoundToInt((float)tempEdge.Origin.X);
                    int y1 = Mathf.RoundToInt((float)tempEdge.Origin.Y);
                    pos1 = new Vector3(x1, 0, y1);

                    if (tempEdge.Next == null)
                    {
                        int x2 = Mathf.RoundToInt((float)tempEdge.Twin.Origin.X);
                        int y2 = Mathf.RoundToInt((float)tempEdge.Twin.Origin.Y);
                        pos2 = new Vector3(x2, 0, y2);
                    }
                    else
                    {
                        int x2 = Mathf.RoundToInt((float)tempEdge.Next.Origin.X);
                        int y2 = Mathf.RoundToInt((float)tempEdge.Next.Origin.Y);
                        pos2 = new Vector3(x2, 0, y2);
                    }

                    tempEdge = tempEdge.Next;

                    Gizmos.color = Color.white;

                    DrawLine(pos1, pos2, 0.5f);

                }
                while (tempEdge != null && firstEdgeID != tempEdge.ID);
            }
        }

    }

    private void DrawVoronoiVertices()
    {
        if (voronoi == null || voronoi.Vertices.Count <= 0) return;

        if (mesh == null) return;

        foreach (var vert in voronoi.Vertices)
        {
            int x = Mathf.RoundToInt((float)vert.X);
            int y = Mathf.RoundToInt((float)vert.Y);
            Vector3 pos = new Vector3(x, 0, y);
            Gizmos.color = Color.blue;
            if (drawCubes)
            {
                Gizmos.DrawCube(pos, Vector3.one);
            }
        }
    }

    private void DrawVoronoiCenters()
    {
        if (voronoi == null || voronoi.Vertices.Count <= 0) return;

        if (mesh == null) return;

        foreach (var vert in mesh.Vertices)
        {
            int x = Mathf.RoundToInt((float)vert.X);
            int y = Mathf.RoundToInt((float)vert.Y);
            Vector3 pos = new Vector3(x, 0, y);
            Gizmos.color = Color.red;
            if (drawCubes)
                Gizmos.DrawCube(pos, Vector3.one * 1.5f);
            DrawLabel(pos, vert.ID, Color.cyan);
        }
    }

    private void DrawVoronoiEdges(int id)
    {
        if (voronoi == null || voronoi.Vertices.Count <= 0) return;

        if (mesh == null) return;

        foreach (var face in voronoi.Faces)
        {
            int firstEdgeID = face.Edge.ID;
            var tempEdge = face.Edge;

            do
            {
                if (tempEdge.Next == null)
                {
                    int x1 = Mathf.RoundToInt((float)tempEdge.Origin.X);
                    int y1 = Mathf.RoundToInt((float)tempEdge.Origin.Y);
                    Vector3 pos1 = new Vector3(x1, 0, y1);
                    int x2 = Mathf.RoundToInt((float)tempEdge.Twin.Origin.X);
                    int y2 = Mathf.RoundToInt((float)tempEdge.Twin.Origin.Y);
                    Vector3 pos2 = new Vector3(x2, 0, y2);
                    Gizmos.color = Color.white;

                    DrawLine(pos1, pos2, 0.5f);
                }
                else
                {
                    int x1 = Mathf.RoundToInt((float)tempEdge.Origin.X);
                    int y1 = Mathf.RoundToInt((float)tempEdge.Origin.Y);
                    Vector3 pos1 = new Vector3(x1, 0, y1);
                    int x2 = Mathf.RoundToInt((float)tempEdge.Next.Origin.X);
                    int y2 = Mathf.RoundToInt((float)tempEdge.Next.Origin.Y);
                    Vector3 pos2 = new Vector3(x2, 0, y2);
                    Gizmos.color = Color.white;

                    DrawLine(pos1, pos2, 0.5f);
                }

                tempEdge = tempEdge.Next;
            }
            while (tempEdge != null && firstEdgeID != tempEdge.ID);
        }
    }

    private void DrawAllVoronoiEdges()
    {
        if (voronoi == null || voronoi.Vertices.Count <= 0) return;

        if (mesh == null) return;

        foreach (var edge in voronoi.HalfEdges)
        {
            if (edge.Origin == null || edge.Next == null) continue;

            int x1 = Mathf.RoundToInt((float)edge.Origin.X);
            int y1 = Mathf.RoundToInt((float)edge.Origin.Y);
            Vector3 pos1 = new Vector3(x1, 0, y1);
            int x2 = Mathf.RoundToInt((float)edge.Next.Origin.X);
            int y2 = Mathf.RoundToInt((float)edge.Next.Origin.Y);
            Vector3 pos2 = new Vector3(x2, 0, y2);
            Gizmos.color = Color.black;

            DrawLine(pos1, pos2, 1.0f);
        }
    }

    private void DrawDelaneyConnections()
    {
        if (voronoi == null || voronoi.Vertices.Count <= 0) return;

        if (mesh == null) return;

        foreach (var edge in mesh.Edges)
        {
            if (edge == null || edge.P0 < 0 || edge.P1 < 0) continue;

            int x1 = Mathf.RoundToInt((float)mesh.vertices[edge.P0].X);
            int y1 = Mathf.RoundToInt((float)mesh.vertices[edge.P0].Y);
            Vector3 pos1 = new Vector3(x1, 0, y1);
            int x2 = Mathf.RoundToInt((float)mesh.vertices[edge.P1].X);
            int y2 = Mathf.RoundToInt((float)mesh.vertices[edge.P1].Y);
            Vector3 pos2 = new Vector3(x2, 0, y2);
            Gizmos.color = Color.black;
            DrawLine(pos1, pos2, 0.5f);
        }
    }

    private void DrawLabel(Vector3 pos, int ID, Color colour)
    {
        string sID = ID + "";
        string name = "textDisplay(" + sID + ")";

        string coords = "(" + pos.x + ", " + pos.z + ")";
        string msg = sID + coords;
        DrawString(msg, pos, 25, 25, colour);
    }

    private void DrawSiteLabel(VoronoiCell node, Color colour)
    {
        Vector3 pos = new Vector3((float)node.SiteCoord.X, 5, (float)node.SiteCoord.Y);
        string sID = node.SiteCoord.ID + "";
        string name = "textDisplay(" + sID + ")";

        int x = Mathf.RoundToInt((float)node.SiteCoord.X);
        int y = Mathf.RoundToInt((float)node.SiteCoord.Y);

        string coords = "(" + x + ", " + y + ")";
        string msg = sID + ": \n";
        msg += coords;
        DrawString(msg, pos, 50, 0, colour);
    }

    private void DrawSiteLabels()
    {
        if (nodes.Count <= 0) return;

        // draw node sites
        foreach (var node in nodes)
        {
            Vector3 pos = new Vector3((float)node.SiteCoord.X, 5, (float)node.SiteCoord.Y);
            string sID = node.SiteCoord.ID + "";
            string name = "textDisplay(" + sID + ")";

            int x = Mathf.RoundToInt((float)node.SiteCoord.X);
            int y = Mathf.RoundToInt((float)node.SiteCoord.Y);

            string coords = "(" + x + ", " + y + ")";
            string msg = sID + ": \n";
            msg += coords;
            DrawString(msg, pos, 10, 0, Color.red);
        }
    }

    private void DrawString(string text, Vector3 worldPos, float oX = 0, float oY = 0, Color? colour = null)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.BeginGUI();

        var restoreColor = GUI.color;

        if (colour.HasValue) GUI.color = colour.Value;
        var view = UnityEditor.SceneView.currentDrawingSceneView;

        if (view == null)
        {
            GUI.color = restoreColor;
            UnityEditor.Handles.EndGUI();
            return;
        }

        Vector3 screenPos = view.camera.WorldToScreenPoint(worldPos);

        if (screenPos.y < 0 || screenPos.y > Screen.height || screenPos.x < 0 || screenPos.x > Screen.width || screenPos.z < 0)
        {
            GUI.color = restoreColor;
            UnityEditor.Handles.EndGUI();
            return;
        }

        UnityEditor.Handles.Label(TransformByPixel(worldPos, oX, oY), text);

        GUI.color = restoreColor;
        UnityEditor.Handles.EndGUI();
#endif
    }

    static Vector3 TransformByPixel(Vector3 position, float x, float y)
    {
        return TransformByPixel(position, new Vector3(x, y));
    }

    static Vector3 TransformByPixel(Vector3 position, Vector3 translateBy)
    {
        Camera cam = UnityEditor.SceneView.currentDrawingSceneView.camera;
        if (cam)
            return cam.ScreenToWorldPoint(cam.WorldToScreenPoint(position) + translateBy);
        else
            return position;
    }

    public void DrawBlobCenters(float cubeSize)
    {
        if (blobs.Count <= 0) return;

        foreach (TileBlob blob in blobs)
        {
            Gizmos.color = blob.TileColour;
            Vector3 center_pos = new Vector3(blob.center.x, 0, blob.center.y);
            Gizmos.DrawCube(center_pos, Vector3.one * cubeSize);
            DrawLabel(center_pos, blob.ID, Color.yellow);
        }
    }

    public void DrawNodeConnections(float cubeSize)
    {
        if (nodes.Count <= 0) return;

        // draw node sites
        foreach (var node in nodes)
        {
            Gizmos.color = Color.red;
            foreach (var neighbour in node.Neighbours)
            {
                VoronoiCell nCell = neighbour as VoronoiCell;
                Vector3 p0 = new Vector3((float)node.SiteCoord.X, 0.0f, (float)node.SiteCoord.Y);
                Vector3 p1 = new Vector3((float)nCell.SiteCoord.X, 0.0f, (float)nCell.SiteCoord.Y);

                DrawLine(p0, p1, 1.0f);
            }

            //Color cColour = node.GraphData.isLand ? Color.green : Color.blue;
            Color cColour = node.GraphData.cellColour;
            Gizmos.color = cColour;
            Vector3 pos = new Vector3((float)node.SiteCoord.X, 0, (float)node.SiteCoord.Y);
            Gizmos.DrawCube(pos, Vector3.one * cubeSize);
            //DrawSiteLabel(node, node.GraphData.cellColour);
        }
    }

    public void DrawBlobConnections(float cubeSize)
    {
        if (blobs.Count <= 0) return;

        foreach (TileBlob blob in blobs)
        {
            foreach (TileBlob neighbourBlob in blob.GetNeighbours())
            {
                Gizmos.color = Color.red;

                Vector3 p0 = new Vector3(blob.center.x, 0.0f, blob.center.y);
                Vector3 p1 = new Vector3(neighbourBlob.center.x, 0.0f, neighbourBlob.center.y);
                DrawLine(p0, p1, 1);
            }

            //Gizmos.color = blob.TileColour;
            //Vector3 center_pos = new Vector3(blob.center.x, 0, blob.center.y);
            //Gizmos.DrawCube(center_pos, Vector3.one * cubeSize);
        }
    }

    public void DrawBorderNodes(float cubeSize)
    {
        if (blobs.Count <= 0) return;

        foreach (TileBlob blob in blobs)
        {
            Gizmos.color = blob.TileColour;
            Vector3 center_pos = new Vector3(blob.center.x, 0, blob.center.y);
            Gizmos.DrawCube(center_pos, Vector3.one * cubeSize);

            foreach (VoronoiCell cell in blob.BorderCells)
            {
                //Vector3 pos = new Vector3((float)cell.SiteCoord.X, 0, (float)cell.SiteCoord.Y);
                //Gizmos.DrawCube(pos, Vector3.one * cubeSize);
            }

        }
    }
    */

    /*
    public void DrawNodeSites()
    {
        if (nodes.Count > 0)
        {
            // draw node sites

            foreach (var node in nodes)
            {
                Gizmos.color = Color.red;
                Vector3 pos = new Vector3((float)node.SiteCoord.X, 0, (float)node.SiteCoord.Y);
                Gizmos.DrawCube(pos, Vector3.one * 7.5f);

                //Gizmos.color = Color.black;
                //foreach (var neighbour in node.Neighbours)
                //{
                //    Vector3 p0 = new Vector3((float)node.SiteCoord.X, 0.0f, (float)node.SiteCoord.Y);
                //    Vector3 p1 = 
                //        new Vector3((float)(neighbour as VoronoiCell).SiteCoord.X, 
                //        0.0f, 
                //        (float)(neighbour as VoronoiCell).SiteCoord.Y);

                //    DrawLine(p0, p1, 1.5f);
                //}
            }

            //if (mesh != null)
            //{
            //    Gizmos.color = Color.black;
            //    foreach (Edge edge in mesh.Edges)
            //    {
            //        Vertex v0 = mesh.vertices[edge.P0];
            //        Vertex v1 = mesh.vertices[edge.P1];
            //        Vector3 p0 = new Vector3((float)v0.x, 0.0f, (float)v0.y);
            //        Vector3 p1 = new Vector3((float)v1.x, 0.0f, (float)v1.y);
            //        DrawLine(p0, p1, 2);
            //    }
            //}
        }
    }
    */

    /*
    public static void DrawLine(Vector3 p1, Vector3 p2, float width)
    {
        int count = 1 + Mathf.CeilToInt(width); // how many lines are needed.
        if (count == 1)
        {
            Gizmos.DrawLine(p1, p2);
        }
        else
        {
            Camera c = Camera.current;
            if (c == null)
            {
                Debug.LogError("Camera.current is null");
                return;
            }
            var scp1 = c.WorldToScreenPoint(p1);
            var scp2 = c.WorldToScreenPoint(p2);

            Vector3 v1 = (scp2 - scp1).normalized; // line direction
            Vector3 n = Vector3.Cross(v1, Vector3.forward); // normal vector

            for (int i = 0; i < count; i++)
            {
                Vector3 o = 0.99f * n * width * ((float)i / (count - 1) - 0.5f);
                Vector3 origin = c.ScreenToWorldPoint(scp1 + o);
                Vector3 destiny = c.ScreenToWorldPoint(scp2 + o);
                Gizmos.DrawLine(origin, destiny);
            }
        }
    }
    */
}
