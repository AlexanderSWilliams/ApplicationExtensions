using Application.IEnumerableExtensions;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Data.XLSX
{
    public static class XLSX
    {
        public static Stream DictionaryToXLSX(this Stream stream, IDictionary<string, List<List<string>>> workSheetsData,
                eOrientation orientation = eOrientation.Portrait, string title = null, bool addDate = false, bool includePageNumbers = false)
        {
            var document = new ExcelPackage();
            var WorkSheetNumber = 1;
            foreach (var workSheetData in workSheetsData)
            {
                var WorkSheet = document.Workbook.Worksheets.Add(workSheetData.Key);
                WorkSheet.PrinterSettings.Orientation = orientation;
                if (title != null)
                    WorkSheet.HeaderFooter.FirstHeader.CenteredText = @"&C&""-,Bold""&12 " + title;
                if (addDate)
                    WorkSheet.HeaderFooter.FirstHeader.RightAlignedText = "&R&9 " + DateTime.Now.ToShortDateString();

                if (includePageNumbers)
                {
                    WorkSheet.HeaderFooter.FirstFooter.RightAlignedText = "&R&9 " + string.Format("Page {0} of {1}", ExcelHeaderFooter.PageNumber, ExcelHeaderFooter.NumberOfPages);
                    WorkSheet.HeaderFooter.OddFooter.RightAlignedText = "&R&9 " + string.Format("Page {0} of {1}", ExcelHeaderFooter.PageNumber, ExcelHeaderFooter.NumberOfPages);
                }

                WorkSheet.Cells[1, 1, 1, workSheetData.Value.First().Count()].Style.Font.Bold = true;
                WorkSheet.PrinterSettings.RepeatRows = new ExcelAddress(String.Format("'{1}'!${0}:${0}", 1, "WorkSheet" + WorkSheetNumber));
                WorkSheet.PrinterSettings.PageOrder = ePageOrder.OverThenDown;

                WorkSheet.Cells.LoadFromArrays(workSheetData.Value.Select(x => x.ToArray()));
                WorkSheet.Cells.AutoFitColumns();

                WorkSheetNumber++;
            }

            document.SaveAs(stream);
            return stream;
        }

        public static Dictionary<string, List<List<string>>> XLSXToEntityDataDictionary(Stream xlsx)
        {
            var Result = new Dictionary<string, List<List<string>>>();
            using (var manualPackage = new ExcelPackage(xlsx))
            {
                var WorkSheets = manualPackage.Workbook.Worksheets;
                foreach (var worksheet in WorkSheets)
                {
                    var TableName = worksheet.Name;

                    var NumRows = worksheet.Dimension.End.Row;
                    var NumCols = worksheet.Dimension.End.Column;

                    for (var i = 1; i <= NumCols; i++)
                    {
                        if (worksheet.Cells[1, i].Value == null)
                        {
                            NumCols = i - 1;
                            break;
                        }
                    }

                    var ListOfListOfStrings = new List<List<string>>();
                    for (var i = 1; i <= NumRows; i++)
                    {
                        var Row = new List<string>();
                        for (var j = 1; j <= NumCols; j++)
                        {
                            var Value = worksheet.Cells[i, j].Value?.ToString();
                            Row.Add(!string.IsNullOrWhiteSpace(Value) ? Value : null);
                        }
                        if (Row.All(x => string.IsNullOrWhiteSpace(x)))
                            break;

                        ListOfListOfStrings.Add(Row);
                    }

                    var data = ListOfListOfStrings.ListOfListOfStringToDictionary();
                    Result.Add(TableName, ListOfListOfStrings);
                }
                return Result;
            }
        }
    }
}