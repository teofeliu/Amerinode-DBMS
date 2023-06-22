using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;
using WebApplication.Models;

namespace WebApplication.Extensions
{
    public sealed class CsvActionResult<T> : FileResult
    {
        private readonly IList<T> _list;
        private readonly char _separator;

        public CsvActionResult(IList<T> list,
            string fileDownloadName,
            char separator = ',')
            : base("text/csv")
        {
            _list = list;
            FileDownloadName = fileDownloadName;
            _separator = separator;
        }

        protected override void WriteFile(HttpResponseBase response)
        {
            var outputStream = response.OutputStream;
            using (var memoryStream = new MemoryStream())
            {
                WriteList(memoryStream);
                outputStream.Write(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
            }
        }

        private void WriteList(Stream stream)
        {
            var streamWriter = new StreamWriter(stream, Encoding.Default);

            WriteHeaderLine(streamWriter);
            streamWriter.WriteLine();
            WriteDataLines(streamWriter);

            streamWriter.Flush();
        }

        private void WriteHeaderLine(StreamWriter streamWriter)
        {
            foreach (MemberInfo member in typeof(T).GetProperties())
            {
                string displayName = string.Empty;

                var attribute = member.GetCustomAttributes(typeof(DisplayNameAttribute), true)
                                        .Cast<DisplayNameAttribute>().FirstOrDefault();
                if (attribute != null)
                {
                    displayName = attribute.DisplayName;
                }

                WriteValue(streamWriter, String.IsNullOrEmpty(displayName)
                    ? member.Name
                    : displayName);
            }
        }

        private void WriteDataLines(StreamWriter streamWriter)
        {
            foreach (T line in _list)
            {
                foreach (MemberInfo member in typeof(T).GetProperties())
                {
                    WriteValue(streamWriter, GetPropertyValue(line, member.Name));
                }
                streamWriter.WriteLine();
            }
        }


        private void WriteValue(StreamWriter writer, String value)
        {
            writer.Write("\"");
            writer.Write(value.Replace("\"", "\"\""));
            writer.Write("\"" + _separator);
        }

        public static string GetPropertyValue(object src, string propName)
        {
            var val = src.GetType().GetProperty(propName).GetValue(src, null);
            if (val == null)
            {
                return string.Empty;
            }

            var t = val.GetType();
            if (t == typeof(DateTime))
            {
                return ((DateTime)val).ToString("dd/MM/yyyy HH:mm:ss");
            }

            return val.ToString();


            //return val == null ? string.Empty : val.ToString();

            //return src.GetType().GetProperty(propName).GetValue(src, null).ToString() ?? "";
        }

    }

    public static partial class Helpers
    {
        public static IdentityRole GetRole(this IdentityUserRole role)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));
            return roleManager.FindById(role.RoleId);
        }
    }
}