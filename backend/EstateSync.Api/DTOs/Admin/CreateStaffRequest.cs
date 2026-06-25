namespace EstateSync.Api.DTOs.Admin;

public record CreateStaffRequest(string FullName, string Email, string PhoneNumber, string Password);
