using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TriangleNet.Geometry;

public class VoronoiBlobGenerator
{
    TriangleNet.Mesh mesh;
    public VoronoiGraphGenerator voronoiGraphGenie = null;
    public HashSet<TileBlob> Blobs = null;
    public VoronoiColourGenerator vColGenie = null;

    public VoronoiBlobGenerator(TriangleNet.Mesh _mesh, VoronoiColourGenerator _vColGenie)
    {
        mesh = _mesh;
        vColGenie = _vColGenie;
        voronoiGraphGenie = new VoronoiGraphGenerator(mesh, _vColGenie);

        Blobs = new HashSet<TileBlob>();

        HashSet<Color> usedColours = new HashSet<Color>();

        List<Color> tileColours = new List<Color>(vColGenie.cellColours);

        for(int i = 0; i < tileColours.Count; i++)
        {
            Color col = tileColours[i];


            // set the colour of the corresponding voronoi cell to its map colour
            // the index of a "colour" of a cell should correspond to its voronoi cell
            // and delaney site id and thus its voronoi graph cell.
            VoronoiCell cell = voronoiGraphGenie.GetNodeByVertexID(i);

            // determine if it is a land tile based on colour range
            float h = 0, s = 0, v = 0;
            Color.RGBToHSV(col, out h, out s, out v);
            int Ecks = Mathf.RoundToInt((float)cell.SiteCoord.X);
            int Why = Mathf.RoundToInt((float)cell.SiteCoord.Y);
            int layer = _vColGenie.GetLayerFromHue(h);
            if (layer < 0)
            {
                Debug.Log("Layer is 0, something is wrong");
                layer = 0;
            }

            bool isLand =
                _vColGenie.heightMap[Ecks, Why] >= _vColGenie.layeredMapGenerators[layer].minHeight;

            cell.GraphData.cellColour = col;
            cell.GraphData.isLand = isLand;

            // check if colour doesn't exist in blobs
            if (usedColours.Contains(col) == false)
            {
                TileBlob nuBlob = new TileBlob(col);
                nuBlob.GraphData.isLand = cell.GraphData.isLand;
                nuBlob.GraphData.LayerID = 
                    cell.GraphData.isLand ? TileLayers.LAND : TileLayers.WATER;
                nuBlob.AddChildNode(cell);
                Blobs.Add(nuBlob);
                usedColours.Add(col);
            }
            else
            {
                // find first blob with matching color and insert new child node
                TileBlob blob = Blobs.First(e => e.TileColour.Equals(col));                
                blob.AddChildNode(cell);
                // todo consider adding a means of predetermining neighbours that belong to the blob
            }
        }

        // I think all blobs now should have all child nodes (which should already be linked
        // to each other). 
        // linking blobs together remains but is this required?
        // Connecting Blobs to Adjacent Blobs
        foreach (TileBlob _blob in Blobs)
        {
            foreach (VoronoiCell _cell in _blob.GetChildren())
            {
                bool isBorderCell = false;
                foreach (VoronoiCell neighbour in _cell.GetNeighbours())
                {
                    if (_blob.GraphData.isLand)
                    {                                             
                        if (neighbour.parent.GraphData.isLand)
                        {
                            _cell.GraphData.landCellNeighbours.Add(neighbour);
                            if (_blob.Equals(neighbour.parent))
                            {
                                _cell.GraphData.friends.Add(neighbour);
                            }
                            else
                            {
                                isBorderCell = true;
                                _blob.AddNeighbour(neighbour.parent);
                                _cell.GraphData.enemies.Add(neighbour);
                            }
                            
                        }
                        else
                        {
                            _blob.AddNeighbour(neighbour.parent);
                            _cell.GraphData.seaCellNeighbours.Add(neighbour);
                            _cell.GraphData.enemies.Add(neighbour);
                        }
                    }
                    else
                    {
                        if (neighbour.parent.GraphData.isLand)
                        {
                            isBorderCell = true;
                            _blob.AddNeighbour(neighbour.parent);
                            _cell.GraphData.landCellNeighbours.Add(neighbour);
                            _cell.GraphData.enemies.Add(neighbour);
                        }
                        else
                        {
                            _cell.GraphData.seaCellNeighbours.Add(neighbour);
                            if (_blob.Equals(neighbour.parent))
                            {
                                _cell.GraphData.friends.Add(neighbour);
                            }
                            else
                            {
                                isBorderCell = true;
                                _cell.GraphData.enemies.Add(neighbour);
                                _blob.AddNeighbour(neighbour.parent);
                            }                            
                        }
                    }
                }
                // add cell to border cell since it probably is on the border
                if (isBorderCell)
                {
                    _blob.BorderCells.Add(_cell);
                }

            }

            _blob.RecalculateCenter();
        }

        Debug.Log("Conditioning blobs...");

        int max_it = 4;
        int count = 0;

        HashSet<TileBlob> affectedBlobs = new HashSet<TileBlob>();
        affectedBlobs = SearchForAndFixBlobs(Blobs);
        do
        {
            affectedBlobs = SearchForAndFixBlobs(affectedBlobs);
            count++;
        }
        while (affectedBlobs.Count > 0 && count < max_it);

        if (count >= max_it)
        {
            Debug.Log("Exceeded max iterations...");
        }
        Debug.Log("Completed " + (count + 1) + " iterations of conditioning...");

        // condition the resulting blob graph for correctness/edge cases
    }

