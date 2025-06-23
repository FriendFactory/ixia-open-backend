using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Database;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Characters;

public interface IAiCharacterRepository
{
    Task<NestedTransaction> BeginTransaction();
    Task<int> SaveChanges();
    Task<AiCharacterImage[]> GetCharacters(long groupId, int skip, int take);
    Task<AiCharacter> GetCharacter(long id, long groupId);
    Task<Group> GetGroupById(long id);
    Task<string> GetGenderNameById(long id);
    Task<AiCharacter> AddCharacter(AiCharacter character);
    Task<AiCharacterImage> AddCharacterImage(AiCharacterImage image);
    Task MarkCharacterAsDeleted(AiCharacter character);
}

public class AiCharacterRepository(IWriteDb writeDb) : IAiCharacterRepository
{
    public Task<NestedTransaction> BeginTransaction()
    {
        return writeDb.BeginTransactionSafe();
    }

    public Task<int> SaveChanges()
    {
        return writeDb.SaveChangesAsync();
    }

    public Task<AiCharacterImage[]> GetCharacters(long groupId, int skip, int take)
    {
        return writeDb.AiCharacterImage.Include(e => e.Character)
                      .Where(e => e.DeletedAt == null)
                      .Where(e => e.Character.GroupId == groupId && e.Character.DeletedAt == null)
                      .OrderBy(e => e.Id)
                      .Skip(skip)
                      .Take(take)
                      .ToArrayAsync();
    }

    public Task<AiCharacter> GetCharacter(long id, long groupId)
    {
        return writeDb.AiCharacter.FirstOrDefaultAsync(e => e.Id == id && e.GroupId == groupId);
    }

    public Task<Group> GetGroupById(long id)
    {
        return writeDb.Group.FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null && !e.IsBlocked);
    }

    public Task<string> GetGenderNameById(long id)
    {
        return writeDb.Gender.Where(e => e.Id == id).Select(e => e.Name).FirstOrDefaultAsync();
    }

    public async Task<AiCharacter> AddCharacter(AiCharacter character)
    {
        ArgumentNullException.ThrowIfNull(character);

        await writeDb.AiCharacter.AddAsync(character);
        await writeDb.SaveChangesAsync();

        return character;
    }

    public async Task<AiCharacterImage> AddCharacterImage(AiCharacterImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        await writeDb.AiCharacterImage.AddAsync(image);
        await writeDb.SaveChangesAsync();

        return image;
    }

    public async Task MarkCharacterAsDeleted(AiCharacter character)
    {
        ArgumentNullException.ThrowIfNull(character);

        await using var transaction = await BeginTransaction();

        character.DeletedAt = DateTime.UtcNow;
        await writeDb.SaveChangesAsync();

        await writeDb.AiCharacterImage.Where(e => e.AiCharacterId == character.Id)
                     .ExecuteUpdateAsync(e => e.SetProperty(p => p.DeletedAt, DateTime.UtcNow));

        await transaction.Commit();
    }
}