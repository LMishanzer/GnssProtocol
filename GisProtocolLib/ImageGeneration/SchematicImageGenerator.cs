using GisProtocolLib.CommonModels;
using SkiaSharp;

namespace GisProtocolLib.ImageGeneration;

public class SchematicImageGenerator : IImageGenerator
{
    private const int ImageWidth = 1050;
    private const int ImageMaxHeight = 1050;
    private const int FieldsSize = 50;
    
    public SchematicImage GenerateImage(IEnumerable<Measurement> measurements)
    {
        var measurementsList = measurements as List<Measurement> ?? measurements.ToList();
        
        var marginValues = GetMarginValues(measurementsList);
        var coordinateList = GetImageCoordinateList(measurementsList, marginValues);
        var maxY = coordinateList.Select(c => c.Y).Max();
        var newHeight = maxY + FieldsSize;
        
        using var bitmap = new SKBitmap(ImageWidth, newHeight);
        using var canvas = new SKCanvas(bitmap);
        
        canvas.Clear(SKColors.White);
        
        var pointPaint = new SKPaint { Color = SKColors.Blue, Style = SKPaintStyle.Fill, IsAntialias = true };
        var textPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        var textFont = new SKFont { Size = 20 };

        foreach (var coordinates in coordinateList)
        {
            canvas.DrawCircle(coordinates.X, coordinates.Y, 5, pointPaint);
            canvas.DrawText(coordinates.Label, coordinates.X - coordinates.Label.Length * 5, coordinates.Y + 25, textFont, textPaint);
        }

        var outputImagePath = "/home/mike/Desktop/new_schema_with_points.png";

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new FileStream(outputImagePath, FileMode.Create);
        var memoryStream = new MemoryStream();
        
        data.SaveTo(memoryStream);
        data.SaveTo(stream);
        
        return new SchematicImage(memoryStream);
    }

    private static MarginValues GetMarginValues(List<Measurement> measurements)
    {
        var marginValues = new MarginValues
        {
            Top = decimal.MaxValue,
            Right = decimal.MaxValue,
            Bottom = decimal.MinValue,
            Left = decimal.MinValue
        };

        foreach (var measurement in measurements)
        {
            marginValues.Top = marginValues.Top > measurement.Latitude ? measurement.Latitude : marginValues.Top;
            marginValues.Right = marginValues.Right > measurement.Longitude ? measurement.Longitude : marginValues.Right;
            marginValues.Bottom = marginValues.Bottom < measurement.Latitude ? measurement.Latitude : marginValues.Bottom;
            marginValues.Left = marginValues.Left < measurement.Longitude ? measurement.Longitude : marginValues.Left;
        }
        
        return marginValues;
    }

    private static List<ImageCoordinates> GetImageCoordinateList(List<Measurement> measurements, MarginValues marginValues)
    {
        const int canvasWidth = ImageWidth - 2 * FieldsSize;
        const int canvasHeight = ImageMaxHeight - 2 * FieldsSize;
        
        var widthScale = Math.Abs(marginValues.Left - marginValues.Right) / canvasWidth;
        var heightScale = Math.Abs(marginValues.Bottom - marginValues.Top) / canvasHeight;
        var scale = Math.Max(widthScale, heightScale);
        
        var imageCoordinateList = new List<ImageCoordinates>(measurements.Count);

        foreach (var measurement in measurements)
        {
            var coordinates = new ImageCoordinates
            {
                Y = (int) Math.Round((measurement.Latitude - marginValues.Top) / scale, 0) + FieldsSize,
                X = canvasWidth - (int) Math.Round((measurement.Longitude - marginValues.Right) / scale, 0) + FieldsSize,
                Label = measurement.Name
            };
            
            imageCoordinateList.Add(coordinates);
        }
        
        return imageCoordinateList;
    }

    private struct MarginValues
    {
        public decimal Top { get; set; }
        public decimal Right { get; set; }
        public decimal Bottom { get; set; }
        public decimal Left { get; set; }
    }

    private struct ImageCoordinates
    {
        public int X { get; set; }
        public int Y { get; set; }

        public string Label { get; set; }
    } 
}