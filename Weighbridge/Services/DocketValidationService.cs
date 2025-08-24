using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Weighbridge.Models;
using Weighbridge.Services;

namespace Weighbridge.Services
{
    public class DocketValidationService : IDocketValidationService
    {
        private readonly ILoggingService _loggingService;
        
        public DocketValidationService(ILoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        public ValidationResult ValidateDocket(ValidationRequest request, MainFormConfig config)
        {
            var result = new ValidationResult();
            
            try
            {
                // Run all validation checks
                var validationChecks = new[]
                {
                    ValidateTimestamp(request),
                    ValidateWeightValues(request, config),
                    ValidateWeightStability(request.LiveWeight, request.IsWeightStable, request.CurrentMode),
                    ValidateTareWeight(request, config),
                    ValidateNetWeight(request, config),
                    ValidateMandatoryFields(request, config),
                    ValidateVehicleRules(request.SelectedVehicle, request),
                    ValidateCrossFieldRules(request, Enumerable.Empty<Transport>()), // TODO: Pass actual transports
                    ValidateBusinessRules(request, config)
                };

                // Combine all results
                foreach (var check in validationChecks)
                {
                    result.Errors.AddRange(check.Errors);
                    result.Warnings.AddRange(check.Warnings);
                }

                result.IsValid = !result.Errors.Any();
                return result;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in ValidateDocket: {ex.Message}", ex);
                result.Errors.Add(new ValidationError
                {
                    Field = "System",
                    Message = "An error occurred during validation.",
                    Severity = ValidationSeverity.Critical,
                    Code = "VALIDATION_ERROR"
                });
                result.IsValid = false;
                return result;
            }
        }

        private ValidationResult ValidateTimestamp(ValidationRequest request)
        {
            var result = new ValidationResult();
            
            // Time-based validation with proper timezone handling
            var utcNow = DateTime.UtcNow;
            var localNow = DateTime.Now;
            var maxAllowedDrift = TimeSpan.FromMinutes(5);

            if (request.OperationTimestamp > localNow.Add(maxAllowedDrift))
            {
                result.Errors.Add(new ValidationError
                {
                    Field = "OperationTimestamp",
                    Message = "Operation timestamp cannot be in the future.",
                    Severity = ValidationSeverity.Error,
                    Code = "TIMESTAMP_FUTURE"
                });
            }

            // Check for reasonable timestamp (not too far in the past)
            var minAllowedTime = localNow.AddHours(-24);
            if (request.OperationTimestamp < minAllowedTime)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Field = "OperationTimestamp", 
                    Message = "Operation timestamp is more than 24 hours old.",
                    Code = "TIMESTAMP_OLD"
                });
            }

