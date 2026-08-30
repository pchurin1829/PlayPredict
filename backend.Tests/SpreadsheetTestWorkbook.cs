using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace PlayPredict.Api.Tests;

internal static class SpreadsheetTestWorkbook
{
    public static MemoryStream CreateXlsx(params SheetData[] sheets) => Create(new XSSFWorkbook(), sheets);

    public static MemoryStream CreateXls(params SheetData[] sheets) => Create(new HSSFWorkbook(), sheets);

    private static MemoryStream Create(IWorkbook workbook, IReadOnlyList<SheetData> sheets)
    {
        using (workbook)
        {
            foreach (var data in sheets)
            {
                var sheet = workbook.CreateSheet(data.Name);
                for (var rowIndex = 0; rowIndex < data.Rows.Length; rowIndex++)
                {
                    var row = sheet.CreateRow(rowIndex);
                    for (var columnIndex = 0; columnIndex < data.Rows[rowIndex].Length; columnIndex++)
                    {
                        var value = data.Rows[rowIndex][columnIndex];
                        if (value is null) continue;
                        var cell = row.CreateCell(columnIndex);
                        switch (value)
                        {
                            case int integer: cell.SetCellValue(integer); break;
                            case double number: cell.SetCellValue(number); break;
                            case DateTime dateTime: cell.SetCellValue(dateTime); break;
                            default: cell.SetCellValue(value.ToString()); break;
                        }
                    }
                }
            }

            var stream = new MemoryStream();
            workbook.Write(stream, leaveOpen: true);
            stream.Position = 0;
            return stream;
        }
    }
}

internal sealed record SheetData(string Name, params object?[][] Rows);
