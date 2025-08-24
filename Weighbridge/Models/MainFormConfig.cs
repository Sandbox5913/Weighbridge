using System;
using System.Collections.Generic;

namespace Weighbridge.Models
{
    public class MainFormConfig
    {
        public FieldConfig Vehicle { get; set; } = new();
        public FieldConfig SourceSite { get; set; } = new();
        public FieldConfig DestinationSite { get; set; } = new();
        public FieldConfig Item { get; set; } = new();
        public FieldConfig Customer { get; set; } = new();
        public FieldConfig Transport { get; set; } = new();
        public FieldConfig Driver { get; set; } = new();
        public FieldConfig Remarks { get; set; } = new();
        
        // Business rule limits
        public decimal MaximumWeight { get; set; } = 100000; // 100 tons default
        public decimal MinimumWeight { get; set; } = 100;    // 100 kg minimum
        public decimal MaximumTareWeight { get; set; } = 50000; // 50 tons max tare
        public decimal MinimumNetWeight { get; set; } = 50;     // 50 kg minimum net
        public bool RequireStabilityForWeighing { get; set; } = true;
        public TimeSpan StabilityTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public int AutoResetDelaySeconds { get; set; } = 10; // Default to 10 seconds

        // New: Business Hours
        public BusinessHoursConfig BusinessHours { get; set; } = new BusinessHoursConfig();
    }

    public class BusinessHoursConfig
    {
        public bool IsEnabled { get; set; } = false;
        public TimeSpan StartTime { get; set; } = new TimeSpan(8, 0, 0); // Default 8:00 AM
        public TimeSpan EndTime { get; set; } = new TimeSpan(17, 0, 0); // Default 5:00 PM
        // You could add specific day-of-week rules here if needed, similar to the old BusinessHourRange
    }
}