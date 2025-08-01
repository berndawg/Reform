# Reform

Reform is an ORM that's extremely easy to use and extend. Stop writing SQL! Use the IReform interface instead. It includes methods like Count, Exists, Select, Insert, Update, Delete, Merge, and even BulkInsert. It supports Symmetric Key entryption and it's fully customizable allowing it to be used even without SQL Server. 

Reform puts the C# developer back in control of how data is shaped and manipulated. Define POCOs, add Reform code attributes, and you're done. It includes support for validation and transactions and it’s the perfect tool for ETL applications.

The IReform<T> interface includes the following methods:

- SqlConnection GetConnection()
- TransactionScope GetScope();
- int Count();
- int Count(SqlConnection connection);
- int Count(List<Filter> filters);
- int Count(SqlConnection connection, List<Filter> filters);
- bool Exists(Filter filter);
- bool Exists(List<Filter> filters);
- bool Exists(SqlConnection connection, List<Filter> filters);
- void Insert(T item);
- void Insert(List<T> items);
- void Insert(SqlConnection connection, T item);
- void Insert(SqlConnection connection, List<T> items);
- void Update(T item);
- void Update(List<T> list);
- void Update(SqlConnection connection, T item);
- void Update(SqlConnection connection, List<T> list);
- void Delete(T item);
- void Delete(List<T> list);
- void Delete(SqlConnection connection, T item);
- void Delete(SqlConnection connection, List<T> list);
- T SelectSingle(List<Filter> filters);
- T SelectSingle(List<Filter> filters, T defaultObject);
- T SelectSingle(SqlConnection connection, List<Filter> filters);
- T SelectSingle(SqlConnection connection, List<Filter> filters, T defaultObject);
- IEnumerable<T> Select();
- IEnumerable<T> Select(Filter filter);
- IEnumerable<T> Select(List<Filter> filters);
- IEnumerable<T> Select(QueryCriteria queryCriteria);
- IEnumerable<T> Select(QueryCriteria queryCriteria, out int totalCount);
- IEnumerable<T> Select(SqlConnection connection, List<Filter> filters);
- IEnumerable<T> Select(SqlConnection connection, QueryCriteria queryCriteria);
- void Truncate();
- void Truncate(SqlConnection connection);
- void BulkInsert(List<T> list);
- void BulkInsert(SqlConnection connection, List<T> list);
- void Merge(List<T> list);
- void Merge(List<T> list, Filter filter);
- void Merge(List<T> list, List<Filter> filters);
- void Merge(SqlConnection connection, List<T> list, List<Filter> filters);

In addition, the Reform<T> implementation supports the following overrides:
- SqlConnection OnGetConnection()
- TransactionScope OnGetScope()
- void OnValidate(SqlConnection connection, T item)
- int OnCount(SqlConnection connection, List<Filter> filters)
- bool OnExists(SqlConnection connection, List<Filter> filters)
- IEnumerable<T> OnSelect(SqlConnection connection, QueryCriteria queryCriteria)
- void OnInsert(SqlConnection connection, T item)
- void OnUpdate(SqlConnection connection, T item)
- void OnDelete(SqlConnection connection, T item)
- void OnBeforeInsert(SqlConnection connection, T item)
- void OnBeforeUpdate(SqlConnection connection, T item)
- void OnAfterInsert(SqlConnection connection, T item)
- void OnAfterUpdate(SqlConnection connection, T item)
- void OnBeforeDelete(SqlConnection connection, T item)
- void OnAfterDelete(SqlConnection connection, T item)
- void OnTruncate(SqlConnection connection)
- void OnBulkInsert(SqlConnection connection, List<T> list)
- void OnMerge(SqlConnection connection, List<T> list, List<Filter> filters)

The Reform implementation is also customizable.  You can register your own types to replace portions of Reform functionality.  Just use the Reformer.RegisterType method to register your own custom implementation of any of the Reform interfaces.

Sample usage

    using System;
    using System.Collections.Generic;
    using Reform.Attributes;
    using Reform.Enum;
    using Reform.Interfaces;
    using Reform.Logic;
    using Reform.Objects;

    namespace ReformSample
    {
        class Program
        {
            static void Main(string[] args)
            {
                Reformer.RegisterType(typeof(IConnectionStringProvider), typeof(ConnectionStringProvider));

                IReform<SysObjects> logic = Reformer.Reform<SysObjects>();

                IEnumerable<SysObjects> list = logic.Select(new Filter(nameof(SysObjects.Name), Operator.Like, "SYS%"));

                foreach (var item in list)
                    Console.WriteLine(item.Name);

                Console.ReadKey();
            }
        }

        [EntityMetadata(DatabaseName="master", TableName="sysobjects")]
        class SysObjects
        {
            [PropertyMetadata(ColumnName = "id")]
            public int Id { get; set; }

            [PropertyMetadata(ColumnName = "name")]
            public string Name { get; set; }
        }

        class ConnectionStringProvider : IConnectionStringProvider
        {
            public string GetConnectionString(string databaseName)
            {
                return $@"Server=.;Database={databaseName};Trusted_Connection=True;";
            }
        }
    }

 
