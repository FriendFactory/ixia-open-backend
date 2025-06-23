using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Frever.Shared.MainDb.Entities;

[Table("role", Schema = "cms")]
public class Role
{
    [Column("id")] public long Id { get; set; }
    [Column("name")] public string Name { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }

    public virtual List<RoleAccessScope> RoleAccessScope { get; set; }
}

[Table("role_access_scope", Schema = "cms")]
public class RoleAccessScope
{
    [Column("role_id")] public long RoleId { get; set; }
    [Column("access_scope")] public string AccessScope { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }

    public virtual Role Role { get; set; }
}

[Table("user_role", Schema = "cms")]
public class UserRole
{
    [Column("group_id")] public long GroupId { get; set; }
    [Column("role_id")] public long RoleId { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }

    public virtual Role Role { get; set; }
}