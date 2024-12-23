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

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private readonly ModelImporter modelImporter;
        private ObservableCollection<SceneObject> sceneObjects;

        public MainWindow()
        {
            InitializeComponent();
            modelImporter = new ModelImporter();
            sceneObjects = new ObservableCollection<SceneObject>();
            sceneTreeView.ItemsSource = sceneObjects;
            _originalMaterials = new Dictionary<GeometryModel3D, Material>();
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

                    //var lineBuilder = new LineBuilder();
                    //lineBuilder.AddBoundingBox(yourModelGeometry.Bound);

                    //var lineModel = new LineGeometryModel3D
                    //{
                    //    Geometry = lineBuilder.ToLineGeometry3D(), // 设置线框几何
                    //    Color = Colors.Green, // 设置线条颜色
                    //    Thickness = 2.0 // 设置线条粗细
                    //};


                    // 创建模型可视化对象
                    var modelVisual = new ModelVisual3D { Content = model };
                    modelContainer.Children.Add(modelVisual);
                    _allModels.Add(model);

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

        // 新增方法：递归存储所有几何模型的原始材质
        private void StoreOriginalMaterials(Model3DGroup modelGroup)
        {
            foreach (Model3D model in modelGroup.Children)
            {
                if (model is GeometryModel3D geometryModel)
                {
                    if (geometryModel.Material != null && !_originalMaterials.ContainsKey(geometryModel))
                    {
                        _originalMaterials[geometryModel] = geometryModel.Material;
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
            modelContainer.Children.Clear();
            sceneObjects.Clear();
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
                                    geometryModel.BackMaterial = materialGroup;
                                }
                                else
                                {
                                    // 如果原来就是DiffuseMaterial，直接替换
                                    geometryModel.Material = material;
                                    geometryModel.BackMaterial = material;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ShowOnlyCurrent_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var currentObject = ((menuItem?.Parent as ContextMenu)?.PlacementTarget as FrameworkElement)?.DataContext as SceneObject;

            if (currentObject != null)
            {
                foreach (var obj in sceneObjects)
                {
                    obj.IsVisible = (obj == currentObject);
                    if (obj.IsVisible)
                    {
                        if (!modelContainer.Children.Contains(obj.Model))
                        {
                            modelContainer.Children.Add(obj.Model);
                        }
                    }
                    else
                    {
                        modelContainer.Children.Remove(obj.Model);
                    }
                }
            }
        }

        private void HideOnlyCurrent_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var currentObject = ((menuItem?.Parent as ContextMenu)?.PlacementTarget as FrameworkElement)?.DataContext as SceneObject;

            if (currentObject != null)
            {
                foreach (var obj in sceneObjects)
                {
                    obj.IsVisible = (obj != currentObject);
                    if (obj.Model != null)
                    {
                        var scaleTransform = GetOrCreateVisibilityTransform(obj.Model);
                        double scale = obj.IsVisible ? 1 : 0;
                        scaleTransform.ScaleX = scale;
                        scaleTransform.ScaleY = scale;
                        scaleTransform.ScaleZ = scale;
                    }
                }
            }
        }

        private void RemoveObject_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = (menuItem.Parent as ContextMenu);
            if (contextMenu != null)
            {
                var sceneObject = ((contextMenu.PlacementTarget as FrameworkElement)?.DataContext as SceneObject);
                if (sceneObject != null)
                {
                    // 从场景中移除3D模型
                    if (sceneObject.Model != null)
                    {
                        modelContainer.Children.Remove(sceneObject.Model);
                    }

                    // 从树形视图中移除对象
                    sceneObjects.Remove(sceneObject);
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

        private void SceneTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ClearAllHighlights();
            if (e.NewValue is SceneObject sceneObject)
            {
                // Get the tag which should contain the 3D object reference
                var modelObject = sceneObject.Model.Content;
                if (modelObject != null)
                {
                    HighlightModel(modelObject);
                }
            }
        }

        // Helper method to clear all highlights
        private void ClearAllHighlights()
        {
            // Iterate through all models and reset their materials
            foreach (var model in _allModels)
            {
                if (model is GeometryModel3D geometryModel)
                {
                    // Restore original material if we have stored it
                    if (_originalMaterials.ContainsKey(geometryModel))
                    {
                        geometryModel.Material = _originalMaterials[geometryModel];
                    }
                }
                else if (model is Model3DGroup model3DGroup)
                {
                    foreach (var item in model3DGroup.Children)
                    {
                        if (item is GeometryModel3D geometryModel1)
                        {
                            if (_originalMaterials.ContainsKey(geometryModel1))
                            {
                                geometryModel1.Material = _originalMaterials[geometryModel1];
                            }
                        }
                    }
                }
            }
        }

        // Helper method to highlight a specific model
        private void HighlightModel(Model3D model)
        {
            if (model is GeometryModel3D geometryModel)
            {
                // Store original material if not already stored
                if (!_originalMaterials.ContainsKey(geometryModel))
                {
                    _originalMaterials[geometryModel] = geometryModel.Material;
                }

                // Create highlight material (e.g., yellow semi-transparent)
                var highlightMaterial = new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 255, 255, 0)));
                geometryModel.Material = highlightMaterial;
                geometryModel.BackMaterial = highlightMaterial;
            }
            else if (model is Model3DGroup modelGroup)
            {
                foreach (var item in modelGroup.Children)
                {
                    HighlightModel(item);
                }
            }
        }

        // Add this field to store original materials
        private Dictionary<GeometryModel3D, Material> _originalMaterials = new Dictionary<GeometryModel3D, Material>();
        private List<Model3D> _allModels = new List<Model3D>();

        private void FillMode_Click(object sender, RoutedEventArgs e)
        {

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

        private void WireModel_Click(object sender, RoutedEventArgs e)
        {
            foreach (var model in _allModels)
            {
                if (model is GeometryModel3D geometryModel)
                {
                    List<List<Edge>> edges = MeshHelper.ExtractBoundaryEdgesByNormal(geometryModel.Geometry as MeshGeometry3D);

                }
                else if (model is Model3DGroup model3DGroup)
                {
                    foreach (var item in model3DGroup.Children)
                    {
                        if (item is GeometryModel3D geometryModel1)
                        {
                            List<List<Edge>> edges = MeshHelper.ExtractBoundaryEdgesByNormal(geometryModel1.Geometry as MeshGeometry3D);
                            var lineModel = CreateLineBuilderFromEdges(edges);

                            // 添加到viewport的Children集合中
                            viewPort3D.Children.Add(lineModel);
                        }
                    }
                }
            }
        }
    }
}