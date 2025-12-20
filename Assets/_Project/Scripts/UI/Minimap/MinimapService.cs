using System.Collections.Generic;
using BlockGeneration;
using GraphModel;
using UnityEngine;

namespace Services
{
    public static class MinimapService
    {
        public static Texture2D GenerateMinimap(
            Graph roadGraph,
            List<Block> blocks,
            List<Block> lots,
            int mapSize,
            int textureSize = 512)
        {
            // Create texture
            Texture2D minimapTexture = new Texture2D(textureSize, textureSize);
            
            // Fill with background color
            Color backgroundColor = new Color(0.15f, 0.15f, 0.15f); // Dark gray
            Color[] pixels = new Color[textureSize * textureSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = backgroundColor;
            }
            minimapTexture.SetPixels(pixels);
            
            // Calculate scale factor
            float scale = textureSize / (mapSize * 2f);
            int centerOffset = textureSize / 2;
            
            // Draw blocks (sidewalks/parks)
            if (blocks != null)
            {
                Color blockColor = new Color(0.3f, 0.5f, 0.3f); // Green for parks
                Color sidewalkColor = new Color(0.25f, 0.25f, 0.25f); // Gray for sidewalks
                
                foreach (var block in blocks)
                {
                    Color colorToUse = block.IsPark ? blockColor : sidewalkColor;
                    DrawBlockOnTexture(minimapTexture, block, scale, centerOffset, colorToUse);
                }
            }
            
            // Draw lots (buildings)
            if (lots != null)
            {
                Color lotColor = new Color(0.6f, 0.6f, 0.7f); // Light blue-gray for buildings
                
                foreach (var lot in lots)
                {
                    DrawBlockOnTexture(minimapTexture, lot, scale, centerOffset, lotColor);
                }
            }
            
            // Draw major roads
            if (roadGraph != null && roadGraph.MajorEdges != null)
            {
                Color majorRoadColor = new Color(0.8f, 0.8f, 0.8f); // White
                DrawEdgesOnTexture(minimapTexture, roadGraph.MajorEdges, scale, centerOffset, majorRoadColor, 2);
            }
            
            // Draw minor roads
            if (roadGraph != null && roadGraph.MinorEdges != null)
            {
                Color minorRoadColor = new Color(0.5f, 0.5f, 0.5f); // Gray
                DrawEdgesOnTexture(minimapTexture, roadGraph.MinorEdges, scale, centerOffset, minorRoadColor, 1);
            }
            
            minimapTexture.Apply();
            return minimapTexture;
        }
        
        private static void DrawBlockOnTexture(Texture2D texture, Block block, float scale, int centerOffset, Color color)
        {
            if (block.Nodes == null || block.Nodes.Count < 3) return;
            
            // Draw filled polygon
            for (int i = 0; i < block.Nodes.Count; i++)
            {
                BlockNode current = block.Nodes[i];
                BlockNode next = block.Nodes[(i + 1) % block.Nodes.Count];
                
                int x1 = Mathf.RoundToInt(current.X * scale) + centerOffset;
                int y1 = Mathf.RoundToInt(current.Y * scale) + centerOffset;
                int x2 = Mathf.RoundToInt(next.X * scale) + centerOffset;
                int y2 = Mathf.RoundToInt(next.Y * scale) + centerOffset;
                
                DrawLine(texture, x1, y1, x2, y2, color, 1);
            }
            
            // Fill the polygon (simple scanline fill)
            FillPolygon(texture, block, scale, centerOffset, color);
        }
        
        private static void FillPolygon(Texture2D texture, Block block, float scale, int centerOffset, Color color)
        {
            // Find bounding box
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            
            foreach (var node in block.Nodes)
            {
                if (node.X < minX) minX = node.X;
                if (node.X > maxX) maxX = node.X;
                if (node.Y < minY) minY = node.Y;
                if (node.Y > maxY) maxY = node.Y;
            }
            
            int startX = Mathf.Max(0, Mathf.RoundToInt(minX * scale) + centerOffset);
            int endX = Mathf.Min(texture.width - 1, Mathf.RoundToInt(maxX * scale) + centerOffset);
            int startY = Mathf.Max(0, Mathf.RoundToInt(minY * scale) + centerOffset);
            int endY = Mathf.Min(texture.height - 1, Mathf.RoundToInt(maxY * scale) + centerOffset);
            
            // Check each pixel in bounding box
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    float worldX = (x - centerOffset) / scale;
                    float worldY = (y - centerOffset) / scale;
                    
                    if (IsPointInPolygon(worldX, worldY, block))
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }
        
        private static bool IsPointInPolygon(float x, float y, Block block)
        {
            bool inside = false;
            int count = block.Nodes.Count;
            
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                float xi = block.Nodes[i].X, yi = block.Nodes[i].Y;
                float xj = block.Nodes[j].X, yj = block.Nodes[j].Y;
                
                bool intersect = ((yi > y) != (yj > y)) && 
                                (x < (xj - xi) * (y - yi) / (yj - yi) + xi);
                if (intersect) inside = !inside;
            }
            
            return inside;
        }
        
        private static void DrawEdgesOnTexture(Texture2D texture, List<Edge> edges, float scale, int centerOffset, Color color, int thickness)
        {
            if (edges == null) return;
            
            foreach (var edge in edges)
            {
                int x1 = Mathf.RoundToInt(edge.NodeA.X * scale) + centerOffset;
                int y1 = Mathf.RoundToInt(edge.NodeA.Y * scale) + centerOffset;
                int x2 = Mathf.RoundToInt(edge.NodeB.X * scale) + centerOffset;
                int y2 = Mathf.RoundToInt(edge.NodeB.Y * scale) + centerOffset;
                
                DrawLine(texture, x1, y1, x2, y2, color, thickness);
            }
        }
        
        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color, int thickness)
        {
            // Bresenham's line algorithm with thickness
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            
            while (true)
            {
                DrawThickPixel(texture, x0, y0, color, thickness);
                
                if (x0 == x1 && y0 == y1) break;
                
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }
        
        private static void DrawThickPixel(Texture2D texture, int x, int y, Color color, int thickness)
        {
            int halfThickness = thickness / 2;
            
            for (int dx = -halfThickness; dx <= halfThickness; dx++)
            {
                for (int dy = -halfThickness; dy <= halfThickness; dy++)
                {
                    int px = x + dx;
                    int py = y + dy;
                    
                    if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                    {
                        texture.SetPixel(px, py, color);
                    }
                }
            }
        }
    }
}