    /*
     * The following are the primary edge cases that arise from
     * the current voronoi based map generation algorithm.
     * 
     * 1. A blob contains a partial hole. i.e there exist one or
     * more land cells inside the area of the sea blob that causes
     * a section of the blob to 'jut out' and have a 'bridge' 
     * separating two or more sections of the blob.
     * 
     * 2. A blob is entirely separated, resulting in two 'islands' 
     * of blobs separated by land blobs.
     * 
     * 3. A blob is too small, a sea blob being merely 1 cell.
     * 
     * 4. Sometimes blobs are too concave, or have protrusions
     * that make it too concave. 
     */
    private HashSet<TileBlob> SearchForAndFixBlobs(HashSet<TileBlob> blobs2Check)
    {
        Debug.Log("Searching for tiles to condition...");
        HashSet<TileBlob> smallBlobsForPruning = new HashSet<TileBlob>();
        HashSet<TileBlob> AddedBlobs = new HashSet<TileBlob>();
        HashSet<TileBlob> affectedBlobs = new HashSet<TileBlob>();
        foreach (TileBlob blob in blobs2Check)
        {
            // too small && is water
            if (blob.GetChildren().Count == 1 && blob.GraphData.isLand == false)
            {                
                // add all too small blobs to be redistributed later based on "bridges".
                // but check to make sure it isn't a lake
                bool isLake = true;
                foreach (TileBlob neighbouringBlob in blob.GetNeighbours())
                {
                    if (neighbouringBlob.GraphData.isLand == false)
                    {
                        isLake = false;
                        break;
                    }
                }

                if (isLake == false) {
                    Debug.Log("Water tile is too small: " + blob.ID);
                    smallBlobsForPruning.Add(blob);
                }
                else
                {
                    Debug.Log("Water tile is a lake... Skipping: " + blob.ID);
                }

            }
            else if (blob.GetChildren().Count > 1 && blob.GraphData.isLand == false)
            {
                // first search to see if the blob is one piece
                List<VoronoiCell> disjointedCells = CheckIfUnitary(blob);

                if (disjointedCells.Count > 0)
                {
                    Debug.Log("Water tile is disjointed with: " + 
                        disjointedCells.Count + " disjointed cells, splitting...");
                    // handle this
                    // possibly by splitting the blob into new blobs (need additional colours)
                    // and re-connecting the blob to its neighbours.
                    AddedBlobs.Add(SplitBlob(blob, new HashSet<VoronoiCell>(disjointedCells)));
                    blob.RecalculateCenter();
                }

                Debug.Log("Checking water tile for branches...");
                // blob may have more than one branch
                List<BlobLimb> limbs = SearchForBridges(blob);
                Debug.Log(limbs.Count + " branches found, recolouring...");
                List<BlobLimb> skippedLimbs = new List<BlobLimb>();
                List<BlobLimb> resolvedLimbs = new List<BlobLimb>();
                List<TileBlob> limblobs = new List<TileBlob>();



                foreach (BlobLimb limb in limbs)
                {
                    if (limb.cells.Count == limb.mBlob.GetChildren().Count)
                    {
                        /*
                        foreach (VoronoiCell cell in limb.cells)
                        {
                            cell.GraphData.isBranch = false;
                        }
                        limb.cells.Clear();
                        */
                        skippedLimbs.Add(limb);
                        continue; // skip
                    }
                    else if (limb.cells.Count > 1)
                    {
                        // split
                        TileBlob splitBlob = 
                            SplitBlob(limb.mBlob, new HashSet<VoronoiCell>(limb.cells));

                        AddedBlobs.Add(splitBlob);
                        limb.mBlob.RecalculateCenter();
                        limblobs.Add(splitBlob);
                        /*
                        foreach (VoronoiCell cell in limb.cells)
                        {
                            cell.GraphData.isBranch = false;
                        }
                        limb.cells.Clear();
                        */
                        resolvedLimbs.Add(limb);
                        continue;
                    }
                    else if (limb.cells.Count == 1)
                    {
                        if (limb.mBlob.GetChildren().Count <= 3)
                        {
                            // blob is too small to bother splitting
                            skippedLimbs.Add(limb);
                        }
                        else
                        {
                            // todo consider improving algorithm by considering resulting interior angles
                            // find neighbouring tile with most connections to add to
                            Dictionary<TileBlob, int> blobAdjacencyCount =
                                new Dictionary<TileBlob, int>();
                            foreach (VoronoiCell neighbour in limb.cells.First().GraphData.enemies)
                            {
                                // check if enemy tile
                                if (neighbour.parent.GraphData.isLand == false)
                                {
                                    if (blobAdjacencyCount.ContainsKey(neighbour.parent))
                                    {
                                        blobAdjacencyCount[neighbour.parent]++;
                                    }
                                    else
                                    {
                                        blobAdjacencyCount.Add(neighbour.parent, 1);
                                    }
                                }
                            }

                            if (blobAdjacencyCount.Count == 0)
                            {
                                // no water tile neighbours, just split
                                // split
                                TileBlob splitBlob =
                                    SplitBlob(limb.mBlob, new HashSet<VoronoiCell>(limb.cells));

                                AddedBlobs.Add(splitBlob);
                                limb.mBlob.RecalculateCenter();
                                limblobs.Add(splitBlob);
                                resolvedLimbs.Add(limb);
                            }
                            else
                            {
                                // assign cell to blob with most adjacencies.
                                var sortedAdjacentBlobs = blobAdjacencyCount.ToList();
                                sortedAdjacentBlobs.Sort(
                                    (pair1, pair2) => pair1.Value.CompareTo(pair2.Value));

                                // if however no such blob exists, i.e all equal in
                                // their number of connections
                                int value = sortedAdjacentBlobs.First().Value;
                                bool allNeighboursAreEqual = true;
                                foreach (var sortedAdjBlob in sortedAdjacentBlobs)
                                {
                                    if (sortedAdjBlob.Value != value)
                                    {
                                        allNeighboursAreEqual = false;
                                        break;
                                    }
                                }

                                if (allNeighboursAreEqual)
                                {
                                    TileBlob splitBlob =
                                        SplitBlob(limb.mBlob, new HashSet<VoronoiCell>(limb.cells));

                                    if (affectedBlobs.Contains(limb.mBlob) == false)
                                    {
                                        affectedBlobs.Add(limb.mBlob);
                                    }

                                    VoronoiCell currentCell = splitBlob.GetChildren().First() as VoronoiCell;
                                    foreach (VoronoiCell neighbour in currentCell.GetNeighbours())
                                    {
                                        if (neighbour.GraphData.isLand != currentCell.GraphData.isLand)
                                            continue;

                                        bool isAdjacentToCurrent = false;
                                        foreach (VoronoiCell m_neighbour in neighbour.GetNeighbours())
                                        {
                                            if (m_neighbour.ID == currentCell.ID ||
                                                m_neighbour.GraphData.isLand != neighbour.GraphData.isLand)
                                                continue;

                                            if (CheckAdjacency(m_neighbour, currentCell))
                                            {
                                                isAdjacentToCurrent = true;
                                                break;
                                            }
                                        }

                                        if (neighbour.GraphData.isLand == false && 
                                            neighbour.parent.GetChildren().Count >= 3 &&
                                            isAdjacentToCurrent == true)
                                        {
                                            if (affectedBlobs.Contains(neighbour.parent) == false)
                                            {
                                                affectedBlobs.Add(neighbour.parent);
                                            }

                                            RedistributeBlob(splitBlob, neighbour);
                                            // add affected blobs to recheck later

                                        }
                                    }

                                    AddedBlobs.Add(splitBlob);
                                }
                                else
                                {
                                    RedistributeBlob(
                                        sortedAdjacentBlobs.Last().Key, new HashSet<VoronoiCell>(limb.cells));
                                    resolvedLimbs.Add(limb);
                                }
                            }
                        }
                    }

                }

                // for debugging purposes
                foreach (BlobLimb skippedLimb in skippedLimbs)
                {
                    foreach (VoronoiCell cell in skippedLimb.cells)
                    {
                        cell.GraphData.isBranch = true;
                    }
                }

                foreach (BlobLimb resolvedLimb in resolvedLimbs)
                {
                    foreach (VoronoiCell cell in resolvedLimb.cells)
                    {
                        cell.GraphData.isBranch = true;
                    }
                }

            }
            else if (blob.GetChildren().Count == 0)
            {
                Debug.Log("Something went wrong, blob is size 0...");
            }
            else
            {
                // skip land blobs
                Debug.Log("Skipping land blobs...");
            }
        }

        foreach (TileBlob blobToAdd in AddedBlobs)
        {
            Blobs.Add(blobToAdd);

            if (blobToAdd.GetChildren().Count == 1)
            {
                smallBlobsForPruning.Add(blobToAdd);
            }
        }

        // assumed that size = 1
        foreach (TileBlob blobToPrune in smallBlobsForPruning)
        {
            if (blobToPrune.GetChildren().Count > 1)
            {
                Debug.Log("Blob is too big to prune..." + blobToPrune.ID);
                continue;
            }

            List<VoronoiCell> seaTiles = new List<VoronoiCell>();
            VoronoiCell origin = blobToPrune.GetChildren().First() as VoronoiCell;
            foreach (VoronoiCell neighbour in origin.GetNeighbours())
            {
                if (neighbour.GraphData.isLand == false)
                {
                    seaTiles.Add(neighbour);
                }
            }

            // if is an inlet
            if (seaTiles.Count == 1)
            {

                if (seaTiles.First().parent.GetChildren().Count > 3)
                {
                    RedistributeBlob(blobToPrune, seaTiles.First());
                }
                else
                {
                    MergeBlobs(seaTiles.First().parent, blobToPrune);
                    Blobs.Remove(blobToPrune);
                }

            }
            else if (seaTiles.Count == 2)
            {
                // straight or semi-inlet
                if (CheckAdjacency(seaTiles[0], seaTiles[1]))
                {
                    // inlet
                    if (seaTiles[0].parent.ID != seaTiles[1].parent.ID)
                    {
                        // well shit
                        // todo need a means of redistributing from one
                        // blob or another
                        // Should check for issues with affected blobs
                        // Such as if still Unitary, no branches, etc.
                        if (seaTiles[0].parent.GetChildren().Count >= 3 &&
                            seaTiles[1].parent.GetChildren().Count >= 3)
                        {

                            RedistributeBlob(blobToPrune, seaTiles[0]);
                            RedistributeBlob(blobToPrune, seaTiles[1]);
                        }
                        else if (seaTiles[0].parent.GetChildren().Count >= 3)
                        {
                            RedistributeBlob(blobToPrune, seaTiles[0]);
                        }
                        else if (seaTiles[1].parent.GetChildren().Count >= 3)
                        {
                            RedistributeBlob(blobToPrune, seaTiles[1]);
                        }

                        // otherwise ignore for now i guess
                        // add affected blobs to recheck later
                        if (affectedBlobs.Contains(seaTiles[0].parent) == false)
                        {
                            affectedBlobs.Add(seaTiles[0].parent);
                        }

                        if (affectedBlobs.Contains(seaTiles[1].parent) == false)
                        {
                            affectedBlobs.Add(seaTiles[1].parent);
                        }

                    }
                    else
                    {
                        // neighbours only one tile
                        MergeBlobs(seaTiles.First().parent, blobToPrune);
                        Blobs.Remove(blobToPrune);
                    }

                }
                else
                {
                    // straight
                    // todo need a means of redistributing from one
                    // blob or another
                    // Should check for issues with affected blobs
                    // Such as if still Unitary, no branches, etc.
                    if (seaTiles[0].parent.GetChildren().Count >= 3 &&
                        seaTiles[1].parent.GetChildren().Count >= 3)
                    {

                        RedistributeBlob(blobToPrune, seaTiles[0]);
                        RedistributeBlob(blobToPrune, seaTiles[1]);

                        // recheck affected blobs
                        if (affectedBlobs.Contains(seaTiles[0].parent) == false)
                        {
                            affectedBlobs.Add(seaTiles[0].parent);
                        }

                        if (affectedBlobs.Contains(seaTiles[1].parent) == false)
                        {
                            affectedBlobs.Add(seaTiles[1].parent);
                        }
                    }
                    else if (seaTiles[0].parent.GetChildren().Count >= 3)
                    {
                        RedistributeBlob(blobToPrune, seaTiles[0]);
                        if (affectedBlobs.Contains(seaTiles[0].parent) == false)
                        {
                            affectedBlobs.Add(seaTiles[0].parent);
                        }
                    }
                    else if (seaTiles[1].parent.GetChildren().Count >= 3)
                    {
                        RedistributeBlob(blobToPrune, seaTiles[1]);
                        if (affectedBlobs.Contains(seaTiles[1].parent) == false)
                        {
                            affectedBlobs.Add(seaTiles[1].parent);
                        }
                    }
                    else
                    {
                        // if neither tile is large enough merge with
                        // either smallest neighbour or closest
                        Dictionary<TileBlob, int> blobAdjacencyCount =
                                new Dictionary<TileBlob, int>();

                        foreach (VoronoiCell neighbour in seaTiles)
                        {
                            if (blobAdjacencyCount.ContainsKey(neighbour.parent))
                            {
                                continue;
                            }
                            else
                            {
                                blobAdjacencyCount.Add(neighbour.parent, 
                                    neighbour.parent.GetChildren().Count);
                            }
                        }

                        var blobAdjacencyCountList = blobAdjacencyCount.ToList();
                        blobAdjacencyCountList.Sort(
                                    (pair1, pair2) => pair1.Value.CompareTo(pair2.Value));


                        MergeBlobs(blobAdjacencyCountList.First().Key, blobToPrune);
                        Blobs.Remove(blobToPrune);
                    }
                }
            }
            else
            {
                // 3 or more
                foreach (VoronoiCell waterNeighbour in seaTiles)
                {
                    if (waterNeighbour.parent.GetChildren().Count >= 3)
                    {
                        // Should check for issues with affected blobs
                        // Such as if still Unitary, no branches, etc.
                        RedistributeBlob(blobToPrune, waterNeighbour);

                        if (affectedBlobs.Contains(waterNeighbour.parent) == false)
                        {
                            affectedBlobs.Add(waterNeighbour.parent);
                        }
                    }                    
                }
            }
        }

        return affectedBlobs;
    }

