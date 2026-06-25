using System;
using System.Collections.Generic;

namespace EstateSync.Api.Models;

public class Role
{
    public Guid RoleId { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
