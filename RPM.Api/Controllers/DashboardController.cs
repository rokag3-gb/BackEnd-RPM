using Google.Cloud.Compute.V1;
using Microsoft.AspNetCore.Mvc;
using P2.API.Services.Commons;
using RPM.Api.App.Queries;
using RPM.Api.Model;
using RPM.Domain.Models;
using RPM.Infra.Clients;
using Swashbuckle.AspNetCore.Annotations;
using System.Diagnostics;

namespace RPM.Api.Controllers
{
    [ApiController]
    [Route("account")]
    public class DashboardController : ControllerBase
    {
        private readonly ILogger<DashboardController> _logger;
        private readonly IInstanceSnapshotQueries _instanceSnapshotQueries;
        private readonly SalesClient _salesClient;
        private readonly InstanceCostCalculator _instanceCostCalculator;

        public DashboardController(ILogger<DashboardController> logger,
                                   IInstanceSnapshotQueries instanceSnapshotQueries,
                                   SalesClient salesClient,
                                   InstanceCostCalculator instanceCostCalculator)
        {
            _logger = logger;
            _instanceSnapshotQueries = instanceSnapshotQueries;
            _salesClient = salesClient;
            _instanceCostCalculator = instanceCostCalculator;
        }

        /// <summary>
        /// 인스턴스들의 사용 금액을 조회합니다.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{accountId}/instancescost")]
        public async IAsyncEnumerable<List<InstanceCostDto>> InstancesCost([SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
                                                                            [SwaggerParameter("검색 년도", Required = true)] int year,
                                                                            [SwaggerParameter("검색 월", Required = true)] int month)
        {
            var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
            var costAsync = _instanceCostCalculator.InstanceCostPerMonth(accountId,
                                                         null,
                                                         new[] { new DateTime(year, month, 1) },
                                                         token);
            await foreach (var instanceCostDto in costAsync) 
            {
                if (instanceCostDto.Any(i => i.IsActivated == true) == false)
                    yield return new List<InstanceCostDto>();
                yield return instanceCostDto;
            }
        }

        /// <summary>
        /// 대상 년월로부터 최근 12개월 간 월별 활성화된 인스턴스 데이터를 조회합니다.
        /// </summary>
        [HttpGet]
        [Route("{accountId}/monthlyActivatedInstances")]
        [SwaggerOperation("대상 년월로부터 최근 12개월 간 월별 활성화된 인스턴스 데이터를 조회합니다.")]
        public async Task<ActionResult<IEnumerable<dynamic>>> MonthlyActivatedInstances(
            [SwaggerParameter("대상 조직 ID", Required = true)] long accountId, 
            [SwaggerParameter("검색 년도", Required = true)] int year,
            [SwaggerParameter("검색 월", Required = true)] int month)
        {
            var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
            var venderList = await _salesClient.GetKindCodeChilds(token, "VEN");

            var snapshots = await _instanceSnapshotQueries.List(accountId, year, month);
            var joinSnapshots = snapshots.GroupJoin(venderList ?? Enumerable.Empty<Code>(), 
                                                    snap => snap.Vendor, 
                                                    code => code.CodeKey, 
                                                    (snap, vender) => new { snap, vender })
                                         .SelectMany(x => x.vender.DefaultIfEmpty(),
                                                    (x, vender) => 
                                                    new 
                                                    {
                                                        SnapshotMonth = x.snap.SnapshotMonth,
                                                        Vendor = x.snap.Vendor,
                                                        VenderName = vender?.Name,
                                                        Type = x.snap.Type,
                                                        InstId = x.snap.InstId,
                                                        AccountId = x.snap.AccountId,
                                                        Name = x.snap.Name
                                                    });

            var groups = joinSnapshots.GroupBy(s => s.SnapshotMonth, (yearMonth, snapGroup) => new
            {
                Date = new { year = int.Parse(yearMonth.Substring(0, 4)), month = int.Parse(yearMonth.Substring(4, 2)) },
                VenderTypes = snapGroup.GroupBy(ins => ins.Vendor, (venderCode, venderList) => new
                {
                    VenderCode = venderCode,
                    VenderName = venderList.FirstOrDefault()?.VenderName,
                    Total = venderList.Count(),
                    Types = venderList.GroupBy(item => item.Type, (type, typeList) => new
                    {
                        Type = type,
                        Count = typeList.Count()
                    })
                })
                //, Instances = snapGroup.Select(i => new
                //{
                //    InstanceId = i.InstId,
                //    AccountId = i.AccountId,
                //    Vender = i.Vendor,
                //    VenderName = i.VenderName,
                //    Name = i.Name,
                //    SnapshotMonth = i.SnapshotMonth,
                //    Type = i.Type
                //})
            });

            return Ok(groups);
        }
    }
}
