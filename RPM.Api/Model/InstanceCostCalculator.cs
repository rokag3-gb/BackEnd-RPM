using P2.API.Services.Commons;
using RPM.Api.App.Queries;
using RPM.Domain.Models;
using RPM.Infra.Clients;
using System.Diagnostics;

namespace RPM.Api.Model
{
    /// <summary>
    /// 워크플로 동작 기록을 바탕으로 인스턴스의 사용 시간을 유추하여 청구 비용을 계산합니다.
    /// </summary>
    public class InstanceCostCalculator
    {
        private readonly ILogger<InstanceCostCalculator> _logger;
        private readonly IInstanceQueries _instanceQueries;
        private readonly IInstanceJobQueries _instanceJobQueries;
        private readonly IInstancePriceQueries _instancePriceQueries;
        private readonly IInstanceSnapshotQueries _instanceSnapshotQueries;
        private readonly IP2Client _p2Client;
        private readonly SalesClient _salesClient;

        public InstanceCostCalculator(ILogger<InstanceCostCalculator> logger,
                                      IInstanceQueries instanceQueries,
                                      IInstanceJobQueries instanceJobQueries,
                                      IInstancePriceQueries instancePriceQueries,
                                      IInstanceSnapshotQueries instanceSnapshotQueries,
                                      IP2Client p2Client,
                                      SalesClient salesClient)
        {
            _logger = logger;
            _instanceQueries = instanceQueries;
            _instanceJobQueries = instanceJobQueries;
            _instancePriceQueries = instancePriceQueries;
            _instanceSnapshotQueries = instanceSnapshotQueries;
            _p2Client = p2Client;
            _salesClient = salesClient;
        }

        /// <summary>
        /// 월 단위 사용 비용을 계산합니다.
        /// </summary>
        public async IAsyncEnumerable<List<InstanceCostDto>> InstanceCostPerMonth(long accountId,
                                                                                  IEnumerable<long>? instanceIds,
                                                                                  IEnumerable<DateTime> settlementDates,
                                                                                  string token)
        {
            foreach (var dt in settlementDates)
            {
                var startMonthDate = new DateTime(dt.Year, dt.Month, 1);
                var startPreviousMonthDate = startMonthDate.AddMonths(-1);
                var endMonthDate = startMonthDate.AddMonths(1);
                var monthSpan = endMonthDate.Subtract(startMonthDate);

                var instanceJobs = await _instanceJobQueries.GetInstanceJobsAsync(accountId, instanceIds);
                var snaps = await _instanceSnapshotQueries.List(accountId, dt.Year, dt.Month);

                var instances = _instanceQueries.GetInstancesByIds(accountId, snaps.Select(s => s.InstId));
                if (instances == null || instances.Count() <= 0)
                {
                    yield return new List<InstanceCostDto>
                    {
                        new InstanceCostDto
                        {
                            SettlementMonth = dt.ToString("yyyy-MM"),
                            IsActivated = false
                        }
                    };
                    continue;
                }

                var prices = await _instancePriceQueries.Get(instanceJobs.Select(ij => ij.InstId));
                var runs = await _p2Client.GetRuns(instanceJobs.Select(ij => ij.JobId),
                                                   startPreviousMonthDate,
                                                   new DateTime(dt.Year, dt.Month, DateTime.DaysInMonth(dt.Year, dt.Month), 23, 59, 59),
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
                                                               Runs = irs.Where(ir => ir.runDate >= startMonthDate).OrderBy(r => r.runDate).ToList()
                                                           };
                                                       });

