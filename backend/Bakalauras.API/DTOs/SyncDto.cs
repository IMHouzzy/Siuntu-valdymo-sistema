public class SyncIntegrationCheckDto
{
    public bool IsConfigured { get; set; }
    public bool IsEnabled { get; set; }
    public string? BaseUrl { get; set; }
    public string Message { get; set; } = "";
}

public class SyncSessionDto
{
    public int CompanyId { get; set; }
    public DateTime StartedAt { get; set; }
    public SyncStatsDto Stats { get; set; } = new();
    public List<ClientConflictDto> ClientConflicts { get; set; } = new();
    public List<ProductConflictDto> ProductConflicts { get; set; } = new();
    public List<OrderConflictDto> OrderConflicts { get; set; } = new();
    
    // Cached Butent data for resolution phase
    public Dictionary<int, ButentClientCacheDto> ButentClientCache { get; set; } = new();
    public Dictionary<int, ButentProductCacheDto> ButentProductCache { get; set; } = new();
    public Dictionary<int, ButentOrderCacheDto> ButentOrderCache { get; set; } = new();
}

// Cache DTOs to store full Butent data
public class ButentClientCacheDto
{
    public int ClientID { get; set; }
    public string? Name { get; set; }
    public string? Vat { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public int? BankCode { get; set; }
}

public class ButentProductCacheDto
{
    public int Code { get; set; }
    public string? Name { get; set; }
    public string? Unit { get; set; }
    public string? ShippingMode { get; set; }
    public bool Vat { get; set; }
    public bool CountableItem { get; set; }
}

public class ButentOrderCacheDto
{
    public int DocumentID { get; set; }
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public double Total { get; set; }
    public string? Date { get; set; }
    public List<ButentOrderItemCacheDto> Items { get; set; } = new();
}

public class ButentOrderItemCacheDto
{
    public int GoodId { get; set; }
    public string? ProductName { get; set; }
    public double Quantity { get; set; }
    public double Price { get; set; }
    public double Vat { get; set; }
}

public class SyncStatsDto
{
    public int TotalClients { get; set; }
    public int TotalProducts { get; set; }
    public int TotalOrders { get; set; }
    public int ClientConflicts { get; set; }
    public int ProductConflicts { get; set; }
    public int OrderConflicts { get; set; }
}

public class ClientConflictDto
{
    public int ExternalClientId { get; set; }
    public string? Name { get; set; }
    public List<FieldConflictDto> Fields { get; set; } = new();
}

public class ProductConflictDto
{
    public int ExternalCode { get; set; }
    public string? Name { get; set; }
    public List<FieldConflictDto> Fields { get; set; } = new();
}

public class OrderConflictDto
{
    public int ExternalDocumentId { get; set; }
    public string? OrderNumber { get; set; }
    public List<FieldConflictDto> Fields { get; set; } = new();
}

public class FieldConflictDto
{
    public string FieldName { get; set; } = "";
    public string Label { get; set; } = "";
    public object? LocalValue { get; set; }
    public object? ButentValue { get; set; }
}

public class SyncResolutionRequestDto
{
    public int CompanyId { get; set; }
    public List<ClientResolutionDto> ClientResolutions { get; set; } = new();
    public List<ProductResolutionDto> ProductResolutions { get; set; } = new();
    public List<OrderResolutionDto> OrderResolutions { get; set; } = new();
}

public class ClientResolutionDto
{
    public int ExternalClientId { get; set; }
    public Dictionary<string, string> FieldChoices { get; set; } = new(); // fieldName -> "local" | "butent"
}

public class ProductResolutionDto
{
    public int ExternalCode { get; set; }
    public Dictionary<string, string> FieldChoices { get; set; } = new();
}

public class OrderResolutionDto
{
    public int ExternalDocumentId { get; set; }
    public Dictionary<string, string> FieldChoices { get; set; } = new();
}

public class SyncReportDto
{
    public DateTime CompletedAt { get; set; }
    public int ClientsUpdated { get; set; }
    public int ProductsUpdated { get; set; }
    public int OrdersUpdated { get; set; }
    public int TotalChanges { get; set; }
    public List<string> Errors { get; set; } = new();
}