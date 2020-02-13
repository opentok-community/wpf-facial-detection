using OpenTok;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Diagnostics;
using System.Drawing;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace CustomVideoRenderer
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:CustomVideoRenderer"
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:CustomVideoRenderer;assembly=SampleVideoRenderer"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right-click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:SampleVideoRenderer/>
    ///
    /// </summary>
    public class SampleVideoRenderer : Control, IVideoRenderer
    {
        private const double SCALE_FACTOR = 4;
        private const int INTERVAL = 33;
        private const double PIXEL_POINT_CONVERSION = (72.0 / 96.0);

        private int FrameWidth = -1;
        private int FrameHeight = -1;
        
        private Stopwatch _watch = Stopwatch.StartNew();

        private WriteableBitmap VideoBitmap;

        private CascadeClassifier _faceClassifier;
        private CascadeClassifier _profileClassifier;

        private System.Drawing.Rectangle[] _faces = new System.Drawing.Rectangle[0];
        private BlockingCollection<Image<Bgr, byte>> _images = new BlockingCollection<Image<Bgr, byte>>(new ConcurrentStack<Image<Bgr, byte>>());
        private CancellationTokenSource _source;

        public bool DetectingFaces { get; private set; }

        static SampleVideoRenderer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SampleVideoRenderer), new FrameworkPropertyMetadata(typeof(SampleVideoRenderer)));            
        }

        ~SampleVideoRenderer()
        {
            _source?.Cancel();
        }

        public SampleVideoRenderer()
        {
            var brush = new ImageBrush();
            brush.Stretch = Stretch.UniformToFill;
            Background = brush;
            _faceClassifier = new CascadeClassifier(@"haarcascade_frontalface_default.xml");            
            _profileClassifier = new CascadeClassifier(@"haarcascade_profileface.xml");
        }

        private void DetectFaces(CancellationToken token)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    while (true)
                    {
                        using (var image = _images.Take(token)) 
                        {
                            _faces = _faceClassifier.DetectMultiScale(image);
                            if (_faces.Length == 0)
                            {
                                _faces = _profileClassifier.DetectMultiScale(image);
                            }
                            if (_images.Count > 25)
                            {
                                _images = new BlockingCollection<Image<Bgr, byte>>(new ConcurrentStack<Image<Bgr, byte>>());
                                GC.Collect();
                            }                            
                        }                            
                    }
                }
                catch (OperationCanceledException)
                {
                    //exit gracefully
                }
            }, null);
        }        

        public void ToggleFaceDetection(bool detectFaces)
        {
            DetectingFaces = detectFaces;
            if (!detectFaces)
            {
                _source?.Cancel();
            }
            else
            {
                _source?.Dispose();
                _source = new CancellationTokenSource();
                var token = _source.Token;
                DetectFaces(token);
            }
        }

        public void RenderFrame(VideoFrame frame)
        {
            // WritableBitmap has to be accessed from a STA thread
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (frame.Width != FrameWidth || frame.Height != FrameHeight)
                    {
                        FrameWidth = frame.Width;
                        FrameHeight = frame.Height;
                        VideoBitmap = new WriteableBitmap(FrameWidth, FrameHeight, 96, 96, PixelFormats.Bgr32, null);

                        if (Background is ImageBrush)
                        {
                            ImageBrush b = (ImageBrush)Background;
                            b.ImageSource = VideoBitmap;
                        }
                        else
                        {
                            throw new Exception("Please use an ImageBrush as background in the SampleVideoRenderer control");
                        }
                    }

                    if (VideoBitmap != null)
                    {
                        VideoBitmap.Lock();
                        {
                            IntPtr[] buffer = { VideoBitmap.BackBuffer };
                            int[] stride = { VideoBitmap.BackBufferStride };
                            frame.ConvertInPlace(OpenTok.PixelFormat.FormatArgb32, buffer, stride);

                            if (DetectingFaces)
                            {
                                using (var image = new Image<Bgr, byte>(frame.Width, frame.Height, stride[0], buffer[0]))
                                {
                                    if (_watch.ElapsedMilliseconds > INTERVAL)
                                    {
                                        var reduced = image.Resize(1.0 / SCALE_FACTOR, Emgu.CV.CvEnum.Inter.Linear);
                                        _watch.Restart();
                                        _images.Add(reduced);
                                    }
                                }
                                DrawRectanglesOnBitmap(VideoBitmap, _faces);
                            }
                        }
                        VideoBitmap.AddDirtyRect(new Int32Rect(0, 0, FrameWidth, FrameHeight));
                        VideoBitmap.Unlock();
                    }
                }
                finally
                {
                    frame.Dispose();
                }
            }));
        }
        public static void DrawRectanglesOnBitmap(WriteableBitmap bitmap, Rectangle[] rectangles)
        {
            foreach (var rect in rectangles)
            {
                var x1 = (int)((rect.X * (int)SCALE_FACTOR) * PIXEL_POINT_CONVERSION);
                var x2 = (int)(x1 + (((int)SCALE_FACTOR * rect.Width) * PIXEL_POINT_CONVERSION));
                var y1 = rect.Y * (int)SCALE_FACTOR;
                var y2 = y1 + ((int)SCALE_FACTOR * rect.Height);
                bitmap.DrawLineAa(x1, y1, x2, y1, strokeThickness: 5, color: Colors.Blue);
                bitmap.DrawLineAa(x1, y1, x1, y2, strokeThickness: 5, color: Colors.Blue);
                bitmap.DrawLineAa(x1, y2, x2, y2, strokeThickness: 5, color: Colors.Blue);
                bitmap.DrawLineAa(x2, y1, x2, y2, strokeThickness: 5, color: Colors.Blue);
            }
        }
    }
}
