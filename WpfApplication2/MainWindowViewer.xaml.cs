using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using WpfApplication2;

namespace Wpf3DViewer
{
    public partial class MainWindowViewer : Window
    {
        private Point? _lastMousePosition = null;
        private bool _isDragging = false;

        public MainWindowViewer()
        {
            InitializeComponent();

            // Subscribe to mouse events
            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseLeftButtonUp += OnMouseLeftButtonUp;
            this.MouseMove += OnMouseMove;
            this.MouseWheel += OnMouseWheel;
            this.Loaded += MainWindowViewer_Loaded;
        }

        private void MainWindowViewer_Loaded(object sender, RoutedEventArgs e)
        {
            var wfl = new WavefrontObjLoader();

            // ��ȡ���·��
            string basePath = Path.Combine("..", "data");
            // ���ص�һ��ģ��
            var model1 = wfl.LoadObjFile(Path.Combine(basePath, "cube_mtllib_after_g.obj"));

            viewport.Children.Add(model1);
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _lastMousePosition = e.GetPosition(this);
            _isDragging = true;
            this.CaptureMouse();
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            this.ReleaseMouseCapture();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _lastMousePosition.HasValue)
            {
                var currentPos = e.GetPosition(this);
                var deltaX = currentPos.X - _lastMousePosition.Value.X;
                var deltaY = currentPos.Y - _lastMousePosition.Value.Y;

                // Rotate camera based on mouse movement
                var rotationSpeed = 0.1;
                var lookDirection = camera.LookDirection;
                var upDirection = camera.UpDirection;

                var yaw = QuaternionExtensions.CreateFromAxisAngle(upDirection, deltaX * rotationSpeed);
                var pitch = QuaternionExtensions.CreateFromAxisAngle(Vector3D.CrossProduct(lookDirection, upDirection), deltaY * rotationSpeed);

                var rotation = Quaternion.Multiply(yaw, pitch);

                camera.LookDirection = lookDirection.TransformNormal(rotation);
                camera.UpDirection = upDirection.TransformNormal(rotation);

                _lastMousePosition = currentPos;
            }
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Zoom in/out based on mouse wheel movement
            var zoomSpeed = 0.1;
            var zoomDirection = Math.Sign(e.Delta) * zoomSpeed;

            var newPosition = camera.Position + camera.LookDirection * zoomDirection;
            camera.Position = newPosition;
        }
    }


    public static class QuaternionExtensions
    {
        // ����һ��Χ��ָ������ת�����Ƕȵ���Ԫ��
        public static Quaternion CreateFromAxisAngle(Vector3D axis, double angleInDegrees)
        {
            double angleInRadians = angleInDegrees * (Math.PI / 180.0);
            double halfAngle = angleInRadians / 2.0;
            double sinHalfAngle = Math.Sin(halfAngle);

            // ȷ���ṩ�����ǵ�λ����

            axis.Normalize();
            Vector3D normalizedAxis = axis;

            return new Quaternion(
                normalizedAxis.X * sinHalfAngle,
                normalizedAxis.Y * sinHalfAngle,
                normalizedAxis.Z * sinHalfAngle,
                Math.Cos(halfAngle)
            );
        }

    }
    public static class Vector3DExtensions
    {
        // ʹ����Ԫ���任�����������ڷ��߱任��
        public static Vector3D TransformNormal(this Vector3D vector, Quaternion quaternion)
        {
            // ��������չΪ��Ԫ�� (x, y, z, 0)����ȷ����ֻ����ת������ƽ��
            var qv = new Quaternion(vector.X, vector.Y, vector.Z, 0);

            // Ӧ����Ԫ���任��q * v * q^-1

            Quaternion quaternion1 = quaternion;
            quaternion1.Invert();

            var transformedQv = Quaternion.Multiply(quaternion, Quaternion.Multiply(qv, quaternion1));

            // ��ȡ�����������
            return new Vector3D(transformedQv.X, transformedQv.Y, transformedQv.Z);
        }
    }
}