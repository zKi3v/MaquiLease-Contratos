using MaquiLease.API.Data;
using MaquiLease.API.Models.DTOs;
using MaquiLease.API.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaquiLease.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssetsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AssetsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AssetDto>>> GetAssets()
        {
            var assets = await _context.Assets
                .Select(a => new AssetDto
                {
                    AssetId = a.AssetId,
                    Code = a.Code,
                    Name = a.Name,
                    Category = a.Category,
                    Brand = a.Brand,
                    Model = a.Model,
                    PurchaseDate = a.PurchaseDate,
                    PurchasePriceCNY = a.PurchasePriceCNY,
                    PurchasePriceUSD = a.PurchasePriceUSD,
                    CurrentValue = a.CurrentValue,
                    Status = a.Status,
                    ImageUrl = a.ImageUrl,
                    Notes = a.Notes
                })
                .ToListAsync();

            return Ok(assets);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AssetDto>> GetAsset(int id)
        {
            var a = await _context.Assets.FindAsync(id);

            if (a == null)
                return NotFound();

            return new AssetDto
            {
                AssetId = a.AssetId,
                Code = a.Code,
                Name = a.Name,
                Category = a.Category,
                Brand = a.Brand,
                Model = a.Model,
                PurchaseDate = a.PurchaseDate,
                PurchasePriceCNY = a.PurchasePriceCNY,
                PurchasePriceUSD = a.PurchasePriceUSD,
                CurrentValue = a.CurrentValue,
                Status = a.Status,
                ImageUrl = a.ImageUrl,
                Notes = a.Notes
            };
        }

        [HttpPost]
        public async Task<ActionResult<AssetDto>> CreateAsset(CreateAssetDto dto)
        {
            if (await _context.Assets.AnyAsync(a => a.Code == dto.Code))
                return BadRequest("Ya existe un activo con este Codigo.");

            var asset = new Asset
            {
                Code = dto.Code,
                Name = dto.Name,
                Category = dto.Category,
                Brand = dto.Brand,
                Model = dto.Model,
                PurchaseDate = dto.PurchaseDate,
                PurchasePriceCNY = dto.PurchasePriceCNY,
                PurchasePriceUSD = dto.PurchasePriceUSD,
                CurrentValue = dto.CurrentValue ?? dto.PurchasePriceUSD,
                Status = dto.Status,
                ImageUrl = dto.ImageUrl,
                Notes = dto.Notes
            };

            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAsset), new { id = asset.AssetId }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsset(int id, CreateAssetDto dto)
        {
            var a = await _context.Assets.FindAsync(id);
            if (a == null) return NotFound();

            a.Code = dto.Code;
            a.Name = dto.Name;
            a.Category = dto.Category;
            a.Brand = dto.Brand;
            a.Model = dto.Model;
            a.PurchaseDate = dto.PurchaseDate;
            a.PurchasePriceCNY = dto.PurchasePriceCNY;
            a.PurchasePriceUSD = dto.PurchasePriceUSD;
            a.CurrentValue = dto.CurrentValue;
            a.Status = dto.Status;
            a.ImageUrl = dto.ImageUrl;
            a.Notes = dto.Notes;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
