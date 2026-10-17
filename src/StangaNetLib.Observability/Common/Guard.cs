namespace StangaNetLib.Observability.Common;

/// <summary>
/// Provides guard clauses to validate arguments.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Provides guard clauses for specific validation scenarios.
    /// </summary>
    public static class Against
    {
        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/> if the argument is null.
        /// </summary>
        /// <param name="argument">The argument to check.</param>
        /// <param name="argumentName">The name of the argument.</param>
        public static void Null(object? argument, string argumentName)
        {
            if (argument is null) throw new ArgumentNullException(argumentName);
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the argument is null or whitespace.
        /// </summary>
        /// <param name="argument">The argument to check.</param>
        /// <param name="argumentName">The name of the argument.</param>
        public static void NullOrWhiteSpace(string? argument, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new ArgumentException($"{argumentName} cannot be null or whitespace.", argumentName);
            }
        }
    }
}
