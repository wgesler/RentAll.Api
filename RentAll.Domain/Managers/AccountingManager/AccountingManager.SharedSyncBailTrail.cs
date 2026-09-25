namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Sync Bail Trail
    /// <summary>
    /// Ordered diagnostic notes for Sync create/replace paths (payments, deposits, transfers).
    /// </summary>
    private sealed class AccountingSyncBailTrail
    {
        /// <summary>Health fix only adjusts document links/stamps — never block on JE posting status.</summary>
        public bool IgnorePostingStatusForLinkRepair { get; init; }

        public List<string> Trail { get; } = [];

        public void Note(string message) => Trail.Add(message);

        public void Bail(string message) => Trail.Add(message);

        public string FormatBailTrail()
            => Trail.Count == 0
                ? "(no trail)"
                : string.Join(Environment.NewLine, Trail.Select((line, index) => $"{index + 1}. {line}"));
    }
    #endregion
}
