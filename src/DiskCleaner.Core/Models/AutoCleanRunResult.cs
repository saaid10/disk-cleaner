namespace DiskCleaner.Core.Models;

/// <summary>
/// Outcome of one whitelisted auto-clean pass (requirements.txt 3i).
/// </summary>
public sealed record AutoCleanRunResult(bool Ran, int ItemsQuarantined, long BytesQuarantined)
{
    public static readonly AutoCleanRunResult NotDue = new(Ran: false, ItemsQuarantined: 0, BytesQuarantined: 0);
}
