using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
namespace WpfApp1
{

    public static class MeshHelper
    {
        private const double NormalSimilarityThreshold = 0.99; // 法线相似度阈值 (cosine similarity)
        public class Edge : IEquatable<Edge>
        {
            public readonly int Vertex1;
            public readonly int Vertex2;
            public readonly Point3D V1Pos;
            public readonly Point3D V2Pos;

            public Edge(int vertex1, int vertex2, Point3D v1Pos, Point3D v2Pos)
            {
                Vertex1 = Math.Min(vertex1, vertex2);
                Vertex2 = Math.Max(vertex1, vertex2);
                Vertex1 = vertex1;
                Vertex2 = vertex2;
                V1Pos = v1Pos;
                V2Pos = v2Pos;
                if (vertex1 > vertex2)
                {
                    Vertex1 = vertex2;
                    Vertex2 = vertex1;
                    V1Pos = v2Pos;
                    V2Pos = v1Pos;
                }
            }

            public bool Equals(Edge other)
            {
                return Vertex1 == other.Vertex1 && Vertex2 == other.Vertex2;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Vertex1, Vertex2);
            }
        }
        public class Triangle
        {
            public int[] Indices { get; set; } // 三角形顶点索引
            public Vector3D Normal { get; set; } // 三角形法线
        }

        public static KeyValuePair<Point3DCollection, Int32Collection> RemoveDuplicateVertices(Point3DCollection pnts, Int32Collection indices)
        {
            // 创建一个字典来存储唯一顶点的位置及其索引，使用自定义比较器处理浮点数精度问题
            var uniqueVertices = new Dictionary<Point3D, int>(new Point3DEqualityComparer());
            var newPositions = new List<Point3D>();
            var newIndexMap = new Dictionary<int, int>(); // 用于记录旧索引到新索引的映射
            var newTriangleIndices = new Int32Collection();

            // 遍历所有顶点，并将唯一的顶点添加到列表中，同时记录新旧索引映射
            for (int i = 0; i < pnts.Count; i++)
            {
                Point3D position = pnts[i];
                if (!uniqueVertices.TryGetValue(position, out int newIndex))
                {
                    newIndex = newPositions.Count;
                    uniqueVertices[position] = newIndex;
                    newPositions.Add(position);
                }
                newIndexMap[i] = newIndex; // 记录新旧索引的映射关系
            }

            // 使用新的顶点位置创建新的 Point3DCollection
            var updatedPositions = new Point3DCollection(newPositions);

            // 更新三角形索引以引用新的顶点索引
            foreach (var oldIndex in indices)
            {
                if (newIndexMap.TryGetValue(oldIndex, out int newIndex))
                {
                    newTriangleIndices.Add(newIndex);
                }
                else
                {
                    throw new InvalidOperationException("索引超出范围或未正确映射。");
                }
            }

            return new KeyValuePair<Point3DCollection, Int32Collection>(updatedPositions, newTriangleIndices);
        }

        // 自定义比较器，考虑浮点数精度问题
        private class Point3DEqualityComparer : IEqualityComparer<Point3D>
        {
            private const double Epsilon = 1e-6;

            public bool Equals(Point3D x, Point3D y)
            {
                return Math.Abs(x.X - y.X) < Epsilon &&
                       Math.Abs(x.Y - y.Y) < Epsilon &&
                       Math.Abs(x.Z - y.Z) < Epsilon;
            }

