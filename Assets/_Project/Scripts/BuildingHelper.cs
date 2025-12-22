using System;
using System.Collections.Generic;
using System.Linq;
using BlockGeneration;
using GraphModel;
using UnityEngine;

namespace Services
{
    public static class BuildingHelper
    {
        /// <summary>
        /// Get the center point on the building's roof at full height
        /// </summary>
        public static Vector3 GetRoofCenter(Block building)
        {
            if (building.Nodes.Count == 0) return Vector3.zero;
            
            float sumX = 0f, sumY = 0f;
            foreach (var node in building.Nodes)
            {
                sumX += node.X;
                sumY += node.Y;
            }
            
            return new Vector3(
                sumX / building.Nodes.Count,
                building.Height,
                sumY / building.Nodes.Count
            );
        }
        
        /// <summary>
        /// Get a random point on the building's roof edge
        /// </summary>
        public static Vector3 GetRandomRoofEdgePoint(Block building, System.Random rand)
        {
            if (building.Nodes.Count < 2) return GetRoofCenter(building);
            
            int edgeIndex = rand.Next(0, building.Nodes.Count);
            var nodeA = building.Nodes[edgeIndex];
            var nodeB = building.Nodes[(edgeIndex + 1) % building.Nodes.Count];
            
            float t = (float)rand.NextDouble();
            return new Vector3(
                Mathf.Lerp(nodeA.X, nodeB.X, t),
                building.Height,
                Mathf.Lerp(nodeA.Y, nodeB.Y, t)
            );
        }
        
        /// <summary>
        /// Get the center point of the building edge that faces the nearest road
        /// This is the ideal door position
        /// </summary>
        public static Vector3 GetDoorPosition(Block building, Graph roadGraph)
        {
            if (building.Nodes.Count < 2) return GetGroundCenter(building);
            
            float minDistance = float.MaxValue;
            int bestEdgeIndex = 0;
            
            // Find edge closest to any road node
            for (int i = 0; i < building.Nodes.Count; i++)
            {
                var nodeA = building.Nodes[i];
                var nodeB = building.Nodes[(i + 1) % building.Nodes.Count];
                var edgeCenter = new Vector2(
                    (nodeA.X + nodeB.X) / 2f,
                    (nodeA.Y + nodeB.Y) / 2f
                );
                
                float distToRoad = GetDistanceToNearestRoadNode(edgeCenter, roadGraph);
                if (distToRoad < minDistance)
                {
                    minDistance = distToRoad;
                    bestEdgeIndex = i;
                }
            }
            
            var doorNodeA = building.Nodes[bestEdgeIndex];
            var doorNodeB = building.Nodes[(bestEdgeIndex + 1) % building.Nodes.Count];
            
            return new Vector3(
                (doorNodeA.X + doorNodeB.X) / 2f,
                0f, // Ground level
                (doorNodeA.Y + doorNodeB.Y) / 2f
            );
        }
        
        /// <summary>
        /// Get the center point of a specific edge by index
        /// </summary>
        public static Vector3 GetEdgeCenter(Block building, int edgeIndex, float height = 0f)
        {
            if (building.Nodes.Count < 2) return Vector3.zero;
            
            edgeIndex = edgeIndex % building.Nodes.Count;
            var nodeA = building.Nodes[edgeIndex];
            var nodeB = building.Nodes[(edgeIndex + 1) % building.Nodes.Count];
            
            return new Vector3(
                (nodeA.X + nodeB.X) / 2f,
                height,
                (nodeA.Y + nodeB.Y) / 2f
            );
        }
        
        /// <summary>
        /// Get the outward normal direction of an edge (useful for placing doors)
        /// </summary>
        public static Vector3 GetEdgeOutwardNormal(Block building, int edgeIndex)
        {
            if (building.Nodes.Count < 2) return Vector3.forward;
            
            edgeIndex = edgeIndex % building.Nodes.Count;
            var nodeA = building.Nodes[edgeIndex];
            var nodeB = building.Nodes[(edgeIndex + 1) % building.Nodes.Count];
            
            // Edge direction
            Vector2 edgeDir = new Vector2(nodeB.X - nodeA.X, nodeB.Y - nodeA.Y);
            
            // Perpendicular (normal)
            Vector2 normal = new Vector2(-edgeDir.y, edgeDir.x).normalized;
            
            // Check if it points outward using the building center
            Vector2 center = new Vector2(
                building.Nodes.Average(n => n.X),
                building.Nodes.Average(n => n.Y)
            );
            Vector2 edgeCenter = new Vector2((nodeA.X + nodeB.X) / 2f, (nodeA.Y + nodeB.Y) / 2f);
            Vector2 toCenter = center - edgeCenter;
            
            // Flip if pointing inward
            if (Vector2.Dot(normal, toCenter) > 0)
            {
                normal = -normal;
            }
            
            return new Vector3(normal.x, 0, normal.y);
        }
        
