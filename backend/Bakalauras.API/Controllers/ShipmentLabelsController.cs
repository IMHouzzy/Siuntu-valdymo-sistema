using Bakalauras.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System.IO.Compression;

[ApiController]
[Route("api/shipments")]
[Authorize]
public class ShipmentLabelsController : ControllerBase
{
    private readonly AppDbContext        _db;
    private readonly IWebHostEnvironment _env;

    public ShipmentLabelsController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    //  Helper 

    private int GetRequiredCompanyId()
    {
        var id = User.GetCompanyId();
        if (id <= 0) throw new UnauthorizedAccessException("No active company selected.");
        return id;
    }

    /// <summary>
    /// Resolves a stored labelFile value (either a relative URL like /labels/1/label_1.pdf
    /// or an absolute URL) to a full filesystem path under wwwroot.
    /// </summary>
    private string? ResolvePhysicalPath(string? labelFile)
    {
        if (string.IsNullOrWhiteSpace(labelFile)) return null;

        // Strip leading slash if present, then combine with wwwroot
        var relative = labelFile.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var webRoot  = _env.WebRootPath
                    ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        var full = Path.Combine(webRoot, relative);
        return System.IO.File.Exists(full) ? full : null;
    }

    // GET /api/shipments/{id}/labels/zip
    /// <summary>
    /// Returns a ZIP archive containing all label PDFs for the shipment.
    /// </summary>
    [HttpGet("{id:int}/labels/zip")]
    public async Task<IActionResult> DownloadLabelsZip(int id)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var shipment = await _db.shipments
            .AsNoTracking()
            .Where(s => s.id_Shipment == id && s.fk_Companyid_Company == companyId)
            .Select(s => new { s.id_Shipment, s.trackingNumber })
            .FirstOrDefaultAsync();

        if (shipment == null) return NotFound();

        var packages = await _db.packages
            .AsNoTracking()
            .Where(p => p.fk_Shipmentid_Shipment == id)
            .OrderBy(p => p.id_Package)
            .Select(p => new { p.id_Package, p.labelFile })
            .ToListAsync();

        if (!packages.Any())
            return NotFound("No packages found for this shipment.");

        // Build ZIP in memory
        var memStream = new MemoryStream();
        using (var archive = new ZipArchive(memStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            int index = 1;
            foreach (var pkg in packages)
            {
                var physPath = ResolvePhysicalPath(pkg.labelFile);
                if (physPath == null) { index++; continue; }

                var entryName = $"label_{shipment.trackingNumber}_P{index:D2}.pdf";
                var entry     = archive.CreateEntry(entryName, CompressionLevel.Fastest);

                await using var entryStream = entry.Open();
                await using var fileStream  = System.IO.File.OpenRead(physPath);
                await fileStream.CopyToAsync(entryStream);

                index++;
            }
        }

        memStream.Position = 0;
        var fileName = $"labels_{shipment.trackingNumber}.zip";
        return File(memStream, "application/zip", fileName);
    }

    // GET /api/shipments/{id}/labels/merged
    /// <summary>
    /// Merges all label PDFs into a single PDF and returns it.
    /// The user can open it, see all pages, and print in one go.
    /// </summary>
    [HttpGet("{id:int}/labels/merged")]
    public async Task<IActionResult> DownloadMergedLabels(int id)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var shipment = await _db.shipments
            .AsNoTracking()
            .Where(s => s.id_Shipment == id && s.fk_Companyid_Company == companyId)
            .Select(s => new { s.id_Shipment, s.trackingNumber })
            .FirstOrDefaultAsync();

        if (shipment == null) return NotFound();

        var packages = await _db.packages
            .AsNoTracking()
            .Where(p => p.fk_Shipmentid_Shipment == id)
            .OrderBy(p => p.id_Package)
            .Select(p => new { p.id_Package, p.labelFile })
            .ToListAsync();

        if (!packages.Any())
            return NotFound("No packages found for this shipment.");

        // Merge with PdfSharpCore
        var outputDoc = new PdfDocument();

        foreach (var pkg in packages)
        {
            var physPath = ResolvePhysicalPath(pkg.labelFile);
            if (physPath == null) continue;

            using var sourceDoc = PdfReader.Open(physPath, PdfDocumentOpenMode.Import);
            foreach (var page in sourceDoc.Pages)
                outputDoc.AddPage(page);
        }

        if (outputDoc.PageCount == 0)
            return NotFound("No label files could be read.");

        var memStream = new MemoryStream();
        outputDoc.Save(memStream, false);
        memStream.Position = 0;

        var fileName = $"all_labels_{shipment.trackingNumber}.pdf";
        return File(memStream, "application/pdf", fileName);
    }
}