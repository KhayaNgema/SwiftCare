using System.Drawing;
using System.Drawing.Imaging;
using ZXing;
using ZXing.Common;
using System.IO;

namespace SwiftCare.Services
{
    public class BarcodeService
    {
        private readonly FileUploadService _fileUploadService;

        public BarcodeService(
            FileUploadService fileUploadService)
        {
            _fileUploadService =
                fileUploadService;
        }


        public async Task<string> GenerateAndSaveBarcodeAsync(
            string barcodeValue,
            string fileName)
        {
            // =====================================================
            // CREATE BARCODE
            // =====================================================

            var writer =
                new BarcodeWriterPixelData
                {
                    Format =
                        BarcodeFormat.CODE_128,

                    Options =
                        new EncodingOptions
                        {
                            Height = 80,
                            Width = 300,
                            Margin = 10
                        }
                };


            var pixelData =
                writer.Write(barcodeValue);


            // =====================================================
            // CREATE BARCODE BITMAP
            // =====================================================

            using var barcodeBitmap =
                new Bitmap(
                    pixelData.Width,
                    pixelData.Height,
                    PixelFormat.Format32bppRgb
                );


            var bitmapData =
                barcodeBitmap.LockBits(
                    new Rectangle(
                        0,
                        0,
                        pixelData.Width,
                        pixelData.Height
                    ),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppRgb
                );


            System.Runtime.InteropServices.Marshal.Copy(
                pixelData.Pixels,
                0,
                bitmapData.Scan0,
                pixelData.Pixels.Length
            );


            barcodeBitmap.UnlockBits(
                bitmapData
            );


            // =====================================================
            // ADD BARCODE VALUE UNDER IMAGE
            // =====================================================

            int labelHeight = 30;

            int totalHeight =
                barcodeBitmap.Height +
                labelHeight;


            using var finalBitmap =
                new Bitmap(
                    barcodeBitmap.Width,
                    totalHeight
                );


            using var graphics =
                Graphics.FromImage(
                    finalBitmap
                );


            graphics.Clear(
                Color.White
            );


            graphics.DrawImage(
                barcodeBitmap,
                0,
                0
            );


            using var font =
                new Font(
                    "Arial",
                    14,
                    FontStyle.Regular
                );


            var textSize =
                graphics.MeasureString(
                    barcodeValue,
                    font
                );


            float xCenter =
                (
                    finalBitmap.Width -
                    textSize.Width
                ) / 2;


            float yText =
                barcodeBitmap.Height + 5;


            using var brush =
                new SolidBrush(
                    Color.Black
                );


            graphics.DrawString(
                barcodeValue,
                font,
                brush,
                xCenter,
                yText
            );


            // =====================================================
            // CONVERT IMAGE TO BYTES
            // =====================================================

            byte[] imageBytes;


            using (var ms =
                   new MemoryStream())
            {
                finalBitmap.Save(
                    ms,
                    ImageFormat.Png
                );

                imageBytes =
                    ms.ToArray();
            }


            // =====================================================
            // SAVE FILE
            // =====================================================

            var uploadedFilePath =
                await _fileUploadService
                    .SaveFileAsync(
                        imageBytes,
                        fileName
                    );


            // =====================================================
            // RETURN WEB PATH
            // =====================================================

            return uploadedFilePath;
        }
    }
}