////////////////////////////////////////////////////////////////////////////////
//           ________                                _____        __
//          /  _____/_______ _____     ____   ____ _/ ____\__ __ |  |
//         /   \  ___\_  __ \\__  \  _/ ___\_/ __ \\   __\|  |  \|  |
//         \    \_\  \|  | \/ / __ \_\  \___\  ___/ |  |  |  |  /|  |__
//          \______  /|__|   (____  / \___  >\___  >|__|  |____/ |____/
//                 \/             \/      \/     \/
// =============================================================================
//           Designed & Developed by Brad Jones <brad @="bjc.id.au" />
// =============================================================================
////////////////////////////////////////////////////////////////////////////////

namespace Graceful
{
    using System;
    using System.IO;
    using System.Linq;
    using Graceful.Utils;
    using Newtonsoft.Json;
    using System.Reflection;
    using Graceful.Extensions;
    using System.Data.SqlClient;
    using System.Collections.Generic;

    public class Context
    {
        /**
         * Helps to ensure we don't start using
         * the global context before it's ready.
         */
        protected static readonly object ThreadLocker = new object();

        /**
         * The global instance of the context will be stored here.
         */
        private static Context _GlobalCtx;

        /**
         * Thread Safe version _GlobalCtx.
         */
        public static Context GlobalCtx
        {
            get
            {
                lock (ThreadLocker)
                {
                    return _GlobalCtx;
                }
            }
        }

        /**
         * For most applications with a single database server you
         * can simply call this method early on in your app bootup.
         */
        public static void Connect(string cs, bool migrate = false, bool log = false)
        {
            lock (ThreadLocker)
            {
                _GlobalCtx = new Context
                (
                    cs,
                    migrate: migrate,
                    log: log
                );
            }
        }

        /**
         * The validated Connection String.
         */
        protected string _ConnectionString;

        /**
         * The connection string that this context will use for SqlConnections.
         * You must pass this to either the static Connect method or the
         * Constructor. Once this value is set, it should not be changed.
         */
        public string ConnectionString
        {
            get
            {
                return this._ConnectionString;
            }

            protected set
            {
                try
                {
                    using (var con = new SqlConnection(value))
                    {
                        // This will throw if the connection string in invalid.
                        con.Open();
                    }
                }
                catch
                {
                    // For a second lets assume the reason we could not open
                    // the connection is because the database does not exist.
                    // Lets attempt to create it.
                    if (!this.AutoCreateDatabase(value))
                    {
                        // Only rethrow if we failed to create the database.
                        throw;
                    }
                }

                this._ConnectionString = value;
            }
        }

        /**
         * Provides a new Opened SqlConnection.
         *
         * ```
         * 	var ctx = new Context("ConnectionString");
         * 	using (var con = ctx.Connection)
         * 	{
         * 		var cmd = new SqlCommand("Query", con);
         * 	}
         * ```
         */
        public SqlConnection Connection
        {
            get
            {
                var con = new SqlConnection(this.ConnectionString);

                try
                {
                    con.Open();
                }
                catch
                {
                    // Ensure we dispose of the connection before rethrowing.
                    con.Dispose(); throw;
                }

                return con;
            }
        }

        /**
         * Cache the DatabaseName lookup.
         */
        protected string _DatabaseName;

        /**
         * Shortcut to the current Database Name.
         */
        public string DatabaseName
        {
            get
            {
                if (this._DatabaseName == null)
                {
                    this._DatabaseName = new SqlConnectionStringBuilder
                    (
                        this.ConnectionString
                    ).InitialCatalog;
                }

                return this._DatabaseName;
            }
        }

        /**
         * Returns a new Query Builder setup to use this context.
         */
        public Query.Builder Qb
        {
            get
            {
                return new Query.Builder(this);
            }
        }

        /**
         * Cache the Models Property.
         */
        protected HashSet<Type> _Models;

        /**
         * Returns a list of all defined models in the current context.
         * ie: Models that have the same Connection String as this Context.
         *
         * > NOTE: Models that have no explicity set ConnectionAttribute
         * > will be included in this list if the Connection String of this
         * > Context does not match any of the ConnectionAttribute's.
         */
        public HashSet<Type> Models
        {
            get
            {
                if (this._Models == null)
                {
                    var globalModels = new HashSet<Type>();
                    var customModels = new HashSet<Type>();

                    Model.GetAllModels().ForEach(model =>
                    {
                        var ctx = model.GetCustomAttribute<ConnectionAttribute>
                        (
                            false
                        );

                        if (ctx == null)
                        {
                            globalModels.Add(model);
                        }
                        else
                        {
                            if (ctx.Value == this.ConnectionString)
                            {
                                customModels.Add(model);
                            }
                        }
                    });

                    if (customModels.Count > 0)
                    {
                        this._Models = customModels;
                    }
                    else
                    {
                        this._Models = globalModels;
                    }
                }

                return this._Models;
            }
        }

        /**
         * If logging has been enabled for the current context, this will
         * get a new MemoryStream initialised by the constructor.
         */
        protected MemoryStream _LogStream;

