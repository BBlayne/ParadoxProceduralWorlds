using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoronoiCellGraphGenerator
{
    public class VoronoiGraph
    {
        public int CountOfCells = 0;
        public int ID = 0;
        public bool isStable = false;

        public List<VoronoiCell> Cells = new List<VoronoiCell>();

        public bool CheckIfStable()
        {
            return false;
        }
    }

    Dictionary<int, VoronoiGraph> cellGraphs = new Dictionary<int, VoronoiGraph>();


}
