using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PhotoBomb
{
    interface PhotoTransform
    {
        BitmapSource apply(BitmapSource bitmapImage);
    }

    class CropTransformation : PhotoTransform
    {
        private Point _selectTopLeft;
        private Point _selectBottomRight;
        private double _scaleHeight;
        private double _scaleWidth;

        public CropTransformation(Point selectTopLeft, Point selectBottomRight, double scaleHeight, double scaleWidth)
        {
            _selectTopLeft = selectTopLeft;
            _selectBottomRight = selectBottomRight;
            _scaleHeight = scaleHeight;
            _scaleWidth = scaleHeight;
        }

        public BitmapSource apply(BitmapSource bitmapImage)
        {
            double scaleHeight = bitmapImage.PixelHeight / _scaleHeight;
            double scaleWidth = bitmapImage.PixelWidth / _scaleWidth;
            double X = _selectTopLeft.X * scaleWidth;
            double Y = _selectTopLeft.Y * scaleHeight;
            double width = ((_selectBottomRight.X - _selectTopLeft.X) * scaleWidth);
            double height = ((_selectBottomRight.Y - _selectTopLeft.Y) * scaleHeight);
            Int32Rect croppedRect = new Int32Rect((int)X, (int)Y, (int)width, (int)height);
            
            return new CroppedBitmap(bitmapImage, croppedRect);
        }
    }

    class RotateTransformation : PhotoTransform
    {
        private double _angle;

        public RotateTransformation(double angle)
        {
            _angle = angle;
        }

        public BitmapSource apply(BitmapSource bitmapImage)
        {
            Canvas surface = new Canvas();

            surface.Width = bitmapImage.PixelWidth;
            surface.Height = bitmapImage.PixelHeight;
            surface.Background = new ImageBrush(bitmapImage);

            Matrix matrix = new Matrix();
            matrix.RotateAt(_angle, bitmapImage.PixelWidth / 2, bitmapImage.PixelHeight / 2);
            surface.RenderTransform = new MatrixTransform(matrix);

            // Get the size of canvas
            Size size = new Size(surface.ActualWidth, surface.ActualHeight);
            // Measure and arrange the surface
            // VERY IMPORTANT
            surface.Measure(size);
            surface.Arrange(new Rect(size));

            // Create a render bitmap and push the surface to it
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
              (int)surface.Width,
              (int)surface.Height,
              96d,
              96d,
              PixelFormats.Pbgra32);
            renderBitmap.Render(surface);

            //create and return a new WriteableBitmap using the RenderTargetBitmap
            return new WriteableBitmap(renderBitmap);

        }
    }
}
