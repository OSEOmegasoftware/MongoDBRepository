using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBRepository
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class MongoCollectionAttribute : Attribute
    {
        public virtual string Name { get; private set; }

        public MongoCollectionAttribute(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Empty collectionname not allowed", "value");
            this.Name = value;
        }
    }
}
