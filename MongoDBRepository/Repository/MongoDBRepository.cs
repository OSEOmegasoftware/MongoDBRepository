using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDBRepository
{
    public class MongoDBRepository<T> where T : IMongoObject
    {
        private static string CONNECTION_STRING_NAME = "MongoServerSettings";
        private static string dbName = null;

        private static volatile MongoDBRepository<T> instance;
        private static object syncRoot = new object();

        public static string DBName => dbName ?? (dbName = ConfigurationManager.AppSettings["MongoDB.DBName"] ?? "adspace");

        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;
        private IMongoCollection<T> collection;

        private string collectionName = "";

        public static MongoDBRepository<T> Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null) instance = new MongoDBRepository<T>();
                    }
                }
                return instance;
            }
        }

        public MongoDBRepository()
        {
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings[CONNECTION_STRING_NAME].ConnectionString;
                _client = new MongoClient(connectionString);
                _database = _client.GetDatabase(DBName);
                collectionName = GetTypeName();
                collection = _database.GetCollection<T>(collectionName);
                FirstInit();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }

        }

        private T FirstInit()
        {
            try
            {
                return collection.Find(new BsonDocument()).FirstOrDefault();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            return default(T);
        }
        private string GetTypeName()
        {
            return  GetCollectionNameFromType(typeof(T));
             
        }

        public IQueryable<T> GetQueryable()
        {
            try
            {
                return collection.AsQueryable();
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return null;
            }

        }

        public T Add(T value)
        {
            try
            {
                collection.InsertOne(value);
                return value;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return default(T);
            }

        }

        public IEnumerable<T> Add(IEnumerable<T> values)
        {
            try
            {
                collection.InsertMany(values);
                return values;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return null;
            }

        }

        public T Update(T value)
        {
            try
            {
                collection.ReplaceOne(x => x.Id == value.Id, value);
                return value;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return default(T);
            }
        }

        public void Delete(T value)
        {
            try
            {
                collection.DeleteOne(x => x.Id == value.Id);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public void Delete(string id)
        {
            try
            {
                collection.DeleteOne(x => x.Id == id);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public IEnumerable<T> GetAll()
        {
            List<T> result = null;
            try
            {
                result = collection.Find(new BsonDocument()).ToListAsync().Result;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            return result;
        }

        public void CreateIndex(string index, string name)
        {
            CreateIndexOptions indexOptions = null;
            if (name != null)
            {
                indexOptions = new CreateIndexOptions() {Name = name};
            }
            try
            {
                var indexes = collection.Indexes.List().ToList();
                foreach (var ind in indexes)
                {
                    if (ind.IsBsonDocument)
                    {
                        var indName = ind.Elements.Where(bsonElement => bsonElement.Name == "name").Select(bsonElement => bsonElement.Value.ToString()).FirstOrDefault();
                        if (indName == name)
                        {
                            return;
                        }
                    }
                }
                collection.Indexes.CreateOne(index, indexOptions);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public IEnumerable<T> Find(Expression<Func<T, bool>> filter)
        {
            try
            {

                return collection.Find(filter).ToListAsync().Result;
            }
            catch (Exception)
            {
                return null;
            }

        }
        public IEnumerable<T> Find(FilterDefinition<T> filter)
        {
            try
            {
                return collection.Find(filter).ToListAsync().Result;
            }
            catch (Exception)
            {
                return null;
            }

        }

        private static string GetCollectionNameFromType(Type entitytype)
        {
            string name;
            try
            {
                Attribute customAttribute = Attribute.GetCustomAttribute((MemberInfo)entitytype, typeof(MongoCollectionAttribute));
                if (customAttribute != null)
                {
                    name = ((MongoCollectionAttribute)customAttribute).Name;
                }
                else
                {
                    /*if (typeof(IMongoObject).IsAssignableFrom(entitytype))
                    {
                        while (!entitytype.BaseType.Equals(typeof(IMongoObject)))
                            entitytype = entitytype.BaseType;
                    }*/
                    name = entitytype.Name;
                }
                return name;
            }
            catch (Exception ex)
            {
                //HandleException(ex);
                return entitytype.Name;
            }
            
        }

        virtual protected void HandleException(Exception ex)
        {
            throw ex;
        }

    }
}
