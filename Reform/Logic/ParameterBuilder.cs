using Reform.Interfaces;

namespace Reform.Logic
{
    public class ParameterBuilder : IParameterBuilder
    {
        /// <summary>
        /// Gets a parameter name in the format @p{index}
        /// </summary>
        /// <param name="columnName">The name of the column (unused in this implementation)</param>
        /// <param name="index">The index of the parameter</param>
        /// <returns>A unique parameter name</returns>
        public string GetParameterName(string columnName, int index)
        {
            return $"@p{index + 1}";
        }
    }
} 