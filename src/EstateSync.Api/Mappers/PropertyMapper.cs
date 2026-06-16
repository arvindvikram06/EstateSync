using System;
using System.Collections.Generic;
using System.Linq;
using EstateSync.Api.DTOs.Properties;
using EstateSync.Api.Models;

namespace EstateSync.Api.Mappers;

public class PropertyMapper
{
    public PropertySummaryResponse MapToSummaryResponse(Property s)
    {
        return new PropertySummaryResponse
        {
            PropertyId = s.PropertyId,
            Title = s.Title,
            MonthlyRent = s.MonthlyRent,
            Bedrooms = s.Bedrooms,
            Bathrooms = s.Bathrooms,
            State = s.Location != null ? s.Location.StateName : string.Empty,
            AddressLine = s.AddressLine,
            PrimaryImage = s.PropertyImages.FirstOrDefault(i => i.IsPrimary)?.Image,
            PropertyType = s.PropertyType.ToString(),
            Status = s.Status.ToString()
        };
    }

    public PropertyImageResponse MapToImageResponse(PropertyImage pi)
    {
        return new PropertyImageResponse
        {
            ImageId = pi.ImageId,
            Image = pi.Image,
            IsPrimary = pi.IsPrimary,
            DisplayOrder = pi.DisplayOrder
        };
    }

    public PropertyDetailResponse MapToDetailResponse(Property s)
    {
        return new PropertyDetailResponse
        {
            PropertyId = s.PropertyId,
            OwnerId = s.OwnerId,
            OwnerName = s.Owner != null ? s.Owner.FullName : string.Empty,
            Title = s.Title,
            Description = s.Description,
            PropertyType = s.PropertyType.ToString(),
            LocationId = s.LocationId,
            State = s.Location != null ? s.Location.StateName : string.Empty,
            District = s.Location != null ? s.Location.District : string.Empty,
            Pincode = s.Location != null ? s.Location.Pincode : string.Empty,
            Country = s.Location != null ? s.Location.Country : string.Empty,
            AddressLine = s.AddressLine,
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            LandArea = s.LandArea,
            BuildupArea = s.BuildupArea,
            MonthlyRent = s.MonthlyRent,
            SecurityDeposit = s.SecurityDeposit,
            Bedrooms = s.Bedrooms,
            Bathrooms = s.Bathrooms,
            Status = s.Status.ToString(),
            Amenities = s.Amenities ?? new List<string>(),
            Images = s.PropertyImages != null ? s.PropertyImages.Select(MapToImageResponse).ToList() : new List<PropertyImageResponse>()
        };
    }

    public Property MapCreateRequestToEntity(CreatePropertyRequest request)
    {
        return new Property
        {
            PropertyId = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            PropertyType = request.PropertyType,
            LocationId = request.LocationId,
            AddressLine = request.AddressLine,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            LandArea = request.LandArea,
            BuildupArea = request.BuildupArea,
            MonthlyRent = request.MonthlyRent,
            SecurityDeposit = request.SecurityDeposit,
            Bedrooms = request.Bedrooms,
            Bathrooms = request.Bathrooms,
            Status = PropertyStatus.Draft
        };
    }

    public void MapUpdateRequestToEntity(UpdatePropertyRequest request, Property entity)
    {
        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.PropertyType = request.PropertyType;
        entity.LocationId = request.LocationId;
        entity.AddressLine = request.AddressLine;
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        entity.LandArea = request.LandArea;
        entity.BuildupArea = request.BuildupArea;
        entity.MonthlyRent = request.MonthlyRent;
        entity.SecurityDeposit = request.SecurityDeposit;
        entity.Bedrooms = request.Bedrooms;
        entity.Bathrooms = request.Bathrooms;
    }
}
