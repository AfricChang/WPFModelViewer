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

        // 添加成员变量存储模型边界
        private Rect3D modelBounds = Rect3D.Empty;

        // 添加一个属性来存储和获取模型
        private Point3D ModelCenter
        {
            get
            {
                if (!IsValidRect3D(modelBounds))
                {
                    // 如果边界无效，返回原点
                    return new Point3D(0, 0, 0);
                }
                return new Point3D(
                    modelBounds.X + modelBounds.SizeX / 2,
                    modelBounds.Y + modelBounds.SizeY / 2,
                    modelBounds.Z + modelBounds.SizeZ / 2
                );
            }
        }

        private readonly MouseCameraController cameraController;

        public MainWindow()
        {
            InitializeComponent();

            // 初始化相机和控制器
            myPCamera = InitializeCamera();
            MainViewport.Camera = myPCamera;
            cameraController = new MouseCameraController(MainViewport, myPCamera, Rect3D.Empty);

            // 初始化光照
            SetupLighting();
            //AddDirectionLight();

            //网格线
            //AddGridLines();

            // 加载3D模型
            LoadAndSetup3DModels();

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

        //平行光/方向光
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

        //聚光灯
        public Model3DGroup CreateSpotLightModel()
        {
            var model3DGroup = new Model3DGroup();
            var directionalLight = new SpotLight
            {
                Color = Colors.White,
                Direction = new Vector3D(0.61, 0.5, 0.61)
            };
            model3DGroup.Children.Add(directionalLight);
            return model3DGroup;
        }

        //点光源
        public Model3DGroup CreatePointLight()
        {
            var model3DGroup = new Model3DGroup();
            var pointLight = new PointLight
            {
                Color = Colors.White,           // 光源颜色
                Position = new Point3D(0, 0, 1000),  // 光源位置
                Range = 20,                     // 光照范围
                ConstantAttenuation = 1.0,      // 常量衰减因子
                LinearAttenuation = 0.1,        // 线性衰减因子
                QuadraticAttenuation = 0.05     // 二次衰减因子
            };
            model3DGroup.Children.Add(pointLight);
            return model3DGroup;
        }

        private void SetupLighting()
        {
            var model3DGroup = new Model3DGroup();

            // 环境光（通过 AmbientLight 实现）
            var ambientLight = new AmbientLight(Color.FromRgb(50, 50, 50));
            model3DGroup.Children.Add(ambientLight);

            // 主光源（方向光）
            var mainLight = new DirectionalLight(
                Colors.White,
                new Vector3D(-1, -1, -1)
            );
            model3DGroup.Children.Add(mainLight);

            // 填充光（点光源）
            var fillLight = new PointLight(
                Color.FromRgb(200, 200, 255),
                new Point3D(50, 50, 50)
            );
            model3DGroup.Children.Add(fillLight);

            // 重点照明（聚光灯）
            var spotLight = new SpotLight(
                Colors.Gray,
                new Point3D(0, 5, 0),
                new Vector3D(0, -1, 0),
                30,  // innerConeAngle
                60   // outerConeAngle
            );
            model3DGroup.Children.Add(spotLight);

            // 添加到场景
            var modelVisual = new ModelVisual3D();
            modelVisual.Content = model3DGroup;
            MainViewport.Children.Add(modelVisual);
        }

        private void LoadAndSetup3DModels()
        {
            var wfl = new WavefrontObjLoader();

            // 获取相对路径
            string basePath = Path.Combine("..", "data");

            // 加载第一个模型
            var model1 = wfl.LoadObjFile(Path.Combine(basePath, "cube_mtllib_after_g.obj"));

            // 加载第二个模型
            var model2 = wfl.LoadObjFile(Path.Combine(basePath, "WusonOBJ.obj"));

            // 加载第三个模型
            var model3 = wfl.LoadObjFile(Path.Combine(basePath, "box.obj"));

            MainViewport.Children.Add(model1);
            MainViewport.Children.Add(model2);
            MainViewport.Children.Add(model3);

            // 计算并保存模型边界
            CalculateAndStoreBounds();

            ChangeView(
                new Vector3D(-1, -1, 0),  // 向下看
                new Vector3D(0, 0, -1)   // 方向
            );
        }

        // 计算和存储边界
        private void CalculateAndStoreBounds()
        {
            modelBounds = Rect3D.Empty;

            foreach (var child in MainViewport.Children)
            {
                if (child is ModelVisual3D modelVisual)
                {
                    var bounds = CalculateVisualBounds(modelVisual);
                    if (!IsValidRect3D(bounds))
                        continue;

                    if (modelBounds.IsEmpty)
                        modelBounds = bounds;
                    else
                        modelBounds = Union(modelBounds, bounds);
                }
            }

            // 输出边界信息用于调试
            System.Diagnostics.Debug.WriteLine($"Model Bounds: X={modelBounds.X}, Y={modelBounds.Y}, Z={modelBounds.Z}");
            System.Diagnostics.Debug.WriteLine($"Size: W={modelBounds.SizeX}, H={modelBounds.SizeY}, D={modelBounds.SizeZ}");

            cameraController.UpdateModelBounds(modelBounds);
        }

        // 检查 Rect3D 是否有效
        private bool IsValidRect3D(Rect3D rect)
        {
            // 检查是否为空
            if (rect.IsEmpty)
                return false;

            // 检查是否包含 NaN
            if (double.IsNaN(rect.X) || double.IsNaN(rect.Y) || double.IsNaN(rect.Z) ||
                double.IsNaN(rect.SizeX) || double.IsNaN(rect.SizeY) || double.IsNaN(rect.SizeZ))
                return false;

            // 检查是否包含无穷大
            if (double.IsInfinity(rect.X) || double.IsInfinity(rect.Y) || double.IsInfinity(rect.Z) ||
                double.IsInfinity(rect.SizeX) || double.IsInfinity(rect.SizeY) || double.IsInfinity(rect.SizeZ))
                return false;

            // 检查大小是否为负数
            if (rect.SizeX < 0 || rect.SizeY < 0 || rect.SizeZ < 0)
                return false;

            return true;
        }

        // 合并Rect3D方法(防止Rect3D NaN问题)
        private Rect3D Union(Rect3D rect1, Rect3D rect2)
        {
            if (!IsValidRect3D(rect1))
                return rect2;
            if (!IsValidRect3D(rect2))
                return rect1;

            double minX = Math.Min(rect1.X, rect2.X);
            double minY = Math.Min(rect1.Y, rect2.Y);
            double minZ = Math.Min(rect1.Z, rect2.Z);
            double maxX = Math.Max(rect1.X + rect1.SizeX, rect2.X + rect2.SizeX);
            double maxY = Math.Max(rect1.Y + rect1.SizeY, rect2.Y + rect2.SizeY);
            double maxZ = Math.Max(rect1.Z + rect1.SizeZ, rect2.Z + rect2.SizeZ);

            return new Rect3D(minX, minY, minZ, maxX - minX, maxY - minY, maxZ - minZ);
        }

        private Rect3D CalculateVisualBounds(ModelVisual3D visual)
        {
            Rect3D bounds = Rect3D.Empty;

            // 处理当前节点的内容
            if (visual.Content != null)
            {
                var contentBounds = CalculateModelBounds(visual.Content);
                if (IsValidRect3D(contentBounds))
                {
                    if (bounds.IsEmpty)
                        bounds = contentBounds;
                    else
                        bounds = Union(bounds, contentBounds);
                }
            }

            // 递归处理所有子节点
            foreach (var child in visual.Children)
            {
                if (child is ModelVisual3D childVisual)
                {
                    var childBounds = CalculateVisualBounds(childVisual);
                    if (IsValidRect3D(childBounds))
                    {
                        if (bounds.IsEmpty)
                            bounds = childBounds;
                        else
                            bounds = Union(bounds, childBounds);
                    }
                }
            }

            return bounds;
        }

        private Rect3D CalculateModelBounds(Model3D model)
        {
            if (model == null)
                return Rect3D.Empty;

            if (model is GeometryModel3D geometryModel)
            {
                var bounds = geometryModel.Bounds;
                var transform = geometryModel.Transform;
                if (transform != null)
                {
                    // 应用变换到边界的八个角点
                    var matrix = transform.Value;
                    var corners = new Point3D[]
                    {
                        new Point3D(bounds.X, bounds.Y, bounds.Z),
                        new Point3D(bounds.X + bounds.SizeX, bounds.Y, bounds.Z),
                        new Point3D(bounds.X, bounds.Y + bounds.SizeY, bounds.Z),
                        new Point3D(bounds.X + bounds.SizeX, bounds.Y + bounds.SizeY, bounds.Z),
                        new Point3D(bounds.X, bounds.Y, bounds.Z + bounds.SizeZ),
                        new Point3D(bounds.X + bounds.SizeX, bounds.Y, bounds.Z + bounds.SizeZ),
                        new Point3D(bounds.X, bounds.Y + bounds.SizeY, bounds.Z + bounds.SizeZ),
                        new Point3D(bounds.X + bounds.SizeX, bounds.Y + bounds.SizeY, bounds.Z + bounds.SizeZ)
                    };

                    var transformedCorners = corners.Select(p => matrix.Transform(p)).ToArray();

                    // 计算变换后的边界
                    var minX = transformedCorners.Min(p => p.X);
                    var minY = transformedCorners.Min(p => p.Y);
                    var minZ = transformedCorners.Min(p => p.Z);
                    var maxX = transformedCorners.Max(p => p.X);
                    var maxY = transformedCorners.Max(p => p.Y);
                    var maxZ = transformedCorners.Max(p => p.Z);

                    return new Rect3D(minX, minY, minZ, maxX - minX, maxY - minY, maxZ - minZ);
                }
                return bounds;
            }
            else if (model is Model3DGroup modelGroup)
            {
                Rect3D groupBounds = Rect3D.Empty;
                foreach (var child in modelGroup.Children)
                {
                    var childBounds = CalculateModelBounds(child);
                    if (groupBounds.IsEmpty)
                        groupBounds = childBounds;
                    else if (!childBounds.IsEmpty)
                        groupBounds.Union(childBounds);
                }

                // 应用组的变换
                if (modelGroup.Transform != null)
                {
                    var matrix = modelGroup.Transform.Value;
                    var corners = new Point3D[]
                    {
                        new Point3D(groupBounds.X, groupBounds.Y, groupBounds.Z),
                        new Point3D(groupBounds.X + groupBounds.SizeX, groupBounds.Y, groupBounds.Z),
                        new Point3D(groupBounds.X, groupBounds.Y + groupBounds.SizeY, groupBounds.Z),
                        new Point3D(groupBounds.X + groupBounds.SizeX, groupBounds.Y + groupBounds.SizeY, groupBounds.Z),
                        new Point3D(groupBounds.X, groupBounds.Y, groupBounds.Z + groupBounds.SizeZ),
                        new Point3D(groupBounds.X + groupBounds.SizeX, groupBounds.Y, groupBounds.Z + groupBounds.SizeZ),
                        new Point3D(groupBounds.X, groupBounds.Y + groupBounds.SizeY, groupBounds.Z + groupBounds.SizeZ),
                        new Point3D(groupBounds.X + groupBounds.SizeX, groupBounds.Y + groupBounds.SizeY, groupBounds.Z + groupBounds.SizeZ)
                    };

                    var transformedCorners = corners.Select(p => matrix.Transform(p)).ToArray();

                    var minX = transformedCorners.Min(p => p.X);
                    var minY = transformedCorners.Min(p => p.Y);
                    var minZ = transformedCorners.Min(p => p.Z);
                    var maxX = transformedCorners.Max(p => p.X);
                    var maxY = transformedCorners.Max(p => p.Y);
                    var maxZ = transformedCorners.Max(p => p.Z);

                    return new Rect3D(minX, minY, minZ, maxX - minX, maxY - minY, maxZ - minZ);
                }
                return groupBounds;
            }

            return Rect3D.Empty;
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
            MainViewport.MouseEnter += MainViewport_MouseEnter;
            MainViewport.MouseLeave += MainViewport_MouseLeave;
        }

        // 鼠标滚轮事件处理
        private void MainViewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            cameraController.OnMouseWheel(e.Delta, e.GetPosition(MainViewport));
        }

        private void MainViewport_MouseLeave(object sender, MouseEventArgs e)
        {
            MainViewport.Effect = null;
        }

        private void MainViewport_MouseEnter(object sender, MouseEventArgs e)
        {
            DropShadowEffect BlurRadius = new DropShadowEffect();

            BlurRadius.BlurRadius = 20;
            BlurRadius.Color = Colors.Yellow;
            BlurRadius.Direction = 0;
            BlurRadius.Opacity = 1;
            BlurRadius.ShadowDepth = 0;
            MainViewport.Effect = BlurRadius;
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

        private void MainViewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            cameraController.OnMouseDown(e.GetPosition(this), e.ChangedButton);
        }

        //鼠标旋转
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            cameraController.OnMouseMove(e.GetPosition(this));
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            cameraController.OnMouseUp(e.ChangedButton);
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
            if (!IsValidRect3D(modelBounds))
            {
                // 如果边界无效，使用原来的方式
                Vector3D position = new Vector3D(myPCamera.Position.X, myPCamera.Position.Y, myPCamera.Position.Z);
                Vector3D rotateAxis = Vector3D.CrossProduct(position, myPCamera.UpDirection);
                RotateTransform3D rt3d = new RotateTransform3D();
                AxisAngleRotation3D rotate = new AxisAngleRotation3D(rotateAxis, angleDeltaFactor * (upDown ? 1 : -1));
                rt3d.Rotation = rotate;
                Matrix3D matrix1 = rt3d.Value;
                Point3D newPosition1 = matrix1.Transform(myPCamera.Position);
                myPCamera.Position = newPosition1;
                myPCamera.LookDirection = new Vector3D(-newPosition1.X, -newPosition1.Y, -newPosition1.Z);

                Vector3D newUpDirection1 = Vector3D.CrossProduct(myPCamera.LookDirection, rotateAxis);
                newUpDirection1.Normalize();
                myPCamera.UpDirection = newUpDirection1;
                return;
            }

            var center = ModelCenter;

            // 计算当前相机相对于中心点的位置
            var relativePosition = myPCamera.Position - center;

            // 计算旋转轴（右手向量，垂直于当前视线和上方向）
            var lookDirection = center - myPCamera.Position;
            lookDirection.Normalize();
            var rotationAxis = Vector3D.CrossProduct(lookDirection, myPCamera.UpDirection);
            rotationAxis.Normalize();

            // 创建旋转变换
            var rotateTransform = new RotateTransform3D();
            var rotation = new AxisAngleRotation3D(
                rotationAxis,
                angleDeltaFactor * (upDown ? 1 : -1)
            );
            rotateTransform.Rotation = rotation;

            // 应��旋转
            var matrix = rotateTransform.Value;
            var newRelativePosition = matrix.Transform(relativePosition);

            // 转换回世界坐标
            var newPosition = newRelativePosition + center;

            // 更新相机位置
            myPCamera.Position = newPosition;

            // 更新相机方向
            myPCamera.LookDirection = center - newPosition;
            myPCamera.LookDirection.Normalize();

            // 更新相机上方向
            var newUpDirection = matrix.Transform(myPCamera.UpDirection);
            myPCamera.UpDirection = newUpDirection;
        }
        // 水平变换：
        private void HorizontalTransform(bool leftRight, double angleDeltaFactor)
        {
            if (!IsValidRect3D(modelBounds))
            {
                // 如果边界无效，使用原来的方式
                Vector3D rotateAxis = myPCamera.UpDirection;
                RotateTransform3D rt3d = new RotateTransform3D();
                AxisAngleRotation3D rotate = new AxisAngleRotation3D(rotateAxis, angleDeltaFactor * (leftRight ? 1 : -1));
                rt3d.Rotation = rotate;
                Matrix3D matrix1 = rt3d.Value;
                Point3D newPosition1 = matrix1.Transform(myPCamera.Position);
                myPCamera.Position = newPosition1;
                myPCamera.LookDirection = new Vector3D(-newPosition1.X, -newPosition1.Y, -newPosition1.Z);
                return;
            }

            var center = ModelCenter;

            // 计算型的Y轴方向（从边界框底部指向顶部）
            var modelYAxis = new Vector3D(0, modelBounds.Location.X, 0);
            modelYAxis.Normalize();

            // 创建以模型中心为原点的旋转变换
            var rotateTransform = new RotateTransform3D();
            var rotation = new AxisAngleRotation3D(
                modelYAxis,  // 使用模型的Y轴作为旋转轴
                angleDeltaFactor * (leftRight ? 1 : -1)
            );
            rotateTransform.Rotation = rotation;

            // 将相机位置转换为相对于模型中心的坐标
            var relativePosition = myPCamera.Position - center;

            // 应用旋转
            var matrix = rotateTransform.Value;
            var newRelativePosition = matrix.Transform(relativePosition);

            // 转换回世界坐标
            var newPosition = newRelativePosition + center;

            // 更新相机位置
            myPCamera.Position = newPosition;

            // 更新相机方向
            myPCamera.LookDirection = center - newPosition;
            myPCamera.LookDirection.Normalize();

            // 保持相机的上方向与模型Y轴对齐
            myPCamera.UpDirection = modelYAxis;
        }

        private void Exit_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void SaveViewportToPng(string filePath, Color backgroundColor)
        {
            // 获取viewport的大小
            var width = (int)MainViewport.ActualWidth;
            var height = (int)MainViewport.ActualHeight;

            // 创建RenderTargetBitmap
            var renderBitmap = new RenderTargetBitmap(
                width, height,
                96, 96,  // DPI设置
                PixelFormats.Pbgra32);

            // 创建DrawingVisual来绘制背景
            var drawingVisual = new DrawingVisual();
            using (var context = drawingVisual.RenderOpen())
            {
                //绘制背景
                context.DrawRectangle(
                    new SolidColorBrush(backgroundColor),
                    null,
                    new Rect(0, 0, width, height));
            }
            renderBitmap.Render(drawingVisual);

            // 渲染Viewport3D
            renderBitmap.Render(MainViewport);

            // 创建PNG编码器
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            // 保存文件
            using (var stream = File.Create(filePath))
            {
                encoder.Save(stream);
            }
        }

        //保存为图片
        private void SaveImage_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG图片|*.png",
                Title = "保存视图为图片"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                // 使用黑色作为默认背景色，这里的颜色可自定义
                //Color.FromArgb A=0，表示全透明。
                SaveViewportToPng(saveFileDialog.FileName, Color.FromArgb(0, 0, 0, 0));
                //SaveViewportToPng(saveFileDialog.FileName, Colors.Black);
                MessageBox.Show("图片保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // 修改视图切换
        private void ChangeView(Vector3D direction, Vector3D up)
        {
            cameraController.SetView(direction, up);
        }

        // 修改各个视图
        private void TopView_Click(object sender, RoutedEventArgs e)
        {
            ChangeView(
                new Vector3D(0, -1, 0),  // 向下看
                new Vector3D(0, 0, -1)   // 上方向
            );
        }

        private void BottomView_Click(object sender, RoutedEventArgs e)
        {
            ChangeView(
                new Vector3D(0, 1, 0),   // 向上看
                new Vector3D(0, 0, 1)    // 上方向
            );
        }

        private void LeftView_Click(object sender, RoutedEventArgs e)
        {
            ChangeView(
                new Vector3D(1, 0, 0),   // 向右看
                new Vector3D(0, 1, 0)    // 上方向
            );
        }

        private void RightView_Click(object sender, RoutedEventArgs e)
        {
            ChangeView(
                new Vector3D(-1, 0, 0),  // 向左看
                new Vector3D(0, 1, 0)    // 上方向
            );
        }

        private void FrontView_Click(object sender, RoutedEventArgs e)
        {
            ChangeView(
                new Vector3D(0, 0, -1),  // 向前看
                new Vector3D(0, 1, 0)    // 上方向
            );
        }

        private void BackView_Click(object sender, RoutedEventArgs e)
        {
            ChangeView(
                new Vector3D(0, 0, 1),   // 向后看
                new Vector3D(0, 1, 0)    // 上方向
            );
        }

        // 修改现有的 CalculateSceneBounds 方法，使用存储的边界
        private Rect3D CalculateSceneBounds()
        {
            return modelBounds;
        }

        private void AddDirectionLight()
        {
            //var myModel3DGroup = CreateLightingModel();
            //var myModel3DGroup = CreateSpotLightModel();
            var myModel3DGroup = CreatePointLight();
            ModelVisual3D modelVisual3D = new ModelVisual3D();
            modelVisual3D.Content = myModel3DGroup;
            MainViewport.Children.Add(modelVisual3D);
        }

        // 创建网格
        private void AddGridLines()
        {
            Model3DGroup model3DGroup = new Model3DGroup();
            ModelVisual3D modelVisual3D = new ModelVisual3D();
            modelVisual3D.Content = model3DGroup;

            // 线的参数
            double lineLength = 95;    // 线长95米
            double spacing = 10;       // 间距10米
            int lineCount = 9;         // 9x9的网格
            double startOffset = -lineLength / 2;  // 起始偏移，使原点在中心
            Color gridColor = Color.FromRgb(127, 127, 127);  // 灰色

            var modelVisual = new ModelVisual3D();
            modelVisual.Content = model3DGroup;

            // 创建平行于X轴的线（沿Y方向排列）
            for (int i = 0; i < lineCount; i++)
            {
                double y = startOffset + (i * spacing);
                var line = new LinesVisual3D(
                    new Point3D(-lineLength / 2, y, 0),
                    new Point3D(lineLength / 2, y, 0),
                    gridColor
                );
                MainViewport.Children.Add(line);
            }

            // 创建平行于Y轴的线（沿X方向排列）
            for (int i = 0; i < lineCount; i++)
            {
                double x = startOffset + (i * spacing);
                var line = new LinesVisual3D(
                    new Point3D(x, -lineLength / 2, 0),
                    new Point3D(x, lineLength / 2, 0),
                    gridColor
                );
                MainViewport.Children.Add(line);
            }
        }

    }

}

