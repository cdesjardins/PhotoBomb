using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
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

namespace PhotoBomb
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private String _imageDir;
        private List<String> _images;
        private List<String> _tracks;
        private int _imageIndex = 0;
        private Boolean _mouseDown = false;
        private Point _selectTopLeft = new Point();
        private Point _selectBottomRight = new Point();
        private Queue<PhotoTransform> _transformations = new Queue<PhotoTransform>();

        public static BitmapImage getImage(String filename)
        {
            BitmapImage bi = new BitmapImage();

            // Begin initialization.
            bi.BeginInit();

            // Set properties.
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.CreateOptions = BitmapCreateOptions.DelayCreation;

            // TODO: optimize this later.
            //bi.DecodePixelHeight = (int)imageCtrl.ActualHeight;
            //bi.DecodePixelWidth = (int)imageCtrl.ActualWidth;
            bi.UriSource = new Uri(filename);

            // End initialization.
            bi.EndInit();
            return bi;
        }

        private void showImage()
        {
            if ((_images != null) && (_images.Count > 0))
            {
                imageCtrl.Source = applyAllTransforms();
            }
        }

        private void init()
        {
            String currentImage = _images[_imageIndex];
            if (_images != null)
            {
                _images.Clear();
            }
            if (_tracks != null)
            {
                _tracks.Clear();
            }
            _imageIndex = 0;
            _transformations.Clear();
            fillImageList();
            int index = _images.IndexOf(currentImage);
            if (index >= 0)
            {
                _imageIndex = index;
            }
        }

        private void fillImageList()
        {
            try
            {
                IEnumerable<String> files = Directory.EnumerateFiles(_imageDir, "*.*", SearchOption.AllDirectories);
                _images = files.Where(s => s.EndsWith(".jpg", true, null) || s.EndsWith(".png", true, null)).ToList();
                _tracks = files.Where(s => s.EndsWith(".gpx", true, null)).ToList();
                if (_images.Count == 0)
                {
                    log("No images found in " + _imageDir);
                }
            }
            catch (ArgumentException ex)
            {
                log(ex.ToString());
            }
            catch (DirectoryNotFoundException ex)
            {
                log(ex.ToString());
            }
            catch (IOException ex)
            {
                log(ex.ToString());
            }
            catch (SecurityException ex)
            {
                log(ex.ToString());
            }
            catch (UnauthorizedAccessException ex)
            {
                log(ex.ToString());
            }
        }

        private void onArrowKey(KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                if (_imageIndex > 0)
                {
                    _imageIndex--;
                    _transformations.Clear();
                    showImage();
                }
            }
            if (e.Key == Key.Right)
            {
                if (_imageIndex < _images.Count)
                {
                    _imageIndex++;
                    _transformations.Clear();
                    showImage();
                }
            }
        }

        private void onDeleteKey()
        {
            if ((_images != null) && (_images.Count > 0))
            {
                try
                {
                    String image = _images[_imageIndex];
                    _images.RemoveAt(_imageIndex);
                    showImage();
                    File.Delete(image);
                    _transformations.Clear();
                    log("Deleted " + image);
                }
                catch (ArgumentException ex)
                {
                    log(ex.ToString());
                }
                catch (DirectoryNotFoundException ex)
                {
                    log(ex.ToString());
                }
                catch (IOException ex)
                {
                    log(ex.ToString());
                }
                catch (NotSupportedException ex)
                {
                    log(ex.ToString());
                }
                catch (UnauthorizedAccessException ex)
                {
                    log(ex.ToString());
                }
            }
        }

        private void onCropKey()
        {
            if (lineTop.IsVisible == true)
            {
                Point p = imageCtrl.TransformToAncestor(Application.Current.MainWindow).Transform(new Point(0, 0));
                Point selectTopLeft = _selectTopLeft;
                Point selectBottomRight = _selectBottomRight;

                selectTopLeft.Offset(-p.X, -p.Y);
                selectBottomRight.Offset(-p.X, -p.Y);

                CropTransformation ct = new CropTransformation(selectTopLeft, selectBottomRight, imageCtrl.ActualHeight, imageCtrl.ActualWidth);
                ImageSource imgSrc = ct.apply(imageCtrl.Source as BitmapSource);
                if (imgSrc != null)
                {
                    _transformations.Enqueue(ct);
                    imageCtrl.Source = imgSrc;
                    log("Image cropped");
                }
            }
            else
            {
                log("Unable to crop, no selection");
            }
        }

        private void onRotateKey(KeyEventArgs e)
        {
            double angle;
            if (double.TryParse(rotateBox.Text, out angle) == true)
            {
                RotateTransformation rt = new RotateTransformation(angle);
                ImageSource imgSrc = rt.apply(imageCtrl.Source as BitmapSource);
                if (imgSrc != null)
                {
                    _transformations.Enqueue(rt);
                    imageCtrl.Source = imgSrc;
                    log("Image rotated");
                }
            }
            else
            {
                log("No rotation angle configured");
            }
        }

        private void onReloadKey()
        {
            init();
            fillImageList();
            showImage();
            log("Reload all images");
        }

        BitmapSource applyAllTransforms()
        {
            BitmapSource bi = getImage(_images[_imageIndex]);

            foreach (PhotoTransform t in _transformations)
            {
                bi = t.apply(bi);
            }
            return bi;
        }

        private void onSaveKey()
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(applyAllTransforms()));

            try
            {
                using (FileStream filestream = new FileStream(_images[_imageIndex].Replace("jpg", "png"), FileMode.Create))
                {
                    encoder.Save(filestream);
                    _transformations.Clear();
                    log("Saved: " + filestream.Name);
                }
            }
            catch (IOException ex)
            {
                log(ex.ToString());
            }
            catch (ArgumentException ex)
            {
                log(ex.ToString());
            }
            catch (NotSupportedException ex)
            {
                log(ex.ToString());
            }
            catch (SecurityException ex)
            {
                log(ex.ToString());
            }
            catch (UnauthorizedAccessException ex)
            {
                log(ex.ToString());
            }
        }

        private void onUndoKey()
        {
            if (_transformations.Count > 0)
            {
                _transformations.Clear();
                showImage();
                log("Undo all");
            }
        }

        private void onHelpKey()
        {
            log("Left/Right arrow - Prev/Next photo");
            log("Delete key - Delete photo");
            log("C - Crop to selection");
            log("r - Rotate clockwise");
            log("Shift+R - Reload all images from dir");
            log("S - Save photo (PNG)");
            log("U - Undo all changes");
            log("Q - Quit application");
            log("H - Help\n\n");
        }

        private void onKeyUpHandler(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) == 0)
            {
                switch (e.Key)
                {
                    case Key.Left:
                    case Key.Right:
                        onArrowKey(e);
                        break;
                    case Key.Delete:
                        onDeleteKey();
                        break;
                    case Key.C:
                        onCropKey();
                        break;
                    case Key.R:
                        if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
                        {
                            onRotateKey(e);
                        }
                        else
                        {
                            onReloadKey();
                        }
                        break;
                    case Key.Q:
                        this.Close();
                        break;
                    case Key.S:
                        onSaveKey();
                        break;
                    case Key.U:
                        onUndoKey();
                        break;
                    case Key.H:
                        onHelpKey();
                        break;
                }
            }

            selectionBoxVisibility(Visibility.Hidden);
        }

        private void selectionBoxVisibility(Visibility vis)
        {
            lineTop.Visibility = vis;
            lineLeft.Visibility = vis;
            lineRight.Visibility = vis;
            lineBottom.Visibility = vis;
        }

        private void drawSelectionBox()
        {
            selectionBoxVisibility(Visibility.Visible);

            lineTop.X1 = _selectTopLeft.X;
            lineTop.Y1 = _selectTopLeft.Y;
            lineTop.X2 = _selectBottomRight.X;
            lineTop.Y2 = _selectTopLeft.Y;

            lineLeft.X1 = _selectTopLeft.X;
            lineLeft.Y1 = _selectTopLeft.Y;
            lineLeft.X2 = _selectTopLeft.X;
            lineLeft.Y2 = _selectBottomRight.Y;

            lineRight.X1 = _selectBottomRight.X;
            lineRight.Y1 = _selectTopLeft.Y;
            lineRight.X2 = _selectBottomRight.X;
            lineRight.Y2 = _selectBottomRight.Y;

            lineBottom.X1 = _selectTopLeft.X;
            lineBottom.Y1 = _selectBottomRight.Y;
            lineBottom.X2 = _selectBottomRight.X;
            lineBottom.Y2 = _selectBottomRight.Y;
        }

        private void onImageClickDown(object sender, MouseButtonEventArgs e)
        {
            Window win = sender as Window;
            selectionBoxVisibility(Visibility.Hidden);
            _selectTopLeft = e.GetPosition(win);
            _selectBottomRight = e.GetPosition(win);
            _mouseDown = true;
        }

        private void onImageClickUp(object sender, MouseButtonEventArgs e)
        {
            Window win = sender as Window;
            _mouseDown = false;
        }

        private void onImageMouseMove(object sender, MouseEventArgs e)
        {
            if (_mouseDown == true)
            {
                Window win = sender as Window;
                _selectBottomRight = e.GetPosition(win);
                drawSelectionBox();
            }
        }

        public void onResize(object sender, RoutedEventArgs e)
        {
            selectionBoxVisibility(Visibility.Hidden);
            showImage();
        }

        private void log(String s)
        {
            outLog.AppendText(s);
            if (s.EndsWith("\n") == false)
            {
                outLog.AppendText("\n");
            }
            outLog.ScrollToEnd();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            TextBox s = sender as TextBox;
            double angle;
            String rotate = s.Text;
            rotate = rotate.Insert(s.CaretIndex, e.Text);
            e.Handled = !((double.TryParse(rotate, out angle)) || rotate.Equals("-") || rotate.Equals(".") || rotate.Equals("-."));
        }

        public MainWindow()
        {
            InitializeComponent();
            AddHandler(Image.MouseDownEvent, new MouseButtonEventHandler(onImageClickDown), true);
            AddHandler(Image.MouseUpEvent, new MouseButtonEventHandler(onImageClickUp), true);
            _imageDir = @"D:\dl\newphotos - Copy";
            fillImageList();
            showImage();
            var window = Window.GetWindow(this);
            window.KeyDown += onKeyUpHandler;
            imageCtrl.Cursor = Cursors.Cross;
        }
    }
}
