using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WPFGlass
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            LocationChanged += MainWindow_LocationChanged;
            Closed += MainWindow_Closed;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.IsOpen = true;
            
            UpdateBackground();
        }

        bool IsOpen = true;

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            this.IsOpen = false;
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {

        }

        async void UpdateBackground()
        {
            WindowInteropHelper windowInteropHelper = new WindowInteropHelper(this);

            System.Windows.Forms.Application.EnableVisualStyles();
            BackgroundMagnifierForm backgroundForm = new BackgroundMagnifierForm();
            backgroundForm.Show();
 

            while (this.IsOpen)
            {
                var target = this.Content as FrameworkElement;
               
                Rect rc = new Rect(new Point(0,0), target.RenderSize);
                rc = new Rect(target.PointToScreen(rc.TopLeft), target.PointToScreen(rc.BottomRight));


                IntPtr bitmapHandle = backgroundForm.GetHBitmap(windowInteropHelper.Handle, (int)rc.Left, (int)rc.Top, (int)rc.Width, (int)rc.Height);
 
                var bmp = ToBitmapSource(bitmapHandle);
                myImage.Source = bmp;
                myImage.InvalidateVisual();

                await Task.Delay(100);
            }
        }
      

        public static BitmapSource ToBitmapSource(IntPtr hBitmap)
        {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
        }
    }
 

}
