using SkiaSharp;

namespace HcPortal.Helpers
{
    public static class SpiderChartRenderer
    {
        /// <summary>
        /// Render radar/spider chart sebagai PNG byte array.
        /// </summary>
        /// <param name="data">List (label, percentage 0..100). Minimum 3 untuk render valid; kalau &lt; 3 return empty array.</param>
        /// <param name="size">Lebar+tinggi canvas (default 500px).</param>
        /// <returns>PNG bytes, atau byte[0] kalau data &lt; 3 elemen.</returns>
        public static byte[] RenderRadarPng(IList<(string label, double percentage)> data, int size = 500)
        {
            if (data == null || data.Count < 3) return Array.Empty<byte>();

            using var bitmap = new SKBitmap(size, size);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            float cx = size / 2f;
            float cy = size / 2f;
            float radius = size * 0.35f;
            int n = data.Count;

            // Grid radial (0/25/50/75/100)
            using var gridPaint = new SKPaint { Color = SKColors.LightGray, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
            for (int level = 1; level <= 4; level++)
            {
                float r = radius * level / 4f;
                using var path = new SKPath();
                for (int i = 0; i < n; i++)
                {
                    double angle = -Math.PI / 2 + 2 * Math.PI * i / n;
                    float x = cx + (float)(r * Math.Cos(angle));
                    float y = cy + (float)(r * Math.Sin(angle));
                    if (i == 0) path.MoveTo(x, y); else path.LineTo(x, y);
                }
                path.Close();
                canvas.DrawPath(path, gridPaint);
            }

            // Axis lines
            using var axisPaint = new SKPaint { Color = SKColors.LightGray, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
            for (int i = 0; i < n; i++)
            {
                double angle = -Math.PI / 2 + 2 * Math.PI * i / n;
                float x = cx + (float)(radius * Math.Cos(angle));
                float y = cy + (float)(radius * Math.Sin(angle));
                canvas.DrawLine(cx, cy, x, y, axisPaint);
            }

            // Data polygon (D-02: stroke RGB(54,162,235), fill RGBA(54,162,235,96))
            using var fillPaint = new SKPaint { Color = new SKColor(54, 162, 235, 96), Style = SKPaintStyle.Fill, IsAntialias = true };
            using var strokePaint = new SKPaint { Color = new SKColor(54, 162, 235), Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true };
            using var dataPath = new SKPath();
            for (int i = 0; i < n; i++)
            {
                double angle = -Math.PI / 2 + 2 * Math.PI * i / n;
                double pct = Math.Clamp(data[i].percentage, 0, 100);
                float r = (float)(radius * pct / 100);
                float x = cx + (float)(r * Math.Cos(angle));
                float y = cy + (float)(r * Math.Sin(angle));
                if (i == 0) dataPath.MoveTo(x, y); else dataPath.LineTo(x, y);
            }
            dataPath.Close();
            canvas.DrawPath(dataPath, fillPaint);
            canvas.DrawPath(dataPath, strokePaint);

            // Labels (truncate > 20 char dengan ellipsis, konsisten Results.cshtml:274)
            using var textFont = new SKFont(SKTypeface.Default, 12);
            using var textPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            for (int i = 0; i < n; i++)
            {
                double angle = -Math.PI / 2 + 2 * Math.PI * i / n;
                float labelR = radius + 25;
                float x = cx + (float)(labelR * Math.Cos(angle));
                float y = cy + (float)(labelR * Math.Sin(angle));
                string label = data[i].label;
                if (label.Length > 20) label = label.Substring(0, 17) + "...";
                var textWidth = textFont.MeasureText(label);
                canvas.DrawText(label, x - textWidth / 2, y, SKTextAlign.Left, textFont, textPaint);
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var pngData = image.Encode(SKEncodedImageFormat.Png, 100);
            return pngData.ToArray();
        }
    }
}
