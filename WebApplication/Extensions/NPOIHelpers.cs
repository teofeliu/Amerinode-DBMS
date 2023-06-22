using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication.Extensions
{
    public static class NPOIHelpers
    {

        public static string GetGenericValue(this ICell cell, DataFormatter formatter)
        {
            string value = string.Empty;

            formatter = formatter ?? new DataFormatter();

            if (cell == null)
            {
                return value; // sometimes the lib read the empty cell as null value
            }

            switch (cell.CellType)
            {
                case CellType.Numeric:
                    value = formatter.FormatCellValue(cell);
                    break;

                case CellType.String:
                    value = cell.RichStringCellValue.String;
                    break;

                case CellType.Blank:
                    value = string.Empty;
                    break;

                case CellType.Boolean:
                    value = cell.BooleanCellValue.ToString();
                    break;

                case CellType.Formula:
                case CellType.Unknown:
                case CellType.Error:
                    value = string.Empty;
                    break;
            }

            return value;
        }

    }
}