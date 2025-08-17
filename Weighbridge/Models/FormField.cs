namespace Weighbridge.Models
{
    public class FormField
    {
        public bool IsVisible { get; set; } = true;
        public string Label { get; set; }
        public bool IsMandatory { get; set; } = false;
    }
}