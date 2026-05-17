using Bakalauras.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Bakalauras.API.Dtos;
[ApiController]
[Route("api/products/")]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductController(AppDbContext db)
    {
        _db = db;
    }

    // -------- Helpers --------

    private int GetRequiredCompanyId()
    {
        var companyId = User.GetCompanyId();
        if (companyId <= 0)
            throw new UnauthorizedAccessException("No active company selected.");
        return companyId;
    }

    // -------- READ (LIST) --------

    [HttpGet("allProducts")]
    public async Task<IActionResult> GetAllProducts()
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var products = await _db.products
            .AsNoTracking()
            .Where(p => p.fk_Companyid_Company == companyId)
            .Select(p => new
            {
                p.id_Product,
                p.name,
                p.description,
                p.price,
                p.canTheProductBeProductReturned,
                p.countableItem,
                p.unit,
                p.shipping_mode,
                p.vat,
                p.creationDate,
                p.externalCode,
            })
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("allProductsFullInfo")]
    public async Task<IActionResult> GetAllProductsFullInfo()
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var products = await _db.products
            .AsNoTracking()
            .Where(p => p.fk_Companyid_Company == companyId)
            .Select(p => new
            {
                p.id_Product,
                p.name,
                p.description,
                p.price,
                p.canTheProductBeProductReturned,
                p.countableItem,
                p.unit,
                p.shipping_mode,
                p.vat,
                p.creationDate,
                p.externalCode,

                groups = p.fk_ProductGroupId_ProductGroups
                    .Select(g => new
                    {
                        g.id_ProductGroup,
                        g.name
                    })
                    .ToList(),

                categories = p.fk_Categoryid_Categories
                    .Select(c => new
                    {
                        c.id_Category,
                        c.name
                    })
                    .ToList(),

                images = p.product_images
                    .OrderBy(i => i.sortOrder)
                    .Select(i => new { i.id_ProductImage, i.url, i.isPrimary, i.sortOrder })
                    .ToList(),
            })
            .ToListAsync();

        return Ok(products);
    }

    // -------- LOOKUPS --------

    [AllowAnonymous]
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _db.categories
            .AsNoTracking()
            .Select(c => new { c.id_Category, c.name })
            .ToListAsync();

        return Ok(categories);
    }

    [AllowAnonymous]
    [HttpGet("productgroups")]
    public async Task<IActionResult> GetProductGroups()
    {
        var groups = await _db.productgroups
            .AsNoTracking()
            .Select(g => new { g.id_ProductGroup, g.name })
            .ToListAsync();

        return Ok(groups);
    }

    // -------- CREATE --------

    [HttpPost("createProduct")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> CreateProduct([FromForm] CreateProductDto dto)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var cat = await _db.categories.FindAsync(dto.categoryId);
        var grp = await _db.productgroups.FindAsync(dto.groupId);
        if (cat == null || grp == null) return BadRequest("Invalid category/group");

        var p = new product
        {
            fk_Companyid_Company = companyId, // ✅ always selected company
            name = dto.name,
            price = dto.price,
            unit = dto.unit ?? "vnt",
            description = dto.description,
            vat = dto.vat,
            canTheProductBeProductReturned = dto.canTheProductBeProductReturned,
            countableItem = dto.countableItem,
            creationDate = DateTime.UtcNow
        };

        p.fk_Categoryid_Categories.Add(cat);
        p.fk_ProductGroupId_ProductGroups.Add(grp);

        _db.products.Add(p);
        await _db.SaveChangesAsync();
        // save images
        if (dto.images != null && dto.images.Count > 0)
        {
            var uploadsRoot = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "products",
                p.id_Product.ToString()
            );
            Directory.CreateDirectory(uploadsRoot);

            var order = 0;
            foreach (var file in dto.images.Where(f => f.Length > 0))
            {
                var ext = Path.GetExtension(file.FileName);
                var filename = $"{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(uploadsRoot, filename);

                await using var fs = System.IO.File.Create(fullPath);
                await file.CopyToAsync(fs);

                var url = $"/uploads/products/{p.id_Product}/{filename}";
                _db.Add(new product_image
                {
                    fk_Productid_Product = p.id_Product,
                    url = url,
                    isPrimary = (order == 0),
                    sortOrder = order
                });
                order++;
            }

            await _db.SaveChangesAsync();

            // jei nėra primary – padarom pirmą primary
            var imgs = await _db.Set<product_image>()
                .Where(x => x.fk_Productid_Product == p.id_Product)
                .OrderBy(x => x.id_ProductImage)
                .ToListAsync();

            if (imgs.Count > 0 && !imgs.Any(x => x.isPrimary))
            {
                imgs[0].isPrimary = true;
                await _db.SaveChangesAsync();
            }
        }

        return Ok(new { p.id_Product });
    }

    // -------- READ (SINGLE) --------

    [HttpGet("product/{id:int}")]
    public async Task<IActionResult> GetProductForEdit(int id)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var p = await _db.products
            .AsNoTracking()
            .Where(x => x.id_Product == id && x.fk_Companyid_Company == companyId)
            .Select(x => new
            {
                x.id_Product,
                x.name,
                x.price,
                x.unit,
                x.description,
                x.vat,
                x.canTheProductBeProductReturned,
                x.countableItem,
                x.fk_Companyid_Company,

                categoryId = x.fk_Categoryid_Categories
                    .Select(c => (int?)c.id_Category)
                    .FirstOrDefault(),

                groupId = x.fk_ProductGroupId_ProductGroups
                    .Select(g => (int?)g.id_ProductGroup)
                    .FirstOrDefault(),

                images = x.product_images
                    .OrderBy(i => i.sortOrder)
                    .Select(i => new { i.id_ProductImage, i.url, i.sortOrder })
                    .ToList(),
            })
            .FirstOrDefaultAsync();

        if (p == null) return NotFound();
        return Ok(p);
    }

    // -------- UPDATE --------

    [HttpPut("editProduct/{id:int}")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UpdateProduct(int id, [FromForm] CreateProductDto dto)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var p = await _db.products
            .Include(x => x.fk_Categoryid_Categories)
            .Include(x => x.fk_ProductGroupId_ProductGroups)
            .Include(x => x.product_images)
            .FirstOrDefaultAsync(x => x.id_Product == id);

        if (p == null) return NotFound();

        if (p.fk_Companyid_Company != companyId)
            return StatusCode(403, "Product not in your company.");

        p.name = dto.name;
        p.price = dto.price;
        p.unit = dto.unit ?? "vnt";
        p.description = dto.description;
        p.vat = dto.vat;
        p.canTheProductBeProductReturned = dto.canTheProductBeProductReturned;
        p.countableItem = dto.countableItem;

        p.fk_Categoryid_Categories.Clear();
        p.fk_ProductGroupId_ProductGroups.Clear();

        var cat = await _db.categories.FindAsync(dto.categoryId);
        var grp = await _db.productgroups.FindAsync(dto.groupId);
        if (cat == null || grp == null) return BadRequest("Invalid category/group");

        p.fk_Categoryid_Categories.Add(cat);
        p.fk_ProductGroupId_ProductGroups.Add(grp);

        // ----- images: keep list -----
        List<int> keepIds = new();
        bool keepListProvided = !string.IsNullOrWhiteSpace(dto.keepImageIdsJson);

        if (keepListProvided)
        {
            try { keepIds = JsonSerializer.Deserialize<List<int>>(dto.keepImageIdsJson!) ?? new(); }
            catch
            {
                keepListProvided = false;
                keepIds = new();
            }
        }

        if (!keepListProvided)
            keepIds = p.product_images.Select(i => i.id_ProductImage).ToList();

        var keepSet = keepIds.ToHashSet();

        var toDelete = p.product_images.Where(img => !keepSet.Contains(img.id_ProductImage)).ToList();

        foreach (var img in toDelete)
        {
            if (!string.IsNullOrWhiteSpace(img.url))
            {
                var physical = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    img.url.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
                );

                if (System.IO.File.Exists(physical))
                    System.IO.File.Delete(physical);
            }

            _db.Remove(img);
        }

        var dir = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "uploads",
            "products",
            p.id_Product.ToString()
        );

        if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
            Directory.Delete(dir, true);

        await _db.SaveChangesAsync();

        // ----- images: order json -----
        List<ImageOrderItem> orderItems = new();
        if (!string.IsNullOrWhiteSpace(dto.imageOrderJson))
        {
            try { orderItems = JsonSerializer.Deserialize<List<ImageOrderItem>>(dto.imageOrderJson) ?? new(); }
            catch { orderItems = new(); }
        }

        if (orderItems.Count == 0)
        {
            var existing = await _db.Set<product_image>()
                .Where(x => x.fk_Productid_Product == p.id_Product)
                .OrderBy(x => x.sortOrder)
                .Select(x => x.id_ProductImage)
                .ToListAsync();

            orderItems = existing.Select(x => new ImageOrderItem { type = "existing", id = x }).ToList();
        }

        var newFiles = dto.images?.Where(f => f.Length > 0).ToList() ?? new List<IFormFile>();

        var newPositions = new List<int>();
        for (int i = 0; i < orderItems.Count; i++)
            if (string.Equals(orderItems[i].type, "new", StringComparison.OrdinalIgnoreCase))
                newPositions.Add(i);

        var uploadsRoot = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "uploads",
            "products",
            p.id_Product.ToString()
        );
        Directory.CreateDirectory(uploadsRoot);

        for (int fi = 0; fi < newFiles.Count; fi++)
        {
            var file = newFiles[fi];

            var ext = Path.GetExtension(file.FileName);
            var filename = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsRoot, filename);

            await using (var fs = System.IO.File.Create(fullPath))
                await file.CopyToAsync(fs);

            var url = $"/uploads/products/{p.id_Product}/{filename}";
            int sortOrder = (fi < newPositions.Count) ? newPositions[fi] : (orderItems.Count + fi);

            _db.Add(new product_image
            {
                fk_Productid_Product = p.id_Product,
                url = url,
                isPrimary = false,
                sortOrder = sortOrder
            });
        }

        await _db.SaveChangesAsync();

        // reorder existing
        var allAfter = await _db.Set<product_image>()
            .Where(x => x.fk_Productid_Product == p.id_Product)
            .ToListAsync();

        for (int pos = 0; pos < orderItems.Count; pos++)
        {
            var it = orderItems[pos];
            if (!string.Equals(it.type, "existing", StringComparison.OrdinalIgnoreCase)) continue;
            if (!it.id.HasValue) continue;

            var img = allAfter.FirstOrDefault(x => x.id_ProductImage == it.id.Value);
            if (img != null) img.sortOrder = pos;
        }

        allAfter = allAfter.OrderBy(x => x.sortOrder).ThenBy(x => x.id_ProductImage).ToList();
        for (int i = 0; i < allAfter.Count; i++)
            allAfter[i].sortOrder = i;

        await _db.SaveChangesAsync();

        // set primary = first by sort order
        allAfter = await _db.Set<product_image>()
            .Where(x => x.fk_Productid_Product == p.id_Product)
            .OrderBy(x => x.sortOrder)
            .ThenBy(x => x.id_ProductImage)
            .ToListAsync();

        if (allAfter.Count > 0)
        {
            for (int i = 0; i < allAfter.Count; i++)
                allAfter[i].isPrimary = (i == 0);

            await _db.SaveChangesAsync();
        }

        return Ok();
    }

    // -------- DELETE --------

    [HttpDelete("deleteProduct/{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var p = await _db.products
            .Include(x => x.fk_Categoryid_Categories)
            .Include(x => x.fk_ProductGroupId_ProductGroups)
            .Include(x => x.product_images)
            .FirstOrDefaultAsync(x => x.id_Product == id);

        if (p == null) return NotFound();

        if (p.fk_Companyid_Company != companyId)
            return StatusCode(403, "Product not in your company.");

        // 1) delete files
        foreach (var img in p.product_images.ToList())
        {
            if (string.IsNullOrWhiteSpace(img.url)) continue;

            var physical = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                img.url.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
            );

            if (System.IO.File.Exists(physical))
                System.IO.File.Delete(physical);
        }

        _db.RemoveRange(p.product_images);

        p.fk_Categoryid_Categories.Clear();
        p.fk_ProductGroupId_ProductGroups.Clear();

        await _db.SaveChangesAsync();

        // 2) delete folder if exists
        var dir = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "uploads",
            "products",
            p.id_Product.ToString()
        );

        if (Directory.Exists(dir))
            Directory.Delete(dir, true);

        _db.products.Remove(p);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}