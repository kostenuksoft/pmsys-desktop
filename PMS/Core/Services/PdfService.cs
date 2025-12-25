using System;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font;
using iText.IO.Font.Constants;
using PMS.Core.Models.DTO;
using PMS.Core.Services.Interfaces;

namespace PMS.Core.Services
{
    public class PdfService : IPdfService
    {
        public async Task<string> GenerateCertificatePdfAsync(CertificateDetail certificate, string outputPath)
        {
            return await Task.Run(() =>
            {
                using (var writer = new PdfWriter(outputPath))
                using (var pdf = new PdfDocument(writer))
                using (var document = new Document(pdf))
                {
                    PdfFont font;
                    PdfFont boldFont;

                    try
                    {
                        var fontPath = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
                            ? "C:\\Windows\\Fonts\\times.ttf"
                            : "/usr/share/fonts/truetype/liberation/LiberationSerif-Regular.ttf";

                        var fontProgram = FontProgramFactory.CreateFont(fontPath);
                        font = PdfFontFactory.CreateFont(fontProgram, PdfEncodings.IDENTITY_H);

                        var boldFontPath = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
                            ? "C:\\Windows\\Fonts\\timesbd.ttf"
                            : "/usr/share/fonts/truetype/liberation/LiberationSerif-Bold.ttf";

                        var boldFontProgram = FontProgramFactory.CreateFont(boldFontPath);
                        boldFont = PdfFontFactory.CreateFont(boldFontProgram, PdfEncodings.IDENTITY_H);
                    }
                    catch
                    {
                        font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                        boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                    }

                    var title = new Paragraph("ДОВІДКА")
                        .SetFont(boldFont)
                        .SetFontSize(20)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(30);
                    document.Add(title);

                    var certNumber = new Paragraph($"№ {certificate.CertificateNumber}")
                        .SetFont(font)
                        .SetFontSize(12)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(20);
                    document.Add(certNumber);

                    var certType = new Paragraph(certificate.TypeDisplay)
                        .SetFont(boldFont)
                        .SetFontSize(14)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(30);
                    document.Add(certType);

                    var issueDate = new Paragraph($"Видано: {certificate.IssueDate:dd.MM.yyyy}")
                        .SetFont(font)
                        .SetFontSize(11)
                        .SetMarginBottom(20);
                    document.Add(issueDate);

                    var patientInfo = new Paragraph("Інформація про пацієнта:")
                        .SetFont(boldFont)
                        .SetFontSize(12)
                        .SetMarginBottom(10);
                    document.Add(patientInfo);

                    var patientDetails = new Paragraph($"ПІБ: {certificate.PatientName}")
                        .SetFont(font)
                        .SetFontSize(11)
                        .SetMarginLeft(20)
                        .SetMarginBottom(5);
                    document.Add(patientDetails);

                    if (certificate.ValidFrom != default)
                    {
                        var validity = new Paragraph($"Дійсна з: {certificate.ValidFrom:dd.MM.yyyy}")
                            .SetFont(font)
                            .SetFontSize(11)
                            .SetMarginLeft(20)
                            .SetMarginBottom(5);
                        document.Add(validity);
                    }

                    if (certificate.ValidUntil.HasValue)
                    {
                        var validUntil = new Paragraph($"Дійсна до: {certificate.ValidUntil.Value:dd.MM.yyyy}")
                            .SetFont(font)
                            .SetFontSize(11)
                            .SetMarginLeft(20)
                            .SetMarginBottom(20);
                        document.Add(validUntil);
                    }

                    if (!string.IsNullOrEmpty(certificate.DiagnosisCode) || !string.IsNullOrEmpty(certificate.DiagnosisName))
                    {
                        var diagnosisHeader = new Paragraph("Діагноз:")
                            .SetFont(boldFont)
                            .SetFontSize(12)
                            .SetMarginBottom(10);
                        document.Add(diagnosisHeader);

                        var diagnosisText = certificate.DiagnosisDisplay;
                        var diagnosis = new Paragraph(diagnosisText)
                            .SetFont(font)
                            .SetFontSize(11)
                            .SetMarginLeft(20)
                            .SetMarginBottom(20);
                        document.Add(diagnosis);
                    }

                    if (!string.IsNullOrEmpty(certificate.Content))
                    {
                        var additionalHeader = new Paragraph("Додаткова інформація:")
                            .SetFont(boldFont)
                            .SetFontSize(12)
                            .SetMarginBottom(10);
                        document.Add(additionalHeader);

                        var additional = new Paragraph(certificate.Content)
                            .SetFont(font)
                            .SetFontSize(11)
                            .SetMarginLeft(20)
                            .SetMarginBottom(30);
                        document.Add(additional);
                    }

                    var doctorInfo = new Paragraph($"Лікар: {certificate.DoctorName}")
                        .SetFont(font)
                        .SetFontSize(11)
                        .SetMarginTop(30)
                        .SetMarginBottom(10);
                    document.Add(doctorInfo);

                    var signatureLine = new Paragraph("________________")
                        .SetFont(font)
                        .SetFontSize(11)
                        .SetTextAlignment(TextAlignment.RIGHT)
                        .SetMarginTop(50);
                    document.Add(signatureLine);

                    var signatureText = new Paragraph("(підпис лікаря)")
                        .SetFont(font)
                        .SetFontSize(9)
                        .SetTextAlignment(TextAlignment.RIGHT);
                    document.Add(signatureText);

                    var stampText = new Paragraph("М.П.")
                        .SetFont(font)
                        .SetFontSize(9)
                        .SetTextAlignment(TextAlignment.RIGHT)
                        .SetMarginTop(10);
                    document.Add(stampText);
                }

                return outputPath;
            });
        }
    }
}
