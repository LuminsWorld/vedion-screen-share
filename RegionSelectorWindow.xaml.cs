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

            // Cover all screens
            var allScreens = System.Windows.Forms.Screen.AllScreens;
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (var s in allScreens)
            {
                minX = Math.Min(minX, s.Bounds.X);
                minY = Math.Min(minY, s.Bounds.Y);
                maxX = Math.Max(maxX, s.Bounds.X + s.Bounds.Width);
                maxY = Math.Max(maxY, s.Bounds.Y + s.Bounds.Height);
            }

            Left   = minX;
            Top    = minY;
            Width  = maxX - minX;
            Height = maxY - minY;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true; // Stop it bubbling up
                WasConfirmed = false;
                Close();
            }
            base.OnKeyDown(e);
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(MainCanvas);
            _isDragging = true;
            HintBorder.Visibility   = Visibility.Collapsed;
            SelectionRect.Visibility = Visibility.Visible;
            SizeLabel.Visibility     = Visibility.Visible;
            MainCanvas.CaptureMouse();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            Point cur = e.GetPosition(MainCanvas);

            double x = Math.Min(_startPoint.X, cur.X);
            double y = Math.Min(_startPoint.Y, cur.Y);
            double w = Math.Abs(cur.X - _startPoint.X);
            double h = Math.Abs(cur.Y - _startPoint.Y);

            Canvas.SetLeft(SelectionRect, x);
            Canvas.SetTop(SelectionRect, y);
            SelectionRect.Width  = w;
            SelectionRect.Height = h;

            // Label below the box
            Canvas.SetLeft(SizeLabel, x + 4);
            Canvas.SetTop(SizeLabel, Math.Max(0, y - 24));
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
                // Too small, reset
                SelectionRect.Visibility = Visibility.Collapsed;
                SizeLabel.Visibility     = Visibility.Collapsed;
                HintBorder.Visibility    = Visibility.Visible;
                return;
            }

            // Account for DPI
            double dpi = PresentationSource.FromVisual(this)
                ?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;

            SelectedRegion = new CaptureArea
            {
                X      = (int)(x * dpi),
                Y      = (int)(y * dpi),
                Width  = (int)(w * dpi),
                Height = (int)(h * dpi)
            };

            WasConfirmed = true;
            Close();
        }
    }
}
