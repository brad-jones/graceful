namespace Graceful.Tests.Models
{
    [ConnectionAttribute(@"Server=localhost\SQLEXPRESS;Database=Graceful_FOO;Trusted_Connection=True;")]
    public class CustomContext : Model<CustomContext>
    {
        public string Foo { get; set; }

        public static void Seed()
        {
            new CustomContext { Foo = "Bar" }.Save();
        }
    }
}
