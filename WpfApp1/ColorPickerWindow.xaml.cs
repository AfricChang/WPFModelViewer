using System.Windows;
using System.Windows.Media;

namespace WpfApp1
{
    public partial class ColorPickerWindow : Window
    {
        public System.Windows.Media.Color SelectedColor { get; private set; }

        public ColorPickerWindow(System.Windows.Media.Color initialColor)
        {
            InitializeComponent();
            
            // 设置初始颜色
            RedSlider.Value = initialColor.R;
            GreenSlider.Value = initialColor.G;
            BlueSlider.Value = initialColor.B;
            AlphaSlider.Value = initialColor.A;
            
            // 初始化后更新颜色
            UpdateColor();
        }

        private void ColorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ColorPreview != null && ColorValue != null)
            {
                UpdateColor();
            }
        }

        private void UpdateColor()
        {
            SelectedColor = System.Windows.Media.Color.FromArgb(
                (byte)AlphaSlider.Value,
                (byte)RedSlider.Value,
                (byte)GreenSlider.Value,
                (byte)BlueSlider.Value);

            ColorPreview.Fill = new SolidColorBrush(SelectedColor);
            ColorValue.Text = $"当前颜色: #{SelectedColor.A:X2}{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
