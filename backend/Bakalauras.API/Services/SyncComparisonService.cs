using System.Net.Http.Headers;
using System.Text;
using Bakalauras.API.Models;
using Bakalauras.API.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Bakalauras.API.Services;

public class SyncComparisonService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    public SyncComparisonService(IServiceScopeFactory scopeFactory, IHttpClientFactory httpClientFactory)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<SyncSessionDto> CompareAllData(int companyId, string baseUrl, string username, string password)
    {
        var session = new SyncSessionDto
        {
            CompanyId = companyId,
            StartedAt = DateTime.Now
        };

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var http = _httpClientFactory.CreateClient();
        http.BaseAddress = new Uri(baseUrl.Trim().TrimEnd('/') + "/");
        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

        var butent = new ButentApiService(http);

        var butentClients = await butent.GetClientsAsync();
        var butentProducts = await butent.GetProductsAsync();
        var butentSales = await butent.GetSalesAsync(DateTime.Now.AddYears(-1).ToString("yyyy-MM-dd"));

        foreach (var client in butentClients)
        {
            session.ButentClientCache[client.ClientID] = new ButentClientCacheDto
            {
                ClientID = client.ClientID,
                Name = client.Name,
                Vat = client.Vat,
                Address = client.Address,
                City = client.City,
                Country = client.Country,
                BankCode = int.TryParse(client.BankCode, out var bc) ? bc : null
            };
        }

        foreach (var product in butentProducts)
        {
            session.ButentProductCache[product.Code] = new ButentProductCacheDto
            {
                Code = product.Code,
                Name = string.IsNullOrWhiteSpace(product.Name) ? $"Prekė {product.Code}" : product.Name.Trim(),
                Unit = string.IsNullOrWhiteSpace(product.Unit) ? "vnt" : product.Unit.Trim(),
                ShippingMode = product.ShippingMode,
                Vat = product.Vat,
                CountableItem = product.CountableItem
            };
        }

        foreach (var sale in butentSales)
        {
            var doc = await butent.GetDocumentAsync(sale.DocumentID);
            if (doc == null) continue;

            var items = await butent.GetDocumentItemsAsync(sale.DocumentID);

            const double VatRate = 0.21;
            var orderItems = new List<ButentOrderItemCacheDto>();

            foreach (var item in items)
            {
                var productName = session.ButentProductCache.TryGetValue(item.Good_Id, out var prod)
                    ? prod.Name
                    : $"Prekė #{item.Good_Id}";

                orderItems.Add(new ButentOrderItemCacheDto
                {
                    GoodId = item.Good_Id,
                    ProductName = productName,
                    Quantity = item.Quantity,
                    Price = item.Price ?? 0,
                    Vat = Math.Round((item.Price ?? 0) * VatRate, 2)
                });
            }

            var clientName = doc.Client_Id.HasValue && session.ButentClientCache.TryGetValue(doc.Client_Id.Value, out var cli)
                ? cli.Name
                : null;

            session.ButentOrderCache[sale.DocumentID] = new ButentOrderCacheDto
            {
                DocumentID = sale.DocumentID,
                ClientId = doc.Client_Id,
                ClientName = clientName,
                Total = doc.Total ?? sale.Total ?? 0,
                Date = doc.Date,
                Items = orderItems
            };
        }

        var clientConflicts = await CompareClients(db, butentClients, companyId);
        session.ClientConflicts = clientConflicts;
        session.Stats.ClientConflicts = clientConflicts.Count;

        var productConflicts = await CompareProducts(db, butentProducts, companyId);
        session.ProductConflicts = productConflicts;
        session.Stats.ProductConflicts = productConflicts.Count;

        var orderConflicts = await CompareOrders(db, session.ButentOrderCache, companyId);
        session.OrderConflicts = orderConflicts;
        session.Stats.OrderConflicts = orderConflicts.Count;

        session.Stats.TotalClients = await db.client_companies
            .Where(cc => cc.fk_Companyid_Company == companyId && cc.externalClientId.HasValue)
            .CountAsync();

        session.Stats.TotalProducts = await db.products
            .Where(p => p.fk_Companyid_Company == companyId && p.externalCode.HasValue)
            .CountAsync();

        session.Stats.TotalOrders = await db.orders
            .Where(o => o.fk_Companyid_Company == companyId && o.externalDocumentId.HasValue)
            .CountAsync();

        return session;
    }

    private async Task<List<ClientConflictDto>> CompareClients(AppDbContext db, List<ButentClientDto> externalClients, int companyId)
    {
        var conflicts = new List<ClientConflictDto>();

        var localClients = await db.client_companies
            .AsNoTracking()
            .Where(cc => cc.fk_Companyid_Company == companyId && cc.externalClientId.HasValue)
            .ToListAsync();

        var localByExtId = localClients.ToDictionary(lc => lc.externalClientId!.Value);

        foreach (var ext in externalClients)
        {
            if (!localByExtId.TryGetValue(ext.ClientID, out var local))
                continue;

            var fieldConflicts = new List<FieldConflictDto>();

            if (!StringEquals(local.vat, ext.Vat))
            {
                fieldConflicts.Add(new FieldConflictDto
                {
                    FieldName = "vat",
                    Label = "PVM kodas",
                    LocalValue = local.vat,
                    ButentValue = ext.Vat
                });
            }

            if (!StringEquals(local.deliveryAddress, ext.Address))
            {
                fieldConflicts.Add(new FieldConflictDto
                {
                    FieldName = "deliveryAddress",
                    Label = "Adresas",
                    LocalValue = local.deliveryAddress,
                    ButentValue = ext.Address
                });
            }

            if (!StringEquals(local.city, ext.City))
            {
                fieldConflicts.Add(new FieldConflictDto
                {
                    FieldName = "city",
                    Label = "Miestas",
                    LocalValue = local.city,
                    ButentValue = ext.City
                });
            }

            if (!StringEquals(local.country, ext.Country))
            {
                fieldConflicts.Add(new FieldConflictDto
                {
                    FieldName = "country",
                    Label = "Šalis",
                    LocalValue = local.country,
                    ButentValue = ext.Country
                });
            }

            var extBankCode = int.TryParse(ext.BankCode, out var bc) ? (int?)bc : null;
            if (local.bankCode != extBankCode)
            {
                fieldConflicts.Add(new FieldConflictDto
                {
                    FieldName = "bankCode",
                    Label = "Banko kodas",
                    LocalValue = local.bankCode,
                    ButentValue = extBankCode
                });
            }

            if (fieldConflicts.Any())
            {
                conflicts.Add(new ClientConflictDto
                {
                    ExternalClientId = ext.ClientID,
                    Name = ext.Name,
                    Fields = fieldConflicts
                });
            }
        }

        return conflicts;
    }

    private async Task<List<ProductConflictDto>> CompareProducts(AppDbContext db, List<ButentProductDto> externalProducts, int companyId)
    {
        var conflicts = new List<ProductConflictDto>();

        // CRITICAL: Include navigation properties to check Type and Group
        var localProducts = await db.products
            .AsNoTracking()
            .Include(p => p.fk_Categoryid_Categories)
            .Include(p => p.fk_ProductGroupId_ProductGroups)
            .Where(p => p.fk_Companyid_Company == companyId && p.externalCode.HasValue)
            .ToListAsync();

        var localByCode = localProducts.ToDictionary(lp => lp.externalCode!.Value);

        foreach (var ext in externalProducts)
        {
            if (!localByCode.TryGetValue(ext.Code, out var local))
                continue;

            var fieldConflicts = new List<FieldConflictDto>();

            // 1. Compare Name
            var extName = string.IsNullOrWhiteSpace(ext.Name) ? $"Prekė {ext.Code}" : ext.Name.Trim();
            if (!StringEquals(local.name, extName))
            {
                fieldConflicts.Add(new FieldConflictDto
                {
                    FieldName = "name",
                    Label = "Pavadinimas",
                    LocalValue = local.name,
                    ButentValue = extName
                });
            }

            // 2. Compare Unit
            var extUnit = string.IsNullOrWhiteSpace(ext.Unit) ? "vnt" : ext.Unit.Trim();
            if (!StringEquals(local.unit, extUnit))
            {
                fieldConflicts.Add(new FieldConflictDto
                {
                    FieldName = "unit",
                    Label = "Mato vienetas",
                    LocalValue = local.unit,
                    ButentValue = extUnit
                });
            }

            // 3. Compare Shipping Mode
            if (!StringEquals(local.shipping_mode, ext.ShippingMode))
            {
                fieldConflicts.Add(new FieldConflictDto
                {
                    FieldName = "shipping_mode",
                    Label = "Pristatymo būdas",
                    LocalValue = local.shipping_mode,
                    ButentValue = ext.ShippingMode
                });
            }

            // 4. Compare VAT
            if (local.vat != ext.Vat)
            {
                fieldConflicts.Add(new FieldConflictDto
                {
                    FieldName = "vat",
                    Label = "PVM",
                    LocalValue = local.vat,
                    ButentValue = ext.Vat
                });
            }

            // 5. Compare Countable Item
            if (local.countableItem != ext.CountableItem)
            {
                fieldConflicts.Add(new FieldConflictDto
                {
                    FieldName = "countableItem",
                    Label = "Skaičiuojama prekė",
                    LocalValue = local.countableItem,
                    ButentValue = ext.CountableItem
                });
            }

            // 6. Compare Category (Type) 
            var localCategoryId = local.fk_Categoryid_Categories.FirstOrDefault()?.id_Category;
            var extCategoryId = ext.Type?.Id;

            if (localCategoryId != extCategoryId)
            {
                var localCategoryName = local.fk_Categoryid_Categories.FirstOrDefault()?.name ?? "Nėra";
                var extCategoryName = ext.Type?.Name ?? "Nėra";

                fieldConflicts.Add(new FieldConflictDto
                {
                    FieldName = "category",
                    Label = "Kategorija (Type)",
                    LocalValue = localCategoryName,
                    ButentValue = extCategoryName
                });
            }

            // 7. Compare Product Group
            var localGroupId = local.fk_ProductGroupId_ProductGroups.FirstOrDefault()?.id_ProductGroup;
            var extGroupId = ext.Group?.Id;

            if (localGroupId != extGroupId)
            {
                var localGroupName = local.fk_ProductGroupId_ProductGroups.FirstOrDefault()?.name ?? "Nėra";
                var extGroupName = ext.Group?.Name ?? "Nėra";

                fieldConflicts.Add(new FieldConflictDto
                {
                    FieldName = "productGroup",
                    Label = "Produktų grupė",
                    LocalValue = localGroupName,
                    ButentValue = extGroupName
                });
            }

            if (fieldConflicts.Any())
            {
                conflicts.Add(new ProductConflictDto
                {
                    ExternalCode = ext.Code,
                    Name = extName,
                    Fields = fieldConflicts
                });
            }
        }

        return conflicts;
    }

    private async Task<List<OrderConflictDto>> CompareOrders(AppDbContext db, Dictionary<int, ButentOrderCacheDto> butentOrderCache, int companyId)
    {
        var conflicts = new List<OrderConflictDto>();

        var extDocIds = butentOrderCache.Keys.ToHashSet();

        var localOrders = await db.orders
            .AsNoTracking()
            .Include(o => o.ordersproducts)
            .ThenInclude(op => op.fk_Productid_ProductNavigation)
            .Include(o => o.fk_Clientid_UsersNavigation)
            .Where(o => o.fk_Companyid_Company == companyId &&
                        o.externalDocumentId.HasValue &&
                        extDocIds.Contains(o.externalDocumentId.Value))
            .ToListAsync();

        var localByExtId = localOrders.ToDictionary(lo => lo.externalDocumentId!.Value);

        var clientMappings = await db.client_companies
            .AsNoTracking()
            .Include(cc => cc.fk_Clientid_UsersNavigation)
            .Where(cc => cc.fk_Companyid_Company == companyId && cc.externalClientId.HasValue)
            .ToDictionaryAsync(cc => cc.externalClientId!.Value, cc => cc.fk_Clientid_UsersNavigation);

        var productMappings = await db.products
            .AsNoTracking()
            .Where(p => p.fk_Companyid_Company == companyId && p.externalCode.HasValue)
            .ToDictionaryAsync(p => p.externalCode!.Value, p => p);

        foreach (var (docId, butentOrder) in butentOrderCache)
        {
            if (!localByExtId.TryGetValue(docId, out var local))
                continue;

            var fieldConflicts = new List<FieldConflictDto>();

            // Compare Client
            if (butentOrder.ClientId.HasValue)
            {
                var expectedClient = clientMappings.TryGetValue(butentOrder.ClientId.Value, out var expUser)
                    ? expUser
                    : null;

                if (expectedClient != null && local.fk_Clientid_Users != expectedClient.id_Users)
                {
                    var localClientName = local.fk_Clientid_UsersNavigation?.name ?? $"User #{local.fk_Clientid_Users}";
                    var butentClientName = butentOrder.ClientName ?? $"Client #{butentOrder.ClientId.Value}";

                    fieldConflicts.Add(new FieldConflictDto
                    {
                        FieldName = "fk_Clientid_Users",
                        Label = "Klientas",
                        LocalValue = localClientName,
                        ButentValue = butentClientName
                    });
                }
            }

            // Compare Total Amount
            if (Math.Abs(local.totalAmount - butentOrder.Total) > 0.01)
            {
                fieldConflicts.Add(new FieldConflictDto
                {
                    FieldName = "totalAmount",
                    Label = "Bendra suma",
                    LocalValue = $"€{Math.Round(local.totalAmount, 2):F2}",
                    ButentValue = $"€{Math.Round(butentOrder.Total, 2):F2}"
                });
            }

            // Compare Date
            var extDate = ParseButentDate(butentOrder.Date)?.Date ?? DateTime.UtcNow.Date;
            if (local.OrdersDate != extDate)
            {
                fieldConflicts.Add(new FieldConflictDto
                {
                    FieldName = "OrdersDate",
                    Label = "Data",
                    LocalValue = local.OrdersDate.ToString("yyyy-MM-dd"),
                    ButentValue = extDate.ToString("yyyy-MM-dd")
                });
            }


            // Compare Order Items 
            var localItems = local.ordersproducts?.ToList() ?? new List<ordersproduct>();
            var butentItems = butentOrder.Items;

            // Build expected items list with product IDs
            var expectedItems = new List<(int productId, double quantity, double price, double vat)>();

            foreach (var bItem in butentItems)
            {
                if (productMappings.TryGetValue(bItem.GoodId, out var product))
                {
                    expectedItems.Add((product.id_Product, bItem.Quantity, bItem.Price, bItem.Vat));
                }
            }

            // Check if there's a conflict using multi-set comparison
            bool itemsHaveConflict = false;

            // Different counts = definite conflict
            if (localItems.Count != expectedItems.Count)
            {
                itemsHaveConflict = true;
            }
            else
            {
                // Convert to comparable tuples and sort for multi-set comparison
                // This handles duplicates correctly while detecting product/price/quantity changes
                // Local items
                var localSet = localItems
                    .Select(li => (
                        productId: li.fk_Productid_Product,
                        quantity: Math.Round(li.quantity, 2),
                        price: Math.Round(li.unitPrice, 2),
                        vat: Math.Round(li.vatValue, 2)
                    ))
                    .OrderBy(x => x.productId)
                    .ThenBy(x => x.price)
                    .ThenBy(x => x.quantity)
                    .ToList();

                // Expected items
                var expectedSet = expectedItems
                    .Select(e => (
                        productId: e.productId,
                        quantity: Math.Round(e.quantity, 2),
                        price: Math.Round(e.price, 2),
                        vat: Math.Round(e.vat, 2)
                    ))
                    .OrderBy(x => x.productId)
                    .ThenBy(x => x.price)
                    .ThenBy(x => x.quantity)
                    .ToList();

                // Compare the sorted sets element by element
                if (localSet.Count != expectedSet.Count)
                {
                    itemsHaveConflict = true;
                }
                else
                {
                    for (int i = 0; i < localSet.Count; i++)
                    {
                        if (localSet[i].productId != expectedSet[i].productId ||
                            Math.Abs(localSet[i].quantity - expectedSet[i].quantity) > 0.01 ||
                            Math.Abs(localSet[i].price - expectedSet[i].price) > 0.02 ||
                            Math.Abs(localSet[i].vat - expectedSet[i].vat) > 0.02)
                        {
                            itemsHaveConflict = true;
                            break;
                        }
                    }
                }
            }

            // Only add conflict if items differ
            if (itemsHaveConflict)
            {
                var localItemsDisplay = new List<string>();
                foreach (var lItem in localItems)
                {
                    var productName = lItem.fk_Productid_ProductNavigation?.name ?? $"ID {lItem.fk_Productid_Product}";
                    localItemsDisplay.Add(
                        $"• {productName}: {lItem.quantity} vnt × €{lItem.unitPrice:F2} (PVM: €{lItem.vatValue:F2})"
                    );
                }

                var butentItemsDisplay = new List<string>();
                foreach (var bItem in butentItems)
                {
                    var productName = productMappings.TryGetValue(bItem.GoodId, out var product)
                        ? product.name
                        : $"ID {bItem.GoodId}";
                    butentItemsDisplay.Add(
                        $"• {productName}: {bItem.Quantity} vnt × €{bItem.Price:F2} (PVM: €{bItem.Vat:F2})"
                    );
                }

                fieldConflicts.Add(new FieldConflictDto
                {
                    FieldName = "orderItems",
                    Label = "Užsakymo prekės",
                    LocalValue = localItemsDisplay.Count > 0
                        ? string.Join("\n", localItemsDisplay)
                        : "Nėra prekių",
                    ButentValue = butentItemsDisplay.Count > 0
                        ? string.Join("\n", butentItemsDisplay)
                        : "Nėra prekių"
                });

            }
            if (fieldConflicts.Any())
            {
                conflicts.Add(new OrderConflictDto
                {
                    ExternalDocumentId = docId,
                    OrderNumber = $"Užsakymas #{local.id_Orders}",
                    Fields = fieldConflicts
                });
            }
        }

        return conflicts;
    }

    public async Task<SyncReportDto> ApplyResolutions(SyncResolutionRequestDto request)
    {
        var report = new SyncReportDto
        {
            CompletedAt = DateTime.Now
        };

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var integration = await db.company_integrations
            .Where(x => x.fk_Companyid_Company == request.CompanyId && x.type == "BUTENT")
            .FirstOrDefaultAsync();

        if (integration == null)
        {
            report.Errors.Add("Būtent integracija nerasta");
            return report;
        }

        var (u, p, b) = IntegrationSecrets.TryUnpack(integration.encryptedSecrets);
        var baseUrl = !string.IsNullOrWhiteSpace(integration.baseUrl) ? integration.baseUrl : b;

        var http = _httpClientFactory.CreateClient();
        http.BaseAddress = new Uri(baseUrl!.Trim().TrimEnd('/') + "/");
        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{u}:{p}"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

        var butent = new ButentApiService(http);

        await using var tx = await db.Database.BeginTransactionAsync();

        try
        {
            var butentClients = await butent.GetClientsAsync();
            var butentProducts = await butent.GetProductsAsync();
            var butentSales = await butent.GetSalesAsync(DateTime.Now.AddYears(-1).ToString("yyyy-MM-dd"));

            var clientsDict = butentClients.ToDictionary(c => c.ClientID);
            var productsDict = butentProducts.ToDictionary(p => p.Code);

            // Apply Client Resolutions
            foreach (var res in request.ClientResolutions)
            {
                var local = await db.client_companies
                    .Where(cc => cc.fk_Companyid_Company == request.CompanyId &&
                                 cc.externalClientId == res.ExternalClientId)
                    .FirstOrDefaultAsync();

                if (local == null || !clientsDict.TryGetValue(res.ExternalClientId, out var ext))
                    continue;

                bool updated = false;

                foreach (var (fieldName, choice) in res.FieldChoices)
                {
                    if (choice != "butent") continue;

                    switch (fieldName)
                    {
                        case "vat":
                            local.vat = ext.Vat;
                            updated = true;
                            break;
                        case "deliveryAddress":
                            local.deliveryAddress = ext.Address;
                            updated = true;
                            break;
                        case "city":
                            local.city = ext.City;
                            updated = true;
                            break;
                        case "country":
                            local.country = ext.Country;
                            updated = true;
                            break;
                        case "bankCode":
                            local.bankCode = int.TryParse(ext.BankCode, out var bc) ? bc : null;
                            updated = true;
                            break;
                    }
                }

                if (updated)
                {
                    report.ClientsUpdated++;
                }
            }

            // Apply Product Resolutions
            foreach (var res in request.ProductResolutions)
            {
                var local = await db.products
                    .Where(p => p.fk_Companyid_Company == request.CompanyId &&
                                p.externalCode == res.ExternalCode)
                    .FirstOrDefaultAsync();

                if (local == null || !productsDict.TryGetValue(res.ExternalCode, out var ext))
                    continue;

                bool updated = false;

                foreach (var (fieldName, choice) in res.FieldChoices)
                {
                    if (choice != "butent") continue;

                    switch (fieldName)
                    {
                        case "name":
                            local.name = string.IsNullOrWhiteSpace(ext.Name) ? $"Prekė {ext.Code}" : ext.Name.Trim();
                            updated = true;
                            break;
                        case "unit":
                            local.unit = string.IsNullOrWhiteSpace(ext.Unit) ? "vnt" : ext.Unit.Trim();
                            updated = true;
                            break;
                        case "shipping_mode":
                            local.shipping_mode = string.IsNullOrWhiteSpace(ext.ShippingMode) ? null : ext.ShippingMode.Trim();
                            updated = true;
                            break;
                        case "vat":
                            local.vat = ext.Vat;
                            updated = true;
                            break;
                        case "countableItem":
                            local.countableItem = ext.CountableItem;
                            updated = true;
                            break;
                        // REPLACE both the "category" and "productGroup" cases
                        // Lines ~690-730 in SyncComparisonService.cs

                        case "category":
                            if (ext.Type?.Id != null)
                            {
                                // Check if category exists
                                var categoryExists = await db.categories
                                    .AnyAsync(c => c.id_Category == ext.Type.Id);

                                if (!categoryExists)
                                {
                                    // Use raw SQL instead of EF Core Add + SaveChanges
                                    // This avoids intermediate SaveChanges that break order item tracking
                                    await db.Database.ExecuteSqlRawAsync(
                                        "INSERT INTO category (id_Category, name) VALUES ({0}, {1})",
                                        ext.Type.Id,
                                        ext.Type.Name ?? $"Category {ext.Type.Id}");
                                }

                                // Update the relationship
                                await db.Database.ExecuteSqlRawAsync(
                                    "DELETE FROM productcategory WHERE fk_Productid_Product = {0}",
                                    local.id_Product);

                                await db.Database.ExecuteSqlRawAsync(
                                    "INSERT INTO productcategory (fk_Productid_Product, fk_Categoryid_Category) VALUES ({0}, {1})",
                                    local.id_Product, ext.Type.Id);

                                updated = true;
                            }
                            break;

                        case "productGroup":
                            if (ext.Group?.Id != null)
                            {
                                // Check if product group exists
                                var groupExists = await db.productgroups
                                    .AnyAsync(g => g.id_ProductGroup == ext.Group.Id);

                                if (!groupExists)
                                {
                                    // Use raw SQL instead of EF Core Add + SaveChanges
                                    // This avoids intermediate SaveChanges that break order item tracking
                                    await db.Database.ExecuteSqlRawAsync(
                                        "INSERT INTO productgroup (id_ProductGroup, name) VALUES ({0}, {1})",
                                        ext.Group.Id,
                                        ext.Group.Name ?? $"Group {ext.Group.Id}");
                                }

                                // Update the relationship
                                await db.Database.ExecuteSqlRawAsync(
                                    "DELETE FROM product_productgroup WHERE fk_Productid_Product = {0}",
                                    local.id_Product);

                                await db.Database.ExecuteSqlRawAsync(
                                    "INSERT INTO product_productgroup (fk_Productid_Product, fk_ProductGroupId_ProductGroup) VALUES ({0}, {1})",
                                    local.id_Product, ext.Group.Id);

                                updated = true;
                            }
                            break;
                    }
                }

                if (updated)
                {
                    report.ProductsUpdated++;
                }
            }

            // Apply Order Resolutions
            foreach (var res in request.OrderResolutions)
            {
                var local = await db.orders
                    .Include(o => o.ordersproducts)
                    .Where(o => o.fk_Companyid_Company == request.CompanyId &&
                                o.externalDocumentId == res.ExternalDocumentId)
                    .FirstOrDefaultAsync();

                if (local == null) continue;

                var doc = await butent.GetDocumentAsync(res.ExternalDocumentId);
                if (doc == null) continue;

                var sale = butentSales.FirstOrDefault(s => s.DocumentID == res.ExternalDocumentId);

                bool updated = false;

                foreach (var (fieldName, choice) in res.FieldChoices)
                {
                    if (choice != "butent") continue;

                    switch (fieldName)
                    {
                        case "fk_Clientid_Users":
                            if (doc.Client_Id.HasValue)
                            {
                                var clientMapping = await db.client_companies
                                    .Where(cc => cc.fk_Companyid_Company == request.CompanyId &&
                                                 cc.externalClientId == doc.Client_Id.Value)
                                    .Select(cc => cc.fk_Clientid_Users)
                                    .FirstOrDefaultAsync();

                                if (clientMapping != 0)
                                {
                                    local.fk_Clientid_Users = clientMapping;
                                    updated = true;
                                }
                            }
                            break;

                        case "totalAmount":
                            local.totalAmount = doc.Total ?? sale?.Total ?? 0;
                            updated = true;
                            break;

                        case "OrdersDate":
                            var extDate = ParseButentDate(doc.Date)?.Date ?? DateTime.UtcNow.Date;
                            local.OrdersDate = extDate;
                            updated = true;
                            break;

                        case "orderItems":
                            var butentItems = await butent.GetDocumentItemsAsync(res.ExternalDocumentId);

                            var productMappings = await db.products
                                .Where(p => p.fk_Companyid_Company == request.CompanyId && p.externalCode.HasValue)
                                .ToDictionaryAsync(p => p.externalCode!.Value, p => p.id_Product);

                            const double VatRate = 0.21;

                            var expectedItems = new List<(int productId, double quantity, double price, double vat)>();
                            foreach (var item in butentItems)
                            {
                                if (productMappings.TryGetValue(item.Good_Id, out var productId))
                                {
                                    var unitPrice = item.Price ?? 0;
                                    var vatValue = Math.Round(unitPrice * VatRate, 2);
                                    expectedItems.Add((productId, item.Quantity, unitPrice, vatValue));
                                }
                            }

                            // Get current items (they are already tracked because we used .Include())
                            var currentItems = local.ordersproducts?.ToList() ?? new List<ordersproduct>();


                            // First, remove all current items
                            foreach (var currentItem in currentItems)
                            {
                                db.ordersproducts.Remove(currentItem);
                            }

                            // Then, add all expected items
                            foreach (var (productId, quantity, price, vat) in expectedItems)
                            {
                                db.ordersproducts.Add(new ordersproduct
                                {
                                    fk_Ordersid_Orders = local.id_Orders,
                                    fk_Productid_Product = productId,
                                    quantity = quantity,
                                    unitPrice = price,
                                    vatValue = vat
                                });
                            }

                            updated = true;
                            break;
                    }
                }

                if (updated)
                {
                    report.OrdersUpdated++;
                }
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            report.TotalChanges = report.ClientsUpdated + report.ProductsUpdated + report.OrdersUpdated;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            report.Errors.Add($"Klaida: {ex.Message}");
        }

        return report;
    }

    private static bool StringEquals(string? a, string? b)
    {
        var trimA = a?.Trim() ?? "";
        var trimB = b?.Trim() ?? "";
        return trimA.Equals(trimB, StringComparison.OrdinalIgnoreCase);
    }

    private static DateTime? ParseButentDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        return DateTime.TryParseExact(
            value.Trim(),
            "yyyy-MM-dd HH:mm:ss",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var dt)
            ? dt
            : null;
    }
}