        /**
         * If we have a valid MemoryStream in "_LogStream" and if someone has
         * asked for a new "LogWriter", then this will contain the contexts
         * StreamWriter, where all query logs will be written to.
         */
        protected StreamWriter _LogWriter;

        /**
         * If logging has been enabled for the context, this will return a new
         * StreamWriter, ready to be written to. Otherwise null.
         */
        public StreamWriter LogWriter
        {
            get
            {
                if (this._LogStream == null) return null;

                if (this._LogWriter == null)
                {
                    this._LogWriter = new StreamWriter(this._LogStream);
                }

                return this._LogWriter;
            }
        }

        /**
         * If logging has been enabled for the context, and if someone has
         * asked for a "LogWriter" we will read it and return the resulting
         * string. Which will represent all the SQL queries run against the
         * database, since the "LogWriter" was first asked for.
         *
         * > NOTE: Once you ask for the Log, we dispose of the LogWriter,
         * > and then setup a new one, so you may call Log many times,
         * > each time you will see the queries that have been executed in
         * > between asking for the Log.
         */
        public string Log
        {
            get
            {
                if (this._LogWriter == null) return null;

                this._LogWriter.Flush();
                this._LogStream.Position = 0;

                using (var streamReader = new StreamReader(this._LogStream))
                {
                    var log = streamReader.ReadToEnd();
                    this._LogWriter.Dispose();
                    this._LogWriter = null;
                    this._LogStream.Dispose();
                    this._LogStream = new MemoryStream();
                    return log;
                }
            }
        }

        /**
         * This represents the discovered relationships.
         *
         * ```
         * 	var ctx = new Context("cs");
         * 	ctx.Relationships.Discovered.ForEach(relation =>
         * 	{
         * 		// ...
         * 	});
         * ```
         */
        public RelationshipDiscoverer Relationships { get; protected set; }

        /**
         * The Newtonsoft Json Serializer that we will use throughout.
         */
        public JsonSerializer JsonSerializer
        {
            get
            {
                if (this._JsonSerializer == null)
                {
                    this._JsonSerializer = JsonSerializer.CreateDefault
                    (
                        new JsonSerializerSettings
                        {
                            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                            ReferenceLoopHandling = ReferenceLoopHandling.Serialize
                        }
                    );
                }

                return this._JsonSerializer;
            }

            set
            {
                this._JsonSerializer = value;
            }
        }

        protected JsonSerializer _JsonSerializer;

        /**
         * Creates a new Context.
         *
         * In essence a Context is an object that contains a valid
         * Connection String to your database and a list of Models
         * that are configured to connect to that Connection String.
         *
         * _see: the Models property for more info on this._
         *
         * > NOTE: The Context also manages Migrations if you let it.
         *
         * You should only have to create your own Context if you have multiple
         * diffrent databases to connect to. For most use cases please use the
         * static "Connect" method.
         */
        public Context(string cs, bool migrate = false, bool log = false, bool inject = true)
        {
            this.ConnectionString = cs;
            this.Relationships = new RelationshipDiscoverer(this.Models);
            if (log) this._LogStream = new MemoryStream();
            if (inject) this.GiveModelsContext();
            if (migrate) new Migrator(this);
        }

        /**
         * Attempts to automatically create the Database.
         *
         * As we are all about a "Code-First" paradigm, the most probable cause
         * for not being able to connect to the database is because the database
         * does not actually exist yet. So we are going to attempt to create it.
         */
        protected bool AutoCreateDatabase(string cs)
        {
            var masterCs = new SqlConnectionStringBuilder(cs);
            var originalCatalog = masterCs.InitialCatalog;
            masterCs.InitialCatalog = "master";

            try
            {
                using (var con = new SqlConnection(masterCs.ConnectionString))
                {
                    con.Open();

                    // Lets check to see if the db exists?
                    using (var exists = new SqlCommand("SELECT database_id FROM sys.databases WHERE Name = @name", con))
                    {
                        exists.Parameters.AddWithValue("@name", originalCatalog);

                        if (exists.ExecuteScalar() == null)
                        {
                            // Great the database doesn't exist, lets create it.
                            using (var create = new SqlCommand("exec ('CREATE DATABASE ' + @name)", con))
                            {
                                create.Parameters.AddWithValue("@name", originalCatalog);
                                create.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Well thats werid the database would seem to
                            // already exist but we can't connect to it with our
                            // original connection string.
                            return false;
                        }
                    }
                }
            }
            catch
            {
                // We tried our best but we still failed :(
                return false;
            }

            // We must "REALLY" close all existing connections
            // so that the new database can be connected to.
            SqlConnection.ClearAllPools();

            // If we make it to here everything went to plan.
            return true;
        }

        /**
         * Loops through all models in the current context and provides them
         * with this Context so they may connect to the database and perform
         * operations.
         */
        protected void GiveModelsContext()
        {
            this.Models.ForEach(model =>
            {
                model.GetProperty
                (
                    "Db",
                    BindingFlags.FlattenHierarchy |
                    BindingFlags.Public |
                    BindingFlags.Static
                ).SetValue(null, this);
            });
        }
    }
}
