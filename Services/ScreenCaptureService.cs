using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace VedionScreenShare.Services
{
    /// <summary>
    /// Capture screen frames and encode as JPEG
    /// </summary>
    public class ScreenCaptureService : IDisposable
    {
        private readonly int _jpegQuality;

        public ScreenCaptureService(int jpegQuality = 75)
        {
            if (jpegQuality < 1 || jpegQuality > 100)
                throw new ArgumentException("JPEG quality must be 1-100", nameof(jpegQuality));
            _jpegQuality = jpegQuality;
        }

        /// <summary>
        /// Capture full screen and return as JPEG bytes
        /// </summary>
        public (byte[] jpegData, int width, int height) CaptureScreen()
        {
            var screenSize = Screen.PrimaryScreen.Bounds;
            return CaptureArea(0, 0, screenSize.Width, screenSize.Height);
        }

        /// <summary>
        /// Capture a specific screen region and return as JPEG bytes
        /// </summary>
        public (byte[] jpegData, int width, int height) CaptureArea(int x, int y, int width, int height)
        {
            try
            {
                using (var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb))
                {
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        // Capture the screen region
                        graphics.CopyFromScreen(
                            sourceX: x,
                            sourceY: y,
                            destinationX: 0,
                            destinationY: 0,
                            blockRegionSize: new Size(width, height),
                            copyPixelOperation: CopyPixelOperation.SourceCopy
                        );
                    }

                    // Encode to JPEG
                    byte[] jpegData = BitmapToJpeg(bitmap);
                    return (jpegData, width, height);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Screen capture failed", ex);
            }
        }

        /// <summary>
        /// Convert Bitmap to JPEG byte array with quality setting
        /// </summary>
        private byte[] BitmapToJpeg(Bitmap bitmap)
        {
            using (var ms = new MemoryStream())
            {
                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(
                    Encoder.Quality,
                    _jpegQuality
                );

                var jpegCodec = GetEncoderInfo("image/jpeg");
                if (jpegCodec == null)
                    throw new InvalidOperationException("JPEG codec not found");

                bitmap.Save(ms, jpegCodec, encoderParams);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Find image encoder by MIME type
        /// </summary>
        private ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            var codecs = ImageCodecInfo.GetImageEncoders();
            foreach (var codec in codecs)
            {
                if (codec.MimeType == mimeType)
                    return codec;
            }
            return null;
        }

        public void Dispose()
        {
            // Nothing to dispose yet, but keep for future resource cleanup
        }
    }
}