                List<InstanceCostDto> response = new List<InstanceCostDto>();
                foreach (var cost in costs)
                {
                    if (cost.Runs.Any() == true)
                    {
                        if (cost.LastRecordOfPreviousMonth != null && cost.LastRecordOfPreviousMonth.Value.actionCode == "ACC-TON")
                            cost.Runs.Insert(0, (cost.Instance.InstId, "ACC-TON", new DateTime(dt.Year, dt.Month, 1)));

                        if (cost.Runs.Last().actionCode == "ACC-TON")
                            cost.Runs.Add((cost.Instance.InstId, "ACC-OFF", endMonthDate));
                    }

                    DateTime? activePeriodFrom = null;
                    TimeSpan? totalActivePeriod = null;
                    foreach (var run in cost.Runs)
                    {
                        if (run.actionCode == "ACC-TON")
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
                        RealCost = totalActivePeriod != null ? totalActivePeriod.Value.TotalHours * cost.Price.Price_KRW : null,
                        SettlementMonth = dt.ToString("yyyy-MM"),
                        IsActivated = true
                    };
                    response.Add(item);
                }

                yield return response;
            }
        }

        /// <summary>
        /// 일 
        /// </summary>
        public async IAsyncEnumerable<dynamic> DailyCost(long accountId, long instanceId, int year, int month, string token)
        {
            var startMonthDate = new DateTime(year, month, 1);
            var startPreviousMonthDate = startMonthDate.AddMonths(-1);
            var endMonthDate = startMonthDate.AddMonths(1);
            var monthSpan = endMonthDate.Subtract(startMonthDate);

            var snap = await _instanceSnapshotQueries.Get(accountId, instanceId, year, month);
            if (snap == null)
            {
                _logger.LogInformation($"해당 리소스(account id - {accountId}, inst id - {instanceId})에 대한 RPM 워크플로가 없음.");
                yield return new List<dynamic>();
            }

            var price = await _instancePriceQueries.Get(new[] { instanceId }).ContinueWith(t => t.Result.FirstOrDefault());
            if (price == null)
            {
                _logger.LogError($"가격 정보를 찾을 수 없습니다. (account id - {accountId}, inst id - {instanceId})");
                throw new Exception("internal server error");
            }

            var instanceJobs = await _instanceJobQueries.GetInstanceJobsAsync(accountId, new[] { instanceId });
            if (instanceJobs == null)
            {
                for (int i = 1; i <= DateTime.DaysInMonth(year, month); i++)
                {
                    yield return new
                    {
                        Day = $"{year}-{month}-{i}",
                        Cost_krw = TimeSpan.FromHours(24).TotalHours * price.Price_KRW
                    };
                }
            }

            var runs = await _p2Client.GetRuns(instanceJobs.Select(ij => ij.JobId),
                                               startMonthDate,
                                               new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59),
                                               new[] { RunState.Success },
                                               token);

            var dt = new DateTime(startPreviousMonthDate.Year,
                                  startPreviousMonthDate.Month,
                                  DateTime.DaysInMonth(startPreviousMonthDate.Year, startPreviousMonthDate.Month),
                                  23,
                                  59,
                                  59);
            var latestRunActionCode = await _p2Client.GetLatest(instanceJobs.Select(ij => ij.JobId),
                                                                null,
                                                                null,
                                                                dt,
                                                                "RUN-SUC",
                                                                token).ContinueWith<string?>(t =>
                                                                {
                                                                    if (t.Status >= TaskStatus.Canceled)
                                                                        return null;
                                                                    if (t.Result == null)
                                                                        return null;

                                                                    return instanceJobs.FirstOrDefault(i => i.JobId == t.Result.JobId)?.ActionCode;
                                                                });

            var instanceJobAndRun = instanceJobs.Join(runs,
                                                      ij => ij.JobId,
                                                      r => r.JobId,
                                                      (ij, r) => (instanceId: ij.InstId, actionCode: ij.ActionCode, runDate: DateTime.Parse(r.CompletedDate))).ToList();

            if (latestRunActionCode == "ACC-OFF")
                instanceJobAndRun.Insert(0, (instanceId, "ACC-OFF", startMonthDate));
            else
                instanceJobAndRun.Insert(0, (instanceId, "ACC-TON", startMonthDate));
            instanceJobAndRun.Add((instanceId, "ACC-OFF", startMonthDate.AddMonths(1)));
            instanceJobAndRun = instanceJobAndRun.OrderBy(ijr => ijr.runDate).ToList();

            DateTime? activePeriodFrom = null;
            List<(int Day, TimeSpan RunningTime)> runningTimes = new List<(int Day, TimeSpan RunningTime)>();
            for (int i = 0; i < instanceJobAndRun.Count(); i++)
            {
                if (instanceJobAndRun[i].actionCode == "ACC-TON")
                {
                    if (activePeriodFrom != null)
                        continue;
                    activePeriodFrom = instanceJobAndRun[i].runDate;
                }
                else
                {
                    if (activePeriodFrom == null)
                        continue;

                    TimeSpan activePeriod = TimeSpan.Zero;
                    if (activePeriodFrom.Value.Day != instanceJobAndRun[i].runDate.Day)
                    {
                        activePeriod = activePeriodFrom.Value.AddDays(1).Date.Subtract(activePeriodFrom.Value);
                        instanceJobAndRun.Insert(i + 1, (instanceId, "ACC-TON", activePeriodFrom.Value.AddDays(1).Date));
                        instanceJobAndRun.Insert(i + 2, (instanceId, "ACC-OFF", instanceJobAndRun[i].runDate));
                    }
                    else
                        activePeriod = instanceJobAndRun[i].runDate.Subtract(activePeriodFrom.Value);
                    if (activePeriod.TotalMilliseconds <= 0)
                    {
                        activePeriodFrom = null;
                        continue;
                    }

                    Debug.WriteLine($"{instanceId} : active period - {activePeriod.TotalHours}");

                    runningTimes.Add((activePeriodFrom.Value.Day, activePeriod));
                    activePeriodFrom = null;
                }
            }

            TimeSpan remainRunningTime = TimeSpan.Zero;
            for (int i = 1; i <= DateTime.DaysInMonth(year, month); i++)
            {
                dynamic? dailyCost = default;
                var runsByDay = runningTimes.Where(r => r.Day == i);
                if (runsByDay.Count() <= 0 && remainRunningTime == TimeSpan.Zero)
                {
#if DEBUG
                    dailyCost = new { Day = $"{year}-{month}-{i}", Cost_krw = 0, Hours = remainRunningTime };
#else
                    dailyCost = new { Day = $"{year}-{month}-{i}", Cost_krw = 0 };
#endif
                    //continue;
                    yield return dailyCost;
                }

                foreach (var runningTime in runsByDay)
                {
                    remainRunningTime = remainRunningTime.Add(runningTime.RunningTime);
                }

                if (remainRunningTime != TimeSpan.Zero)
                {
                    var comp = remainRunningTime.CompareTo(TimeSpan.FromHours(24));
                    if (comp >= 0)
                    {
#if DEBUG
                        dailyCost = new { Day = $"{year}-{month}-{i}", Cost_krw = TimeSpan.FromHours(24).TotalHours * price.Price_KRW, Hours = remainRunningTime };
#else
                        dailyCost = new { Day = $"{year}-{month}-{i}", Cost_krw = TimeSpan.FromHours(24).TotalHours * price.Price_KRW };
#endif
                        remainRunningTime = remainRunningTime.Subtract(TimeSpan.FromHours(24));
                        yield return dailyCost;
                    }
                    else
                    {
#if DEBUG
                        dailyCost = new { Day = $"{year}-{month}-{i}", Cost_krw = remainRunningTime.TotalHours * price.Price_KRW, Hours = remainRunningTime };
#else
                        dailyCost = new { Day = $"{year}-{month}-{i}", Cost_krw = remainRunningTime.TotalHours * price.Price_KRW };
#endif
                        remainRunningTime = TimeSpan.Zero;
                        yield return dailyCost;
                    }
                }
            }
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
        public string SettlementMonth { get; set; } = string.Empty;
        public bool IsActivated { get; set; }
        public long InstanceId { get; set; }
        public string? InstanceName { get; set; }
        public string? InstanceType { get; set; }
        public string? Vendor { get; set; }
        public string? VendorName { get; set; }
        public TimeSpan? ActiveDuration { get; set; }
        public double WholeMonthCost { get; set; }
        public double? RealCost { get; set; }
    }
}
