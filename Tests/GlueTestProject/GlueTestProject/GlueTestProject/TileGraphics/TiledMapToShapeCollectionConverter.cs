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
        public static ShapeCollection ToShapeCollection(this TiledMapSave tiledMapSave, string layerName = null)
        {
            var scs = tiledMapSave.ToShapeCollectionSave(layerName);

            return scs.ToShapeCollection();
        }

        public static ShapeCollectionSave ToShapeCollectionSave(this TiledMapSave tiledMapSave, string layerName)
        {
            MapLayer mapLayer = null;
            if (!string.IsNullOrEmpty(layerName))
            {
                mapLayer = tiledMapSave.Layers.FirstOrDefault(l => l.Name.Equals(layerName));
            }
            var shapes = new ShapeCollectionSave();

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
                        ///November 8th, 2015
                        ///Jesse Crafts-Finch
                        ///If a polygon has a gid, and therefore an image associate with it, it will be turned into a spritesave, not a polygon. 
                        if (@object.gid != null)
                        {
                            continue; 
                        }

                        if (@object.polygon != null)
                        {
                            foreach (mapObjectgroupObjectPolygon polygon in @object.polygon)
                            {
                                // TODO: Make this a rectangle object
                                PolygonSave p = tiledMapSave.ConvertTmxObjectToFrbPolygonSave(@object.Name,
                                    @object.x, @object.y, @object.Rotation, polygon.points, true);
                                if (p != null)
                                {
                                    shapes.PolygonSaves.Add(p);                                   
                                }
                            }
                        }

                        if (@object.polyline != null)
                        {
                            foreach (mapObjectgroupObjectPolyline polyline in @object.polyline)
                            {
                                PolygonSave p = tiledMapSave.ConvertTmxObjectToFrbPolygonSave(@object.Name, 
                                    @object.x, @object.y, @object.Rotation, polyline.points, false);
                                if (p != null)
                                {
                                    shapes.PolygonSaves.Add(p);
                                }
                            }
                        }

                       

                        if (@object.polygon == null && @object.polyline == null)
                        {
                            PolygonSave p = tiledMapSave.ConvertTmxObjectToFrbPolygonSave(@object.Name, @object.x, @object.y, @object.width, @object.height, @object.Rotation, @object.ellipse);
                            if (p != null)
                            {
                                shapes.PolygonSaves.Add(p);
                            }
                        }

                        
                    }
                }
            }
            return shapes;
        }



        private static PolygonSave ConvertTmxObjectToFrbPolygonSave(this TiledMapSave tiledMapSave, string name, double x, double y, double w, double h, double rotation, mapObjectgroupObjectEllipse ellipse)
        {
            var pointsSb = new StringBuilder();

            if (ellipse == null)
            {
                pointsSb.AppendFormat("{0},{1}", -w/2, -h/2);

                pointsSb.AppendFormat(" {0},{1}", w/2, -h/2);
                pointsSb.AppendFormat(" {0},{1}", w/2, h/2);
                pointsSb.AppendFormat(" {0},{1}", -w/2, h/2);
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

            return tiledMapSave.ConvertTmxObjectToFrbPolygonSave(name, x + w / 2.0f, y + h / 2.0f, rotation, pointsSb.ToString(), true);
        }

        private static PolygonSave ConvertTmxObjectToFrbPolygonSave(this TiledMapSave tiledMapSave, string name, double x, double y, double rotation, string points, bool connectBackToStart)
        {
            if (string.IsNullOrEmpty(points))
            {
                return null;
            }

            var polygon = new PolygonSave();
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
                        X = Convert.ToDouble(xy[0]),
                        Y = -Convert.ToDouble(xy[1])
                    };
                }).ToList();

            if (connectBackToStart)
            {
                pointsList.Add(new Point(pointsList[0].X, pointsList[0].Y));
            }

            polygon.Points = pointsList.ToArray();
            polygon.X = (float)x;
            polygon.Y = (float)-y;
            polygon.RotationZ = -MathHelper.ToRadians((float)rotation);

            return polygon;
        }


    }
}