        /// <summary>
        /// Get the nearest road node to a building
        /// </summary>
        public static Node GetNearestRoadNode(Block building, Graph roadGraph)
        {
            Vector2 buildingCenter = new Vector2(
                building.Nodes.Average(n => n.X),
                building.Nodes.Average(n => n.Y)
            );
            
            Node nearest = null;
            float minDist = float.MaxValue;
            
            foreach (var node in roadGraph.MajorNodes.Concat(roadGraph.MinorNodes))
            {
                float dist = Vector2.Distance(buildingCenter, new Vector2(node.X, node.Y));
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = node;
                }
            }
            
            return nearest;
        }
        
        /// <summary>
        /// Get distance from a point to the nearest road node
        /// </summary>
        public static float GetDistanceToNearestRoadNode(Vector2 point, Graph roadGraph)
        {
            float minDist = float.MaxValue;
            
            foreach (var node in roadGraph.MajorNodes.Concat(roadGraph.MinorNodes))
            {
                float dist = Vector2.Distance(point, new Vector2(node.X, node.Y));
                if (dist < minDist)
                {
                    minDist = dist;
                }
            }
            
            return minDist;
        }
        
        /// <summary>
        /// Get all corner positions of the building at a given height
        /// </summary>
        public static List<Vector3> GetCorners(Block building, float height = 0f)
        {
            var corners = new List<Vector3>();
            foreach (var node in building.Nodes)
            {
                corners.Add(new Vector3(node.X, height, node.Y));
            }
            return corners;
        }
        
        /// <summary>
        /// Get the ground center of the building
        /// </summary>
        public static Vector3 GetGroundCenter(Block building)
        {
            return GetRoofCenter(building) - new Vector3(0, building.Height, 0);
        }
        
        /// <summary>
        /// Get a random point inside the building at a given height
        /// </summary>
        public static Vector3 GetRandomInteriorPoint(Block building, System.Random rand, float height = 0f)
        {
            // Use centroid as starting point and random offset
            var center = GetGroundCenter(building);
            center.y = height;
            
            // Get approximate radius
            float maxDist = 0f;
            foreach (var node in building.Nodes)
            {
                float dist = Vector2.Distance(
                    new Vector2(center.x, center.z),
                    new Vector2(node.X, node.Y)
                );
                if (dist > maxDist) maxDist = dist;
            }
            
            // Random point within radius (simplified)
            float angle = (float)(rand.NextDouble() * Math.PI * 2);
            float radius = (float)(rand.NextDouble() * maxDist * 0.7f); // 0.7 to stay inside
            
            return center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
        }
        
        /// <summary>
        /// Check if a 2D point is inside the building (at any height)
        /// </summary>
        public static bool IsPointInside(Block building, Vector2 point)
        {
            // Ray casting algorithm
            int intersections = 0;
            for (int i = 0; i < building.Nodes.Count; i++)
            {
                var nodeA = building.Nodes[i];
                var nodeB = building.Nodes[(i + 1) % building.Nodes.Count];
                
                if (RayIntersectsSegment(point, nodeA, nodeB))
                {
                    intersections++;
                }
            }
            
            return (intersections % 2) == 1;
        }
        
        private static bool RayIntersectsSegment(Vector2 point, BlockNode a, BlockNode b)
        {
            if (a.Y > b.Y)
            {
                var temp = a;
                a = b;
                b = temp;
            }
            
            if (point.y < a.Y || point.y > b.Y) return false;
            if (point.x >= Mathf.Max(a.X, b.X)) return false;
            if (point.x < Mathf.Min(a.X, b.X)) return true;
            
            float xIntersection = (point.y - a.Y) * (b.X - a.X) / (b.Y - a.Y) + a.X;
            return point.x < xIntersection;
        }
        
        /// <summary>
        /// Get the 2D axis-aligned bounding box of the building
        /// </summary>
        public static Bounds GetBounds2D(Block building)
        {
            if (building.Nodes.Count == 0) return new Bounds(Vector3.zero, Vector3.zero);
            
            float minX = building.Nodes.Min(n => n.X);
            float maxX = building.Nodes.Max(n => n.X);
            float minY = building.Nodes.Min(n => n.Y);
            float maxY = building.Nodes.Max(n => n.Y);
            
            Vector3 center = new Vector3((minX + maxX) / 2f, 0, (minY + maxY) / 2f);
            Vector3 size = new Vector3(maxX - minX, 0, maxY - minY);
            
            return new Bounds(center, size);
        }
        
