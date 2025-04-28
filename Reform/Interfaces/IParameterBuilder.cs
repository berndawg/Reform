namespace Reform.Interfaces
{
    public interface IParameterBuilder
    {
        /// <summary>
        /// Gets a parameter name for a given column and index
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="index">The index of the parameter</param>
        /// <returns>A unique parameter name</returns>
        string GetParameterName(string columnName, int index);
    }
} 