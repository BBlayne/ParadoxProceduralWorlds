using UnityEngine;

namespace Jobberwocky.GeometryAlgorithms.Source.Parameters
{
    public class Voronoi2DParameters : Parameters
    {
        public Voronoi2DParameters() : base()
        {
            Bounded = true;
        }

        public Voronoi2DParameters(Mesh mesh) : this()
        {
            Mesh = mesh;
        }
        
        /// <summary>
        /// Points used for the generation of a Voronoi diagram
        /// </summary>
        public Vector3[] Points { get; set; }
        
        /// <summary>
        /// The boundary points if any for the triangulation
        /// </summary>
        public Vector3[] Boundary { get; set; }

        /// <summary>
        /// The holes and hole points if any for the triangulation
        /// </summary>
        public Vector3[][] Holes { get; set; }
        
        public Mesh Mesh { get; private set; }

        /// <summary>
        /// Set to true to get a bounded Voronoi diagram
        /// </summary>
        public bool Bounded { get; set; }
    }
}
