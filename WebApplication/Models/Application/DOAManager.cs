using System;
using System.Collections.Generic;
using System.Linq;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System.IO;
using System.Linq.Dynamic;
using System.Security.Principal;
using System.Data.Entity;
using WebApplication.Extensions;

namespace WebApplication.Models.Application
{
    public class DOAManager
    {
        DataModel DB = new DataModel();
        RequestManager rm = new RequestManager();

        private DataFormatter _formatter = new DataFormatter();


        public DOAManager() { }

        #region Import
        public void CreateDOAFromExcelStream(Stream stream, String extension)
        {
            ISheet sheet;
            IWorkbook wb;
            if (extension == ".xlsx") wb = new XSSFWorkbook(stream); else wb = new HSSFWorkbook(stream);
            sheet = wb.GetSheetAt(0);

            /**
             * Bloqueio para não importar um serial number que tiver status ativo no sistema
             */
            var doam = new DOAManager();
            for (int row = 1; row <= sheet.LastRowNum; row++)
            {
                var sheetRow = sheet.GetRow(row);
                if (sheetRow != null)
                {
                    var tm = sheetRow.GetCell(0).GetGenericValue(_formatter);

                    if (tm.ToUpper() == "END") break; // eof
                    var serialNumber = sheetRow.GetCell(3).GetGenericValue(_formatter);

                    doam.ValidateDOA(serialNumber);
                }
            }
            /** eof Bloqueio para não importar um serial number que tiver status ativo no sistema */

            List<DOA> DOAs = new List<DOA>();
            var importId = String.Format("{0:yyyyMMddHHmmss}", DateTime.Now);

            // cells default formmating 
            IDataFormat format = wb.CreateDataFormat();
            ICellStyle dateStyle = wb.CreateCellStyle();
            dateStyle.DataFormat = format.GetFormat("mm/dd/yyyy");


            // row = 1; ignora a primeira linha
            for (int row = 1; row <= sheet.LastRowNum; row++)
            {
                var sheetRow = sheet.GetRow(row);

                if (sheetRow != null)
                {
                    var tm = sheetRow.GetCell(0).GetGenericValue(_formatter);

                    if (tm.ToUpper() == "END") break; // eof

                    var model = sheetRow.GetCell(1).GetGenericValue(_formatter);
                    var manufacturer = sheetRow.GetCell(2).GetGenericValue(_formatter) ?? string.Empty;
                    var serialNumber = sheetRow.GetCell(3).GetGenericValue(_formatter);
                    // applying custom cell formatting
                    var customDateCell = sheetRow.GetCell(4);
                    customDateCell.CellStyle = dateStyle;
                    var date = DateTime.ParseExact(customDateCell.GetGenericValue(_formatter), "MM/dd/yyyy", null);

                    var invoice = sheetRow.GetCell(5).GetGenericValue(_formatter) ?? string.Empty;

                    DOAs.Add(new DOA(importId, tm, model, manufacturer, serialNumber, 
                        invoice, date));

                    //var tm = sheetRow.GetCell(0).StringCellValue;
                    //if (tm.ToUpper() == "END") break;
                    //var modelo = sheetRow.GetCell(1).StringCellValue;
                    //var fabricante = sheetRow.GetCell(2) != null ? sheetRow.GetCell(2).StringCellValue : "";
                    //var serial = sheetRow.GetCell(3).StringCellValue;
                    //var dataRecebimento = sheetRow.GetCell(4).DateCellValue;
                    //var NF = sheetRow.GetCell(5) != null ? sheetRow.GetCell(5).StringCellValue : "";

                    //DOAs.Add(new DOA(importId, tm, modelo, fabricante, serial, NF, dataRecebimento));
                }
            }

            DB.DOAs.AddRange(DOAs);
            DB.SaveChanges();

            VerifyIfDOA(DOAs);
        }
        #endregion

        public void VerifyIfDOA(List<DOA> DOAs)
        {
            foreach(var doa in DOAs)
            {
                var refurb = DB.RefurbRequests.FirstOrDefault(r => r.SerialNumber == doa.SerialNumber
                    && !r.Cancelled
                    && doa.DateReceived < r.DateRequested
                    && DbFunctions.DiffDays(r.DateRequested, doa.DateReceived) <= 90
                );

                if (refurb != null)
                {
                    refurb.isDOA = true;
                    DB.Entry(refurb).State = EntityState.Modified;
                    DB.SaveChanges();
                }
            }            
        }

        //private string ProcessCell(ICell cell)
        //{
        //    string value = string.Empty;

        //    if(cell == null)
        //    {
        //        return value; // sometimes the lib read the empty cell as null value
        //    }

        //    switch (cell.CellType)
        //    {
        //        case CellType.Numeric:
        //            value = _formatter.FormatCellValue(cell);
        //            break;

        //        case CellType.String:
        //            value = cell.RichStringCellValue.String;
        //            break;

        //        case CellType.Blank:
        //            value = string.Empty;
        //            break;

        //        case CellType.Boolean:
        //            value = cell.BooleanCellValue.ToString();
        //            break;

        //        case CellType.Formula:
        //        case CellType.Unknown:
        //        case CellType.Error:
        //            value = string.Empty;
        //            break;
        //    }

        //    return value;
        //}

        public void ValidateDOA(string serialNumber)
        {
            if (DB.DOAs.Any(rr => rr.SerialNumber == serialNumber))
            {
                throw new InvalidOperationException(
                    String.Format("You cannot register the same serial number ({0}) more than once.", serialNumber));
            }
        }

        public void Dispose()
        {
            DB.Dispose();
        }
    }
}