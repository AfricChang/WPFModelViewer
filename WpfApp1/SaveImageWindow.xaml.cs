using System.Windows;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;

namespace WpfApp1
{
    public partial class SaveImageWindow : Window
    {
        public int ImageWidth { get; private set; }
        public int ImageHeight { get; private set; }
        public BackgroundType SelectedBackground { get; private set; }

        public enum BackgroundType
        {
            Transparent,
            White,
            Black
        }

        public SaveImageWindow()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(WidthTextBox.Text, out int width) || width <= 0)
            {
                MessageBox.Show("请输入有效的宽度值", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(HeightTextBox.Text, out int height) || height <= 0)
            {
                MessageBox.Show("请输入有效的高度值", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ImageWidth = width;
            ImageHeight = height;

            if (TransparentBackground.IsChecked == true)
                SelectedBackground = BackgroundType.Transparent;
            else if (WhiteBackground.IsChecked == true)
                SelectedBackground = BackgroundType.White;
            else
                SelectedBackground = BackgroundType.Black;

            DialogResult = true;
            Close();
        }
    }
}
