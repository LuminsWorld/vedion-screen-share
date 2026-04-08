using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using VedionScreenShare.Models;

namespace VedionScreenShare.Services;

public static class CaptureService
{
    /// <summary>Captures the full primary screen or a specific region. Returns clean JPEG bytes (EXIF stripped).</summary>
    public static byte[] Capture(CaptureRegion? region = null, int jpegQuality = 60)
    {
        int x = 0, y = 0;
        int w = region?.Width  > 0 ? region.Width  : (int)SystemParameters_WorkArea_Width();
        int h = region?.Height > 0 ? region.Height : (int)SystemParameters_WorkArea_Height();
        if (region is not null) { x = region.X; y = region.Y; }

        using var bmp    = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using var g      = Graphics.FromImage(bmp);
        g.CopyFromScreen(x, y, 0, 0, new Size(w, h), CopyPixelOperation.SourceCopy);

        // Strip EXIF: draw onto a fresh bitmap (GDI+ never copies metadata on draw)
        using var clean  = new Bitmap(w, h, PixelFormat.Format24bppRgb);
        using var g2     = Graphics.FromImage(clean);
        g2.DrawImage(bmp, 0, 0, w, h);

        using var ms     = new MemoryStream();
        var encoder      = GetJpegEncoder();
        var encoderParms = new EncoderParameters(1);
        encoderParms.Param[0] = new EncoderParameter(Encoder.Quality, (long)jpegQuality);
        clean.Save(ms, encoder, encoderParms);
        return ms.ToArray();
    }

    private static ImageCodecInfo GetJpegEncoder() =>
        ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);

    // Use Win32 screen dimensions (handles DPI scaling)
    private static double SystemParameters_WorkArea_Width()
    {
        return System.Windows.SystemParameters.PrimaryScreenWidth;
    }
    private static double SystemParameters_WorkArea_Height()
    {
        return System.Windows.SystemParameters.PrimaryScreenHeight;
    }
}
