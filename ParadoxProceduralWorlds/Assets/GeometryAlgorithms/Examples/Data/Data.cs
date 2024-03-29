﻿namespace Jobberwocky.GeometryAlgorithms.Examples.Data
{
    // Enum of all the available shapes
    public enum ShapeType
    {
        Bird,
        Circle,
        CircleWithHole,
        Cube,
        Dude,
        Horse13k,
        Owl15k,
        Random2D,
        Random3D,
        Sphere,
        Square,
        SquareWithHole,
        Tank,
    }

    public class Data
    {
        /// <summary>
        /// Returns the shape given the shape type
        /// </summary>
        /// <param name="type">The type of the shape</param>
        /// <returns></returns>
        public static Shape Get(ShapeType type)
        {
            Shape shape = null;
            switch (type)
            {
                case ShapeType.Bird:
                    shape = new Bird();
                    break;
                case ShapeType.Circle:
                    shape = new Circle();
                    break;
                case ShapeType.CircleWithHole:
                    shape = new CircleWithHole();
                    break;
                case ShapeType.Cube:
                    shape = new Cube();
                    break;
                case ShapeType.Dude:
                    shape = new Dude();
                    break;
                case ShapeType.Horse13k:
                    shape = new Horse13k();
                    break;
                case ShapeType.Owl15k:
                    shape = new Owl15k();
                    break;
                case ShapeType.Random2D:
                    shape = new Random2D();
                    break;
                case ShapeType.Random3D:
                    shape = new Random3D();
                    break;
                case ShapeType.Sphere:
                    shape = new Sphere();
                    break;
                case ShapeType.Square:
                    shape = new Square();
                    break;
                case ShapeType.SquareWithHole:
                    shape = new SquareWithHole();
                    break;
                case ShapeType.Tank:
                    shape = new Tank();
                    break;
                default:
                    shape = new Dude();
                    break;
            }

            return shape;
        }
    }
}