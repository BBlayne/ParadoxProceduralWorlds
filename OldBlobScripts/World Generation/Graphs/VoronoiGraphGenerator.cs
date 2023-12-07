using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TriangleNet.Geometry;

public class GraphNetwork
{
    public IGraphNode startNode = null; // an arbitrary starting node
    public List<IGraphNode> topNodes = null; // list of all of the topmost nodes
}

public class VoronoiGraphGenerator
{
    public List<VoronoiCell> nodes = new List<VoronoiCell>();

    // vertices/sites that had no outgoing neighbours
    public List<VoronoiCell> null_nodes = new List<VoronoiCell>();

    // ctor, take a delaney mesh and convert it into a node graph
    public VoronoiGraphGenerator(TriangleNet.Mesh delaney_mesh)
    {

        foreach (Vertex site in delaney_mesh.Vertices)
        {
            VoronoiCell vCell = new VoronoiCell(site);
            nodes.Add(vCell);
        }

        foreach (VoronoiCell node in nodes)
        {
            List<Vertex> neighbours = FindAllNeighbourVertices(node.SiteCoord);

            foreach (Vertex neighbour in neighbours)
            {
                node.AddNeighbour(nodes[neighbour.ID]);
            }
        }

        Debug.Log("# of nodes: " + nodes.Count);
    }

    // ctor, take a delaney mesh and convert it into a node graph
    public VoronoiGraphGenerator(TriangleNet.Mesh delaney_mesh, VoronoiColourGenerator colorGenie)
    {

        foreach (Vertex site in delaney_mesh.Vertices)
        {
            VoronoiCell vCell = new VoronoiCell(site);
            nodes.Add(vCell);
        }

        foreach (VoronoiCell node in nodes)
        {
            List<Vertex> neighbours = FindAllNeighbourVertices(node.SiteCoord);

            foreach (Vertex neighbour in neighbours)
            {
                node.AddNeighbour(nodes[neighbour.ID]);
            }

            if (neighbours.Count == 0)
            {
                null_nodes.Add(node);
            }
        }

        // handling sites without outgoing neighbours
        foreach (VoronoiCell null_node in null_nodes)
        {
            List<VoronoiCell> neighbours = new List<VoronoiCell>();
            foreach (VoronoiCell node in nodes)
            {
                // could this be optimized with another foreach+break?
                if (node.GetNeighbours().Contains(null_node))
                {
                    null_node.AddNeighbour(node);
                }
            }
        }

        Debug.Log("# of nodes: " + nodes.Count);
    }

    // Find a node based on its vertex id
    public VoronoiCell GetNodeByVertexID(int ID)
    {
        /*
        VoronoiCell cell = nodes.First(i => i.SiteCoord.ID == ID);
        */
        if (ID >= nodes.Count || ID < 0) return null;

        return nodes[ID];
    }

    private List<Vertex> FindAllNeighbourVertices(Vertex origin)
    {
        List<Vertex> foundVerts = new List<Vertex>();

        Queue<TriangleNet.Topology.Triangle> trianglesToCheck = 
            new Queue<TriangleNet.Topology.Triangle>();

        List<TriangleNet.Topology.Triangle> checkedTriangles = 
            new List<TriangleNet.Topology.Triangle>();

        // The triangle our origin vertice is a part of.
        TriangleNet.Topology.Triangle first_tri = origin.tri.Triangle;

        /*
         * A triangle sometimes is null. I do not know why, but it turns out
         * to be a real vertex and this means it loses all connection to the
         * rest of the graph, so its paramount to reconstruct its neighbours
         * based on which current triangles are currently connected to it.
         */
        if (first_tri == null) {
            Debug.Log("first_tri is null");
            return foundVerts;
        } 

        trianglesToCheck.Enqueue(first_tri);

        while (trianglesToCheck.Count > 0)
        {
            var current_tri = trianglesToCheck.Dequeue();

            //if (current_tri.neighbors.Count() <= 0) continue;
            foreach (var triangleToCheck in current_tri.neighbors)
            {
                if (triangleToCheck.Triangle.ID < 0) continue; // for some reason this exists
                if (checkedTriangles.Any(i => i.ID == triangleToCheck.Triangle.ID) == true) continue;

                if (triangleToCheck.Triangle.GetVertex(0).ID == origin.ID ||
                    triangleToCheck.Triangle.GetVertex(1).ID == origin.ID ||
                    triangleToCheck.Triangle.GetVertex(2).ID == origin.ID)
                {
                    trianglesToCheck.Enqueue(triangleToCheck.Triangle);
                }
            }
            checkedTriangles.Add(current_tri);

            foreach (Vertex vertToAdd in current_tri.vertices)
            {
                if (vertToAdd.ID == origin.ID) continue;

                // Linq
                if (foundVerts.Any(i => i.ID == vertToAdd.ID) == false)
                    foundVerts.Add(vertToAdd);

            }
        }

        return foundVerts;
    }
}
