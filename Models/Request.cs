namespace VSMSWebClient.Models
{
    public class Request
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? SecondName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Uuid { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public string? SendTime { get; set; }
        public bool IsSent { get; set; } = false;
    }
}