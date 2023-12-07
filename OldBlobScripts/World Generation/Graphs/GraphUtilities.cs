using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class GraphUtilities
{
    public static bool CheckIfBlobIsAdjacent(VoronoiCell origin, TileBlob destination)
    {
        HashSet<VoronoiCell> closed_nodes = new HashSet<VoronoiCell>();

        List<VoronoiCell> open_nodes = new List<VoronoiCell>();
        open_nodes.Add(origin);

        while (open_nodes.Count > 0)
        {
            VoronoiCell current = open_nodes.First();

            for (int i = 1; i < open_nodes.Count; i++)
            {
                if (open_nodes[i].fCost < current.fCost || open_nodes[i].fCost == current.fCost)
                {
                    if (open_nodes[i].hCost < current.hCost)
                        current = open_nodes[i];
                }
            }

            open_nodes.Remove(current);
            closed_nodes.Add(current);

            if (current.parent.ID == destination.ID)
            {
                // clean up
                foreach (var node in closed_nodes)
                {
                    node.gCost = 0;
                    node.hCost = 0;
                }

                foreach (var node in open_nodes)
                {
                    node.gCost = 0;
                    node.hCost = 0;
                }

                // retrace path or just return truce
                return true;
            }

            foreach (VoronoiCell neighbour in current.GetNeighbours())
            {
                if (neighbour.parent.ID != current.parent.ID &&
                    neighbour.parent.ID != destination.ID)
                {
                    continue;
                }

                float newCostToNeighbour = current.gCost + GetDistance(current, destination);
                if (newCostToNeighbour < neighbour.gCost || open_nodes.Contains(neighbour) == false)
                {
                    neighbour.gCost = newCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, destination);
                    
                    if (open_nodes.Contains(neighbour) == false && 
                        closed_nodes.Contains(neighbour) == false)
                    {
                        open_nodes.Add(neighbour);
                    }
                }
            }
        }

        return false;
    }

    public static float GetDistance(VoronoiCell nodeA, VoronoiCell nodeB)
    {
        Vector2 nodeAPos = new Vector2((float)nodeA.SiteCoord.X, (float)nodeA.SiteCoord.Y);
        Vector2 nodeBPos = new Vector2((float)nodeB.SiteCoord.X, (float)nodeB.SiteCoord.Y);

        return Vector2.Distance(nodeAPos, nodeBPos);
    }

    public static float GetDistance(VoronoiCell nodeA, TileBlob nodeB)
    {
        Vector2 nodeAPos = new Vector2((float)nodeA.SiteCoord.X, (float)nodeA.SiteCoord.Y);
        Vector2 nodeBPos = new Vector2(nodeB.center.x, nodeB.center.y);

        return Vector2.Distance(nodeAPos, nodeBPos);
    }

    public static float GetDistance(TileBlob nodeA, TileBlob nodeB)
    {
        Vector2 nodeAPos = new Vector2(nodeA.center.x, nodeA.center.y);
        Vector2 nodeBPos = new Vector2(nodeB.center.x, nodeB.center.y);

        return Vector2.Distance(nodeAPos, nodeBPos);
    }

    public static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        int polygonLength = polygon.Length, i = 0;
        bool inside = false;
        // x, y for tested point.
        float pointX = point.x, pointY = point.y;
        // start / end point for the current polygon segment.
        float startX, startY, endX, endY;
        Vector2 endPoint = polygon[polygonLength - 1];
        endX = endPoint.x;
        endY = endPoint.y;
        while (i < polygonLength)
        {
            startX = endX; startY = endY;
            endPoint = polygon[i++];
            endX = endPoint.x; endY = endPoint.y;
            //
            inside ^= (endY > pointY ^ startY > pointY) /* ? pointY inside [startY;endY] segment ? */
                      && /* if so, test if it is under the segment */
                      ((pointX - endX) < (pointY - endY) * (startX - endX) / (startY - endY));
        }
        return inside;
    }
}
