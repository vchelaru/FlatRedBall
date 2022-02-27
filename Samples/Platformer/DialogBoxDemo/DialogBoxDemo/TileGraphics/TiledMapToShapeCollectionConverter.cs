using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Content.Polygon;
using FlatRedBall.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Point = FlatRedBall.Math.Geometry.Point;

namespace TMXGlueLib
{
    public static class TiledMapToShapeCollectionConverter
    {
        public static ShapeCollection ToShapeCollection(this TiledMapSave tiledMapSave, string layerName)
        {
            MapLayer mapLayer = null;
            if (!string.IsNullOrEmpty(layerName))
            {
                mapLayer = tiledMapSave.Layers.FirstOrDefault(l => l.Name.Equals(layerName));
            }
            var shapes = new ShapeCollection();

            if ((mapLayer != null && !mapLayer.IsVisible && mapLayer.VisibleBehavior == TMXGlueLib.TiledMapSave.LayerVisibleBehavior.Skip) ||
                tiledMapSave.objectgroup == null || tiledMapSave.objectgroup.Count == 0)
            {
                return shapes;
            }

            foreach (mapObjectgroup group in tiledMapSave.objectgroup)
            {
                if (group.@object != null && !string.IsNullOrEmpty(group.Name) && (string.IsNullOrEmpty(layerName) || group.Name.Equals(layerName)))
                {
                    foreach (mapObjectgroupObject @object in group.@object)
                    {
                        AddShapeToShapeCollection(@object, shapes);
                    }

                }
            }
            return shapes;
        }

        public static void AddShapeToShapeCollection(mapObjectgroupObject @object, ShapeCollection shapes)
        {

            //////////////////////////Early out////////////////////////////////
            ///November 8th, 2015
            ///Jesse Crafts-Finch
            ///If a polygon has a gid, and therefore an image associate with it, it will be turned into a spritesave, not a polygon. 
            if (@object.gid != null)
            {
                return;
            }
            ////////////////////////End Early Out/////////////////////////////////
            Polygon polygon;
            AxisAlignedRectangle rectangle;
            Circle circle;

            ConvertTiledObjectToFrbShape(@object, true, out polygon, out rectangle, out circle);

            if (polygon != null)
            {
                shapes.Polygons.Add(polygon);
            }
            if (rectangle != null)
            {
                shapes.AxisAlignedRectangles.Add(rectangle);
            }
            if (circle != null)
            {
                shapes.Circles.Add(circle);
            }
        }

        public static void ConvertTiledObjectToFrbShape(mapObjectgroupObject @object, bool applyVisibility, out Polygon polygon, out AxisAlignedRectangle rectangle, out Circle circle)
        {
            polygon = null;
            rectangle = null;
            circle = null;
            if (@object.polygon != null)
            {
                foreach (mapObjectgroupObjectPolygon tiledPolygon in @object.polygon)
                {
                    // TODO: Make this a rectangle object
                    polygon = ConvertTmxObjectToFrbPolygon(@object.Name,
                        @object.x, @object.y, @object.Rotation, tiledPolygon.points, true);
                    if (applyVisibility)
                    {
                        polygon.Visible = tiledPolygon.Visible == 1;
                    }
                }
            }

            if (@object.polyline != null)
            {
                foreach (mapObjectgroupObjectPolyline polyline in @object.polyline)
                {
                    polygon = ConvertTmxObjectToFrbPolygon(@object.Name,
                        @object.x, @object.y, @object.Rotation, polyline.points, false);
                    polygon.Visible = polyline.Visible == 1;
                }
            }

            if (@object.polygon == null && @object.polyline == null)
            {
                if (@object.Rotation == 0 && @object.ellipse == null)
                {
                    rectangle = new AxisAlignedRectangle()
                    {
                        Name = @object.Name,
                        X = (float)@object.x + (@object.width / 2),
                        Y = (float)-@object.y - (@object.height / 2),
                        ScaleX = @object.width / 2,
                        ScaleY = @object.height / 2,
                    };
                    if (applyVisibility)
                    {
                        rectangle.Visible = @object.Visible == 1;
                    }

                }
                else if (@object.ellipse != null && @object.width == @object.height)
                {
                    circle = new Circle()
                    {
                        Name = @object.Name,
                        X = (float)@object.x + (@object.width / 2),
                        Y = (float)-@object.y - (@object.height / 2),
                        Radius = @object.width / 2
                    };
                    if (applyVisibility)
                    {
                        circle.Visible = @object.Visible == 1;
                    }
                }
                else
                {
                    polygon = ConvertTmxObjectToFrbPolygon(@object.Name, @object.x, @object.y, @object.width, @object.height, @object.Rotation, @object.ellipse);
                }
            }
        }

