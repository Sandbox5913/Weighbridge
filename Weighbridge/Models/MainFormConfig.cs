using System.Collections.Generic;

namespace Weighbridge.Models
{
    public class MainFormConfig
    {
        public FormField EntranceWeight { get; set; } = new FormField { Label = "Entrance Weight" };
        public FormField ExitWeight { get; set; } = new FormField { Label = "Exit Weight" };
        public FormField NetWeight { get; set; } = new FormField { Label = "Net Weight" };
        public FormField Vehicle { get; set; } = new FormField { Label = "Vehicle" };
        public FormField SourceSite { get; set; } = new FormField { Label = "Source Site" };
        public FormField DestinationSite { get; set; } = new FormField { Label = "Destination Site" };
        public FormField Item { get; set; } = new FormField { Label = "Item" };
        public FormField Customer { get; set; } = new FormField { Label = "Customer" };
        public FormField Transport { get; set; } = new FormField { Label = "Transport" };
        public FormField Driver { get; set; } = new FormField { Label = "Driver" };
        public FormField Remarks { get; set; } = new FormField { Label = "Remarks" };
    }
}