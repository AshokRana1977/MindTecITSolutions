using System;

namespace MindtecFinance.Models
{
    public class Ticket
    {
        public int TicketID { get; set; }
        public int CompanyID { get; set; }
        public DateTime RaiseDate { get; set; } = DateTime.Now;
        public string? Priority { get; set; }
        public string? Description { get; set; }
        public string? Summary { get; set; }
        public string? RaisedBy { get; set; }
        public string? AssignedTo { get; set; }
        public string Status { get; set; } = "Open";
        
        // Navigation / Display Properties
        public string? CompanyName { get; set; }
    }
    public class TicketLog
    {
        public int Logid { get; set; }
        public int Ticketid { get; set; }
        public string? Actionlog { get; set; }
        public DateTime LogAT { get; set; } = DateTime.Now;
        public string? LogBy { get; set; }
        public string? AssignedTo { get; set; }
        public string? Status { get; set; }
    }
}
