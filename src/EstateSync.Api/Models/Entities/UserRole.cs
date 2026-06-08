using System;

namespace EstateSync.Api.Models;

public class UserRole
{
    public Guid UserRoleId { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
}
