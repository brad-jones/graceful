namespace Graceful.Tests.Models
{
    [SqlTableNameAttribute("i_am_special")]
    public class CustomTableName : Model<CustomTableName>
    {
        [SqlLengthAttribute("(255)")]
        public string CustomLengthString { get; set; }

        public static bool InvokeableMethod()
        {
            return true;
        }
    }
}