    /* 
     * Check if the blob has any disjoint cells (i.e disconnected cells)
     * that split the blob. Return the list of all disjointed cells if
     * any.
     * 
     * todo infinite loop here
     */
    private List<VoronoiCell> CheckIfUnitary(TileBlob blob)
    {
        Queue<VoronoiCell> cellsToCheck = new Queue<VoronoiCell>();
        cellsToCheck.Enqueue(blob.GetChildren()[0] as VoronoiCell);
        HashSet<VoronoiCell> checkedCells = new HashSet<VoronoiCell>();

        while (cellsToCheck.Count > 0)
        {
            VoronoiCell cellToCheck = cellsToCheck.Dequeue();

            foreach (VoronoiCell cell in cellToCheck.GetNeighbours())
            {

                if (cell.GraphData.cellColour.Equals(blob.TileColour) == true)
                {
                    // check that the current cell doesn't match any already checked cells
                    if (checkedCells.Contains(cell) == false &&
                        cellsToCheck.Contains(cell) == false)
                    {
                        cellsToCheck.Enqueue(cell);
                    }                    
                }
            }

            checkedCells.Add(cellToCheck);
        }

        List<VoronoiCell> disjointCells = new List<VoronoiCell>();

        // check for mismatch
        if (checkedCells.Count < blob.GetChildren().Count)
        {
            // if the number of checked cells is less than the 
            // total number of cells than this implied the blob
            // is disjoint. We need to find the missing cells.
            foreach (var cell in blob.GetChildren())
            {
                if (checkedCells.Contains(cell) == false)
                {
                    // if current cell isn't in checked cells, it must be a disjoint cell.
                    disjointCells.Add(cell as VoronoiCell);
                }
            }
        }

        // make sure to return the list that's smaller, albeit there is an edge case
        // if they are equal...
        return (disjointCells.Count <= checkedCells.Count) ? disjointCells : checkedCells.ToList();
    }

