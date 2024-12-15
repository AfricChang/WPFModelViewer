using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WpfApplication2
{
    public class LinesVisual3D : ModelVisual3D
    {
        public LinesVisual3D(Point3D start, Point3D end, Color color)
        {
            var material = new DiffuseMaterial(new SolidColorBrush(color));
            var geometry = new MeshGeometry3D();

            // 创建一个扁平的四边形来表示线
            double thickness = 0.02;  // 线的粗细
            Vector3D direction = end - start;
            
            // 计算垂直于线的方向向量
            Vector3D right;
            if (direction.Z == 0)  // 如果线在XY平面上
            {
                right = new Vector3D(0, 0, 1);
            }
            else
            {
                right = Vector3D.CrossProduct(direction, new Vector3D(0, 1, 0));
            }
            right.Normalize();
            right *= thickness;

            // 添加4个顶点，形成一个扁平的四边形
            geometry.Positions.Add(start - right);  // 左下
            geometry.Positions.Add(start + right);  // 右下
            geometry.Positions.Add(end + right);    // 右上
            geometry.Positions.Add(end - right);    // 左上

            // 添加两个三角形来形成四边形
            geometry.TriangleIndices.Add(0);
            geometry.TriangleIndices.Add(1);
            geometry.TriangleIndices.Add(2);
            geometry.TriangleIndices.Add(0);
            geometry.TriangleIndices.Add(2);
            geometry.TriangleIndices.Add(3);

            // 添加法线，使线条从两面都可见
            Vector3D normal1 = Vector3D.CrossProduct(direction, right);
            Vector3D normal2 = -normal1;
            normal1.Normalize();
            normal2.Normalize();

            geometry.Normals.Add(normal1);
            geometry.Normals.Add(normal1);
            geometry.Normals.Add(normal2);
            geometry.Normals.Add(normal2);

            var model = new GeometryModel3D(geometry, material)
            {
                BackMaterial = material  // 使线条双面可见
            };
            this.Content = model;
        }
    }
} 