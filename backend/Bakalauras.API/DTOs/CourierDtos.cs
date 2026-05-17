namespace Bakalauras.API.Dtos
{
    public class CourierDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        /// <summary>CUSTOM | DPD_PARCEL | DPD_HOME | LP_EXPRESS_PARCEL | LP_EXPRESS_HOME | …</summary>
        public string Type { get; set; } = "CUSTOM";
        public string? ContactPhone { get; set; }
        public int? DeliveryTermDays { get; set; }
        public double? DeliveryPrice { get; set; }
        /// <summary>True = belongs exclusively to this company.</summary>
        public bool IsOwn { get; set; }
        /// <summary>True = user must select a locker/pickup-point when choosing this courier.</summary>
        public bool SupportsLockers { get; set; }
    }

    /// <summary>Used for POST and PUT on /api/companies/{companyId}/couriers</summary>
    public class UpsertCourierDto
    {
        public string Name { get; set; } = "";
        public string? ContactPhone { get; set; }
        public int? DeliveryTermDays { get; set; }
        public double? DeliveryPrice { get; set; }
    }

    public class CourierStatusUpdateDto
    {
        public int StatusTypeId { get; set; }
        public string? SignatureDataUrl { get; set; }
    }
}