    // A limb is defined as the subgraph of connected cells
    // that are attached to the rest of the blob via only a
    // single point, i.e "bridge" between the two subgraphs.
    struct BlobLimb {
        public VoronoiCell startPoint;
        public VoronoiCell endPoint;
        public List<VoronoiCell> cells;
        public int limbID;
        public int _blobID;
        public TileBlob mBlob;

        public BlobLimb(TileBlob blob)
        {
            cells = new List<VoronoiCell>();
            endPoint = null;
            startPoint = null;
            mBlob = blob;
            _blobID = mBlob.ID;
            limbID = 0;
        }
    }

    // gets limb index
    private int CheckIfCellIsInLimb(VoronoiCell cell, List<BlobLimb> limbs2Check)
    {
        int count = 0;
        foreach (BlobLimb limb in limbs2Check)
        {
            if (limb.cells.Contains(cell))
            {
                return count;
            }

            count++;
        }

        return -1;
    }

    private bool isAnInlet(VoronoiCell cell2Check)
    {
        int landCount = 0;
        foreach (VoronoiCell neighbour in cell2Check.GetNeighbours())
        {
            if (neighbour.GraphData.isLand)
            {
                landCount++;
            }
        }

        if (landCount == (cell2Check.GetNeighbours().Count - 1))
        {
            return true;
        }

        return false;
    }

