using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.IO;
using Path = System.IO.Path;

namespace WpfApplication2
{

    public partial class MainWindow : Window
    {

        // 添加常量定义
        private const double MOUSE_SENSITIVITY = 0.4;
        private const double CAMERA_INITIAL_FOV = 1000;
        private const double CAMERA_INITIAL_Z = 200;
        private const double ZOOM_SCALE_FACTOR = 13;
        private const int MOUSE_WHEEL_DELTA = 120;

        private readonly PerspectiveCamera myPCamera;
        private Point mouseLastPosition;

        public MainWindow()
        {
            InitializeComponent();

            // 初始化相机
            myPCamera = InitializeCamera();
            vp.Camera = myPCamera;

            // 初始化光照
            var myModel3DGroup = CreateLightingModel();

            // 加载3D模型
            LoadAndSetup3DModels(myModel3DGroup);

            // 添加鼠标事件
            SetupMouseEvents();
        }

        private PerspectiveCamera InitializeCamera()
        {
            return new PerspectiveCamera
            {
                Position = new Point3D(0, 0, CAMERA_INITIAL_Z),
                LookDirection = new Vector3D(0, 0, -1),
                FieldOfView = CAMERA_INITIAL_FOV
            };
        }

        private Model3DGroup CreateLightingModel()
        {
            var model3DGroup = new Model3DGroup();
            var directionalLight = new DirectionalLight
            {
                Color = Colors.White,
                Direction = new Vector3D(0.61, 0.5, 0.61)
            };
            model3DGroup.Children.Add(directionalLight);
            return model3DGroup;
        }

        private void LoadAndSetup3DModels(Model3DGroup modelGroup)
        {
            var wfl = new WavefrontObjLoader();

            // 获取相对路径
            string basePath = Path.Combine("..", "data");

            // 加载第一个模型
            var model1 = wfl.LoadObjFile(Path.Combine(basePath, "Lancer_Evolution_10.obj"));
            model1.Content = modelGroup;

            // 加载第二个模型
            var model2 = wfl.LoadObjFile(Path.Combine(basePath, "精细人体.obj"));
            model2.Content = modelGroup;

            // 设置第二个模型的变换
            model2.Transform = CreateModelTransform();

            // 添加到视图
            vp.Children.Add(model1);
            vp.Children.Add(model2);
        }

        private Transform3D CreateModelTransform()
        {
            var transformGroup = new Transform3DGroup();

            // 旋转变换
            transformGroup.Children.Add(new RotateTransform3D
            {
                Rotation = new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90)
            });
            transformGroup.Children.Add(new RotateTransform3D
            {
                Rotation = new AxisAngleRotation3D(new Vector3D(0, 0, 1), -45)
            });

            // 平移变换
            transformGroup.Children.Add(new TranslateTransform3D(110, -100, -50));

            // 缩放变换
            transformGroup.Children.Add(new ScaleTransform3D(1.5, 1.5, 1.6));

            return transformGroup;
        }

        private void SetupMouseEvents()
        {
            vp.MouseEnter += Vp_MouseEnter;
            vp.MouseLeave += Vp_MouseLeave;
        }

        // 优化鼠标滚轮事件处理
        private void VP_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var currentPosition = myPCamera.Position;
            var lookDirection = myPCamera.LookDirection;
            lookDirection.Normalize();
            lookDirection *= ZOOM_SCALE_FACTOR;

            var isZoomingIn = e.Delta == MOUSE_WHEEL_DELTA;
            var newPosition = isZoomingIn ?
                (currentPosition + lookDirection) :
                (currentPosition - lookDirection);

