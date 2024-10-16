using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Dsafa.WpfColorPicker;
using System.IO;
using Microsoft.Win32;
using System.Drawing;
using System.Drawing.Imaging;

namespace Keyer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Bitmap Bitmap1;
        private Bitmap Bitmap2;

        private BitmapImage BI1;
        private BitmapImage BI2;

        private System.Windows.Media.Color KeyColor = Colors.Blue;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadImageBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Select a picture";
            op.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
              "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
              "Portable Network Graphic (*.png)|*.png";
            if (op.ShowDialog() == true)
            {
                Bitmap tmp = new Bitmap(op.FileName);
                if(tmp.Width==1920 && tmp.Height == 1080)
                {
                    Bitmap1 = new Bitmap(op.FileName);
                    Bitmap2 = new Bitmap(op.FileName);
                    UpdPage();
                }
                else
                {
                    string messageBoxText = "The image must have 1920x1080 resolution";
                    string caption = "Warning";
                    MessageBoxButton button = MessageBoxButton.OK;
                    MessageBoxImage icon = MessageBoxImage.Warning;
                    MessageBoxResult result;

                    result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                }
            }
        }

        private void SaveImageBtn_Click(object sender, RoutedEventArgs e)
        {
            if (BI2 == null)
            {
                string messageBoxText = "No image is loaded";
                string caption = "Warning";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Warning;
                MessageBoxResult result;

                result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                return;
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            //saveFileDialog.Filter = "PNG file (*.png)|";
            saveFileDialog.FileName = "output.png";
           

            if (saveFileDialog.ShowDialog() == true)
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Interlace = PngInterlaceOption.Off; 
                encoder.Frames.Add(BitmapFrame.Create(BI2));
                try{
                    using (var fileStream = new FileStream(saveFileDialog.FileName+".png", FileMode.Create))
                        encoder.Save(fileStream);
                }
                catch (Exception ee)
                {
                    string messageBoxText = "Output image name must be different then the input image name";
                    string caption = "Warning";
                    MessageBoxButton button = MessageBoxButton.OK;
                    MessageBoxImage icon = MessageBoxImage.Warning;
                    MessageBoxResult result;

                    result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                    
                    Console.WriteLine(ee.Message);
                }



            }
            


        }

        private void ColorBtn_Click(object sender, RoutedEventArgs e)
        {
            var initialColor = KeyColor;
            var dialog = new ColorPickerDialog(initialColor);
            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                KeyColor = dialog.Color;
                ColorRect.Fill = new SolidColorBrush(KeyColor);
                UpdPage();
            }

        }
        private void UpdPage()
        {
            if (Bitmap1 != null)
            {
                BitmapImage bi = new BitmapImage();

                bi.BeginInit();
                MemoryStream ms = new MemoryStream();
                Bitmap1.Save(ms, ImageFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);
                bi.StreamSource = ms;
                bi.EndInit();

                OriginalImage.Source = bi;

                Bitmap2 = RemoveBackground(Bitmap1, KeyColor.R, KeyColor.G, KeyColor.B, 50 );
                //Bitmap1;

                BitmapImage bi2 = new BitmapImage();

                bi2.BeginInit();
                MemoryStream ms2 = new MemoryStream();
                Bitmap2.Save(ms2, ImageFormat.Png);
                ms2.Seek(0, SeekOrigin.Begin);
                bi2.StreamSource = ms2;
                bi2.EndInit();

                BI2 = bi2;
                ModifiedImage.Source = bi2;
            }
        }

        private Bitmap RemoveBackground(Bitmap input, int R_, int G_, int B_, int eps)
        {
            RectangleF cloneRect = new RectangleF(0, 0, input.Width, input.Height);
            Bitmap clone = input.Clone(cloneRect, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                //new Bitmap(1920, 1080, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            {
                //using (input)
                //using (Graphics gr = Graphics.FromImage(clone))
                //{
                //    gr.DrawImage(input, new System.Drawing.Rectangle(0, 0, 1920, 1080));
                //}

                var data = clone.LockBits(new System.Drawing.Rectangle(0, 0, 1920, 1080), ImageLockMode.ReadWrite, clone.PixelFormat);

                var bytes = Math.Abs(data.Stride) * clone.Height;
                byte[] rgba = new byte[bytes];
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, rgba, 0, bytes);

                var pixels = Enumerable.Range(0, rgba.Length / 4).Select(x => new {
                    B = rgba[x * 4],
                    G = rgba[(x * 4) + 1],
                    R = rgba[(x * 4) + 2],
                    A = rgba[(x * 4) + 3],
                    MakeTransparent = new Action(
                        () => { 
                            //rgba[x * 4] = 0;
                            //rgba[(x * 4)+1] = 0;
                            //rgba[(x * 4) + 2] = 0;
                            rgba[(x * 4) + 3] = 0;
                        })
                });

                pixels
                    .AsParallel()
                    .ForAll(p =>
                    {
                        int dR = Math.Abs(p.R - R_ );
                        int dG = Math.Abs(p.G - G_);
                        int dB = Math.Abs(p.B - B_);
                        double d = Math.Sqrt(dR*dR+dG*dG+dB*dB);

                        //if (p.G != min && (p.G == max || max - p.G < 120) && (max - min) > 0)*/
                        if (d <= eps)
                            p.MakeTransparent();
                        else
                        {
                            //p.MakeTransparent();
                        }
                    });

                System.Runtime.InteropServices.Marshal.Copy(rgba, 0, data.Scan0, bytes);
                clone.UnlockBits(data);

                return clone;
            }
        }

    }
}
