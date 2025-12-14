namespace StoreManagementAPI.DTOs
{
    public class TransactionDto
    {
        public int Id { get; set; }
        public string ReferenceCode { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Sale" or "Import"
        public string PartnerName { get; set; } = string.Empty; // Customer or Supplier Name
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
