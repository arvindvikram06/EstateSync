using System;

namespace EstateSync.Api.Models;

public class RefreshToken
{
    public Guid RefreshTokenId { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public required string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public required string TokenFamily { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
