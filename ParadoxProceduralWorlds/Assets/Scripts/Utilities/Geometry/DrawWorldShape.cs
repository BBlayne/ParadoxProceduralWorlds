using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DrawWorldShape
{
    // returns list of points to colour to form a line
    // could implement the different kinds of lines but 
    // this one is Supercover Line algorithm
    public static List<Coord> DrawLine(Coord p0, Coord p1)
    {
        var dx = p1.x - p0.x;
        var dy = p1.y - p0.y;
        var nx = System.Math.Abs(dx);
        var ny = System.Math.Abs(dy);

        var sign_x = dx > 0 ? 1 : -1;
        var sign_y = dy > 0 ? 1 : -1;

        Coord p = new Coord(p0.x, p0.y);

        List<Coord> points = new List<Coord>();
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
            points.Add(new Coord(p.x, p.y));
        }
        return points;
    }

    public static List<Vector3> DrawLine2D(Vector3 p0, Vector3 p1)
    {
        var dx = p1.x - p0.x;
        var dy = p1.y - p0.y;
        var nx = System.Math.Abs(dx);
        var ny = System.Math.Abs(dy);

        var sign_x = dx > 0 ? 1 : -1;
        var sign_y = dy > 0 ? 1 : -1;

        Vector3 p = new Vector3(p0.x, p0.y, 0);

        List<Vector3> points = new List<Vector3>();
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
            points.Add(new Vector3(p.x, p.y, 0));
        }
        return points;
    }
}
