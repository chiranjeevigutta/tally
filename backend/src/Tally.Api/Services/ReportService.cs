using System.Globalization;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Tally.Api.Dtos;
using Tally.Api.Models;

namespace Tally.Api.Services;

public class StatementViewModel
{
    public required string DisplayName { get; init; }
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required IReadOnlyList<ExpenseDto> Expenses { get; init; }
    public required IReadOnlyList<BudgetLineDto> Budgets { get; init; }

    public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture);
    public decimal Total => Expenses.Sum(e => e.Amount);
    public decimal TotalBudget => Budgets.Sum(b => b.Limit);
}

public interface IReportService
{
    byte[] BuildExcel(IReadOnlyList<ExpenseDto> expenses, DateOnly from, DateOnly to);
    byte[] BuildPdf(IReadOnlyList<ExpenseDto> expenses, DateOnly from, DateOnly to, string ownerName);
}

public class ReportService : IReportService
{
    private static readonly CultureInfo Money = CultureInfo.GetCultureInfo("en-US");

    public byte[] BuildExcel(IReadOnlyList<ExpenseDto> expenses, DateOnly from, DateOnly to)
    {
        using var workbook = new XLWorkbook();

        var sheet = workbook.Worksheets.Add("Expenses");
        string[] headers = ["Date", "Description", "Category", "Payment", "Amount", "Notes"];
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        var row = 2;
        foreach (var e in expenses)
        {
            sheet.Cell(row, 1).Value = e.Date.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(row, 2).Value = e.Description;
            sheet.Cell(row, 3).Value = e.CategoryName;
            sheet.Cell(row, 4).Value = PaymentLabel(e.PaymentMethod);
            sheet.Cell(row, 5).Value = e.Amount;
            sheet.Cell(row, 6).Value = e.Notes ?? string.Empty;
            row++;
        }

        var header = sheet.Range(1, 1, 1, headers.Length);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#2F6B4F");
        header.Style.Font.FontColor = XLColor.White;

        sheet.Column(1).Style.DateFormat.Format = "yyyy-mm-dd";
        sheet.Column(5).Style.NumberFormat.Format = "$#,##0.00";

        if (expenses.Count > 0)
        {
            sheet.Cell(row, 4).Value = "Total";
            sheet.Cell(row, 4).Style.Font.Bold = true;
            sheet.Cell(row, 5).FormulaA1 = $"SUM(E2:E{row - 1})";
            sheet.Cell(row, 5).Style.Font.Bold = true;
            sheet.Range(1, 1, row - 1, headers.Length).SetAutoFilter();
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();

        // second sheet with totals per category
        var summary = workbook.Worksheets.Add("By category");
        summary.Cell(1, 1).Value = $"Spending {from:yyyy-MM-dd} to {to:yyyy-MM-dd}";
        summary.Cell(1, 1).Style.Font.Bold = true;
        summary.Cell(3, 1).Value = "Category";
        summary.Cell(3, 2).Value = "Transactions";
        summary.Cell(3, 3).Value = "Total";
        summary.Range(3, 1, 3, 3).Style.Font.Bold = true;

        var r = 4;
        foreach (var g in expenses.GroupBy(e => e.CategoryName).OrderByDescending(g => g.Sum(e => e.Amount)))
        {
            summary.Cell(r, 1).Value = g.Key;
            summary.Cell(r, 2).Value = g.Count();
            summary.Cell(r, 3).Value = g.Sum(e => e.Amount);
            r++;
        }
        summary.Column(3).Style.NumberFormat.Format = "$#,##0.00";
        summary.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] BuildPdf(IReadOnlyList<ExpenseDto> expenses, DateOnly from, DateOnly to, string ownerName)
    {
        var total = expenses.Sum(e => e.Amount);
        var byCategory = expenses
            .GroupBy(e => new { e.CategoryName, e.CategoryColor })
            .Select(g => new { g.Key.CategoryName, g.Key.CategoryColor, Total = g.Sum(e => e.Amount) })
            .OrderByDescending(x => x.Total)
            .ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(36);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor("#18241F"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Tally").FontSize(20).Bold().FontColor("#2F6B4F");
                        row.RelativeItem().AlignRight().Text($"Generated {DateTime.Now:MMM d, yyyy}").FontColor("#5B6B7A");
                    });
                    col.Item().PaddingTop(4).Text($"Expense report for {ownerName}").FontSize(13).SemiBold();
                    col.Item().Text($"{from:MMM d, yyyy} – {to:MMM d, yyyy}").FontColor("#5B6B7A");
                    col.Item().PaddingVertical(10).LineHorizontal(1).LineColor("#D5DED8");
                });

                page.Content().Column(col =>
                {
                    col.Spacing(14);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Total spent").FontColor("#5B6B7A");
                            c.Item().Text(total.ToString("C", Money)).FontSize(18).Bold();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Transactions").FontColor("#5B6B7A");
                            c.Item().Text(expenses.Count.ToString()).FontSize(18).Bold();
                        });
                    });

                    if (byCategory.Count > 0)
                    {
                        col.Item().Text("By category").SemiBold().FontSize(12);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.ConstantColumn(14); c.RelativeColumn(); c.ConstantColumn(90); c.ConstantColumn(50); });
                            foreach (var c in byCategory)
                            {
                                table.Cell().PaddingVertical(3).AlignMiddle().Width(8).Height(8).Background(c.CategoryColor);
                                table.Cell().PaddingVertical(3).Text(c.CategoryName);
                                table.Cell().PaddingVertical(3).AlignRight().Text(c.Total.ToString("C", Money));
                                table.Cell().PaddingVertical(3).AlignRight().Text(total == 0 ? "0%" : $"{c.Total / total:P0}").FontColor("#5B6B7A");
                            }
                        });
                    }

                    col.Item().Text("Transactions").SemiBold().FontSize(12);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(70);
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                            c.ConstantColumn(80);
                        });

                        table.Header(h =>
                        {
                            foreach (var title in new[] { "Date", "Description", "Category", "Amount" })
                            {
                                var cell = h.Cell().Background("#E8EFEA").Padding(5);
                                if (title == "Amount") cell.AlignRight().Text(title).SemiBold();
                                else cell.Text(title).SemiBold();
                            }
                        });

                        var i = 0;
                        foreach (var e in expenses)
                        {
                            var bg = i++ % 2 == 0 ? "#FFFFFF" : "#F6F9F7";
                            table.Cell().Background(bg).Padding(5).Text(e.Date.ToString("MMM d", Money));
                            table.Cell().Background(bg).Padding(5).Text(e.Description);
                            table.Cell().Background(bg).Padding(5).Text(e.CategoryName);
                            table.Cell().Background(bg).Padding(5).AlignRight().Text(e.Amount.ToString("C", Money));
                        }
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Page ").FontColor("#5B6B7A");
                    t.CurrentPageNumber().FontColor("#5B6B7A");
                    t.Span(" of ").FontColor("#5B6B7A");
                    t.TotalPages().FontColor("#5B6B7A");
                });
            });
        });

        return document.GeneratePdf();
    }

    public static string PaymentLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.BankTransfer => "Bank transfer",
        _ => method.ToString()
    };
}
