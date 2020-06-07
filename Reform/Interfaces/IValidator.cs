using System.Collections.Generic;
using Reform.Enum;

namespace Reform.Interfaces
{
    public interface IValidator<T> where T : class
    {
        void Validate(T item);
    }
}