namespace DiskCleaner.Core.Models;

/// <summary>
/// Outcome of one whitelisted auto-clean pass (requirements.txt 3i). Carries the
/// quarantined items themselves so a before/after report (3j) can be built directly
/// from a scheduled run, not just from manual actions.
/// </summary>
public sealed record AutoCleanRunResult(bool Ran, int ItemsQuarantined, long BytesQuarantined, IReadOnlyList<QuarantineItem> Items)
{
    public static readonly AutoCleanRunResult NotDue = new(Ran: false, ItemsQuarantined: 0, BytesQuarantined: 0, Array.Empty<QuarantineItem>());
}
