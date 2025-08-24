using SQLite;
using System;

namespace Weighbridge.Models
{
    [Table("dockets")]
    public class Docket : IEntity // Implement the IEntity interface
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public decimal EntranceWeight { get; set; }
        public decimal ExitWeight { get; set; }
        public decimal NetWeight { get; set; }
        public int? VehicleId { get; set; }
        public int? SourceSiteId { get; set; }
        public int? DestinationSiteId { get; set; }
        public int? ItemId { get; set; }
        public int? CustomerId { get; set; }
        public int? TransportId { get; set; }
        public int? DriverId { get; set; }
        public string? Remarks { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Status { get; set; }
        public DateTime UpdatedAt { get; set; } // Add this line
        public TransactionType TransactionType { get; set; }
        public string? WeighingMode { get; set; }
    }

    // Validation Request Object
    public class ValidationRequest
    {
        public decimal LiveWeight { get; set; }
        public decimal? TareWeight { get; set; }
        public WeighingMode CurrentMode { get; set; }
        public bool IsWeightStable { get; set; }
        public string VehicleRegistration { get; set; } = string.Empty;
        public Vehicle? SelectedVehicle { get; set; }
        public Site? SelectedSourceSite { get; set; }
        public Site? SelectedDestinationSite { get; set; }
        public Item? SelectedItem { get; set; }
        public Customer? SelectedCustomer { get; set; }
        public Transport? SelectedTransport { get; set; }
        public Driver? SelectedDriver { get; set; }
        public string? Remarks { get; set; }
        public decimal? EntranceWeight { get; set; } // For net weight calculation
        public DateTime OperationTimestamp { get; set; } = DateTime.Now;
    }

    // Validation Result
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new();
        public List<ValidationWarning> Warnings { get; set; } = new();

        public bool HasCriticalErrors => Errors.Any(e => e.Severity == ValidationSeverity.Critical);
        public bool HasWarnings => Warnings.Any();
    }

    public class ValidationError
    {
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ValidationSeverity Severity { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    public class ValidationWarning
    {
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    // Result types for clean return values
    public record OperationResult(bool Success, string Message, string[] Errors, Docket? Docket, Vehicle? Vehicle, bool ShouldResetForm, bool ShouldShowInfo, string InfoMessage)
    {
        public static OperationResult Succeeded(string message, Docket? docket = null, Vehicle? vehicle = null, bool shouldResetForm = false, bool shouldShowInfo = false, string infoMessage = "") =>
            new(true, message, Array.Empty<string>(), docket, vehicle, shouldResetForm, shouldShowInfo, infoMessage);
            
        public static OperationResult Failed(string message, params string[] errors) =>
            new(false, message, errors, null, null, false, false, "");
    }
}