using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPM.Domain.Models
{
    public record InstancePrice
    {
        public long InstId { get; init; }
        public string Unit { get; init; }
        public float Price_USD { get; init; }
        public float Price_KRW { get; init; }
        public string Region { get; init; }
        public string Sku { get; init; }
        public string Azure_OSDisk_OSType2 { get; init; }
        public string Azure_ShortMeterName { get; init; }
        public string Azure_meterName { get; init; }
        public string Azure_subcategory { get; init; }
        public string AWS_PlatformDetails { get; init; }
        public string AWS_PlatformDetails2 { get; init; }
        public string AWS_offerTermFullCode { get; init; }
        public string AWS_description { get; init; }
        public string AWS_beginRange { get; init; }
        public string AWS_endRange { get; init; }
        public string Google_SNos { get; init; }
        public string Google_MachineType { get; init; }
        public string Google_MachineType2 { get; init; }
        public string Google_Preemptible { get; init; }
        public string Google_ConsumeReservationType { get; init; }
        public string Google_skuNames { get; init; }
        public string Google_resourceGroups { get; init; }
        public string Google_usageType { get; init; }
        public string Google_descriptions { get; init; }
    }
}
