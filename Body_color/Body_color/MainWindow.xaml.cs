using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Microsoft.Kinect;


namespace Body_color
{
    public partial class MainWindow : Window
    {
        // Kinect
        KinectSensor kinect;
        CoordinateMapper mapper;
        MultiSourceFrameReader multiReader;

        // Color
        FrameDescription colorFrameDesc;
        ColorImageFormat colorFormat = ColorImageFormat.Bgra;
        byte[] colorBuffer;

        //Body
        Body[] bodies;

        //WPF
        WriteableBitmap colorBitmap;
        int colorStride;
        Int32Rect colorRect;



        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                kinect = KinectSensor.GetDefault();
                kinect.Open();

                mapper = kinect.CoordinateMapper;

                colorFrameDesc = kinect.ColorFrameSource.CreateFrameDescription(colorFormat);
                colorBuffer = new byte[colorFrameDesc.LengthInPixels *colorFrameDesc.BytesPerPixel];

                //body
                bodies = new Body[kinect.BodyFrameSource.BodyCount];

                multiReader = kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Body );

                multiReader.MultiSourceFrameArrived +=multiReader_MultiSourceFrameArrived;


                colorBitmap = new WriteableBitmap(colorFrameDesc.Width, colorFrameDesc.Height,96, 96, PixelFormats.Bgra32, null);
                colorStride = colorFrameDesc.Width * (int)colorFrameDesc.BytesPerPixel;
                colorRect = new Int32Rect(0, 0,colorFrameDesc.Width, colorFrameDesc.Height);
                colorBuffer = new byte[colorStride * colorFrameDesc.Height];

                ImageColor.Source = colorBitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (multiReader != null)
            {
                multiReader.MultiSourceFrameArrived -= multiReader_MultiSourceFrameArrived;
                multiReader.Dispose();
                multiReader = null;
            }

            if (kinect != null)
            {
                kinect.Close();
                kinect = null;
            }
        }

        void multiReader_MultiSourceFrameArrived(object sender,MultiSourceFrameArrivedEventArgs e)
        {
            var multiFrame = e.FrameReference.AcquireFrame();
            if (multiFrame == null)
            {
                return;
            }

            UpdateColorFrame(multiFrame);
            UpdateBodyFrame(multiFrame);

            DrawColorFrame();

        }

        private void UpdateColorFrame(MultiSourceFrame multiFrame)
        {
            using (var colorFrame = multiFrame.ColorFrameReference.AcquireFrame())
            {
                if (colorFrame == null)
                {
                    return;
                }

                colorFrame.CopyConvertedFrameDataToArray(colorBuffer, colorFormat);
            }
        }

        private void UpdateBodyFrame(MultiSourceFrame multiFrame)
        {
            using (var bodyFrame = multiFrame.BodyFrameReference.AcquireFrame())
            {
                if (bodyFrame == null)
                {
                    return;
                }

                bodyFrame.GetAndRefreshBodyData(bodies);
            }

            CanvasBody.Children.Clear();

            foreach (Body b in bodies)
            {
                if (b.IsTracked)
                {
                    ColorSpacePoint cspL = mapper.MapCameraPointToColorSpace(b.Joints[JointType.HandLeft].Position);
                    ColorSpacePoint cspR = mapper.MapCameraPointToColorSpace(b.Joints[JointType.HandRight].Position);


                    Ellipse elpL = new Ellipse() { Width = 100, Height = 100, StrokeThickness = 5, Stroke = Brushes.Red };
                    elpL.RenderTransform = new TranslateTransform(cspL.X-100, cspL.Y-100);

                    Ellipse elpR = new Ellipse() { Width = 100, Height = 100, StrokeThickness = 5, Stroke = Brushes.Blue };
                    elpR.RenderTransform = new TranslateTransform(cspR.X-150, cspR.Y-100);

                    CanvasBody.Children.Add(elpL);
                    CanvasBody.Children.Add(elpR);

                }
            }
        }


        private void DrawColorFrame()
        {
            colorBitmap.WritePixels(colorRect, colorBuffer,colorStride, 0);
        }

        
    }
}
