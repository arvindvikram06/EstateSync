using System;
using System.Collections.Generic;
using System.Linq;
using EstateSync.Api.DTOs.Properties;
using EstateSync.Api.Models;
using EstateSync.Api.Models.Enums;

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
            State = s.State,
            City = s.City,
            AddressLine = s.AddressLine,
            PrimaryImage = s.PropertyImages.FirstOrDefault(i => i.IsPrimary)?.Image,
            PropertyType = s.PropertyType.ToString(),
            Status = s.Status.ToString(),
            OwnerName = ResolveOwnerDisplayName(s.Owner),
            OwnerEmail = s.Owner?.Email ?? string.Empty,
            CreatedAt = s.CreatedAt,
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
            OwnerName = ResolveOwnerDisplayName(s.Owner),
            Title = s.Title,
            Description = s.Description,
            PropertyType = s.PropertyType.ToString(),
            State = s.State,
            City = s.City,
            Pincode = s.Pincode,
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
            State = request.State,
            City = request.City,
            Pincode = request.Pincode,
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

    private static string ResolveOwnerDisplayName(User? owner)
    {
        if (owner == null) return string.Empty;
        if (owner.OwnerProfile?.OwnerType == OwnerType.Company && !string.IsNullOrWhiteSpace(owner.OwnerProfile.CompanyName))
            return owner.OwnerProfile.CompanyName;
        return owner.FullName;
    }

    public void MapUpdateRequestToEntity(UpdatePropertyRequest request, Property entity)
    {
        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.PropertyType = request.PropertyType;
        entity.State = request.State;
        entity.City = request.City;
        entity.Pincode = request.Pincode;
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

    /// <summary>
    /// Applies only the non-null fields from a patch request to an existing draft entity.
    /// Amenities with a non-null list (even empty) fully replace the existing list.
    /// Returns true if any field was changed.
    /// </summary>
    public bool ApplyPatchToEntity(PatchPropertyDraftRequest patch, Property entity)
    {
        bool changed = false;

        if (patch.PropertyType.HasValue)  { entity.PropertyType  = patch.PropertyType.Value;  changed = true; }
        if (patch.State       != null)    { entity.State         = patch.State;                changed = true; }
        if (patch.City        != null)    { entity.City          = patch.City;                 changed = true; }
        if (patch.Pincode     != null)    { entity.Pincode       = patch.Pincode;              changed = true; }
        if (patch.AddressLine != null)    { entity.AddressLine   = patch.AddressLine;          changed = true; }
        if (patch.Latitude    .HasValue)  { entity.Latitude      = patch.Latitude;             changed = true; }
        if (patch.Longitude   .HasValue)  { entity.Longitude     = patch.Longitude;            changed = true; }
        if (patch.Bedrooms    .HasValue)  { entity.Bedrooms      = patch.Bedrooms.Value;       changed = true; }
        if (patch.Bathrooms   .HasValue)  { entity.Bathrooms     = patch.Bathrooms.Value;      changed = true; }
        if (patch.LandArea    .HasValue)  { entity.LandArea      = patch.LandArea.Value;       changed = true; }
        if (patch.BuildupArea .HasValue)  { entity.BuildupArea   = patch.BuildupArea.Value;    changed = true; }
        if (patch.Title       != null)    { entity.Title         = patch.Title;                changed = true; }
        if (patch.Description != null)    { entity.Description   = patch.Description;          changed = true; }
        if (patch.MonthlyRent .HasValue)  { entity.MonthlyRent   = patch.MonthlyRent.Value;    changed = true; }
        if (patch.SecurityDeposit.HasValue) { entity.SecurityDeposit = patch.SecurityDeposit.Value; changed = true; }
        if (patch.Amenities   != null)
        {
            entity.Amenities = patch.Amenities
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => a.Replace(" ", "").ToLowerInvariant())
                .Distinct()
                .ToList();
            changed = true;
        }

        return changed;
    }
}
