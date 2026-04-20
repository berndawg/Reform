namespace ReformIntegrationTests
{
    internal static class Assert
    {
        public static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException($"Expected: {expected}, Actual: {actual}");
        }

        public static void True(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException("Expected: True, Actual: False");
        }

        public static void False(bool condition)
        {
            if (condition)
                throw new InvalidOperationException("Expected: False, Actual: True");
        }

        public static void Null(object value)
        {
            if (value != null)
                throw new InvalidOperationException($"Expected: null, Actual: {value}");
        }

        public static void Single<T>(IList<T> list)
        {
            if (list.Count != 1)
                throw new InvalidOperationException($"Expected single element, Actual count: {list.Count}");
        }

        public static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Expected: {typeof(TException).Name}, Actual: {ex.GetType().Name}: {ex.Message}");
            }

            throw new InvalidOperationException($"Expected: {typeof(TException).Name}, but no exception was thrown");
        }
    }
}