    private bool isAStraight(VoronoiCell cell2Check)
    {
        int landCount = 0;
        int waterCount = 0;
        foreach (VoronoiCell neighbour in cell2Check.GetNeighbours())
        {
            if (neighbour.GraphData.isLand)
            {
                landCount++;
            }
            else
            {
                waterCount++;
            }
        }

        if (landCount >= waterCount)
        {
            return true;
        }

        return false;
    }

    private bool CheckAndFindAdjacentLimb(List<BlobLimb> limbs2Check, 
                                            VoronoiCell current, 
                                            out int index)
    {

        foreach (VoronoiCell friend in current.GraphData.friends)
        {
            index = 0;
            foreach (BlobLimb limb in limbs2Check)
            {
                if (limb.cells.Any(elem => elem.Equals(friend)))
                {
                    return true;
                }
                index++;
            }
        }

        index = -1;
        return false;
    }

    /*
     * When searching for bridges, the possibility exists that a blob can
     * possess more than one bridge.
     */
    private List<BlobLimb> SearchForBridges(TileBlob blob)
    {
        List<BlobLimb> limbs = new List<BlobLimb>();
        HashSet<VoronoiCell> checkedCells = new HashSet<VoronoiCell>();
        // starting cell
        VoronoiCell startCell = blob.GetChildren()[0] as VoronoiCell;

        Queue<VoronoiCell> cellsToCheck = new Queue<VoronoiCell>();
        cellsToCheck.Enqueue(startCell);

        while (cellsToCheck.Count > 0)
        {
            VoronoiCell currentCell = cellsToCheck.Dequeue();
            List<VoronoiCell> friendNeighbours = currentCell.GraphData.friends.ToList();

            // Check if it is at the end
            if (friendNeighbours.Count == 1)
            {
                if (isAStraight(currentCell))
                {
                    // nothing to do here, not a bridge (yet)
                    // or maybe it is
                    int index;
                    if (CheckAndFindAdjacentLimb(limbs, currentCell, out index))
                    {
                        limbs[index].cells.Add(currentCell);
                    }
                    else
                    {
                        BlobLimb nuLimb = new BlobLimb(blob);
                        nuLimb.limbID = limbs.Count();
                        nuLimb.cells.Add(currentCell);
                        nuLimb.endPoint = currentCell;
                        limbs.Add(nuLimb);
                    }
                }
                else
                {
                    int index;
                    if (CheckAndFindAdjacentLimb(limbs, currentCell, out index))
                    {
                        limbs[index].cells.Add(currentCell);
                    }
                    else
                    {
                        BlobLimb nuLimb = new BlobLimb(blob);
                        nuLimb.limbID = limbs.Count();
                        nuLimb.cells.Add(currentCell);
                        nuLimb.endPoint = currentCell;
                        limbs.Add(nuLimb);
                    }
                }
            }
            else if (friendNeighbours.Count == 2)
            {
                // Check if bridge node
                // check if the two neighbours connect
                if (CheckAdjacency(friendNeighbours[0], friendNeighbours[1]) == false)
                {
                    // they are not adjacent, so in the middle section of a bridge
                    // check previous if exists
                    // first check if the previous cell, connects to the current cell
                    // since its possible the currentCell "jumps" from some random
                    // other cell in the current blob.
                    if (isAStraight(currentCell))
                    {
                        // is a bridge but doesn't count if its also a straight
                    }
                    else
                    {
                        int index;
                        if (CheckAndFindAdjacentLimb(limbs, currentCell, out index))
                        {
                            limbs[index].cells.Add(currentCell);
                        }
                        else
                        {
                            BlobLimb nuLimb = new BlobLimb(blob);
                            nuLimb.limbID = limbs.Count();
                            nuLimb.cells.Add(currentCell);
                            limbs.Add(nuLimb);
                        }
                    }
                }
            }
            else
            {
                // 3 or more connections in the blob
                // check if connects to a bridge, set start cell.
                // Find which two friends connect to each other
                // doesn't check for each possible combination
                // of connections
                bool connected = false;
                foreach (VoronoiCell friend in friendNeighbours)
                {
                    if (connected) break;

                    foreach (VoronoiCell another_friend in friendNeighbours)
                    {
                        if (friend.SiteCoord.ID != another_friend.SiteCoord.ID)
                        {
                            if (CheckAdjacency(friend, another_friend))
                            {
                                connected = true;
                                break;
                            }
                        }
                    }
                }

                // todo consider the case where a cell connects to multiple branches
                if (connected)
                {
                    // confirms this is a beginning point for a branch/limb
                    foreach (VoronoiCell friend in friendNeighbours)
                    {
                        int limbIndex = CheckIfCellIsInLimb(friend, limbs);
                        if (limbIndex >= 0)
                        {
                            BlobLimb limb = limbs[limbIndex];
                            limb.startPoint = currentCell;
                            limbs[limbIndex] = limb;
                        }
                    }
                }

            }

            checkedCells.Add(currentCell);
            // enqueue each neighbour in the current blob to be checked next
            // skip if already enqueued.
            foreach (VoronoiCell friend in friendNeighbours)
            {
                if (cellsToCheck.Contains(friend) == false &&
                    checkedCells.Contains(friend) == false)
                {
                    cellsToCheck.Enqueue(friend);
                }
            }
        }

        return limbs;
    }

