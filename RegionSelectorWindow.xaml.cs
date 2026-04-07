using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VedionScreenShare.Models;

namespace VedionScreenShare
{
    public partial class RegionSelectorWindow : Window
    {
        private Point _startPoint;
        private bool _isDragging = false;

        public CaptureArea SelectedRegion { get; private set; }
        public bool WasConfirmed { get; private set; }

        public RegionSelectorWindow()
        {
            InitializeComponent();
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    WasConfirmed = false;
                    Close();
                }
            };
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(MainCanvas);
            _isDragging = true;
            SelectionRect.Visibility = Visibility.Visible;
            SizeLabel.Visibility = Visibility.Visible;
            HintText.Visibility = Visibility.Collapsed;
            MainCanvas.CaptureMouse();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            Point current = e.GetPosition(MainCanvas);

            double x = Math.Min(_startPoint.X, current.X);
            double y = Math.Min(_startPoint.Y, current.Y);
            double w = Math.Abs(current.X - _startPoint.X);
            double h = Math.Abs(current.Y - _startPoint.Y);

            Canvas.SetLeft(SelectionRect, x);
            Canvas.SetTop(SelectionRect, y);
            SelectionRect.Width  = w;
            SelectionRect.Height = h;

            Canvas.SetLeft(SizeLabel, x + 4);
            Canvas.SetTop(SizeLabel, y + 4);
            SizeLabel.Text = $"{(int)w} × {(int)h}";
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging) return;

            _isDragging = false;
            MainCanvas.ReleaseMouseCapture();

            Point end = e.GetPosition(MainCanvas);

            int x = (int)Math.Min(_startPoint.X, end.X);
            int y = (int)Math.Min(_startPoint.Y, end.Y);
            int w = (int)Math.Abs(end.X - _startPoint.X);
            int h = (int)Math.Abs(end.Y - _startPoint.Y);

            if (w < 10 || h < 10)
            {
                // Too small — ignore
                SelectionRect.Visibility = Visibility.Collapsed;
                SizeLabel.Visibility = Visibility.Collapsed;
                HintText.Visibility = Visibility.Visible;
                _isDragging = false;
                return;
            }

            // Account for DPI scaling
            double dpiScale = PresentationSource.FromVisual(this)
                ?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;

            SelectedRegion = new CaptureArea
            {
                X      = (int)(x * dpiScale),
                Y      = (int)(y * dpiScale),
                Width  = (int)(w * dpiScale),
                Height = (int)(h * dpiScale)
            };

            WasConfirmed = true;
            Close();
        }
    }
}
