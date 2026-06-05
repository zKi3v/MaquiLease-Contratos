using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Linq;
using MaquiLease.API.Models.Entities;

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

        public byte[] GenerateContractPdf(Contract contract, string clientName, string clientRuc, string clientAddress, string assetName, string serviceName, string leasingTerms)
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
                    page.Content().Element(x => ComposeContractContent(x, contract, clientName, clientRuc, clientAddress, assetName, serviceName, leasingTerms));
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

        void ComposeContractContent(IContainer container, Contract contract, string clientName, string clientRuc, string clientAddress, string assetName, string serviceName, string leasingTerms)
        {
            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                column.Spacing(8);

                column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5)
                    .Text($"CONTRATO DE ARRENDAMIENTO / SERVICIO - {contract.ContractNumber}")
                    .FontSize(16).SemiBold();

                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text($"Tipo: {contract.ContractType.ToUpper()}");
                    row.RelativeItem().AlignRight().Text($"Fecha de Emisión: {contract.CreatedAt:dd/MM/yyyy}");
                });

                column.Item().PaddingTop(15).Text("Partes Contratantes").Underline().SemiBold();
                column.Item().Text($"Arrendador: MaquiLease S.A.C.");
                column.Item().Text($"Cliente / Arrendatario: {clientName}");
                column.Item().Text($"RUC: {clientRuc}");
                column.Item().Text($"Dirección: {clientAddress}");

                column.Item().PaddingTop(15).Text("Detalles del Acuerdo").Underline().SemiBold();
                column.Item().Text($"Vigencia: Desde {contract.StartDate:dd/MM/yyyy} hasta {contract.EndDate:dd/MM/yyyy}");
                column.Item().Text($"Monto Total: {contract.Currency} {contract.TotalAmount:N2}");
                column.Item().Text($"Número de Cuotas: {contract.NumberOfInstallments} mensuales");
                
                if (contract.InterestRate > 0)
                    column.Item().Text($"Tasa de Interés Mensual: {contract.InterestRate}%");
                if (contract.PenaltyRate > 0)
                    column.Item().Text($"Tasa de Penalidad por Atraso: {contract.PenaltyRate}%");

                if (!string.IsNullOrEmpty(assetName))
                {
                    column.Item().Text($"Activo / Maquinaria: {assetName}");
                }
                if (!string.IsNullOrEmpty(serviceName))
                {
                    column.Item().Text($"Servicio de Soporte: {serviceName}");
                }

                if (!string.IsNullOrEmpty(leasingTerms))
                {
                    column.Item().PaddingTop(10).Text("Cláusulas Adicionales / Notas").Underline().SemiBold();
                    column.Item().Text(leasingTerms).Italic().FontSize(10);
                }

                // Cronograma de Amortización
                if (contract.Installments != null && contract.Installments.Any())
                {
                    column.Item().PaddingTop(15).Text("Cronograma de Pagos (Cuotas)").Underline().SemiBold();
                    column.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(0.5f);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().BorderBottom(1).Padding(2).Text("Nº").SemiBold();
                            header.Cell().BorderBottom(1).Padding(2).Text("Vencimiento").SemiBold();
                            header.Cell().BorderBottom(1).Padding(2).AlignRight().Text("Importe").SemiBold();
                            header.Cell().BorderBottom(1).Padding(2).AlignRight().Text("Estado").SemiBold();
                        });

                        foreach (var inst in contract.Installments.OrderBy(i => i.InstallmentNumber))
                        {
                            table.Cell().Padding(2).Text($"{inst.InstallmentNumber}");
                            table.Cell().Padding(2).Text($"{inst.DueDate:dd/MM/yyyy}");
                            table.Cell().Padding(2).AlignRight().Text($"{contract.Currency} {inst.Amount:N2}");
                            table.Cell().Padding(2).AlignRight().Text($"{inst.Status.ToUpper()}");
                        }
                    });
                }

                // Sección de Firmas
                column.Item().PaddingTop(30).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignCenter().Text("_________________________");
                        col.Item().AlignCenter().Text("Firma Autorizada");
                        col.Item().AlignCenter().Text("MaquiLease S.A.C.").FontSize(9).FontColor(Colors.Grey.Medium);
                    });

                    row.RelativeItem().Column(col =>
                    {
                        byte[]? signatureBytes = null;
                        if (!string.IsNullOrEmpty(contract.SignatureHash))
                        {
                            var base64Data = contract.SignatureHash;
                            if (base64Data.Contains(","))
                            {
                                base64Data = base64Data.Split(',')[1];
                            }
                            try
                            {
                                signatureBytes = Convert.FromBase64String(base64Data);
                            }
                            catch { }
                        }

                        if (signatureBytes != null)
                        {
                            col.Item().AlignCenter().Width(120).Height(50).Image(signatureBytes);
                            col.Item().AlignCenter().Text("Firmado Electrónicamente").FontSize(9).FontColor(Colors.Green.Darken2);
                        }
                        else
                        {
                            col.Item().AlignCenter().Height(50).Text("Firma pendiente de registro").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                        }
                        col.Item().AlignCenter().Text("_________________________");
                        col.Item().AlignCenter().Text("Firma del Cliente");
                        col.Item().AlignCenter().Text(clientName).FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });

                if (!string.IsNullOrEmpty(contract.SignatureHash))
                {
                    column.Item().PaddingTop(15).Text(x =>
                    {
                        x.Span("Hash de Firma Digital: ").SemiBold().FontSize(8);
                        x.Span(contract.SignatureHash.Length > 64 ? contract.SignatureHash.Substring(0, 64) + "..." : contract.SignatureHash).FontSize(8).FontColor(Colors.Grey.Darken2);
                    });
                }
            });
        }
    }
}