    private bool CheckAdjacency(VoronoiCell nodeA, VoronoiCell nodeB)
    {
        return nodeA.GetNeighbours().Contains(nodeB);
    }

    private int GetNumSameBlobNeighbours(VoronoiCell cell)
    {
        int count = 0;
        foreach (VoronoiCell neighbour in cell.GetNeighbours())
        {
            if (neighbour.GraphData.cellColour.Equals(cell.GraphData.cellColour))
            {
                count++;
            }
        }

        return count;
    }

    private TileBlob SplitBlob(TileBlob blobToSplit, HashSet<VoronoiCell> CellsToRedist)
    {
        VoronoiColourGenerator.ColourLayer cl = 
            vColGenie.cellColourLayers[(int)blobToSplit.GraphData.LayerID];
        
        Color nuColour = cl.extraColours[cl.usedIndex];
        cl.usedIndex++;

        // apply changes to usedIndex
        vColGenie.cellColourLayers[(int)blobToSplit.GraphData.LayerID] = cl;

        TileBlob nuBlob = new TileBlob(nuColour);

        RedistributeBlob(nuBlob, CellsToRedist);

        if (GraphUtilities.CheckIfBlobIsAdjacent(blobToSplit.GetChildren().First() as VoronoiCell, nuBlob))
        {
            nuBlob.AddNeighbour(blobToSplit);
            blobToSplit.AddNeighbour(nuBlob);
        }

        blobToSplit.RecalculateCenter();
        nuBlob.RecalculateCenter();
        return nuBlob;
    }

