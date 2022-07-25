namespace TaskApi.Models
{
    public class TaskItem
    {
        public long Id { get; set; }
        public string? Description { get; set; }
        public string? Priority { get; set; }
        public string? Status { get; set; }
        public long CustomerId { get; set; }
    }
}