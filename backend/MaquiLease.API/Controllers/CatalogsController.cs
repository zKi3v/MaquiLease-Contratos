using System.Collections.Generic;
using System.Threading.Tasks;
using MaquiLease.API.Data;
using MaquiLease.API.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaquiLease.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CatalogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CatalogsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("sectors")]
        public async Task<ActionResult<IEnumerable<ClientSector>>> GetSectors()
        {
            return Ok(await _context.ClientSectors.ToListAsync());
        }

        [HttpGet("categories-assets")]
        public async Task<ActionResult<IEnumerable<AssetCategory>>> GetAssetCategories()
        {
            return Ok(await _context.AssetCategories.ToListAsync());
        }

        [HttpGet("brands-assets")]
        public async Task<ActionResult<IEnumerable<AssetBrand>>> GetAssetBrands()
        {
            return Ok(await _context.AssetBrands.ToListAsync());
        }

        [HttpGet("categories-services")]
        public async Task<ActionResult<IEnumerable<ServiceCategory>>> GetServiceCategories()
        {
            return Ok(await _context.ServiceCategories.ToListAsync());
        }
    }
}
