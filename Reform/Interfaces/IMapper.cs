namespace Reform.Interfaces
{
    /// <summary>
    /// Interface for mapping between objects
    /// </summary>
    public interface IMapper
    {
        /// <summary>
        /// Maps properties from one object to another
        /// </summary>
        /// <typeparam name="TSource">The source type to map from</typeparam>
        /// <typeparam name="TDestination">The destination type to map to</typeparam>
        /// <param name="source">The source object</param>
        /// <returns>A new instance of the destination type with mapped properties</returns>
        TDestination Map<TSource, TDestination>(TSource source) where TDestination : class, new();

        /// <summary>
        /// Maps properties from one object to an existing instance of another
        /// </summary>
        /// <typeparam name="TSource">The source type to map from</typeparam>
        /// <typeparam name="TDestination">The destination type to map to</typeparam>
        /// <param name="source">The source object</param>
        /// <param name="destination">The destination object to map to</param>
        void Map<TSource, TDestination>(TSource source, TDestination destination) where TDestination : class;
    }
} 