        /// <summary>
        /// Get the full 3D bounding box including height
        /// </summary>
        public static Bounds GetBounds3D(Block building)
        {
            var bounds2D = GetBounds2D(building);
            bounds2D.center = new Vector3(bounds2D.center.x, building.Height / 2f, bounds2D.center.z);
            bounds2D.size = new Vector3(bounds2D.size.x, building.Height, bounds2D.size.z);
            return bounds2D;
        }
        
        /// <summary>
        /// Get all buildings within a certain radius of a point
        /// </summary>
        public static List<Block> GetBuildingsInRadius(Vector2 point, float radius, List<Block> allBuildings)
        {
            var result = new List<Block>();
            float radiusSqr = radius * radius;
            
            foreach (var building in allBuildings)
            {
                Vector2 buildingCenter = new Vector2(
                    building.Nodes.Average(n => n.X),
                    building.Nodes.Average(n => n.Y)
                );
                
                if ((buildingCenter - point).sqrMagnitude <= radiusSqr)
                {
                    result.Add(building);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Calculate approximate perimeter of the building
        /// </summary>
        public static float GetPerimeter(Block building)
        {
            float perimeter = 0f;
            for (int i = 0; i < building.Nodes.Count; i++)
            {
                var nodeA = building.Nodes[i];
                var nodeB = building.Nodes[(i + 1) % building.Nodes.Count];
                perimeter += Vector2.Distance(
                    new Vector2(nodeA.X, nodeA.Y),
                    new Vector2(nodeB.X, nodeB.Y)
                );
            }
            return perimeter;
        }
        
        /// <summary>
        /// Calculate approximate area of the building (2D)
        /// </summary>
        public static float GetArea(Block building)
        {
            // Shoelace formula
            float area = 0f;
            for (int i = 0; i < building.Nodes.Count; i++)
            {
                var nodeA = building.Nodes[i];
                var nodeB = building.Nodes[(i + 1) % building.Nodes.Count];
                area += (nodeA.X * nodeB.Y - nodeB.X * nodeA.Y);
            }
            return Mathf.Abs(area) / 2f;
        }
        
        /// <summary>
        /// Get the nearest building to a given point
        /// </summary>
        public static Block GetNearestBuilding(Vector2 point, List<Block> allBuildings)
        {
            Block nearest = null;
            float minDist = float.MaxValue;
            
            foreach (var building in allBuildings)
            {
                Vector2 buildingCenter = new Vector2(
                    building.Nodes.Average(n => n.X),
                    building.Nodes.Average(n => n.Y)
                );
                
                float dist = Vector2.Distance(point, buildingCenter);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = building;
                }
            }
            
            return nearest;
        }
        
        /// <summary>
        /// Get approximate walking distance between two buildings (straight line * 1.4 for streets)
        /// </summary>
        public static float GetApproximateWalkingDistance(Block buildingA, Block buildingB)
        {
            Vector2 centerA = new Vector2(
                buildingA.Nodes.Average(n => n.X),
                buildingA.Nodes.Average(n => n.Y)
            );
            Vector2 centerB = new Vector2(
                buildingB.Nodes.Average(n => n.X),
                buildingB.Nodes.Average(n => n.Y)
            );
            
            // Multiply by 1.4 as rough estimate for Manhattan distance on grid
            return Vector2.Distance(centerA, centerB) * 1.4f;
        }
        
        /// <summary>
        /// Get all edges of the building as a list of edge pairs
        /// </summary>
        public static List<(Vector3 start, Vector3 end)> GetEdges3D(Block building, float height = 0f)
        {
            var edges = new List<(Vector3, Vector3)>();
            for (int i = 0; i < building.Nodes.Count; i++)
            {
                var nodeA = building.Nodes[i];
                var nodeB = building.Nodes[(i + 1) % building.Nodes.Count];
                edges.Add((
                    new Vector3(nodeA.X, height, nodeA.Y),
                    new Vector3(nodeB.X, height, nodeB.Y)
                ));
            }
            return edges;
        }
        
        /// <summary>
        /// Get a spawn point slightly outside the door position (for deliveries)
        /// </summary>
        public static Vector3 GetDeliverySpawnPoint(Block building, Graph roadGraph, float offsetDistance = 1f)
        {
            var doorPos = GetDoorPosition(building, roadGraph);
            
            // Find which edge this is on
            float minDist = float.MaxValue;
            int edgeIndex = 0;
            for (int i = 0; i < building.Nodes.Count; i++)
            {
                var edgeCenter = GetEdgeCenter(building, i, 0f);
                float dist = Vector3.Distance(doorPos, edgeCenter);
                if (dist < minDist)
                {
                    minDist = dist;
                    edgeIndex = i;
                }
            }
            
            // Get outward normal and offset
            var normal = GetEdgeOutwardNormal(building, edgeIndex);
            return doorPos + normal * offsetDistance;
        }
    }
}