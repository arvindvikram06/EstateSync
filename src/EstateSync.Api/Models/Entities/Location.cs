using System;
using System.Collections.Generic;

namespace EstateSync.Api.Models;

public class Location
{
    public Guid LocationId { get; set; }
    public required string StateName { get; set; }
    public required string District { get; set; }
    public required string Pincode { get; set; }
    public required string Country { get; set; }

    public ICollection<Property> Properties { get; set; } = new List<Property>();
}
