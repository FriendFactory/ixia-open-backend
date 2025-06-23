namespace Common.Models.Database.Interfaces;

/// <summary>
///     Entity has GroupId
/// </summary>
public interface IGroupAccessible
{
    long GroupId { get; set; }
}