using LeadFlow.Domain.Entities;
using LeadFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LeadFlow.Tests.Infrastructure.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class LeadPersistenceTests(PostgreSqlDatabaseFixture database)
{
    [Fact]
    public void Model_MapsLeadPropertiesAndIndexes()
    {
        using var context = database.CreateContext();
        var lead = context.Model.FindEntityType(typeof(Lead))!;

        Assert.False(lead.FindProperty(nameof(Lead.SubmissionId))!.IsNullable);
        Assert.Equal(100, lead.FindProperty(nameof(Lead.Name))!.GetMaxLength());
        Assert.False(lead.FindProperty(nameof(Lead.Name))!.IsNullable);
        Assert.Equal(50, lead.FindProperty(nameof(Lead.Phone))!.GetMaxLength());
        Assert.Equal(254, lead.FindProperty(nameof(Lead.Email))!.GetMaxLength());
        Assert.Equal(150, lead.FindProperty(nameof(Lead.Service))!.GetMaxLength());
        Assert.Equal(2000, lead.FindProperty(nameof(Lead.CustomerComment))!.GetMaxLength());
        Assert.Equal(4000, lead.FindProperty(nameof(Lead.ManagerComment))!.GetMaxLength());

        Assert.Equal(typeof(string), lead.FindProperty(nameof(Lead.Status))!.GetTypeMapping().Converter!.ProviderClrType);
        Assert.Equal(typeof(string), lead.FindProperty(nameof(Lead.Source))!.GetTypeMapping().Converter!.ProviderClrType);
        Assert.Contains(
            lead.GetIndexes(),
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Lead.Status), nameof(Lead.CreatedAtUtc)]));
        Assert.Contains(
            lead.GetIndexes(),
            index => index.IsUnique && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Lead.SubmissionId)]));

        var history = context.Model.FindEntityType(typeof(LeadStatusHistory))!;
        Assert.Contains(
            history.GetIndexes(),
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(LeadStatusHistory.LeadId)]));
    }

    [Fact]
    public async Task SaveChanges_RejectsLeadWithOverlongRequiredName()
    {
        await using var context = database.CreateContext();
        var lead = CreateLead();
        lead.Name = new string('И', 101);
        context.Leads.Add(lead);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_PersistsLeadAndStatusHistoryAtomically()
    {
        var lead = CreateLead();

        await using (var context = database.CreateContext())
        {
            context.Leads.Add(lead);
            context.LeadStatusHistory.Add(new LeadStatusHistory
            {
                Id = Guid.NewGuid(),
                Lead = lead,
                NewStatus = LeadStatus.New,
                ChangedAtUtc = lead.CreatedAtUtc
            });

            await context.SaveChangesAsync();
        }

        await using var verificationContext = database.CreateContext();
        var savedLead = await verificationContext.Leads
            .Include(item => item.StatusHistory)
            .SingleAsync(item => item.Id == lead.Id);

        Assert.Equal(LeadStatus.New, savedLead.Status);
        Assert.Equal(LeadSource.Website, savedLead.Source);
        var history = Assert.Single(savedLead.StatusHistory);
        Assert.Null(history.PreviousStatus);
        Assert.Equal(LeadStatus.New, history.NewStatus);
    }

    [Fact]
    public async Task SaveChanges_RollsBackLeadWhenRelatedHistoryViolatesForeignKey()
    {
        var lead = CreateLead();

        await using (var context = database.CreateContext())
        {
            context.Leads.Add(lead);
            context.LeadStatusHistory.Add(new LeadStatusHistory
            {
                Id = Guid.NewGuid(),
                LeadId = Guid.NewGuid(),
                NewStatus = LeadStatus.New,
                ChangedAtUtc = lead.CreatedAtUtc
            });

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        await using var verificationContext = database.CreateContext();
        Assert.False(await verificationContext.Leads.AnyAsync(item => item.Id == lead.Id));
    }

    [Fact]
    public async Task RemovingLead_CascadesToStatusHistory()
    {
        var lead = CreateLead();

        await using (var context = database.CreateContext())
        {
            context.Leads.Add(lead);
            context.LeadStatusHistory.Add(new LeadStatusHistory
            {
                Id = Guid.NewGuid(),
                Lead = lead,
                NewStatus = LeadStatus.New,
                ChangedAtUtc = lead.CreatedAtUtc
            });
            await context.SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var persistedLead = await context.Leads.SingleAsync(item => item.Id == lead.Id);
            context.Leads.Remove(persistedLead);
            await context.SaveChangesAsync();
        }

        await using var verificationContext = database.CreateContext();
        Assert.False(await verificationContext.LeadStatusHistory.AnyAsync(item => item.LeadId == lead.Id));
    }

    private static Lead CreateLead()
    {
        return new Lead
        {
            Id = Guid.NewGuid(),
            SubmissionId = Guid.NewGuid(),
            Name = "Иван Петров",
            Phone = "+7 999 123-45-67",
            Email = "ivan@example.com",
            Service = "Разработка сайта",
            CustomerComment = "Нужен лендинг.",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Status = LeadStatus.New,
            Source = LeadSource.Website
        };
    }
}
