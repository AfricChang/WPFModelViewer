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

namespace WpfApplication2
{

    public partial class MainWindow : Window
    {
        //声明摄像头
        PerspectiveCamera myPCamera;
        //鼠标灵敏度调节
        double mouseDeltaFactor = 0.4;

        public MainWindow()
        {
            InitializeComponent();
            
            //摄像头
            myPCamera = new PerspectiveCamera();
            myPCamera.Position = new Point3D(0, 0, 200);
            myPCamera.LookDirection = new Vector3D(0, 0, -1);
            myPCamera.FieldOfView = 1000;
            vp.Camera = myPCamera;

            Model3DGroup myModel3DGroup = new Model3DGroup();
            //光源
            //AmbientLight （自然光）
            //DirectionalLight （方向光）
            //PointLight （点光源）
            //SpotLight （聚光源）
            DirectionalLight myDirectionalLight = new DirectionalLight();
            myDirectionalLight.Color = Colors.White;
            myDirectionalLight.Direction = new Vector3D(0.61, 0.5, 0.61);
            myModel3DGroup.Children.Add(myDirectionalLight);

            //DirectionalLight myDirectionalLight2 = new DirectionalLight();
            //myDirectionalLight2.Color = Colors.White;
            //myDirectionalLight2.Direction = new Vector3D(0.61, 0.5, 0.61);
            //myModel3DGroup.Children.Add(myDirectionalLight2);



            //new一个loader对象
            WavefrontObjLoader wfl = new WavefrontObjLoader();
            //ModelVisual3DWithName是WavefrontObjLoader定义的继承ModelVisual3D的对象，直接使用ModelVisual3D也是可以的
            //导入obj，第一个模型命名为m
            ModelVisual3DWithName m = wfl.LoadObjFile(@"E:\CAD\musou\ModelViewer\WpfApplication2\Lancer_Evolution_10.obj");          
            m.Content = myModel3DGroup;
            //导入obj，第二个模型命名为n
            var n = wfl.LoadObjFile(@"E:\CAD\musou\ModelViewer\WpfApplication2\精细人体.obj");
            n.Content = myModel3DGroup;

            //下面是调整n的位置，初学者可以先注释掉。
            var tt = new TranslateTransform3D();
            tt.OffsetX = 110;
            tt.OffsetZ = -50;
            tt.OffsetY =-100;
            var tr = new RotateTransform3D();            
            tr.Rotation = new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90);

            var tr2 = new RotateTransform3D();
            tr2.Rotation= new AxisAngleRotation3D(new Vector3D(0, 0, 1), -45);

            var ts = new ScaleTransform3D();
            ts.ScaleX = 1.5;
            ts.ScaleY = 1.5;
            ts.ScaleZ = 1.6;
            var tg = new Transform3DGroup();
            tg.Children.Add(tr); tg.Children.Add(tr2); tg.Children.Add(tt); tg.Children.Add(ts);
            n.Transform = tg;
            //将两个模型添加到场景中
            vp.Children.Add(m);
            vp.Children.Add(n);
            //添加鼠标事件，用于显示隐藏光晕特效
            vp.MouseEnter += Vp_MouseEnter;
            vp.MouseLeave += Vp_MouseLeave; 
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





        //鼠标位置
        Point mouseLastPosition;

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
            if (Mouse.LeftButton == MouseButtonState.Pressed)//如果按下鼠标左键

            {

                Point newMousePosition = e.GetPosition(this);

                if (mouseLastPosition.X != newMousePosition.X)

                {
                    //进行水平旋转
                    HorizontalTransform(mouseLastPosition.X < newMousePosition.X, mouseDeltaFactor);//水平变换

                }

                if (mouseLastPosition.Y != newMousePosition.Y)// change position in the horizontal direction

                {
                    //进行垂直旋转
                    VerticalTransform(mouseLastPosition.Y > newMousePosition.Y, mouseDeltaFactor);//垂直变换 

                }

                mouseLastPosition = newMousePosition;

            }

        }

        //鼠标滚轮缩放
        private void VP_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double scaleFactor = 13;
            //120 near ,   -120 far
            System.Diagnostics.Debug.WriteLine(e.Delta.ToString());
            Point3D currentPosition = myPCamera.Position;
            Vector3D lookDirection = myPCamera.LookDirection;//new Vector3D(camera.LookDirection.X, camera.LookDirection.Y, camera.LookDirection.Z);
            lookDirection.Normalize();

            lookDirection *= scaleFactor;

            if (e.Delta == 120)//getting near
            {
               //myPCamera.FieldOfView /= 1.2;

                if ((currentPosition.X + lookDirection.X) * currentPosition.X > 0)
                {
                    currentPosition += lookDirection;
                }
            }
            if (e.Delta == -120)//getting far
            {
               //myPCamera.FieldOfView *= 1.2;
                currentPosition -= lookDirection;
            }

            Point3DAnimation positionAnimation = new Point3DAnimation();
            positionAnimation.BeginTime = new TimeSpan(0, 0, 0);
            positionAnimation.Duration = TimeSpan.FromMilliseconds(100);
            positionAnimation.To = currentPosition;
            positionAnimation.From = myPCamera.Position;
            positionAnimation.Completed += new EventHandler(positionAnimation_Completed);
            myPCamera.BeginAnimation(PerspectiveCamera.PositionProperty, positionAnimation, HandoffBehavior.Compose);
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
       