    /*
     * Redistributed voronoi cells from one blob to another
     */
    private void RedistributeBlob(TileBlob receiver, HashSet<VoronoiCell> CellsToRedist)
    {
        if (CellsToRedist.Count <= 0) return; // no cells to redistribute

        HashSet<TileBlob> adjacentBlobs2Check = new HashSet<TileBlob>();
        
        // currently we'll only support redistributing between two blobs
        TileBlob original = CellsToRedist.First().parent;
       
        // check if we're splitting or redistributing
        foreach (VoronoiCell cell in receiver.GetChildren()) // should probably be border children
        {
            HashSet<VoronoiCell> nuEnemies = new HashSet<VoronoiCell>(cell.GraphData.enemies);
            foreach (VoronoiCell neighbour in cell.GraphData.enemies)
            {
                if (CellsToRedist.Contains(neighbour))
                {
                    cell.GraphData.friends.Add(neighbour);
                    nuEnemies.Remove(neighbour);
                }
            }

            cell.GraphData.enemies = nuEnemies;
        }

        foreach (VoronoiCell cell in CellsToRedist)
        {
            // add cell to dest. blob, remove from origin blob
            cell.GraphData.cellColour = receiver.TileColour;
            receiver.AddChildNode(cell);
            original.GetChildren().Remove(cell);
            
            // update friends
            foreach (VoronoiCell oldFriend in cell.GraphData.friends)
            {
                if (CellsToRedist.Contains(oldFriend) == false)
                {
                    oldFriend.GraphData.friends.Remove(cell);
                    oldFriend.GraphData.enemies.Add(cell);
                }                
            }

            cell.GraphData.friends.Clear();
            cell.GraphData.enemies.Clear();

            // updating friends & enemies
            foreach (VoronoiCell neighbour in cell.GetNeighbours())
            {
                if (neighbour.GraphData.isLand == cell.GraphData.isLand)
                {
                    if (neighbour.GraphData.cellColour.Equals(receiver.TileColour) ||
                        CellsToRedist.Contains(neighbour))
                    {
                        cell.GraphData.friends.Add(neighbour);
                    }
                    else
                    {
                        cell.GraphData.enemies.Add(neighbour);
                    }
                }
                else
                {
                    cell.GraphData.enemies.Add(neighbour);
                }
            }
        }

        //
        // Update Blob level Adjacencies
        //

        // add possibly affected neighbouring blobs to the list of
        // blobs to check for issues. Which is any blob that is not
        // the original or receiving blob but is adjacent to the
        // cells being redistributed.
        foreach (VoronoiCell cell in CellsToRedist)
        {
            foreach (VoronoiCell neighbour in cell.GraphData.enemies)
            {
                if (neighbour.parent.ID != original.ID)
                {
                    adjacentBlobs2Check.Add(neighbour.parent);
                }
            }            
        }


        foreach (TileBlob blob2Check in adjacentBlobs2Check)
        {
            if (receiver.GetNeighbours().Contains(blob2Check) == false)
            {
                receiver.AddNeighbour(blob2Check);
            }

            if (blob2Check.GetNeighbours().Contains(receiver) == false)
            {
                blob2Check.AddNeighbour(receiver);
            }

            if (GraphUtilities.CheckIfBlobIsAdjacent(
                original.GetChildren().First() as VoronoiCell, blob2Check) == false)
            {
                blob2Check.GetNeighbours().Remove(original);
                original.GetNeighbours().Remove(blob2Check);
            }
            else
            {
                blob2Check.AddNeighbour(original);
                original.AddNeighbour(blob2Check);
            }

            if (GraphUtilities.CheckIfBlobIsAdjacent(
                receiver.GetChildren().First() as VoronoiCell, blob2Check) == false)
            {
                blob2Check.GetNeighbours().Remove(receiver);
                receiver.GetNeighbours().Remove(blob2Check);
            }
            else
            {
                blob2Check.AddNeighbour(receiver);
                receiver.AddNeighbour(blob2Check);
            }
        }

        receiver.RecalculateCenter();
        original.RecalculateCenter();

    }

