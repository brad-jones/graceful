////////////////////////////////////////////////////////////////////////////////
//            ________                                _____        __
//           /  _____/_______ _____     ____   ____ _/ ____\__ __ |  |
//          /   \  ___\_  __ \\__  \  _/ ___\_/ __ \\   __\|  |  \|  |
//          \    \_\  \|  | \/ / __ \_\  \___\  ___/ |  |  |  |  /|  |__
//           \______  /|__|   (____  / \___  >\___  >|__|  |____/ |____/
//                  \/             \/      \/     \/
// =============================================================================
//           Designed & Developed by Brad Jones <brad @="bjc.id.au" />
// =============================================================================
////////////////////////////////////////////////////////////////////////////////

namespace Graceful.Extensions
{
    using System;
    using System.Text;
    using System.Globalization;
    using System.Data.SqlClient;
    using System.Collections.Generic;

    public static class ExtensionMethods
    {
        /**
         * A Linq`ish way of iterating over an Enumerable with an index.
         *
         * ```
         * 	var fooList = new List<string>{ "abc", "xyz" };
         *
         * 	fooList.ForEachWithIndex((key, value) =>
         *  {
         *  	Console.WriteLine(key + ": " + value);
         * 	});
         * ```
         *
         * _Credit: http://stackoverflow.com/questions/43021_
         */
        public static void ForEachWithIndex<T>(this IEnumerable<T> enumerable, Action<int, T> handler)
        {
            int key = 0;
            foreach (T value in enumerable)
            {
                handler(key++, value);
            }
        }

        /**
         * Creates a string representation of the command
         * for logging and debugging purposes.
         *
         * ```
         * 	var cmd = new SqlCommand(query, connection);
         * 	var trace = cmd.ToTraceString();
         * ```
         *
         * _Credit: http://git.io/v3H1T_
         */
        public static string ToTraceString(this SqlCommand command, int affectedRecords = 0)
        {
            if (command == null) throw new ArgumentNullException("command");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.Append(command.CommandText);

            foreach (SqlParameter param in command.Parameters)
            {
                if (param != null)
                {
                    sb.AppendLine().AppendFormat
                    (
                        CultureInfo.InvariantCulture,
                        "-- {0}: {1} {2} (Size = {3}) [{4}]",
                        param.ParameterName,
                        param.Direction,
                        param.SqlDbType,
                        param.Size,
                        param.Value
                    );
                }
            }

            sb.AppendLine();
            sb.AppendLine("-- [" + affectedRecords + "] records affected.");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
