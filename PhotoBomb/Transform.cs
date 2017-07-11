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
    enum PhotoTransformQuality
    {
        LOW,
        HI
    };

    interface PhotoTransform
    {
        BitmapSource apply(BitmapSource bitmapImage, PhotoTransformQuality quality);
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

        public BitmapSource apply(BitmapSource bitmapImage, PhotoTransformQuality quality)
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

        public BitmapSource apply(BitmapSource bitmapImage, PhotoTransformQuality quality)
        {
            double an = _angle * Math.PI / 180;
            double cos = Math.Abs(Math.Cos(an));
            double sin = Math.Abs(Math.Sin(an));
            double nw = bitmapImage.PixelWidth * cos + bitmapImage.PixelHeight * sin;
            double nh = bitmapImage.PixelWidth * sin + bitmapImage.PixelHeight * cos;

            Canvas canvas = new Canvas();
            canvas.Width = bitmapImage.PixelWidth;
            canvas.Height = bitmapImage.PixelHeight;
            canvas.Background = new ImageBrush(bitmapImage);

            Matrix matrix = new Matrix();
            matrix.Translate((nw - bitmapImage.PixelWidth) / 2, (nh - bitmapImage.PixelHeight) / 2);
            matrix.Scale(bitmapImage.PixelWidth / nw, bitmapImage.PixelHeight / nh);
            matrix.RotateAt(_angle, bitmapImage.PixelWidth / 2, bitmapImage.PixelHeight / 2);
            canvas.RenderTransform = new MatrixTransform(matrix);

            // Get the size of canvas
            Size size = new Size(canvas.ActualWidth, canvas.ActualHeight);
            // Measure and arrange the surface VERY IMPORTANT
            canvas.Measure(size);
            canvas.Arrange(new Rect(size));

            // Create a render bitmap and push the surface to it
            RenderTargetBitmap renderBitmap;
            double DpiX = 96d;
            double DpiY = 96d;
            if (quality == PhotoTransformQuality.HI)
            {
                DpiX = bitmapImage.DpiX;
                DpiY = bitmapImage.DpiY;
            }

            renderBitmap = new RenderTargetBitmap((int)canvas.Width, (int)canvas.Height, DpiX, DpiY, PixelFormats.Pbgra32);
            renderBitmap.Render(canvas);

            //create and return a new WriteableBitmap using the RenderTargetBitmap
            return new WriteableBitmap(renderBitmap);
        }
    }
}