    /*
     * Redistributed a single voronoi cell from one blob to another
     */
    private void RedistributeBlob(TileBlob receiver, VoronoiCell CellToRedist)
    {
        HashSet<VoronoiCell> temp = new HashSet<VoronoiCell>();
        temp.Add(CellToRedist);

        RedistributeBlob(receiver, temp);

    }

    // Merge two blobs, return merged blob
    private TileBlob MergeBlobs(TileBlob targetBlob, TileBlob blob2Merge)
    {
        foreach (VoronoiCell childNode in blob2Merge.GetChildren())
        {
            childNode.parent = targetBlob;
            childNode.GraphData.cellColour = targetBlob.TileColour;

            HashSet<VoronoiCell> nuEnemies = new HashSet<VoronoiCell>(childNode.GraphData.enemies);
            // update friends & enemies of cells being merged
            foreach (VoronoiCell neighbour in childNode.GraphData.enemies)
            {
                if (neighbour.parent.Equals(targetBlob))
                {
                    childNode.GraphData.friends.Add(neighbour);
                    nuEnemies.Remove(neighbour);

                    neighbour.GraphData.friends.Add(childNode);
                    neighbour.GraphData.enemies.Remove(childNode);
                }

                /*
                if (neighbour.parent.ID == targetBlob.ID &&
                   childNode.GraphData.friends.Contains(neighbour))
                {
                    childNode.GraphData.friends.Add(neighbour);
                }
                */

                childNode.GraphData.enemies = nuEnemies;
            }

            targetBlob.AddChildNode(childNode);
        }

        foreach (TileBlob neighbour in blob2Merge.GetNeighbours())
        {
            if (neighbour.ID != targetBlob.ID && 
                neighbour.GetNeighbours().Contains(targetBlob) == false)
            {
                neighbour.AddNeighbour(targetBlob);
                if (targetBlob.GetNeighbours().Contains(neighbour) == false)
                {
                    targetBlob.AddNeighbour(neighbour);
                }
                neighbour.GetNeighbours().Remove(blob2Merge);
            }
            else if (neighbour.ID != targetBlob.ID &&
                    neighbour.GetNeighbours().Contains(targetBlob) == true)
            {
                neighbour.GetNeighbours().Remove(blob2Merge);
            }
        }

        targetBlob.GetNeighbours().Remove(blob2Merge);

        targetBlob.RecalculateCenter();

        return targetBlob;
    }
}
