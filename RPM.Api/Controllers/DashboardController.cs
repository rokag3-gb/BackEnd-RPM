using Google.Cloud.Compute.V1;
using Microsoft.AspNetCore.Mvc;
using P2.API.Services.Commons;
using RPM.Api.App.Queries;
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
        private readonly IInstanceJobQueries _instanceJobQueries;
        private readonly IInstanceQueries _instanceQueries;
        private readonly IInstancePriceQueries _instancePriceQueries;
        private readonly IInstanceSnapshotQueries _instanceSnapshotQueries;
        private readonly IP2Client _p2Client;
        private readonly SalesClient _salesClient;

        public DashboardController(ILogger<DashboardController> logger,
                                   IInstanceJobQueries instanceJobQueries,
                                   IInstanceQueries instanceQueries,
                                   IInstancePriceQueries instancePriceQueries,
                                   IInstanceSnapshotQueries instanceSnapshotQueries,
                                   IP2Client p2Client,
                                   SalesClient salesClient)
        {
            _logger = logger;
            _instanceJobQueries = instanceJobQueries;
            _instanceQueries = instanceQueries;
            _instancePriceQueries = instancePriceQueries;
            _instanceSnapshotQueries = instanceSnapshotQueries;
            _p2Client = p2Client;
            _salesClient = salesClient;
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
        public async Task<ActionResult<IEnumerable<InstanceCostDto>>> InstancesCost([SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
                                                                            [SwaggerParameter("검색 년도", Required = true)] int year,
                                                                            [SwaggerParameter("검색 월", Required = true)] int month)
        {
            var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
            var startMonthDate = new DateTime(year, month, 1);
            var startPreviousMonthDate = startMonthDate.AddMonths(-1);
            var endMonthDate = startMonthDate.AddMonths(1);
            var monthSpan = endMonthDate.Subtract(startMonthDate);

            var instanceJobs = await _instanceJobQueries.GetInstanceJobsAsync(accountId, null);
            var instances = _instanceQueries.GetInstances(accountId, null);
            var prices = await _instancePriceQueries.Get(instances.Select(ij => ij.InstId));
            var runs = await _p2Client.GetRuns(instanceJobs.Select(ij => ij.JobId),
                                               startPreviousMonthDate,
                                               new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59),
                                               new[] { RunState.Success },
                                               token);
            var venderList = await _salesClient.GetKindCodeChilds(token, "VEN");

            var instanceWithVenderName = instances.GroupJoin(venderList ?? Enumerable.Empty<Code>(),
                                                             i => i.Vendor,
                                                             v => v.CodeKey,
                                                             (i, v) => new { i, v })
                                                  .SelectMany(x => x.v.DefaultIfEmpty(),
                                                              (x, v) =>
                                                              new
                                                              {
                                                                  InstId = x.i.InstId,
                                                                  Name = x.i.Name,
                                                                  Type = x.i.Type,
                                                                  Vendor = x.i.Vendor,
                                                                  VendorName = v?.Name
                                                              });

            var instanceAndPrice = instanceWithVenderName.Join(prices,
                                                  i => i.InstId,
                                                  p => p.InstId,
                                                  (i, p) => (instance: i, price: p));

            var instanceJobAndRun = instanceJobs.Join(runs,
                                                      ij => ij.JobId,
                                                      r => r.JobId,
                                                      (ij, r) => (instanceId: ij.InstId, actionCode: ij.ActionCode, runDate: DateTime.Parse(r.CompletedDate)));

            var costs = instanceAndPrice.GroupJoin(instanceJobAndRun,
                                                   ip => ip.instance.InstId,
                                                   ir => ir.instanceId,
                                                   (ip, irs) =>
                                                   {
                                                       return new 
                                                       {
                                                           Instance = ip.instance,
                                                           Price = ip.price,
                                                           LastRecordOfPreviousMonth = irs.Where(ir => ir.runDate < startMonthDate)?.LastOrDefault(),
                                                           Runs = irs.Where(ir => ir.runDate >= startMonthDate).ToList()
                                                       };
                                                   });

            List<InstanceCostDto> response = new List<InstanceCostDto>();
            foreach (var cost in costs)
            {
                if (cost.Runs.Any() == true)
                {
                    if (cost.LastRecordOfPreviousMonth != null && cost.LastRecordOfPreviousMonth.Value.actionCode == "ACT-TON")
                        cost.Runs.Insert(0, (cost.Instance.InstId, "ACT-TON", new DateTime(year, month, 1)));

                    if (cost.Runs.Last().actionCode == "ACT-TON")
                        cost.Runs.Add((cost.Instance.InstId, "ACT-OFF", endMonthDate));
                }

                DateTime? activePeriodFrom = null;
                TimeSpan? totalActivePeriod = null;
                foreach (var run in cost.Runs)
                {
                    if (run.actionCode == "ACT-TON")
                    {
                        if (activePeriodFrom != null)
                            continue;
                        activePeriodFrom = run.runDate;
                    }
                    else
                    {
                        if (activePeriodFrom == null)
                            continue;

                        var activePeriod = run.runDate.Subtract(activePeriodFrom.Value);
                        Debug.WriteLine($"{cost.Instance.InstId} : active period - {activePeriod.TotalHours}");
                        if (totalActivePeriod == null)
                            totalActivePeriod = TimeSpan.Zero;
                        totalActivePeriod = totalActivePeriod.Value.Add(activePeriod);
                        activePeriodFrom = null;
                    }
                }
                Debug.WriteLine($"{cost.Instance.InstId} : total active period - {totalActivePeriod.GetValueOrDefault().TotalHours}");

                var item = new InstanceCostDto
                {
                    InstanceId = cost.Instance.InstId,
                    InstanceName = cost.Instance.Name,
                    InstanceType = cost.Instance.Type,
                    Vendor = cost.Instance.Vendor,
                    VendorName = cost.Instance.VendorName,
                    ActiveDuration = totalActivePeriod != null ? totalActivePeriod : monthSpan,
                    WholeMonthCost = monthSpan.TotalHours * cost.Price.Price_KRW,
                    RealCost = totalActivePeriod != null ? totalActivePeriod.Value.TotalHours * cost.Price.Price_KRW : null
                };
                response.Add(item);
            }

            return Ok(response);
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

        private record InstanceCost
        {
            public Domain.Models.Instance Instance { get; set; }
            public InstancePrice Price { get; set; }
            public (long InstanceId, string ActionCode, DateTime RunDate)? LastRecordOfPreviousMonth { get; set; }
            public List<(long InstanceId, string ActionCode, DateTime RunDate)> Runs { get; set; }
        }
    }

    public class InstanceCostDto
    {
        public long InstanceId { get; set; }
        public string InstanceName { get; set; }
        public string InstanceType { get; set; }
        public string Vendor { get; set; }
        public string VendorName { get; set; }
        public TimeSpan? ActiveDuration { get; set; }
        public double WholeMonthCost { get; set; }
        public double? RealCost { get; set; }
    }
}
