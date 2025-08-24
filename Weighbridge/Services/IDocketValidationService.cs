using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Weighbridge.Models;

namespace Weighbridge.Services
{
    public interface IDocketValidationService
    {
        ValidationResult ValidateDocket(ValidationRequest request, MainFormConfig config);
        ValidationResult ValidateWeightStability(decimal weight, bool isStable, WeighingMode mode);
        ValidationResult ValidateVehicleRules(Vehicle? vehicle, ValidationRequest request);
        ValidationResult ValidateCrossFieldRules(ValidationRequest request, IEnumerable<Transport> availableTransports);
        ValidationResult ValidateBusinessRules(ValidationRequest request, MainFormConfig config);
    }
}