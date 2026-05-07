// ── Services/LabelGenerator.cs ───────────────────────────────────────────────
// Dependencies:
//   dotnet add package QuestPDF
//   dotnet add package ZXing.Net
//   dotnet add package ZXing.Net.Bindings.SkiaSharp
//   dotnet add package SkiaSharp
//
// QuestPDF community licence is free for revenue < $1M/yr.
// Add to Program.cs:  QuestPDF.Settings.License = LicenseType.Community;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.Rendering;
using ZXing.SkiaSharp;

namespace Bakalauras.API.Services
{
    public static class LabelGenerator
    {
        // Base URL for tracking QR code — change to your actual frontend URL
        private static string TrackingBaseUrl =>
            $"{(Environment.GetEnvironmentVariable("FrontendBaseUrl") ?? "http://46.101.161.47").TrimEnd('/')}/client/track/";

        /// <summary>
        /// Generates a professional shipping-label PDF with Code-128 barcode and QR code.
        /// Saves to wwwroot/labels/{shipmentId}/label_{packageIndex}.pdf
        /// Returns the relative URL /labels/{shipmentId}/label_{packageIndex}.pdf
        /// </summary>
        public static string Generate(
            string webRootPath,
            int shipmentId,
            int packageIndex,
            int totalPackages,
            string trackingNumber,
            string senderName,
            string senderAddress,
            string senderPhone,
            string recipientName,
            string recipientAddress,
            string recipientPhone,
            string courierName,
            string shippingDate,
            string estimatedDelivery)
        {
            var dir = Path.Combine(webRootPath, "labels", shipmentId.ToString());
            Directory.CreateDirectory(dir);

            var fileName = $"label_{packageIndex}.pdf";
            var fullPath = Path.Combine(dir, fileName);
            var relativeUrl = $"/labels/{shipmentId}/{fileName}";

            // ── Generate barcode and QR images ────────────────────────────────
            var barcodeBytes = GenerateBarcode(trackingNumber);
            var qrBytes = GenerateQrCode($"{TrackingBaseUrl}{trackingNumber}");

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A6);
                    page.Margin(6, Unit.Millimetre);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        // ── TOP HEADER BAR ────────────────────────────────────
                        col.Item()
                            .Background("#1e3a5f")
                            .Padding(6)
                            .Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text(courierName)
                                        .Bold().FontColor(Colors.White).FontSize(13);
                                    c.Item().Text("SIUNTIMO ETIKETĖ")
                                        .FontColor("#90caf9").FontSize(7).Italic();
                                });
                                row.ConstantItem(50).AlignRight().AlignMiddle()
                                    .Text($"{packageIndex} / {totalPackages}")
                                    .Bold().FontColor(Colors.White).FontSize(14);
                            });

                        col.Item().PaddingTop(5);

                        // ── TRACKING NUMBER + BARCODE ─────────────────────────
                        col.Item()
                            .Border(1.5f)
                            .BorderColor("#1e3a5f")
                            .Padding(4)
                            .Column(inner =>
                            {
                                inner.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text("SEKIMO NUMERIS")
                                            .FontSize(6).FontColor(Colors.Grey.Darken1).Bold();
                                        c.Item().PaddingTop(1)
                                            .Text(trackingNumber)
                                            .Bold().FontSize(11).FontColor("#1e3a5f");
                                    });
                                    // QR code in top-right corner
                                    if (qrBytes != null)
                                    {
                                        row.ConstantItem(48)
                                            .AlignRight()
                                            .Image(qrBytes)
                                            .FitArea();
                                    }
                                });

                                // Barcode below tracking number
                                if (barcodeBytes != null)
                                {
                                    inner.Item().PaddingTop(3)
                                        .Height(28)
                                        .Image(barcodeBytes)
                                        .FitArea();
                                }
                            });

                        col.Item().PaddingTop(4);

                        // ── SENDER / RECIPIENT ────────────────────────────────
                        col.Item().Row(row =>
                        {
                            // Sender box (left)
                            row.RelativeItem()
                                .Border(0.5f)
                                .BorderColor(Colors.Grey.Lighten1)
                                .Padding(5)
                                .Column(c =>
                                {
                                    c.Item().Text("SIUNTĖJAS")
                                        .Bold().FontSize(6).FontColor(Colors.Grey.Darken2);
                                    c.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                                    c.Item().PaddingTop(2).Text(senderName)
                                        .Bold().FontSize(8);
                                    c.Item().Text(senderAddress)
                                        .FontSize(7).FontColor(Colors.Grey.Darken2);
                                    if (!string.IsNullOrWhiteSpace(senderPhone))
                                        c.Item().Text($"{senderPhone}")
                                            .FontSize(7).FontColor(Colors.Grey.Darken1);
                                });

                            row.ConstantItem(5);

                            // Recipient box (right) — highlighted
                            row.RelativeItem()
                                .Border(2)
                                .BorderColor("#1e3a5f")
                                .Background("#f0f4ff")
                                .Padding(5)
                                .Column(c =>
                                {
                                    c.Item().Text("GAVĖJAS")
                                        .Bold().FontSize(6).FontColor("#1e3a5f");
                                    c.Item().LineHorizontal(0.5f).LineColor("#90caf9");
                                    c.Item().PaddingTop(2).Text(recipientName)
                                        .Bold().FontSize(8).FontColor("#1e3a5f");
                                    c.Item().Text(recipientAddress)
                                        .FontSize(7).FontColor(Colors.Grey.Darken2);
                                    if (!string.IsNullOrWhiteSpace(recipientPhone))
                                        c.Item().Text($"{recipientPhone}")
                                            .FontSize(7).FontColor(Colors.Grey.Darken1);
                                });
                        });

                        col.Item().PaddingTop(4);

                        // ── DATES + QR SCAN INFO ──────────────────────────────
                        col.Item()
                            .Background("#f8fafc")
                            .Border(0.5f)
                            .BorderColor(Colors.Grey.Lighten2)
                            .Padding(4)
                            .Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Row(dateRow =>
                                    {
                                        dateRow.RelativeItem().Column(dc =>
                                        {
                                            dc.Item().Text("SIUNTIMO DATA")
                                                .FontSize(6).FontColor(Colors.Grey.Darken1).Bold();
                                            dc.Item().Text(shippingDate)
                                                .Bold().FontSize(9).FontColor("#1e3a5f");
                                        });
                                        dateRow.RelativeItem().Column(dc =>
                                        {
                                            dc.Item().Text("PRISTATYMO DATA")
                                                .FontSize(6).FontColor(Colors.Grey.Darken1).Bold();
                                            dc.Item().Text(estimatedDelivery)
                                                .Bold().FontSize(9).FontColor("#1e3a5f");
                                        });
                                    });
                                    col.Item().PaddingTop(3).Text("Nuskenuokite QR kodą sekimui")
                                        .FontSize(6).FontColor(Colors.Grey.Darken1).Italic();
                                });
                            });

                        col.Item().PaddingTop(5);

                        // ── FOOTER ────────────────────────────────────────────
                        col.Item()
                            .Background("#1e3a5f")
                            .Padding(3)
                            .AlignCenter()
                            .Text($"Sugeneruota: {DateTime.Now:yyyy-MM-dd HH:mm}")
                            .FontSize(6).FontColor("#90caf9");
                    });
                });
            }).GeneratePdf(fullPath);

            return relativeUrl;
        }

        // ── Barcode generator (Code 128) ──────────────────────────────────────

        private static byte[]? GenerateBarcode(string content)
        {
            try
            {
                var writer = new BarcodeWriterPixelData
                {
                    Format = BarcodeFormat.CODE_128,
                    Options = new EncodingOptions
                    {
                        Width = 600,
                        Height = 120,
                        Margin = 4,
                        PureBarcode = true
                    }
                };

                var pixelData = writer.Write(content);

                using var bitmap = new SKBitmap(
                    new SKImageInfo(pixelData.Width, pixelData.Height, SKColorType.Bgra8888));

                System.Runtime.InteropServices.Marshal.Copy(
                    pixelData.Pixels, 0, bitmap.GetPixels(), pixelData.Pixels.Length);

                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);

                return data.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Barcode failed: {ex.Message}");
                return null;
            }
        }

        // ── QR code generator ─────────────────────────────────────────────────
        // Links to the tracking page — scanning it with a phone opens the tracking URL.
        // Couriers or clients can scan it to quickly pull up the shipment status.

        private static byte[]? GenerateQrCode(string content)
        {
            try
            {
                var options = new QrCodeEncodingOptions
                {
                    Width = 400,
                    Height = 400,
                    Margin = 1
                };

                options.Hints[EncodeHintType.ERROR_CORRECTION] =
                    ZXing.QrCode.Internal.ErrorCorrectionLevel.M;

                var writer = new BarcodeWriterPixelData
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = options
                };

                var pixelData = writer.Write(content);

                using var bitmap = new SKBitmap(
                    new SKImageInfo(pixelData.Width, pixelData.Height, SKColorType.Bgra8888));

                System.Runtime.InteropServices.Marshal.Copy(
                    pixelData.Pixels, 0, bitmap.GetPixels(), pixelData.Pixels.Length);

                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);

                return data.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"QR failed: {ex.Message}");
                return null;
            }
        }
    }
}
