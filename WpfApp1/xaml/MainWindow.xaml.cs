using Microsoft.Win32;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Media;
using HelixToolkit.Wpf;
using System.Windows.Media.Media3D;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Brushes = System.Windows.Media.Brushes;
using System.Windows.Media.Imaging;
using System.IO;
using MessageBox = System.Windows.MessageBox;
using System.Security.Policy;
using System.Windows.Threading;
using static WpfApp1.MeshHelper;
using MeshGeometry3D = System.Windows.Media.Media3D.MeshGeometry3D;
using GeometryModel3D = System.Windows.Media.Media3D.GeometryModel3D;
using Material = System.Windows.Media.Media3D.Material;
using DiffuseMaterial = System.Windows.Media.Media3D.DiffuseMaterial;
using CheckBox = System.Windows.Controls.CheckBox;
using System.Windows.Input;
using Point = System.Windows.Point;
using System.Reflection;
using ComboBox = System.Windows.Controls.ComboBox;
using Color = System.Windows.Media.Color;

namespace WpfApp1
{
    public enum DisplayMode
    {
        FillMode = 0,
        WireMode = 1
    }
    public partial class MainWindow : Window
    {
        private readonly ModelImporter modelImporter = new ModelImporter();
        private ObservableCollection<SceneObject> sceneObjects = new ObservableCollection<SceneObject>();
        private Dictionary<Model3D, List<LinesVisual3D>> m_dicModelVisualLines = new Dictionary<Model3D, List<LinesVisual3D>>();
        private Dictionary<Model3D, ModelVisual3D> m_dicModelVisualModels = new Dictionary<Model3D, ModelVisual3D>();
        private Dictionary<GeometryModel3D, Material> m_originalMaterials = new Dictionary<GeometryModel3D, Material>();
        private HashSet<GeometryModel3D> m_setSelectedModels = new HashSet<GeometryModel3D>();
        private List<Model3D> m_allModels = new List<Model3D>();
        //默认选中颜色
        //选中对象
        private SolidColorBrush m_selectedColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 255, 255, 0));
        //隐藏颜色
        private SolidColorBrush m_hiddenColor = new SolidColorBrush()
        {
            Color = Color.FromArgb(5, 255, 255, 255),
            Opacity = 0
        };
        private DisplayMode m_displayMode = DisplayMode.FillMode;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        private void HideGeometryModel3d(GeometryModel3D geometryModel)
        {
            DiffuseMaterial highlightMaterial = new DiffuseMaterial(m_hiddenColor);
            highlightMaterial.Color = Color.FromArgb(5, 255, 255, 255);
            highlightMaterial.AmbientColor = Color.FromArgb(5, 255, 255, 255);
            geometryModel.Material = highlightMaterial;
            geometryModel.SetValue(VisibilityProperty, Visibility.Hidden);
            //geometryModel.BackMaterial = highlightMaterial;

            m_setSelectedModels.Add(geometryModel);
        }

        private void HideModel(Model3D model)
        {
            if (model is GeometryModel3D geometryModel)
            {
                HideGeometryModel3d(geometryModel);
            }
            else if (model is Model3DGroup modelGroup)
            {
                foreach (var item in modelGroup.Children)
                {
                    HideModel(item);
                }
            }
        }

        private void PopulateSceneTreeView(Model3DGroup modelGroup, String name)
        {
            sceneTreeView.AllowDrop = true;
            TreeViewItem newItem = new TreeViewItem { Header = name };

            sceneTreeView.Items.Add(newItem);
            AddModelGroupToTreeView(modelGroup, null);
        }

        private void AddModelGroupToTreeView(Model3DGroup modelGroup, TreeViewItem parentItem)
        {
            foreach (var model in modelGroup.Children)
            {
                TreeViewItem newItem = new TreeViewItem { Header = model.GetName() };
                if (parentItem == null)
                {
                    sceneTreeView.Items.Add(newItem);
                }
                else
                {
                    parentItem.Items.Add(newItem);
                }

                if (model is Model3DGroup childGroup)
                {
                    AddModelGroupToTreeView(childGroup, newItem);
                }
            }
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "OBJ文件|*.obj|所有文件|*.*",
                Title = "选择3D模型文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 加载新模型
                    Model3DGroup model = modelImporter.Load(openFileDialog.FileName);
                    StoreOriginalMaterials(model);

                    // 创建模型可视化对象
                    var modelVisual = new ModelVisual3D { Content = model };
                    modelContainer.Children.Clear();
                    modelContainer.Children.Add(modelVisual);
                    m_allModels.Add(model);
                    RemoveBackMaterial(model);

                    m_dicModelVisualModels.Add(model, modelContainer);

                    // 将模型的子项添加到场景对象树中
                    PopulateSceneTreeView(model, Path.GetFileNameWithoutExtension(openFileDialog.FileName));
                    // 添加到场景树
                    var fileName = System.IO.Path.GetFileName(openFileDialog.FileName);
                    var sceneObject = new SceneObject
                    {
                        Name = fileName,
                        IsVisible = true,
                        Model = modelVisual
                    };
                    sceneObjects.Add(sceneObject);

                    // 调整视图以适应模型
                    viewPort3D.ZoomExtents();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"加载模型时出错：{ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void RemoveBackMaterial(Model3D model)
        {
            if (model is GeometryModel3D geometryModel)
            {
                geometryModel.BackMaterial = null;
            }
            else if (model is Model3DGroup modelGroup)
            {
                foreach (var item in modelGroup.Children)
                {
                    RemoveBackMaterial(item);
                }
            }
        }

        private void StoreOriginalMaterials(Model3DGroup modelGroup)
        {
            foreach (Model3D model in modelGroup.Children)
            {
                if (model is GeometryModel3D geometryModel)
                {
                    if (geometryModel.Material != null && !m_originalMaterials.ContainsKey(geometryModel))
                    {
                        m_originalMaterials[geometryModel] = geometryModel.Material;
                    }
                }
                else if (model is Model3DGroup childGroup)
                {
                    StoreOriginalMaterials(childGroup);
                }
            }
        }

        private void ClearModel_Click(object sender, RoutedEventArgs e)
        {
            while (viewPort3D.Children.Count > 3)
            {
                viewPort3D.Children.RemoveAt(viewPort3D.Children.Count - 1);
            }
            modelContainer.Children.Clear();
            sceneObjects.Clear();
            sceneTreeView.Items.Clear();
            m_dicModelVisualLines.Clear();

            m_dicModelVisualModels.Clear();
            m_originalMaterials.Clear();
            m_setSelectedModels.Clear();
            m_allModels.Clear();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.MessageBox.Show("Are you sure you want to exit?", "Exit", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes)
            {
                System.Windows.Application.Current.Shutdown();
            }
        }

        private void ResetView_Click(object sender, RoutedEventArgs e)
        {
            if (viewPort3D != null)
            {
                viewPort3D.ZoomExtents();
                viewPort3D.Camera.Position = new Point3D(8, 8, 8);
                viewPort3D.Camera.LookDirection = new Vector3D(-1, -1, -1);
                viewPort3D.Camera.UpDirection = new Vector3D(0, 1, 0);
            }
        }

        private void SetBackgroundColor_Click(object sender, RoutedEventArgs e)
        {
            // 打开颜色选择器
            var colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // 设置背景颜色
                var selectedColor = colorDialog.Color;
                viewPort3D.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(selectedColor.A, selectedColor.R, selectedColor.G, selectedColor.B));
            }
        }

        private ScaleTransform3D GetOrCreateVisibilityTransform(ModelVisual3D model)
        {
            // 如果当前Transform是Transform3DGroup
            if (model.Transform is Transform3DGroup group)
            {
                // 查找已存在的ScaleTransform3D
                foreach (var transform in group.Children)
                {
                    if (transform is ScaleTransform3D scaleTransform)
                    {
                        return scaleTransform;
                    }
                }

                // 如果没找到，创建新的并添加到组中
                var newScale = new ScaleTransform3D(1, 1, 1);
                group.Children.Add(newScale);
                return newScale;
            }
            // 如果当前Transform是ScaleTransform3D
            else if (model.Transform is ScaleTransform3D existingScale)
            {
                return existingScale;
            }
            // 如果当前Transform是其他类型或为null
            else
            {
                var group1 = new Transform3DGroup();
                // 保存原有的Transform
                if (model.Transform != null)
                {
                    group1.Children.Add(model.Transform);
                }
                var newScale = new ScaleTransform3D(1, 1, 1);
                group1.Children.Add(newScale);
                model.Transform = group1;
                return newScale;
            }
        }

        private void SceneObject_VisibilityChanged(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as System.Windows.Controls.CheckBox;
            if (checkbox != null)
            {
                var sceneObject = checkbox.DataContext as SceneObject;
                if (sceneObject != null && sceneObject.Model != null)
                {
                    sceneObject.IsVisible = checkbox.IsChecked ?? false;
                    var scaleTransform = GetOrCreateVisibilityTransform(sceneObject.Model);
                    double scale = checkbox.IsChecked == true ? 1 : 0;
                    scaleTransform.ScaleX = scale;
                    scaleTransform.ScaleY = scale;
                    scaleTransform.ScaleZ = scale;
                }
                else if (checkbox.DataContext is GeometryModel3D geometryModel3D)
                {
                    //geometryModel3D.IsVisible = checkbox.IsChecked ?? false;
                }
            }
        }

        private void SetColor_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = (menuItem.Parent as ContextMenu);
            if (contextMenu != null)
            {
                var sceneObject = ((contextMenu.PlacementTarget as FrameworkElement)?.DataContext as SceneObject);
                if (sceneObject?.Model?.Content is Model3DGroup model)
                {
                    // 获取第一个几何体的当前颜色作为参考
                    System.Windows.Media.Color currentColor = Colors.White;
                    if (model.Children.Count > 0 && model.Children[0] is GeometryModel3D firstGeometry)
                    {
                        var currentMaterial = firstGeometry.Material;
                        if (currentMaterial is MaterialGroup materialGroup)
                        {
                            var diffuseMaterial = materialGroup.Children[0] as DiffuseMaterial;
                            if (diffuseMaterial?.Brush is SolidColorBrush brush)
                            {
                                currentColor = brush.Color;
                            }
                        }
                        else if (currentMaterial is DiffuseMaterial diffuseMaterial)
                        {
                            if (diffuseMaterial.Brush is SolidColorBrush brush)
                            {
                                currentColor = brush.Color;
                            }
                        }
                    }

                    var colorDialog = new ColorPickerWindow(currentColor);
                    colorDialog.Owner = this;

                    if (colorDialog.ShowDialog() == true)
                    {
                        var color = colorDialog.SelectedColor;
                        var material = new DiffuseMaterial(new SolidColorBrush(color));

                        // 遍历所有几何体并应用新颜色
                        foreach (var child in model.Children)
                        {
                            if (child is GeometryModel3D geometryModel)
                            {
                                var currentMaterial = geometryModel.Material;
                                if (currentMaterial is MaterialGroup)
                                {
                                    var materialGroup = new MaterialGroup();
                                    materialGroup.Children.Add(material);
                                    // 保留原MaterialGroup中的其他材质（从索引1开始）
                                    for (int i = 1; i < ((MaterialGroup)currentMaterial).Children.Count; i++)
                                    {
                                        materialGroup.Children.Add(((MaterialGroup)currentMaterial).Children[i]);
                                    }
                                    geometryModel.Material = materialGroup;
                                    //geometryModel.BackMaterial = materialGroup;
                                }
                                else
                                {
                                    // 如果原来就是DiffuseMaterial，直接替换
                                    geometryModel.Material = material;
                                    //geometryModel.BackMaterial = material;
                                }
                            }
                        }
                    }
                }
            }
        }


        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            var saveImageDialog = new SaveImageWindow();
            saveImageDialog.Owner = this;

            if (saveImageDialog.ShowDialog() == true)
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PNG图片|*.png",
                    Title = "保存图片"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        // 保存当前背景色
                        var originalBackground = viewPort3D.Background;

                        // 设置背景色
                        if (saveImageDialog.SelectedBackground == SaveImageWindow.BackgroundType.Transparent)
                        {
                            viewPort3D.Background = Brushes.Transparent;
                        }
                        else if (saveImageDialog.SelectedBackground == SaveImageWindow.BackgroundType.White)
                        {
                            viewPort3D.Background = Brushes.White;
                        }
                        else
                        {
                            viewPort3D.Background = Brushes.Black;
                        }

                        // 等待两个渲染帧完成后再截图
                        Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
                        {
                            Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
                            {
                                try
                                {
                                    var renderBitmap = new RenderTargetBitmap(
                                        (int)viewPort3D.ActualWidth,
                                        (int)viewPort3D.ActualHeight,
                                        96, 96,
                                        PixelFormats.Pbgra32);
                                    renderBitmap.Render(viewPort3D);

                                    // 创建PNG编码器
                                    var encoder = new PngBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                                    // 保存图片
                                    using (var stream = File.Create(saveFileDialog.FileName))
                                    {
                                        encoder.Save(stream);
                                    }

                                    // 恢复原始背景色
                                    viewPort3D.Background = originalBackground;

                                    MessageBox.Show("图片保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                catch (Exception ex)
                                {
                                    // 恢复原始背景色
                                    viewPort3D.Background = originalBackground;
                                    MessageBox.Show($"保存图片时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }));
                        }));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"保存图片时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }


        private void ClearAllHighlights()
        {
            foreach (var model in m_allModels)
            {
                if (model is GeometryModel3D geometryModel)
                {
                    if (m_originalMaterials.ContainsKey(geometryModel))
                    {
                        geometryModel.Material = m_originalMaterials[geometryModel];
                    }
                }
                else if (model is Model3DGroup model3DGroup)
                {
                    foreach (var item in model3DGroup.Children)
                    {
                        if (item is GeometryModel3D geometryModel1)
                        {
                            if (m_originalMaterials.ContainsKey(geometryModel1))
                            {
                                geometryModel1.Material = m_originalMaterials[geometryModel1];
                            }
                        }
                    }
                }
            }
        }

        //Control+鼠标左键  +/-选
        private void HighlightGeometryModel3d(GeometryModel3D geometryModel,
            SolidColorBrush newBrush)
        {
            DiffuseMaterial highlightMaterial = new DiffuseMaterial(newBrush);
            geometryModel.Material = highlightMaterial;
            //geometryModel.BackMaterial = highlightMaterial;

            m_setSelectedModels.Add(geometryModel);
        }

        private void HighlightModel(Model3D model, SolidColorBrush solidColorBrush)
        {
            if (model is GeometryModel3D geometryModel)
            {
                HighlightGeometryModel3d(geometryModel, solidColorBrush);
            }
            else if (model is Model3DGroup modelGroup)
            {
                foreach (var item in modelGroup.Children)
                {
                    HighlightModel(item, solidColorBrush);
                }
            }
        }

        LinesVisual3D CreateLineBuilderFromEdges(List<List<Edge>> edges)
        {
            LinesVisual3D linesVisual3D = new LinesVisual3D();
            linesVisual3D.Thickness = 1;
            linesVisual3D.Color = Colors.Red;

            Point3DCollection point3Ds = new Point3DCollection();
            foreach (var item in edges)
            {
                foreach (var e in item)
                {
                    point3Ds.Add(e.V1Pos);
                    point3Ds.Add(e.V2Pos);
                }
            }
            linesVisual3D.Points = point3Ds;
            return linesVisual3D;
        }

        //Control+鼠标左键  +选
        //Shift+鼠标左键    -选
        private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePosition = e.GetPosition(viewPort3D);
            HitTestResult hit = VisualTreeHelper.HitTest(viewPort3D, mousePosition);
            //线框模式下

            //填充模式下
            if (m_displayMode == DisplayMode.FillMode)
            {
                if (!Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    ClearAllHighlights();
                }
            }
            else
            {
                //没有按下Ctrl，设置为线框模式下的颜色
                if (!Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    foreach (var model in m_allModels)
                    {
                        HighlightModel(model, m_hiddenColor);
                    }
                }

            }

            //线对象不需要选中
            if (!(hit.VisualHit is LinesVisual3D) && hit is RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult)
            {
                var geometryModel = rayMeshGeometry3DHitTestResult.ModelHit as GeometryModel3D;
                //已经选中
                if (m_setSelectedModels.Contains(geometryModel) &&
                    ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control))
                {
                    m_setSelectedModels.Remove(geometryModel);
                    if (m_displayMode == DisplayMode.WireMode)
                    {
                        geometryModel.Material = new DiffuseMaterial(m_hiddenColor);
                        //geometryModel.BackMaterial = new DiffuseMaterial(m_hiddenColor);
                    }
                    else
                    {
                        geometryModel.Material = m_originalMaterials[geometryModel];
                        //geometryModel.BackMaterial = m_originalMaterials[geometryModel];
                    }
                    return;
                }
                HighlightGeometryModel3d(geometryModel, m_selectedColor);
            }
        }

        private LinesVisual3D? CreateLineVisualModelFromGeometryModel(GeometryModel3D geometryModel)
        {
            if (geometryModel == null)
                return null;

            List<List<Edge>> edges = MeshHelper.ExtractBoundaryEdgesByNormal(geometryModel.Geometry as MeshGeometry3D);
            if (edges == null)
            {
                return null;
            }
            var lineModel = CreateLineBuilderFromEdges(edges);
            if (lineModel == null)
            {
                return null;
            }
            lineModel.SetName(geometryModel.GetName());
            return lineModel;
        }

        private void ShowModelLines(Model3D model, Transform3D parentTransform3D, List<LinesVisual3D> linesVisual3Ds)
        {
            if (model is GeometryModel3D geometryModel)
            {
                var lineModel = CreateLineVisualModelFromGeometryModel(geometryModel);
                if (lineModel != null)
                {
                    //别忘了矩阵，虽然可能大多数情况为单位矩阵
                    if (parentTransform3D != null)
                    {
                        lineModel.Transform = new MatrixTransform3D()
                        {
                            Matrix = parentTransform3D.Value * model.Transform.Value
                        };
                    }
                    else
                    {
                        lineModel.Transform = model.Transform;
                    }

                    viewPort3D.Children.Add(lineModel);
                    linesVisual3Ds.Add(lineModel);
                }
            }
            else if (model is Model3DGroup model3DGroup)
            {
                foreach (var item in model3DGroup.Children)
                {
                    if (parentTransform3D == null)
                    {
                        ShowModelLines(item, item.Transform, linesVisual3Ds);
                    }
                    else
                    {
                        var transform = new MatrixTransform3D();
                        transform.Matrix = parentTransform3D.Value * item.Transform.Value;

                        ShowModelLines(item, transform, linesVisual3Ds);
                    }
                }
            }

        }

        private void DisplayModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null)
            {
                return;
            }

            //填充,移除线框模型
            if (comboBox.SelectedIndex == 0)
            {
                m_displayMode = DisplayMode.FillMode;
                if (m_allModels.Count > 0)
                {
                    foreach (var item in m_dicModelVisualLines)
                    {
                        foreach (var linesVisual in item.Value)
                        {
                            viewPort3D.Children.Remove(linesVisual);
                        }
                    }
                    ClearAllHighlights();
                }
            }
            //轮廓，移除填充模型
            else
            {
                m_displayMode = DisplayMode.WireMode;
                foreach (var item in m_dicModelVisualModels)
                {
                    HideModel(item.Key);
                    //viewPort3D.Children.Remove(item.Value);
                }

                foreach (var model in m_allModels)
                {
                    if (m_dicModelVisualLines.ContainsKey(model))
                    {
                        foreach (var item in m_dicModelVisualLines[model])
                        {
                            viewPort3D.Children.Add(item);
                        }
                    }
                    else
                    {
                        List<LinesVisual3D> linesVisual3Ds = new List<LinesVisual3D>();
                        ShowModelLines(model, model.Transform, linesVisual3Ds);
                        m_dicModelVisualLines.Add(model, linesVisual3Ds);
                    }
                }

            }

        }

        private void SelectColor_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerWindow colorPickerWindow = new ColorPickerWindow(m_selectedColor.Color);
            if (colorPickerWindow.ShowDialog() == true)
            {
                m_selectedColor.Color = colorPickerWindow.SelectedColor;
            }
        }
    }
}