            return result;
        }

        private ValidationResult ValidateWeightValues(ValidationRequest request, MainFormConfig config)
        {
            var result = new ValidationResult();

            // Live weight validation
            if (request.LiveWeight < 0)
            {
                result.Errors.Add(new ValidationError
                {
                    Field = "LiveWeight",
                    Message = "Weight cannot be negative.",
                    Severity = ValidationSeverity.Error,
                    Code = "WEIGHT_NEGATIVE"
                });
            }

            if (request.LiveWeight > config.MaximumWeight)
            {
                result.Errors.Add(new ValidationError
                {
                    Field = "LiveWeight",
                    Message = $"Weight ({request.LiveWeight:F2}) exceeds maximum allowed weight ({config.MaximumWeight:F2}).",
                    Severity = ValidationSeverity.Critical,
                    Code = "WEIGHT_EXCEEDS_MAX"
                });
            }

            if (request.LiveWeight < config.MinimumWeight && request.LiveWeight > 0)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Field = "LiveWeight",
                    Message = $"Weight ({request.LiveWeight:F2}) is below minimum recommended weight ({config.MinimumWeight:F2}).",
                    Code = "WEIGHT_BELOW_MIN"
                });
            }

            // Check for suspicious weight values
            if (request.LiveWeight > 0 && request.LiveWeight < 10)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Field = "LiveWeight",
                    Message = "Weight value seems unusually low. Please verify reading.",
                    Code = "WEIGHT_SUSPICIOUS_LOW"
                });
            }

            return result;
        }

        public ValidationResult ValidateWeightStability(decimal weight, bool isStable, WeighingMode mode)
        {
            var result = new ValidationResult();

            // Weight stability check for modes that require it
            var modesRequiringStability = new[]
            {
                WeighingMode.TwoWeights,
                WeighingMode.SingleWeight,
                WeighingMode.EntryAndTare,
                WeighingMode.TareAndExit
            };

            if (modesRequiringStability.Contains(mode) && !isStable)
            {
                result.Errors.Add(new ValidationError
                {
                    Field = "WeightStability",
                    Message = "Weight must be stable before capturing. Please wait for stability indicator.",
                    Severity = ValidationSeverity.Error,
                    Code = "WEIGHT_UNSTABLE"
                });
            }

            return result;
        }

        private ValidationResult ValidateTareWeight(ValidationRequest request, MainFormConfig config)
        {
            var result = new ValidationResult();

            var modesUsingTare = new[] { WeighingMode.EntryAndTare, WeighingMode.TareAndExit };
            
            if (modesUsingTare.Contains(request.CurrentMode) && request.TareWeight.HasValue)
            {
                var tareWeight = request.TareWeight.Value;
                
                if (tareWeight < 0)
                {
                    result.Errors.Add(new ValidationError
                    {
                        Field = "TareWeight",
                        Message = "Tare weight cannot be negative.",
                        Severity = ValidationSeverity.Error,
                        Code = "TARE_NEGATIVE"
                    });
                }

                if (tareWeight > config.MaximumTareWeight)
                {
                    result.Errors.Add(new ValidationError
                    {
                        Field = "TareWeight",
                        Message = $"Tare weight ({tareWeight:F2}) exceeds maximum allowed ({config.MaximumTareWeight:F2}).",
                        Severity = ValidationSeverity.Error,
                        Code = "TARE_EXCEEDS_MAX"
                    });
                }

                // Tare weight reasonableness check
                if (tareWeight > request.LiveWeight && request.CurrentMode == WeighingMode.EntryAndTare)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Field = "TareWeight",
                        Message = "Tare weight exceeds current weight. This may indicate an error.",
                        Code = "TARE_EXCEEDS_GROSS"
                    });
                }
            }

            return result;
        }

        private ValidationResult ValidateNetWeight(ValidationRequest request, MainFormConfig config)
        {
            var result = new ValidationResult();

            if (request.CurrentMode == WeighingMode.TwoWeights && request.EntranceWeight.HasValue)
            {
                var netWeight = Math.Abs(request.EntranceWeight.Value - request.LiveWeight);
                
                if (netWeight < config.MinimumNetWeight)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Field = "NetWeight",
                        Message = $"Net weight ({netWeight:F2}) is below minimum recommended ({config.MinimumNetWeight:F2}).",
                        Code = "NET_WEIGHT_LOW"
                    });
                }

                // Check for zero or near-zero net weight
                if (netWeight < 1)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Field = "NetWeight",
                        Message = "Net weight is very low. Please verify weights are correct.",
                        Code = "NET_WEIGHT_ZERO"
                    });
                }
            }

            return result;
        }

        private ValidationResult ValidateMandatoryFields(ValidationRequest request, MainFormConfig config)
        {
            var result = new ValidationResult();

            var mandatoryFieldChecks = new[]
            {
                (config.Vehicle.IsMandatory, string.IsNullOrWhiteSpace(request.VehicleRegistration) && request.SelectedVehicle == null, 
                 "Vehicle", "Please enter or select a vehicle.", "VEHICLE_REQUIRED"),
                (config.SourceSite.IsMandatory, request.SelectedSourceSite == null, 
                 "SourceSite", "Please select a source site.", "SOURCE_SITE_REQUIRED"),
                (config.DestinationSite.IsMandatory, request.SelectedDestinationSite == null, 
                 "DestinationSite", "Please select a destination site.", "DESTINATION_SITE_REQUIRED"),
                (config.Item.IsMandatory, request.SelectedItem == null, 
                 "Item", "Please select an item/material.", "ITEM_REQUIRED"),
                (config.Customer.IsMandatory, request.SelectedCustomer == null, 
                 "Customer", "Please select a customer.", "CUSTOMER_REQUIRED"),
                (config.Transport.IsMandatory, request.SelectedTransport == null, 
                 "Transport", "Please select a transport company.", "TRANSPORT_REQUIRED"),
                (config.Driver.IsMandatory, request.SelectedDriver == null, 
                 "Driver", "Please select a driver.", "DRIVER_REQUIRED"),
                (config.Remarks.IsMandatory, string.IsNullOrWhiteSpace(request.Remarks), 
                 "Remarks", "Please enter remarks.", "REMARKS_REQUIRED")
            };

            foreach (var (isMandatory, isEmpty, field, message, code) in mandatoryFieldChecks)
            {
                if (isMandatory && isEmpty)
                {
                    result.Errors.Add(new ValidationError
                    {
                        Field = field,
                        Message = message,
                        Severity = ValidationSeverity.Error,
                        Code = code
                    });
                }
            }

            return result;
        }

        public ValidationResult ValidateVehicleRules(Vehicle? vehicle, ValidationRequest request)
        {
            var result = new ValidationResult();

            if (vehicle == null) return result;

            // Vehicle capacity check
            if (vehicle.MaxWeight > 0 && request.LiveWeight > vehicle.MaxWeight)
            {
                result.Errors.Add(new ValidationError
                {
                    Field = "Vehicle",
                    Message = $"Weight ({request.LiveWeight:F2}) exceeds vehicle's maximum capacity ({vehicle.MaxWeight:F2}).",
                    Severity = ValidationSeverity.Critical,
                    Code = "VEHICLE_CAPACITY_EXCEEDED"
                });
            }

            // Tare weight reasonableness check
            if (vehicle.TareWeight > 0 && 
                request.LiveWeight < vehicle.TareWeight && 
                request.CurrentMode != WeighingMode.TareAndExit)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Field = "Vehicle",
                    Message = $"Current weight ({request.LiveWeight:F2}) is less than vehicle's tare weight ({vehicle.TareWeight:F2}). Please verify.",
                    Code = "WEIGHT_BELOW_TARE"
                });
            }

            // Vehicle restrictions
            if (request.SelectedItem != null && 
                vehicle.RestrictedMaterials?.Contains(request.SelectedItem.Id) == true)
            {
                result.Errors.Add(new ValidationError
                {
                    Field = "Vehicle",
                    Message = $"Vehicle {vehicle.LicenseNumber} is not authorized to transport {request.SelectedItem.Name}.",
                    Severity = ValidationSeverity.Critical,
                    Code = "VEHICLE_MATERIAL_RESTRICTED"
                });
            }

            // Vehicle status checks
            if (vehicle.IsBlocked)
            {
                result.Errors.Add(new ValidationError
                {
                    Field = "Vehicle",
                    Message = $"Vehicle {vehicle.LicenseNumber} is currently blocked from operations.",
                    Severity = ValidationSeverity.Critical,
                    Code = "VEHICLE_BLOCKED"
                });
            }

            return result;
        }

        public ValidationResult ValidateCrossFieldRules(ValidationRequest request, IEnumerable<Transport> availableTransports)
        {
            var result = new ValidationResult();

            // Source and destination cannot be the same
            if (request.SelectedSourceSite != null && 
                request.SelectedDestinationSite != null && 
                request.SelectedSourceSite.Id == request.SelectedDestinationSite.Id)
            {
                result.Errors.Add(new ValidationError
                {
                    Field = "Sites",
                    Message = "Source and destination sites cannot be the same.",
                    Severity = ValidationSeverity.Error,
                    Code = "SAME_SOURCE_DESTINATION"
                });
            }

            // Material-specific validations
            if (request.SelectedItem != null)
            {
                // Required transport company check
                if (request.SelectedItem.RequiredTransportId.HasValue &&
                    (request.SelectedTransport == null || 
                     request.SelectedTransport.Id != request.SelectedItem.RequiredTransportId.Value))
                {
                    var requiredTransport = availableTransports
                        .FirstOrDefault(t => t.Id == request.SelectedItem.RequiredTransportId.Value);
                        
                    result.Errors.Add(new ValidationError
                    {
                        Field = "Transport",
                        Message = $"Material {request.SelectedItem.Name} requires transport company: {requiredTransport?.Name ?? "Unknown"}",
                        Severity = ValidationSeverity.Error,
                        Code = "TRANSPORT_REQUIRED_FOR_MATERIAL"
                    });
                }

                // Hazardous material driver certification check
                if (request.SelectedItem.IsHazardous && 
                    (request.SelectedDriver == null || !request.SelectedDriver.IsHazmatCertified))
                {
                    result.Errors.Add(new ValidationError
                    {
                        Field = "Driver",
                        Message = $"Hazardous material {request.SelectedItem.Name} requires a HAZMAT certified driver.",
                        Severity = ValidationSeverity.Critical,
                        Code = "HAZMAT_CERTIFICATION_REQUIRED"
                    });
                }
            }

            // Customer-specific validations
            if (request.SelectedCustomer != null && 
                request.SelectedItem != null &&
                request.SelectedCustomer.RestrictedMaterials?.Contains(request.SelectedItem.Id) == true)
            {
                result.Errors.Add(new ValidationError
                {
                    Field = "Customer",
                    Message = $"Customer {request.SelectedCustomer.Name} is not authorized for material {request.SelectedItem.Name}.",
                    Severity = ValidationSeverity.Critical,
                    Code = "CUSTOMER_MATERIAL_RESTRICTED"
                });
            }

            return result;
        }

        public ValidationResult ValidateBusinessRules(ValidationRequest request, MainFormConfig config)
        {
            var result = new ValidationResult();

            // Time-based business rules
            var businessHours = config.BusinessHours;
            if (businessHours?.IsEnabled == true)
            {
                var currentTime = request.OperationTimestamp.TimeOfDay;
                if (currentTime < businessHours.StartTime || currentTime > businessHours.EndTime)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Field = "BusinessHours",
                        Message = "Operation is outside normal business hours.",
                        Code = "OUTSIDE_BUSINESS_HOURS"
                    });
                }
            }

            // Weight trend analysis (if previous weights are available)
            // This would require historical data access - placeholder for future implementation
            
            return result;
        }
    }
}