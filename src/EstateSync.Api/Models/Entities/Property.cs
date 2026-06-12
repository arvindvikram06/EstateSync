using System;
using System.Collections.Generic;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class Property : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }

    public Guid LocationId { get; set; }
    public Location? Location { get; set; }

    public required string Title { get; set; }
    public required string Description { get; set; }
    public PropertyType PropertyType { get; set; }
    public PropertyStatus Status { get; set; }
    public required string AddressLine { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? Latitude { get; set; }
    public decimal LandArea { get; set; }
    public decimal BuildupArea { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal SecurityDeposit { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }

    public Guid? ApproveById { get; set; }
    public User? ApproveBy { get; set; }
    public string? ApproveRemarks { get; set; }

    public List<string> Amenities { get; set; } = new List<string>();
    public ICollection<PropertyImage> PropertyImages { get; set; } = new List<PropertyImage>();
    public ICollection<SavedProperty> SavedProperties { get; set; } = new List<SavedProperty>();
    public ICollection<PropertyDocument> PropertyDocuments { get; set; } = new List<PropertyDocument>();
    public ICollection<RentalApplication> RentalApplications { get; set; } = new List<RentalApplication>();
    public ICollection<Lease> Leases { get; set; } = new List<Lease>();
}
