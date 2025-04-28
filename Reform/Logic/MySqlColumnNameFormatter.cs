using Reform.Interfaces;

namespace Reform.Logic
{
    public class MySqlColumnNameFormatter : IColumnNameFormatter
    {
        public string Format(string columnName)
        {
            return $"`{columnName}`";
        }
    }
} 