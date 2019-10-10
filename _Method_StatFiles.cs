using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using compleadapi.Common;
using compleadapi.DAL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace compleadapi.Controllers
{
    public partial class ApiController : ControllerBase
    {
        [Authorize(AuthenticationSchemes = "Basic, Bearer")]
        [Route("{ordertype}.{format}")]
        [HttpGet]
        public async Task<IActionResult> GetFile(OrderType ordertype,
                                                 FileType format,
                                                 DateTime? date1,
                                                 DateTime? date2)
        {
            _cm.GetDates(date1, date2, out DateTime start_date, out DateTime end_date);

            var partinfo = (PartnerInfo)GetUserInfo();

            if (!partinfo.IsApproved || partinfo.IsAnal)
            return StatusCode(403, new Dictionary<string, string>()
            {
                ["method"] = "files",
                ["status"] = "Access denied",
                ["errorMsg"] = "Нет доступа в раздел файлов"
            });

            var sqlparameters = new SqlParameters
            {
                { "part_id", partinfo.UserId },
                { "start_date", start_date },
                { "end_date", end_date }
            };

            string command = ordertype switch
            {
                OrderType.report_stat => $"EXEC dbo.GetReport {sqlparameters}",
                OrderType.cross_stat => $"EXEC dbo.GetCross {sqlparameters}",
                _ => null
            };

            var keys = ordertype switch
            {
                OrderType.report_stat => new string[] { "id", "name", "city", "status", "createddate" },
                OrderType.cross_stat => new string[] { "id", "name", "city", "phone", "address" },
                _ => null
            };

            List<Dictionary<string, object>> data;
            try
            {
                data = (await _db.SQLSendQueryAsync<QueryData>(command, sqlparameters).ConfigureAwait(false))[0];
            }
            catch (Exception ex)
            {
                return StatusCode(503, ex.Message);
            }

            var contentType = format switch
            {
                FileType.xml => "application/xml",
                FileType.xlsx => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => ""
            };

            string fileDownloadName = $"{ordertype}_from_{start_date:yyyy-MM-dd}_to_{end_date:yyyy-MM-dd}.{format}";

            byte[] fileContents = await FileFormatter.GetFile(format, data, keys);

            if (fileContents == null || fileContents.Length == 0)
                return NotFound();

            return File(
                fileContents: fileContents,
                contentType: contentType,
                fileDownloadName: fileDownloadName
                );
        }
    }
}
