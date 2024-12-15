using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WpfApplication2
{
    public class MouseCameraController
    {
        private readonly PerspectiveCamera camera;
        private readonly Viewport3D ViewPort;
        private Point lastMousePosition;
        private bool isRotating;
        private bool isPanning;
        private Rect3D modelBounds;

        // 常量定义
        private const double ROTATION_SPEED = 0.3;
        private const double ZOOM_SPEED = 0.1;
        private const double PAN_SPEED = 0.01;
        private const double MIN_DISTANCE = 0.1;
        private const double MAX_DISTANCE = 1000;

        public MouseCameraController(Viewport3D _ViewPort, PerspectiveCamera camera, Rect3D bounds)
        {
            this.ViewPort = _ViewPort;
            this.camera = camera;
            this.modelBounds = bounds;
        }

        public void UpdateModelBounds(Rect3D bounds)
        {
            modelBounds = bounds;
        }

        public void OnMouseDown(Point position, MouseButton button)
        {
            lastMousePosition = position;
            if (button == MouseButton.Left)
                isRotating = true;
            else if (button == MouseButton.Right)
                isPanning = true;
        }

        public void OnMouseUp(MouseButton button)
        {
            if (button == MouseButton.Left)
                isRotating = false;
            else if (button == MouseButton.Right)
                isPanning = false;
        }

        public void OnMouseMove(Point position)
        {
            if (!isRotating && !isPanning)
                return;

            Vector delta = position - lastMousePosition;

            if (isRotating)
                HandleRotation(delta);
            else if (isPanning)
                HandlePanning(delta);

            lastMousePosition = position;
        }

        public void OnMouseWheel(double delta)
        {
            var zoomFactor = delta > 0 ? 0.9 : 1.1;
            var center = GetModelCenter();

            // 获取当前相机到中心点的距离
            var currentDistance = (camera.Position - center).Length;

            // 计算新的距离
            var newDistance = currentDistance * zoomFactor;

            // 限制缩放范围
            if (newDistance < MIN_DISTANCE || newDistance > MAX_DISTANCE)
                return;

            // 获取相机到目标点的方向
            var direction = camera.LookDirection;
            direction.Normalize();

            // 计算新的相机位置
            var newPosition = center - direction * newDistance;

            // 更新相机位置
            camera.Position = newPosition;
        }

        private void HandleRotation(Vector delta)
        {
            // 只处理主要移动方向
            if (Math.Abs(delta.X) > Math.Abs(delta.Y))
                RotateHorizontal(delta.X);
            else if (Math.Abs(delta.Y) > Math.Abs(delta.X))
                RotateVertical(delta.Y);
        }

        private void RotateHorizontal(double delta)
        {
            var center = GetModelCenter();
            var modelYAxis = new Vector3D(0, modelBounds.SizeY, 0);
            modelYAxis.Normalize();

            var angle = -delta * ROTATION_SPEED;
            var rotation = new RotateTransform3D(new AxisAngleRotation3D(modelYAxis, angle));

            var relativePosition = camera.Position - center;
            var newPosition = rotation.Transform(relativePosition) + center;

            camera.Position = newPosition;
            camera.LookDirection = center - newPosition;
            camera.LookDirection.Normalize();
            camera.UpDirection = modelYAxis;
        }

        private void RotateVertical(double delta)
        {
            var center = GetModelCenter();
            var lookDirection = center - camera.Position;
            lookDirection.Normalize();
            var right = Vector3D.CrossProduct(lookDirection, camera.UpDirection);
            right.Normalize();

            var angle = -delta * ROTATION_SPEED;
            var rotation = new RotateTransform3D(new AxisAngleRotation3D(right, angle));

            var relativePosition = camera.Position - center;
            var newPosition = rotation.Transform(relativePosition) + center;

            // 限制垂直旋转角度
            var upAngle = Vector3D.AngleBetween(newPosition - center, new Vector3D(0, 1, 0));
            if (upAngle < 5 || upAngle > 175)
                return;

            camera.Position = newPosition;
            camera.LookDirection = center - newPosition;
            camera.LookDirection.Normalize();
            camera.UpDirection = rotation.Transform(camera.UpDirection);
        }

        private void HandlePanning(Vector delta)
        {
            var right = Vector3D.CrossProduct(camera.LookDirection, camera.UpDirection);
            right.Normalize();
            var distance = (camera.Position - GetModelCenter()).Length;

            var translation = (right * -delta.X + camera.UpDirection * delta.Y) * (distance * PAN_SPEED);
            camera.Position += translation;
        }

        private void HandleZoom(double delta)
        {
            var zoomDirection = camera.LookDirection;
            zoomDirection.Normalize();
            var distance = (camera.Position - GetModelCenter()).Length;

            var zoomAmount = delta * ZOOM_SPEED * distance;
            var newDistance = distance - zoomAmount;

            if (newDistance < MIN_DISTANCE || newDistance > MAX_DISTANCE)
                return;

            camera.Position += zoomDirection * zoomAmount;
        }

        private Point3D GetModelCenter()
        {
            return new Point3D(
                modelBounds.X + modelBounds.SizeX / 2,
                modelBounds.Y + modelBounds.SizeY / 2,
                modelBounds.Z + modelBounds.SizeZ / 2
            );
        }

        public void SetView(Vector3D direction, Vector3D up)
        {
            var center = GetModelCenter();
            var distance = Math.Sqrt(
                modelBounds.SizeX * modelBounds.SizeX +
                modelBounds.SizeY * modelBounds.SizeY +
                modelBounds.SizeZ * modelBounds.SizeZ
            );

            camera.Position = center - direction * distance;
            camera.LookDirection = direction;
            camera.UpDirection = up;
        }

        // 添加一个方法来根据鼠标位置获取3D空间中的点
        private Point3D? GetPoint3DFromMousePosition(Point mousePosition)
        {
            if (ViewPort == null)
                return null;

            // 使用 HitTest 获取鼠标位置的 3D 点
            var hitResult = VisualTreeHelper.HitTest(ViewPort, mousePosition) as RayMeshGeometry3DHitTestResult;
            if (hitResult != null)
            {
                return hitResult.PointHit;
            }

            // 如果没有击中任何物体，计算与XY平面的交点
            Point3D rayOrigin = camera.Position;
            Vector3D rayDirection = camera.LookDirection;

            // 如果射线几乎平行于XY平面，返回null
            if (Math.Abs(rayDirection.Z) < 0.0001)
                return null;

            // 计算射线与XY平面的交点
            double t = -rayOrigin.Z / rayDirection.Z;
            return rayOrigin + rayDirection * t;
        }

        // 修改缩放方法，基于鼠标位置
        public void OnMouseWheel(double delta, Point mousePosition)
        {
            var zoomFactor = delta > 0 ? 0.9 : 1.1;

            // 获取鼠标位置对应的3D点
            var targetPoint = GetPoint3DFromMousePosition(mousePosition);
            if (!targetPoint.HasValue)
            {
                targetPoint = GetModelCenter();  // 如果无法获取鼠标位置的3D点，使用模型中心
            }

            // 计算相机到目标点的向量
            var toTarget = targetPoint.Value - camera.Position;
            var distance = toTarget.Length;

            // 计算新的距离
            var newDistance = distance * zoomFactor;

            // 限制缩放范围
            if (newDistance < MIN_DISTANCE || newDistance > MAX_DISTANCE)
                return;

            // 计算新的相机位置
            var direction = toTarget;
            direction.Normalize();
            var newPosition = targetPoint.Value - direction * newDistance;

            // 更新相机位置
            camera.Position = newPosition;
            camera.LookDirection = targetPoint.Value - newPosition;
            camera.LookDirection.Normalize();
        }
    }
}