        private static Polygon ConvertTmxObjectToFrbPolygon(string name,
            double x, double y, double w, double h, double rotation, mapObjectgroupObjectEllipse ellipse)
        {
            var pointsSb = new StringBuilder();

            if (ellipse == null)
            {
                pointsSb.AppendFormat("{0},{1}", -w / 2, -h / 2);

                pointsSb.AppendFormat(" {0},{1}", w / 2, -h / 2);
                pointsSb.AppendFormat(" {0},{1}", w / 2, h / 2);
                pointsSb.AppendFormat(" {0},{1}", -w / 2, h / 2);
            }
            else
            {
                const double a = .5;
                const double b = .5;

                // x = a cos t
                // y = b cos t
                var first = true;
                string firstPoint = "";
                for (var angle = 0; angle <= 360; angle += 18)
                {
                    var radians = MathHelper.ToRadians(angle);


                    // This code made the position of the poly be top left, not optimized!
                    //var newx = a*Math.Cos(radians)*w+w/2;
                    //var newy = b*Math.Sin(radians)*h+h/2;

                    var newx = a * Math.Cos(radians) * w;
                    var newy = b * Math.Sin(radians) * h;

                    if (first)
                    {
                        firstPoint = string.Format("{0},{1}", newx, newy);
                    }
                    pointsSb.AppendFormat("{2}{0},{1}", newx, newy, first ? "" : " ");
                    first = false;
                }

                pointsSb.AppendFormat(" {0}", firstPoint);
            }

            return ConvertTmxObjectToFrbPolygon(name, x + w / 2.0f, y + h / 2.0f, rotation, pointsSb.ToString(), true);
        }

        private static Polygon ConvertTmxObjectToFrbPolygon(string name,
            double x, double y, double rotation, string points, bool connectBackToStart)
        {
            if (string.IsNullOrEmpty(points))
            {
                return null;
            }

            var polygon = new Polygon();
            string[] pointString = points.Split(" ".ToCharArray());

            polygon.Name = name;

            // Nov. 19th, 2014 - Domenic:
            // I am ripping this code apart a little, because shapes really should not involve tile sizes in their x/y calculations.
            // I'm not sure why this was ever done this way, as TMX gives the X/Y and width/height already. The old way was basically to convert
            // the x/y coordinates into tile based coordinates and then re-convert back to full x/y coordinates. This makes no sense any more to me.
            //
            // Having examined TMX format a little more, it seems that the x/y position is always specified
            //
            //float fx = x;
            //float fy = y;

            //if ("orthogonal".Equals(tiledMapSave.orientation))
            //{
            //    fx -= tiledMapSave.tilewidth / 2.0f;
            //    fy -= tiledMapSave.tileheight + (tiledMapSave.tileheight / 2.0f);
            //}
            //else if ("isometric".Equals(tiledMapSave.orientation))
            //{
            //    fx -= tiledMapSave.tilewidth / 4.0f;
            //    fy -= tiledMapSave.tileheight / 2.0f;
            //}

            //tiledMapSave.CalculateWorldCoordinates(
            //    0, fx / tiledMapSave.tileheight, fy / tiledMapSave.tileheight, 
            //    tiledMapSave.tilewidth, tiledMapSave.tileheight, 
            //    w * tiledMapSave.tilewidth, out newx, out newy, out z);

            //polygon.X = newx - tiledMapSave.tilewidth / 2.0f;
            //polygon.Y = newy - tiledMapSave.tileheight / 2.0f;
            //var pointsArr = new Point[pointString.Length + (connectBackToStart ? 1 : 0)];

            var pointsList =
                pointString.Select(p =>
                {
                    var xy = p.Split(",".ToCharArray());
                    return new Point
                    {
                        X = Convert.ToDouble(xy[0], System.Globalization.NumberFormatInfo.InvariantInfo),
                        Y = -Convert.ToDouble(xy[1], System.Globalization.NumberFormatInfo.InvariantInfo)
                    };
                }).ToList();

            if (connectBackToStart)
            {
                pointsList.Add(new Point(pointsList[0].X, pointsList[0].Y));
            }

            if (IsClockwise(pointsList) == false)
            {
                pointsList.Reverse();
            }

            polygon.Points = pointsList.ToArray();
            polygon.X = (float)x;
            polygon.Y = (float)-y;
            polygon.RotationZ = -MathHelper.ToRadians((float)rotation);

            return polygon;
        }

        // From:
        // https://stackoverflow.com/questions/1165647/how-to-determine-if-a-list-of-polygon-points-are-in-clockwise-order
        private static bool IsClockwise(List<Point> pointsList)
        {
            double sum = 0;
            for (int i = 0; i < pointsList.Count - 1; i++)
            {
                var point = pointsList[i];
                var pointAfter = pointsList[i + 1];

                sum += (pointAfter.X - point.X) * (pointAfter.Y + point.Y);
            }

            return sum > 0;
        }
    }
}
