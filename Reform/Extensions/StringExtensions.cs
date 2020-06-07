// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
namespace Reform.Extensions
{
    public static class StringExtensions
    {
        #region String extensions

        public static string RemoveFromEnd(this string fromString, string stringToBeRemoved)
        {
            if (string.IsNullOrWhiteSpace(fromString))
                return fromString;

            return fromString.EndsWith(stringToBeRemoved)
                ? fromString.Substring(0, fromString.Length - stringToBeRemoved.Length)
                : fromString;
        }

        #endregion
    }
}