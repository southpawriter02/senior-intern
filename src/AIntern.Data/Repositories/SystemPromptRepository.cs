using Microsoft.EntityFrameworkCore;
using AIntern.Core.Entities;
using AIntern.Core.Interfaces;
using AIntern.Core.Templates;

namespace AIntern.Data.Repositories;

/// <summary>
/// Repository implementation for system prompt data access operations.
/// </summary>
public sealed class SystemPromptRepository : ISystemPromptRepository
{
    private readonly IDbContextFactory<AInternDbContext> _contextFactory;

    public SystemPromptRepository(IDbContextFactory<AInternDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<SystemPromptEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.SystemPrompts
            .AsNoTracking()
            .FirstOrDefaultAsync(sp => sp.Id == id, ct);
    }

    public async Task<IReadOnlyList<SystemPromptEntity>> GetAllAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.SystemPrompts
            .AsNoTracking()
            .OrderByDescending(sp => sp.IsDefault)
            .ThenBy(sp => sp.Name)
            .ToListAsync(ct);
    }

    public async Task<SystemPromptEntity?> GetDefaultAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.SystemPrompts
            .AsNoTracking()
            .FirstOrDefaultAsync(sp => sp.IsDefault, ct);
    }

    public async Task<SystemPromptEntity> CreateAsync(SystemPromptEntity prompt, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        context.SystemPrompts.Add(prompt);
        await context.SaveChangesAsync(ct);
        return prompt;
    }

    public async Task UpdateAsync(SystemPromptEntity prompt, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        prompt.UpdatedAt = DateTime.UtcNow;
        context.SystemPrompts.Update(prompt);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var prompt = await context.SystemPrompts.FindAsync(new object[] { id }, ct);
        if (prompt is null)
            return;

        if (prompt.IsBuiltIn)
            throw new InvalidOperationException("Cannot delete built-in system prompts.");

        // Clear SystemPromptId from conversations using this prompt
        await context.Conversations
            .Where(c => c.SystemPromptId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.SystemPromptId, (Guid?)null), ct);

        context.SystemPrompts.Remove(prompt);
        await context.SaveChangesAsync(ct);
    }

    public async Task SetDefaultAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        // Clear existing default
        await context.SystemPrompts
            .Where(sp => sp.IsDefault)
            .ExecuteUpdateAsync(s => s.SetProperty(sp => sp.IsDefault, false), ct);

        // Set new default
        await context.SystemPrompts
            .Where(sp => sp.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(sp => sp.IsDefault, true), ct);
    }

    public async Task IncrementUsageCountAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        await context.SystemPrompts
            .Where(sp => sp.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(sp => sp.UsageCount, sp => sp.UsageCount + 1), ct);
    }

    public async Task<SystemPromptEntity?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.SystemPrompts
            .AsNoTracking()
            .FirstOrDefaultAsync(sp => sp.Name == name, ct);
    }

    public async Task<IReadOnlyList<SystemPromptEntity>> GetUserPromptsAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.SystemPrompts
            .AsNoTracking()
            .Where(sp => !sp.IsBuiltIn)
            .OrderBy(sp => sp.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SystemPromptEntity>> GetBuiltInPromptsAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.SystemPrompts
            .AsNoTracking()
            .Where(sp => sp.IsBuiltIn)
            .OrderByDescending(sp => sp.IsDefault)
            .ThenBy(sp => sp.Category)
            .ThenBy(sp => sp.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SystemPromptEntity>> GetByCategoryAsync(string category, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.SystemPrompts
            .AsNoTracking()
            .Where(sp => sp.Category == category)
            .OrderByDescending(sp => sp.IsDefault)
            .ThenBy(sp => sp.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SystemPromptEntity>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<SystemPromptEntity>();

        var lowerQuery = query.ToLowerInvariant();

        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.SystemPrompts
            .AsNoTracking()
            .Where(sp =>
                sp.Name.ToLower().Contains(lowerQuery) ||
                (sp.Description != null && sp.Description.ToLower().Contains(lowerQuery)) ||
                sp.Content.ToLower().Contains(lowerQuery))
            .OrderByDescending(sp => sp.IsDefault)
            .ThenBy(sp => sp.Name)
            .ToListAsync(ct);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var query = context.SystemPrompts.Where(sp => sp.Name == name);

        if (excludeId.HasValue)
            query = query.Where(sp => sp.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task SeedBuiltInPromptsAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        // Check if any built-in prompts exist
        if (await context.SystemPrompts.AnyAsync(sp => sp.IsBuiltIn, ct))
            return;

        var templates = SystemPromptTemplates.GetAllTemplates();
        context.SystemPrompts.AddRange(templates);
        await context.SaveChangesAsync(ct);
    }
}
