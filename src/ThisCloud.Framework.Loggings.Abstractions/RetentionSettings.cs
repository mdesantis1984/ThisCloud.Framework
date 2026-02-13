namespace ThisCloud.Framework.Loggings.Abstractions;

/// <summary>
/// Configuration settings for log retention policies.
/// </summary>
public sealed class RetentionSettings
{
    private int _days = 30;

    /// <summary>
    /// Gets or sets the number of days to retain log data.
    /// </summary>
    /// <remarks>
    /// Default: <c>30</c> days.
    /// Valid range: 1 to 3650 days (approximately 10 years).
    /// This is a logical TTL; cleanup is the responsibility of the host application.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is less than 1 or greater than 3650.
    /// </exception>
    public int Days
    {
        get => _days;
        set
        {
            if (value < 1 || value > 3650)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Days),
                    value,
                    "Days must be between 1 and 3650.");
            }
            _days = value;
        }
    }
}
