using Microsoft.EntityFrameworkCore;
using StoreManagementAPI.Data;
using StoreManagementAPI.DTOs;
using StoreManagementAPI.Models;

namespace StoreManagementAPI.Services
{
    public interface IWarehouseService
    {
        Task<List<WarehouseDto>> GetAllWarehouses();
        Task<WarehouseDto?> GetWarehouseById(int warehouseId);
        Task<WarehouseDto> CreateWarehouse(WarehouseDto dto);
        Task<WarehouseDto> UpdateWarehouse(int warehouseId, WarehouseDto dto);
        Task<bool> DeleteWarehouse(int warehouseId);
    }

    public class WarehouseService : IWarehouseService
    {
        private readonly StoreDbContext _context;

        public WarehouseService(StoreDbContext context)
        {
            _context = context;
        }

        public async Task<List<WarehouseDto>> GetAllWarehouses()
        {
            var warehouses = await _context.Warehouses
                .Where(w => w.Status != "deleted")
                .ToListAsync();

            return warehouses.Select(w => MapToDto(w)).ToList();
        }

        public async Task<WarehouseDto?> GetWarehouseById(int warehouseId)
        {
            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.WarehouseId == warehouseId && w.Status != "deleted");

            if (warehouse == null)
                return null;

            return MapToDto(warehouse);
        }

        public async Task<WarehouseDto> CreateWarehouse(WarehouseDto dto)
        {
            var warehouse = new Warehouse
            {
                WarehouseName = dto.WarehouseName,
                Address = dto.Address,
                Status = "active"
            };

            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            return MapToDto(warehouse);
        }

        public async Task<WarehouseDto> UpdateWarehouse(int warehouseId, WarehouseDto dto)
        {
            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.WarehouseId == warehouseId && w.Status != "deleted");

            if (warehouse == null)
                throw new Exception("Không tìm thấy kho hàng");

            warehouse.WarehouseName = dto.WarehouseName;
            warehouse.Address = dto.Address;
            warehouse.Status = dto.Status;

            await _context.SaveChangesAsync();

            return MapToDto(warehouse);
        }

        public async Task<bool> DeleteWarehouse(int warehouseId)
        {
            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.WarehouseId == warehouseId);

            if (warehouse == null)
                return false;

            // Soft delete
            warehouse.Status = "deleted";
            await _context.SaveChangesAsync();
            return true;
        }

        private WarehouseDto MapToDto(Warehouse w)
        {
            return new WarehouseDto
            {
                WarehouseId = w.WarehouseId,
                WarehouseName = w.WarehouseName,
                Address = w.Address,
                Status = w.Status
            };
        }
    }
}
