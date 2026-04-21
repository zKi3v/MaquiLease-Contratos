using MaquiLease.API.Data;
using MaquiLease.API.Models.DTOs;
using MaquiLease.API.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaquiLease.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContractsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ContractsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContractDto>>> GetContracts()
        {
            var contracts = await _context.Contracts
                .Include(c => c.Client)
                .Include(c => c.Asset)
                .Include(c => c.Service)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ContractDto
                {
                    ContractId = c.ContractId,
                    ContractNumber = c.ContractNumber,
                    ClientId = c.ClientId,
                    ClientName = c.Client.BusinessName,
                    AssetId = c.AssetId,
                    AssetCode = c.Asset != null ? c.Asset.Code : null,
                    ServiceId = c.ServiceId,
                    ServiceName = c.Service != null ? c.Service.Name : null,
                    ContractType = c.ContractType,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    TotalAmount = c.TotalAmount,
                    Currency = c.Currency,
                    PaymentFrequency = "mensual",
                    InitialPayment = 0,
                    LeasingTerms = "Estándar",
                    Status = c.Status,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(contracts);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ContractDto>> GetContract(int id)
        {
            var c = await _context.Contracts
                .Include(x => x.Client)
                .Include(x => x.Asset)
                .Include(x => x.Service)
                .Include(x => x.Installments)
                .FirstOrDefaultAsync(x => x.ContractId == id);

            if (c == null) return NotFound();

            var dto = new ContractDto
            {
                ContractId = c.ContractId,
                ContractNumber = c.ContractNumber,
                ClientId = c.ClientId,
                ClientName = c.Client.BusinessName,
                AssetId = c.AssetId,
                AssetCode = c.Asset?.Code,
                ServiceId = c.ServiceId,
                ServiceName = c.Service?.Name,
                ContractType = c.ContractType,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                TotalAmount = c.TotalAmount,
                Currency = c.Currency,
                PaymentFrequency = "mensual",
                InitialPayment = 0,
                LeasingTerms = "Estándar",
                Status = c.Status,
                SignatureHash = c.SignatureHash,
                CreatedAt = c.CreatedAt,
                Installments = c.Installments.Select(i => new InstallmentDto
                {
                    InstallmentId = i.InstallmentId,
                    InstallmentNumber = i.InstallmentNumber,
                    DueDate = i.DueDate,
                    Amount = i.Amount,
                    PenaltyAmount = i.PenaltyAmount,
                    Status = i.Status,
                    PaidAmount = i.PaidAmount,
                    PaidDate = i.PaidDate
                }).OrderBy(i => i.InstallmentNumber).ToList()
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<ContractDto>> CreateContract(CreateContractDto dto)
        {
            var contract = new Contract
            {
                ContractNumber = "CTR-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                ClientId = dto.ClientId,
                AssetId = dto.AssetId,
                ServiceId = dto.ServiceId,
                ContractType = dto.ContractType,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                TotalAmount = dto.TotalAmount,
                Currency = dto.Currency,
                NumberOfInstallments = dto.NumberOfInstallments,
                Status = "activo",
                SignatureHash = dto.SignatureHash,
                CreatedAt = DateTime.UtcNow
            };

            // Cálculo del cronograma de cuotas (Installments)
            decimal amountToFinance = dto.TotalAmount - dto.InitialPayment;
            if (dto.NumberOfInstallments > 0 && amountToFinance > 0)
            {
                decimal amountPerInstallment = Math.Round(amountToFinance / dto.NumberOfInstallments, 2);
                
                for (int i = 1; i <= dto.NumberOfInstallments; i++)
                {
                    var dueDate = dto.StartDate.AddMonths(i);
                    // Ajuste de centavos en la ultima cuota si es necesario
                    decimal installmentAmount = (i == dto.NumberOfInstallments) 
                        ? amountToFinance - (amountPerInstallment * (dto.NumberOfInstallments - 1)) 
                        : amountPerInstallment;

                    contract.Installments.Add(new Installment
                    {
                        InstallmentNumber = i,
                        DueDate = dueDate,
                        Amount = installmentAmount,
                        PenaltyAmount = 0,
                        Status = "pendiente",
                        PaidAmount = 0,
                        NotifiedApproaching = false,
                        NotifiedOverdue = false
                    });
                }
            }

            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();

            // Si hay un activo implicado en arrendamiento, cambiarlo a "alquilado"
            if (dto.AssetId.HasValue && dto.ContractType.Contains("arrendamiento", StringComparison.OrdinalIgnoreCase))
            {
                var asset = await _context.Assets.FindAsync(dto.AssetId);
                if (asset != null)
                {
                    asset.Status = "alquilado";
                    await _context.SaveChangesAsync();
                }
            }

            return CreatedAtAction(nameof(GetContract), new { id = contract.ContractId }, new { contract.ContractId });
        }
    }
}
