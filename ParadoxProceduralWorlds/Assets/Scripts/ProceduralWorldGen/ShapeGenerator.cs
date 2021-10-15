using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * This class is for taking a Height Map and generating
 * "shape" data roughly corresponding to geographic features.
 * 
 * Desired:
 * - A list of all landmasses and their size in pixels.
 * - A list of all water masses such as lakes, oceans and their size.
 * - The pixels that make up the "border" of each mass.
 * 
 * Need to use Flood fill algorithm to scan the whole image;
 * I can probably do this okay-ish on the CPU, it'll lag but 
 * doesn't need to be real time; but putting on the GPU would 
 * be faster. 
 * 
 * GPU Solution is best probably for colouring the provinces
 * later based off of relaxed voronoi partitioning; but for
 * my Object Oriented purposes I need CPU based solution, 
 * perhaps I can improve performance with multithreading.
 */

public class ShapeGenerator
{

}
