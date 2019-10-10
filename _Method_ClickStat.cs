using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using compleadapi.Common;
using compleadapi.DAL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ASP_NET_CORE_Samples.Controllers
{
    public partial class ApiController : ControllerBase
    {
        [Authorize(AuthenticationSchemes = "Basic, Bearer")]
        [Route("{techtype}/clickstat")]
        [HttpGet]
        public async Task<IActionResult> ClickStat(TechType techtype, DateTime? date1, DateTime? date2, string obj_type, string obj_id)
        {
            var partinfo = (PartnerInfo)GetUserInfo();

            if (!partinfo.IsApproved || partinfo.IsAnal)
                return StatusCode(403, new Dictionary<string, string>()
                {
                    ["method"] = "clickstat",
                    ["status"] = "Access denied",
                    ["errorMsg"] = "Нет доступа в раздел статистики кликов"
                });

            _cm.GetDates(date1, date2, out DateTime start_date, out DateTime end_date);

            var sqlparameters = new SqlParameters
            {
                { "start_date", start_date },
                { "end_date", end_date },
                { "p_service_type", (int)techtype},
                { "p_obj_type", obj_type },
                { "p_obj_id", obj_id },
                { "p_idp", partinfo.Idp }
            };

            var data = new List<Dictionary<string, object>>();

            try
            {
                data = (await _db.SQLSendQueryMySQLAsync("getdata", sqlparameters).ConfigureAwait(false))[0];
            }
            catch (Exception ex)
            {
                return StatusCode(503, ex.Message);
            }

            var hashes = data.Select(item => new { hash = (string)item["hash"], service_type = (sbyte)item["service_type"] }).ToArray();

            using var hashdata = new DataTable();

            hashdata.Columns.Add("hash", typeof(string));
            hashdata.Columns.Add("service_type", typeof(int));
            foreach (var item in hashes)
            {
                hashdata.Rows.Add(item.hash, item.service_type);
            }

            var sqlcommand = new SqlCommand("GetClickStat")
            {
                CommandType = CommandType.StoredProcedure
            };
            SqlParameter tvpParam = sqlcommand.Parameters.AddWithValue("@hashes", hashdata);
            tvpParam.SqlDbType = SqlDbType.Structured;

            var statdata = new List<Dictionary<string, object>>();

            try
            {
                statdata = (await _db.SQLSendQueryFromCommandAsync(sqlcommand).ConfigureAwait(false))[0];
            }
            catch (Exception ex)
            {
                return StatusCode(503, ex.Message);
            }
            
            var result = (from d in data
                          join s in statdata
                          on d["hash"] equals s["hash"]
                          select new Dictionary<string, object>
                          {
                              { "date" , d["date"] },
                              { "obj_type" , d["obj_type"] },
                              { "obj_id" , d["obj_id"]},
                              { "phone" , d["phone"]},
                              { "ip" , d["ip"] },
                              { "host" , d["host"] },
                              { "uri" , d["uri"] },
                              { "referer" , d["referer"] },
                              { "orderid" , s["orderid"] },
                              { "orderstat" , s["orderstat"] }
                          }).ToList();

            string fileDownloadName = $"{techtype}_clickstat_from_{start_date:yyyy-MM-dd}_to_{end_date:yyyy-MM-dd}.xlsx";

            byte[] fileContents = await FileFormatter.GetFile(FileType.xlsx, result);

            if (fileContents == null || fileContents.Length == 0)
                return NotFound();

            return File(
                fileContents: fileContents,
                contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileDownloadName: fileDownloadName
                );
        }
    }
}
