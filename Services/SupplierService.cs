using Microsoft.EntityFrameworkCore;
using StoreManagementAPI.Data;
using StoreManagementAPI.Models;
using StoreManagementAPI.Repositories;

namespace StoreManagementAPI.Services
{
    public interface ISupplierService
    {
        Task<IEnumerable<Supplier>> SearchSupplier(string searchItem);
        Task<IEnumerable<Supplier>> GetSupplierByStatus(string status);
        Task<bool> CheckPhoneExists(string phone);
        Task<bool> CheckEmailExists(string email);
    }

    public class SupplierService : ISupplierService
    {
        private readonly IRepository<Supplier> _supplierRepository;
        public readonly StoreDbContext _context;
        public SupplierService(StoreDbContext context, IRepository<Supplier> supplierRepository)
        {
            _context = context;
            _supplierRepository = supplierRepository;
        }


        public async Task<IEnumerable<Supplier>> SearchSupplier(string searchItem)
        {
            if (string.IsNullOrWhiteSpace(searchItem))
            {
                return await _supplierRepository.GetAllAsync();
            }

            searchItem = searchItem.ToLower().Trim();

            var suppliers = await _context.Suppliers
                .Where(s =>
                    s.Name.ToLower().Contains(searchItem) ||
                    (s.Phone != null && s.Phone.Contains(searchItem)) ||
                    (s.Address != null && s.Address.ToLower().Contains(searchItem)))
                .ToListAsync();
            return suppliers;
        }

        public async Task<IEnumerable<Supplier>> GetSupplierByStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return await _supplierRepository.GetAllAsync();
            }
        
            var suppliers = await _context.Suppliers
                .Where(s=> s.Status.ToLower().Contains(status)).ToListAsync();

            return suppliers;
        }

        public async Task<bool> CheckPhoneExists(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return true; 
            }

            var Phone = phone.Trim().ToLower();
            bool exists = await _context.Suppliers
                .AnyAsync(s => s.Phone != null && s.Phone.ToLower() == Phone);

            return exists;
        }

        public async Task<bool> CheckEmailExists(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return true;
            }
            var Email = email.Trim().ToLower();
            bool exists = await _context.Suppliers
                .AnyAsync(s => s.Email != null && s.Email.ToLower() == Email);
            return exists;
        }


    }
}