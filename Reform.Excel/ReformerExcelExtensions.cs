using Microsoft.Extensions.DependencyInjection;
using Reform.Interfaces;

namespace Reform.Excel
{
    public static class ReformerExcelExtensions
    {
        public static Reformer UseExcel<T>(this Reformer reformer, string filePath) where T : class
            => reformer.Register<IReform<T>>(sp => new ExcelReform<T>(
                filePath,
                sp.GetRequiredService<IMetadataProvider<T>>(),
                sp.GetRequiredService<IValidator<T>>()));
    }
}