            public int GetHashCode(Point3D obj)
            {
                // 使用位移和异或运算生成哈希码，确保即使有小误差也能得到相同的哈希码
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + obj.X.GetHashCode();
                    hash = hash * 23 + obj.Y.GetHashCode();
                    hash = hash * 23 + obj.Z.GetHashCode();
                    return hash;
                }
            }
        }

        public static Dictionary<Vector3D, List<Tuple<int, int>>> ExtractBoundaryEdgesByNormal(List<Triangle> triangles)
        {
            // 按照法线分组三角形
            var normalGroups = new Dictionary<Vector3D, List<Triangle>>();

            foreach (var tri in triangles)
            {
                // 找到最相似的法线分组，或者创建新的分组
                bool added = false;
                foreach (var groupKey in normalGroups.Keys.ToList())
                {
                    if (AreNormalsSimilar(tri.Normal, groupKey))
                    {
                        normalGroups[groupKey].Add(tri);
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    normalGroups[tri.Normal] = new List<Triangle> { tri };
                }
            }

            // 提取每个分组中的边界边
            var boundaryEdgesByNormal = new Dictionary<Vector3D, List<Tuple<int, int>>>();

            foreach (var group in normalGroups)
            {
                var edgeCount = new Dictionary<Tuple<int, int>, int>();
                var allEdges = new HashSet<Tuple<int, int>>();

                foreach (var tri in group.Value)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int start = tri.Indices[i];
                        int end = tri.Indices[(i + 1) % 3];
                        var edge = Tuple.Create(start, end);
                        var reverseEdge = Tuple.Create(end, start);

                        if (!allEdges.Contains(edge) && !allEdges.Contains(reverseEdge))
                        {
                            allEdges.Add(edge);
                            edgeCount[edge] = 1;
                        }
                        else if (allEdges.Contains(edge))
                        {
                            edgeCount[edge]++;
                        }
                        else if (allEdges.Contains(reverseEdge))
                        {
                            edgeCount[reverseEdge]++;
                        }
                    }
                }

                // 提取只出现一次的边作为边界边
                var boundaryEdges = edgeCount.Where(pair => pair.Value == 1).Select(pair => pair.Key).ToList();

                boundaryEdgesByNormal[group.Key] = boundaryEdges;
            }

            return boundaryEdgesByNormal;
        }


        public static List<List<Edge>> ExtractBoundaryEdgesByNormal(MeshGeometry3D mesh)
        {
            var posIndices = RemoveDuplicateVertices(mesh.Positions, mesh.TriangleIndices);
            List<Triangle> tris = new List<Triangle>();


            for (int i = 0; i < posIndices.Value.Count; i += 3)
            {
                int v1 = posIndices.Value[i];
                int v2 = posIndices.Value[i + 1];
                int v3 = posIndices.Value[i + 2];
                Triangle triangle = new Triangle()
                {
                    Indices = new int[] { v1, v2, v3 }
                };
                Point3D p0 = posIndices.Key[v1];
                Point3D p1 = posIndices.Key[v2];
                Point3D p2 = posIndices.Key[v3];
                triangle.Normal = Vector3D.CrossProduct((p0 - p1), (p2 - p1));
                triangle.Normal.Normalize();
                tris.Add(triangle);
            }
            var boundaryEdges = new List<List<Edge>>();
            var dicTTT = ExtractBoundaryEdgesByNormal(tris);
            if (dicTTT != null)
            {
                foreach (var tupleEdges in dicTTT.Values)
                {
                    List<Edge> es = new List<Edge>();
                    foreach (var item in tupleEdges)
                    {
                        int v0 = item.Item1;
                        int v1 = item.Item2;
                        Edge edge = new Edge(v0, v1, posIndices.Key[v0], posIndices.Key[v1]);
                        es.Add(edge);
                    }
                    boundaryEdges.Add(es)
;
                }
            }
            return boundaryEdges;
        }

        private static bool AreNormalsSimilar(Vector3D n1, Vector3D n2)
        {
            // 计算法线之间的夹角余弦值
            double dotProduct = Vector3D.DotProduct(n1, n2);
            double magnitudeProduct = n1.Length * n2.Length;

            if (magnitudeProduct == 0)
                return false;

            double cosineSimilarity = dotProduct / magnitudeProduct;
            return Math.Abs(cosineSimilarity - 1.0) <= (1.0 - NormalSimilarityThreshold);
        }

    }

}