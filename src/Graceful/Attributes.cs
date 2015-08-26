namespace Graceful
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class ConnectionAttribute : Attribute
    {
        public readonly string Value;

        public ConnectionAttribute(string value)
        {
            this.Value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SqlTableNameAttribute : Attribute
    {
        public readonly string Value;

        public SqlTableNameAttribute(string value)
        {
            this.Value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SqlLengthAttribute : Attribute
    {
        public readonly string Value;

        public SqlLengthAttribute(string value)
        {
            this.Value = value;
        }
    }
}
