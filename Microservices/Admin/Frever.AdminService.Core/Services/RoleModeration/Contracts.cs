namespace Frever.AdminService.Core.Services.RoleModeration;

public class AccessScopeDto
{
    public string Name { get; set; }
    public string Value { get; set; }
}

public class RoleDto
{
    public long Id { get; set; }
    public string Name { get; set; }
    public AccessScopeDto[] AccessScopes { get; set; }
}

public class UserRoleDto
{
    public long GroupId { get; set; }
    public string Email { get; set; }
    public string Nickname { get; set; }
    public RoleDto[] Roles { get; set; }
}

public class RoleModel
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string[] AccessScopes { get; set; } = [];
}

public class UserRoleModel
{
    public long GroupId { get; set; }
    public long[] RoleIds { get; set; } = [];
}