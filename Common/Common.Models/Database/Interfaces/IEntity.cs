namespace Common.Models.Database.Interfaces;

/// <summary>
///     A class is one of main entities and has pk 'Id';
/// </summary>
public interface IEntity
{
    long Id { get; set; }
}