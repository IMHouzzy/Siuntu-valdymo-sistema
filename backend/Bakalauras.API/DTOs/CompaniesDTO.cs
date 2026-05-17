namespace Bakalauras.API.Dtos
{
    public class CompanyUpsertDto
    {
        public string Name { get; set; } = "";
        public string CompanyCode { get; set; } = "";
        public bool Active { get; set; } = true;
        public string? Image { get; set; }
        public string DocumentCode { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Address { get; set; } = "";
        public string Email { get; set; } = "";

        // Legacy free-text address strings (kept for backwards compat)
        public string? ShippingAddress { get; set; }
        public string? ReturnAddress { get; set; }

        // Structured shipping (sender) address
        public string? ShippingStreet { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingPostalCode { get; set; }
        public string ShippingCountry { get; set; } = "LT";

        // Structured return address
        public string? ReturnStreet { get; set; }
        public string? ReturnCity { get; set; }
        public string? ReturnPostalCode { get; set; }
        public string ReturnCountry { get; set; } = "LT";
    }

    public class MemberUpsertDto
    {
        public int UserId { get; set; }
        public string Role { get; set; } = "STAFF";
        public string? Position { get; set; }
        public DateTime? StartDate { get; set; }
    }

    public class AssignableUserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
        public string Surname { get; set; } = "";
    }
}