            // 只在放大时检查位置
            if (!isZoomingIn || (newPosition.X * currentPosition.X > 0))
            {
                AnimateCameraPosition(currentPosition, newPosition);
            }
        }

        private void AnimateCameraPosition(Point3D from, Point3D to)
        {
            var animation = new Point3DAnimation
            {
                BeginTime = TimeSpan.Zero,
                Duration = TimeSpan.FromMilliseconds(100),
                From = from,
                To = to
            };
            animation.Completed += positionAnimation_Completed;
            myPCamera.BeginAnimation(PerspectiveCamera.PositionProperty, animation, HandoffBehavior.Compose);
        }

        private void Vp_MouseLeave(object sender, MouseEventArgs e)
        {
            vp.Effect = null;
        }

        private void Vp_MouseEnter(object sender, MouseEventArgs e)
        {
            DropShadowEffect BlurRadius = new DropShadowEffect();

            BlurRadius.BlurRadius = 20;
            BlurRadius.Color = Colors.Yellow;
            BlurRadius.Direction = 0;
            BlurRadius.Opacity = 1;
            BlurRadius.ShadowDepth = 0;
            vp.Effect = BlurRadius;
        }


        public HitTestResultBehavior HTResult(HitTestResult rawresult)
        {
            //MessageBox.Show(rawresult.ToString());
            // RayHitTestResult rayResult = rawresult as RayHitTestResult;
            RayHitTestResult rayResult = rawresult as RayHitTestResult;
            if (rayResult != null)
            {
                //RayMeshGeometry3DHitTestResult rayMeshResult = rayResult as RayMeshGeometry3DHitTestResult;
                RayHitTestResult rayMeshResultrayResult = rayResult as RayHitTestResult;
                if (rayMeshResultrayResult != null)
                {
                    //GeometryModel3D hitgeo = rayMeshResult.ModelHit as GeometryModel3D;
                    var visual3D = rawresult.VisualHit as ModelVisual3D;

                    //do something

                }
            }

            return HitTestResultBehavior.Continue;
        }

        private void vp_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mouseLastPosition = e.GetPosition(this);
            //RayHitTestParameters hitParams = new RayHitTestParameters(myPCamera.Position, myPCamera.LookDirection);
            //VisualTreeHelper.HitTest(vp.Children[0], null, ResultCallback, hitParams);

            //下面是进行点击触发检测，可忽略，注释
            Point3D testpoint3D = new Point3D(mouseLastPosition.X, mouseLastPosition.Y, 0);
            Vector3D testdirection = new Vector3D(mouseLastPosition.X, mouseLastPosition.Y, 100);
            PointHitTestParameters pointparams = new PointHitTestParameters(mouseLastPosition);
            RayHitTestParameters rayparams = new RayHitTestParameters(testpoint3D, testdirection);

            //test for a result in the Viewport3D
            VisualTreeHelper.HitTest(vp, null, HTResult, pointparams);


        }

        //鼠标旋转
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)//如按下鼠标左键

            {

                Point newMousePosition = e.GetPosition(this);

                if (mouseLastPosition.X != newMousePosition.X)

                {
                    //进行水平旋转
                    HorizontalTransform(mouseLastPosition.X < newMousePosition.X, MOUSE_SENSITIVITY);//水平变换

                }

                if (mouseLastPosition.Y != newMousePosition.Y)// change position in the horizontal direction

                {
                    //进行垂直旋转
                    VerticalTransform(mouseLastPosition.Y > newMousePosition.Y, MOUSE_SENSITIVITY);//垂直变换 

                }

                mouseLastPosition = newMousePosition;

            }

        }

        void positionAnimation_Completed(object sender, EventArgs e)
        {
            Point3D position = myPCamera.Position;
            myPCamera.BeginAnimation(PerspectiveCamera.PositionProperty, null);
            myPCamera.Position = position;
        }

        // 垂直变换
        private void VerticalTransform(bool upDown, double angleDeltaFactor)
        {
            Vector3D postion = new Vector3D(myPCamera.Position.X, myPCamera.Position.Y, myPCamera.Position.Z);
            Vector3D rotateAxis = Vector3D.CrossProduct(postion, myPCamera.UpDirection);
            RotateTransform3D rt3d = new RotateTransform3D();
            AxisAngleRotation3D rotate = new AxisAngleRotation3D(rotateAxis, angleDeltaFactor * (upDown ? 1 : -1));
            rt3d.Rotation = rotate;
            Matrix3D matrix = rt3d.Value;
            Point3D newPostition = matrix.Transform(myPCamera.Position);
            myPCamera.Position = newPostition;
            myPCamera.LookDirection = new Vector3D(-newPostition.X, -newPostition.Y, -newPostition.Z);

            //update the up direction
            Vector3D newUpDirection = Vector3D.CrossProduct(myPCamera.LookDirection, rotateAxis);
            newUpDirection.Normalize();
            myPCamera.UpDirection = newUpDirection;
        }
        // 水平变换：
        private void HorizontalTransform(bool leftRight, double angleDeltaFactor)
        {
            Vector3D postion = new Vector3D(myPCamera.Position.X, myPCamera.Position.Y, myPCamera.Position.Z);
            Vector3D rotateAxis = myPCamera.UpDirection;
            RotateTransform3D rt3d = new RotateTransform3D();
            AxisAngleRotation3D rotate = new AxisAngleRotation3D(rotateAxis, angleDeltaFactor * (leftRight ? 1 : -1));
            rt3d.Rotation = rotate;
            Matrix3D matrix = rt3d.Value;
            Point3D newPostition = matrix.Transform(myPCamera.Position);
            myPCamera.Position = newPostition;
            myPCamera.LookDirection = new Vector3D(-newPostition.X, -newPostition.Y, -newPostition.Z);
        }

    }

}

