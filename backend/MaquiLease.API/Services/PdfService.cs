using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace MaquiLease.API.Services
{
    public class PdfService
    {
        public byte[] GeneratePaymentReceipt(string clientName, string contractNumber, string paymentMethod, decimal amount, DateTime date, string documentNumber)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(x => ComposeContent(x, clientName, contractNumber, paymentMethod, amount, date, documentNumber));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("MaquiLease S.A.C.").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text("Gestión de Maquinaria Pesada").FontSize(14).FontColor(Colors.Grey.Medium);
                });
                row.ConstantItem(100).Height(50).Placeholder(); // Placeholder for Logo
            });
        }

        void ComposeContent(IContainer container, string clientName, string contractNumber, string paymentMethod, decimal amount, DateTime date, string documentNumber)
        {
            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                column.Spacing(5);

                column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Text("COMPROBANTE DE PAGO (RECIBO)").FontSize(16).SemiBold();

                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text($"Nº Documento: {documentNumber}");
                    row.RelativeItem().AlignRight().Text($"Fecha: {date:dd/MM/yyyy HH:mm}");
                });

                column.Item().PaddingTop(20).Text("Datos del Cliente").Underline().SemiBold();
                column.Item().Text($"Cliente: {clientName}");
                column.Item().Text($"Contrato Asociado: {contractNumber}");

                column.Item().PaddingTop(20).Text("Detalle del Pago").Underline().SemiBold();

                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().BorderBottom(1).Padding(2).Text("Concepto").SemiBold();
                        header.Cell().BorderBottom(1).Padding(2).AlignRight().Text("Método").SemiBold();
                        header.Cell().BorderBottom(1).Padding(2).AlignRight().Text("Importe").SemiBold();
                    });

                    table.Cell().Padding(2).Text("Abono a cuota de contrato");
                    table.Cell().Padding(2).AlignRight().Text(paymentMethod.ToUpper());
                    table.Cell().Padding(2).AlignRight().Text($"$ {amount:0.00}");
                });

                column.Item().PaddingTop(30).AlignRight().Text($"Total Pagado: $ {amount:0.00}").FontSize(14).SemiBold();
                
                column.Item().PaddingTop(50).AlignCenter().Text("_________________________");
                column.Item().AlignCenter().Text("Firma Autorizada");
                column.Item().AlignCenter().Text("MaquiLease").FontSize(10).FontColor(Colors.Grey.Medium);
            });
        }
    }
}
