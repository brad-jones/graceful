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

namespace Graceful.Utils
{
    using System;
    using System.Text;
    using System.Linq;
    using System.Data;
    using Graceful.Query;
    using System.Reflection;
    using Graceful.Extensions;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class Migrator
    {
        /**
         * When Migrations are run there is the chance that columns already in
         * a table may no longer be needed if the corresponding class property
         * has been removed.
         *
         * By default we will leave these column intact.
         *
         * However in a staging environment you may like to allow these columns
         * to be deleted while the application is under heavy development.
         *
         * ```cs
         * 	Graceful.Utils.Migrator.DataLossAllowed = true;
         * ```
         */
        public static bool DataLossAllowed = false;

        /**
         * A Context for us to use.
         */
        protected readonly Context Ctx;

        /**
         * Simple type to represent a foreign key column that needs to be added.
         */
        protected struct ForeignKey
        {
            public string TableName;
            public string ColumnName;
        }

        /**
         * A list of deferred foreign key columns to add
         * after all tables have been CREATED / UPDATED.
         */
        protected virtual List<ForeignKey> ForeignKeys
        {
            get
            {
                if (this._ForeignKeys == null)
                {
                    this._ForeignKeys = new List<ForeignKey>();
                }

                return this._ForeignKeys;
            }
        }

        protected List<ForeignKey> _ForeignKeys;

        /**
         * Simple type to represent a foreign key contraint.
         */
        protected struct FkConstraint
        {
            public string LocalTableName;
            public string LocalColumnName;
            public string ForeignTableName;
            public string ForeignColumnName;
        }

        /**
         * A list of foreign key contraints to add
         * after all tables have been created.
         */
        protected virtual List<FkConstraint> FkConstraints
        {
            get
            {
                if (this._FkConstraints == null)
                {
                    this._FkConstraints = new List<FkConstraint>();
                }

                return this._FkConstraints;
            }
        }

        protected List<FkConstraint> _FkConstraints;

        /**
         * Simple type to represent a unique column contraint.
         */
        protected struct UniqueConstraint
        {
            public string Table;
            public string Column;
            public bool Strict;
        }

        /**
         * A list of unique column contraints to add
         * after all tables have been created.
         */
        protected virtual List<UniqueConstraint> UniqueConstraints
        {
            get
            {
                if (this._UniqueConstraints == null)
                {
                    this._UniqueConstraints = new List<UniqueConstraint>();
                }

                return this._UniqueConstraints;
            }
        }

        protected List<UniqueConstraint> _UniqueConstraints;

        /**
         * Relationships have 2 sides, but they only have 1 backing schema.
         *
         * Consider if we create a pivot table for the relationship between
         * Foo & Bar, when we are creatings Foo's table. But then when we get
         * around to creating Bar's table, we don't need to worry about the
         * pivot table.
         *
         * Anytime we go to perform some action based on a relationship,
         * we first check to see if we have already dealt with said
         * relationship by seeing if this list contains the "foreign" property
         * of the relationship.
         */
        protected virtual List<PropertyInfo> DealtWithRelationships
        {
            get
            {
                if (this._DealtWithRelationships == null)
                {
                    this._DealtWithRelationships = new List<PropertyInfo>();
                }

                return this._DealtWithRelationships;
            }
        }

        protected List<PropertyInfo> _DealtWithRelationships;

        /**
         * A valid Graceful Context must be injected here.
         */
        public Migrator(Context Ctx)
        {
            this.Ctx = Ctx;

            // These will get re-created/added once the schema has been updated.
            this.DropForeignKeyContraints();
            this.DropAllIndexes();

            // Loop through all models in the context.
            this.Ctx.Models.ForEach(model =>
            {
                // This is the migration query we will execute for the model.
                var query = new StringBuilder();

                // Grab the table name of the model.
                var tableName = Model.Dynamic(model).SqlTableName;

                // Grab the models mapped properties.
                var mappedProps = Model.Dynamic(model).MappedProps;

                // Are we CREATING or UPDATING?
                if (this.Ctx.Qb.TableExists(tableName))
                {
                    this.BuildAlterTableQuery(query, tableName, mappedProps);
                }
                else
                {
                    this.BuildCreateTableQuery(query, tableName, mappedProps);
                }

                // Execute the query.
                this.Ctx.Qb.Execute(query.ToString());
            });

            // Re-add our indexes and contraints
            this.CreateAllDeferredForeignKeys();
            this.CreateUniqueIndexes();
        }

        /**
         * Drops all "GRACEFUL_" created indexes.
         *
         * Any indexes created manually or by Sql Server
         * it's self will not be touched.
         */
        protected virtual void DropAllIndexes()
        {
            var DropIndexes = new StringBuilder();
            this.Ctx.Qb
            .SELECT("Idx.name AS INDEX_NAME, Obj.name AS TABLE_NAME")
            .FROM("{0} AS Idx", new SqlTable(this.Ctx, "sys.indexes"))
            .JOIN("{0} AS Obj ON Idx.object_id = Obj.object_id", new SqlTable(this.Ctx, "sys.all_objects"))
            .WHERE("Idx.name IS NOT NULL AND Idx.name LIKE 'GRACEFUL_%'")
            .Rows.ForEach(result =>
            {
                DropIndexes.Append("DROP INDEX ");
                DropIndexes.Append(new SqlId(result["INDEX_NAME"] as string).Value);
                DropIndexes.Append(" ON ");
                DropIndexes.Append(new SqlTable(this.Ctx, result["TABLE_NAME"] as string).Value);
                DropIndexes.Append(";\n");
            });
            if (DropIndexes.Length > 0)
            {
                this.Ctx.Qb.Execute(DropIndexes.ToString());
            }
        }

        /**
         * Drops all Foreign Key Contraints.
         *
         * Before migrating we drop all existing foreign key contraints.
         * This is so that we may remove old columns, if DataLossAllowed
         * is set to true. And so we can ensure only relvent contraints
         * exist after the migration has run.
         */
        protected virtual void DropForeignKeyContraints()
        {
            var DropContraints = new StringBuilder();
            this.Ctx.Qb.SELECT("*").FROM("INFORMATION_SCHEMA.TABLE_CONSTRAINTS")
            .WHERE("{0} = {1}", new SqlId("CONSTRAINT_TYPE"), "FOREIGN KEY")
            .Rows.ForEach(fkC =>
            {
                DropContraints.Append("ALTER TABLE ");
                DropContraints.Append(new SqlId(this.Ctx.DatabaseName + ".dbo." + fkC["TABLE_NAME"] as string).Value);
                DropContraints.Append(" DROP CONSTRAINT ");
                DropContraints.Append(new SqlId(fkC["CONSTRAINT_NAME"] as string).Value);
                DropContraints.Append(";\n");
            });
            if (DropContraints.Length > 0)
            {
                this.Ctx.Qb.Execute(DropContraints.ToString());
            }
        }

        /**
         * All deferred foreign key columns and constraints must be added after
         * all the tables have been created, otherwise we may attempt to add a
         * constraint for a table that does not yet exist.
         */
        protected virtual void CreateAllDeferredForeignKeys()
        {
            // Add any of the deferred foreign key columns.
            var AddFks = new StringBuilder();
            this.ForeignKeys.Distinct().ToList().ForEach(fk =>
            {
                AddFks.Append("ALTER TABLE ");
                AddFks.Append(new SqlId(this.Ctx.DatabaseName + ".dbo." + fk.TableName).Value);
                if (this.Ctx.Qb.ColumnExists(fk.TableName, fk.ColumnName))
                {
                    AddFks.Append(" ALTER COLUMN ");
                }
                else
                {
                    AddFks.Append(" ADD ");
                }
                AddFks.Append(new SqlId(fk.ColumnName).Value);
                AddFks.Append(" INT NULL;\n");
            });
            if (AddFks.Length > 0)
            {
                this.Ctx.Qb.Execute(AddFks.ToString());
            }

            // Now re add all contraints
            var AddContraints = new StringBuilder();
            this.FkConstraints.Distinct().ToList().ForEach(fk =>
            {
                AddContraints.Append("ALTER TABLE ");
                AddContraints.Append(new SqlId(this.Ctx.DatabaseName + ".dbo." + fk.LocalTableName).Value);
                AddContraints.Append(" ADD CONSTRAINT ");
                AddContraints.Append(new SqlId
                (
                    "Fk_" +
                    fk.LocalTableName +
                    "_" +
                    fk.LocalColumnName +
                    "_" +
                    fk.ForeignTableName +
                    "_" +
                    fk.ForeignColumnName
                ).Value);
                AddContraints.Append(" FOREIGN KEY (");
                AddContraints.Append(new SqlId(fk.LocalColumnName).Value);
                AddContraints.Append(") REFERENCES ");
                AddContraints.Append(new SqlId(fk.ForeignTableName).Value);
                AddContraints.Append("(");
                AddContraints.Append(new SqlId(fk.ForeignColumnName).Value);
                AddContraints.Append(");\n");
            });
            if (AddContraints.Length > 0)
            {
                this.Ctx.Qb.Execute(AddContraints.ToString());
            }
        }

        /**
         * Creates Unique Indexes for properties that have the Unique Attribute.
         *
         * Because SQL Server supports an odd "non-standard" UNIQUE contraint
         * where multiple NULL values are not allowed we create our own
         * "Filtered" indexes.
         *
         * see: http://dba.stackexchange.com/questions/80514
         *
         * > NOTE: A strict unique3 index may still be created by supplying
         * > strict=true to the Unique Attribute.
         */
        protected virtual void CreateUniqueIndexes()
        {
            var UniqueIndexes = new StringBuilder();

            this.UniqueConstraints.Distinct().ToList().ForEach(uc =>
            {
                UniqueIndexes.Append("CREATE UNIQUE INDEX ");
                UniqueIndexes.Append(new SqlId("GRACEFUL_UNIQUE_INDEX_" + uc.Table + "_" + uc.Column).Value);
                UniqueIndexes.Append(" ON ");
                UniqueIndexes.Append(new SqlTable(this.Ctx, uc.Table).Value);
                UniqueIndexes.Append("(");
                UniqueIndexes.Append(new SqlId(uc.Column).Value);
                UniqueIndexes.Append(")");

                if (uc.Strict == false)
                {
                    UniqueIndexes.Append(" WHERE ");
                    UniqueIndexes.Append(new SqlId(uc.Column).Value);
                    UniqueIndexes.Append(" IS NOT NULL");
                }

                UniqueIndexes.Append(";\n");
            });

            if (UniqueIndexes.Length > 0)
            {
                this.Ctx.Qb.Execute(UniqueIndexes.ToString());
            }
        }

        /**
         * Builds the SQL Query that will be used to CREATE
         * a new table that matches the Model Class.
         */
        protected void BuildCreateTableQuery(StringBuilder query, string table, List<PropertyInfo> props)
        {
            // Open the CREATE TABLE statement
            query.Append("CREATE TABLE ");
            query.Append(new SqlId(this.Ctx.DatabaseName + ".dbo." + table).Value);
            query.Append("\n(\n");

            // Loop through each of the properties
            props.ForEach(prop =>
            {
                // Check to see if the type is a built in clr type or not.
                if (TypeMapper.IsClrType(prop.PropertyType))
                {
                    // Append the name of the column
                    query.Append("\t");
                    query.Append(new SqlId(prop.Name).Value);
                    query.Append(" ");

                    // Append the column type
                    query.Append(this.GetColumnType(prop));

                    // Append the column length
                    query.Append(this.GetColumnLength(prop));

                    // Is the column the primary key?
                    if (prop.GetCustomAttribute(typeof(KeyAttribute)) != null)
                    {
                        query.Append(" IDENTITY(1,1) PRIMARY KEY");
                    }
                    else
                    {
                        // Set the nullability of the column
                        if (this.IsNullableProperty(prop))
                        {
                            query.Append(" NULL");
                        }
                        else
                        {
                            query.Append(" NOT NULL");
                        }

                        // Is the column unique?
                        if (this.IsUnique(prop))
                        {
                            this.UniqueConstraints.Add(new UniqueConstraint
                            {
                                Table = table,
                                Column = prop.Name,
                                Strict = prop.GetCustomAttribute<UniqueAttribute>(false).Strict
                            });
                        }
                    }

                    // Next column
                    query.Append(",\n");
                }
                else
                {
                    // The type is a Complex Type, ie: a relationship.
                    // Lets get the pre discovered relationship details.
                    var relation = this.Ctx.Relationships.Discovered.Single
                    (
                        r => r.LocalProperty == prop
                    );

                    // Have we already dealt with the other side of this relationship?
                    if (this.DealtWithRelationships.Any(p => p == relation.ForeignProperty))
                    {
                        // We have so there is nothing for us to do here.
                        // Continue on to the next property.
                        return;
                    }
                    else
                    {
                        // We have not dealt with this relationship.
                        // So lets make it known that we have now.
                        this.DealtWithRelationships.Add(prop);
                    }

                    // Take action based on the relationship type.
                    switch (relation.Type)
                    {
                        case RelationshipDiscoverer.Relation.RelationType.MtoM:

                            // Create the pivot table, we don't need to add
                            // anything at all to this tables definition.
                            this.CreatePivotTable(relation);

                        break;

                        case RelationshipDiscoverer.Relation.RelationType.MtoO:

                            // Create a new foreign key column in the foreign table.
                            this.AddFKToFT(relation);

                        break;

                        case RelationshipDiscoverer.Relation.RelationType.OtoM:

                            // Create a new foreign key column in this table.
                            this.AddFKToLTOm(query, relation);

                        break;

                        case RelationshipDiscoverer.Relation.RelationType.OtoO:

                            // Create a new foreign key column in this table.
                            this.AddFKToLTOo(query, relation);

                        break;
                    }
                }
            });

            // Remove the last comma
            if (query[query.Length - 2] == ',')
            {
                query.Remove(query.Length - 2, 2);
            }

            // Close the statement
            query.Append("\n);");
        }

        /**
         * Builds the SQL Query that will be used to ALTER
         * an existing table to match the Model Class.
         */
        protected void BuildAlterTableQuery(StringBuilder query, string table, List<PropertyInfo> props)
        {
            // Loop through each of the properties.
            // Regardless of if the column is of the correct data type or not
            // we will create an ALTER statement for it. That way we can be
            // sure the column is the correct type.
            props.ForEach(prop =>
            {
                // Skip the Primary Key, this should never change.
                if (prop.Name == "Id") return;

                // Check to see if the type is a built in clr type or not.
                if (TypeMapper.IsClrType(prop.PropertyType))
                {
                    // Open the ALTER TABLE statement
                    query.Append("ALTER TABLE ");
                    query.Append(new SqlId(this.Ctx.DatabaseName + ".dbo." + table).Value);

                    // Does the column already exist?
                    var colExists = this.Ctx.Qb.ColumnExists(table, prop.Name);
                    if (colExists)
                    {
                        query.Append(" ALTER COLUMN ");
                    }
                    else
                    {
                        query.Append(" ADD ");
                    }

                    // Append the name of the column
                    query.Append(new SqlId(prop.Name).Value);
                    query.Append(" ");

                    // Append the column type
                    query.Append(this.GetColumnType(prop));

                    // Append the column length
                    query.Append(this.GetColumnLength(prop));

                    // Set the nullability of the column
                    if (this.IsNullableProperty(prop))
                    {
                        query.Append(" NULL");
                    }
                    else
                    {
                        query.Append(" NOT NULL");

                        // If we are "ADDING" a new column and the table is not
                        // empty then we need to set a default value that existing
                        // rows will get set to. An empty string seems to work.
                        if (!colExists && !this.Ctx.Qb.TableEmpty(table))
                        {
                            query.Append(" DEFAULT ''");
                        }
                    }

                    // Is the column unique?
                    if (this.IsUnique(prop))
                    {
                        this.UniqueConstraints.Add(new UniqueConstraint
                        {
                            Table = table,
                            Column = prop.Name,
                            Strict = prop.GetCustomAttribute<UniqueAttribute>(false).Strict
                        });
                    }

                    // Next column
                    query.Append(";\n");
                }
                else
                {
                    // The type is a Complex Type, ie: a relationship.
                    // Lets get the pre discovered relationship details.
                    var relation = this.Ctx.Relationships.Discovered.Single
                    (
                        r => r.LocalProperty == prop
                    );

                    // Have we already dealt with the other side of this relationship?
                    if (this.DealtWithRelationships.Any(p => p == relation.ForeignProperty))
                    {
                        // We have so there is nothing for us to do here.
                        // Continue on to the next property.
                        return;
                    }
                    else
                    {
                        // We have not dealt with this relationship.
                        // So lets make it known that we have now.
                        this.DealtWithRelationships.Add(prop);
                    }

                    // Take action based on the relationship type.
                    switch (relation.Type)
                    {
                        case RelationshipDiscoverer.Relation.RelationType.MtoM:

                            // Create the pivot table, we don't need to add
                            // anything at all to this tables definition.
                            this.UpdatePivotTable(query, relation);

                        break;

                        case RelationshipDiscoverer.Relation.RelationType.MtoO:

                            // Create a new foreign key column
                            // in the foreign table.
                            this.UpdateFKToFT(relation);

                        break;

                        case RelationshipDiscoverer.Relation.RelationType.OtoM:

                            // Create a new foreign key
                            // for the 1:Many relationship.
                            this.UpdateFKToLTOm(query, relation);

                        break;

                        case RelationshipDiscoverer.Relation.RelationType.OtoO:

                            // Create a new foreign key for the 1:1 relationship.
                            this.UpdateFKToLTOo(query, relation);

                        break;
                    }
                }
            });

            // If we are allowed to sustain some data losses,
            // we will remove any columns that are no longer needed.
            if (DataLossAllowed)
            {
                using (var reader = this.Ctx.Qb
                .SELECT("{0}", new SqlId("COLUMN_NAME"))
                .FROM("INFORMATION_SCHEMA.COLUMNS")
                .WHERE("{0} = {1}", new SqlId("TABLE_NAME"), table)
                .Reader)
                {
                    while (reader.Read())
                    {
                        var existingCol = reader.GetString(0);

                        if (!props.Exists(p => p.Name == existingCol))
                        {
                            // Guard against deleting Foregin Key Cols
                            if (!this.Ctx.Relationships.Discovered.Any(r => r.ForeignKeyTableName == table && r.ForeignKeyColumnName == existingCol))
                            {
                                query.Append("ALTER TABLE ");
                                query.Append(new SqlId(this.Ctx.DatabaseName + ".dbo." + table).Value);
                                query.Append(" DROP COLUMN ");
                                query.Append(new SqlId(existingCol).Value);
                                query.Append(";\n");
                            }
                        }
                    }
                }
            }
        }

        /**
         * Given a property we will return the name of the SqlDbType.
         */
        protected string GetColumnType(PropertyInfo prop)
        {
            var sqlType = prop.GetCustomAttribute<SqlTypeAttribute>();

            if (sqlType != null) return sqlType.Value.ToString().ToUpper();

            return TypeMapper.GetDBType(prop.PropertyType).ToString().ToUpper();
        }

        /**
         * Given a property we will return the length value that
         * will be appended directly after the SqlDbType name.
         *
         * ie: ```nvarchar(MAX)```
         */
        protected string GetColumnLength(PropertyInfo prop)
        {
            // Check for a custom length attribute
            var sqlLength = prop.GetCustomAttribute<SqlLengthAttribute>();
            if (sqlLength != null) return sqlLength.Value;

            // Grab the SqlDbType
            SqlDbType sqlType;
            if (prop.GetCustomAttribute<SqlTypeAttribute>() != null)
            {
                sqlType = prop.GetCustomAttribute<SqlTypeAttribute>().Value;
            }
            else
            {
                sqlType = TypeMapper.GetDBType(prop.PropertyType);
            }

            // Types that can have the max length
            // option lets give it to them.
            switch (sqlType)
            {
                case SqlDbType.VarChar:
                case SqlDbType.NVarChar:
                case SqlDbType.VarBinary:
                    return "(MAX)";

                default:
                    return "";
            }
        }

        /**
         * Given a property we will work out if the property
         * is allowed to have a null value or not.
         */
        protected bool IsNullableProperty(PropertyInfo prop)
        {
            var type = prop.PropertyType;

            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    return true;
                }
            }

            if (type == typeof(string) || type == typeof(object))
            {
                return true;
            }

            return false;
        }

        /**
         * Given a property work out if the property is set to be unique or not.
         */
        protected bool IsUnique(PropertyInfo prop)
        {
            if (prop.GetCustomAttribute<UniqueAttribute>(false) != null)
            {
                return true;
            }

            return false;
        }

        /**
         * When a Many to Many relationship is found, this will first check to
         * see if the Pivot Table already exists and if not we then create the
         * needed Pivot Table to support the relationship.
         */
        protected void CreatePivotTable(RelationshipDiscoverer.Relation relation)
        {
            // It is possible the table has already been created.
            if (this.Ctx.Qb.TableExists(relation.PivotTableName)) return;

            // Okay lets create it.
            this.Ctx.Qb.Execute
            (
                "CREATE TABLE @TableName\n" +
                "(\n" +
                    "\t@Col1 INT NOT NULL,\n" +
                    "\t@Col2 INT NOT NULL,\n" +
                    "\tCONSTRAINT @Contraint\n" +
                    "\tPRIMARY KEY (@Col1,@Col2)\n" +
                ");",
                new Dictionary<string, object>
                {
                    {"@TableName", new SqlId(this.Ctx.DatabaseName + ".dbo." + relation.PivotTableName)},
                    {"@Col1", new SqlId(relation.PivotTableFirstColumnName)},
                    {"@Col2", new SqlId(relation.PivotTableSecondColumnName)},
                    {"@Contraint", new SqlId("Pk_"+relation.PivotTableName+"_Id")}
                }
            );

            // Make sure the foreign key contraints get created.
            FkConstraints.Add(new FkConstraint
            {
                LocalTableName = relation.PivotTableName,
                LocalColumnName = relation.PivotTableFirstColumnName,
                ForeignTableName = relation.LocalTableName,
                ForeignColumnName = "Id"
            });

            FkConstraints.Add(new FkConstraint
            {
                LocalTableName = relation.PivotTableName,
                LocalColumnName = relation.PivotTableSecondColumnName,
                ForeignTableName = relation.ForeignTableName,
                ForeignColumnName = "Id"
            });
        }

        /**
         * Runs when a Many:Many relationship is found and it's possible the
         * pivot table may already exist. ie: Running migrations on an existing
         * database.
         */
        protected void UpdatePivotTable(StringBuilder query, RelationshipDiscoverer.Relation relation)
        {
            // Make sure the table exists, if not we create it.
            if (!this.Ctx.Qb.TableExists(relation.PivotTableName))
            {
                this.CreatePivotTable(relation); return;
            }

            // Alter or Add Col1
            query.Append("ALTER TABLE ");
            query.Append(new SqlId(this.Ctx.DatabaseName + ".dbo." + relation.PivotTableName).Value);
            var col1Exists = this.Ctx.Qb.ColumnExists(relation.PivotTableName, relation.PivotTableFirstColumnName);
            if (col1Exists)
            {
                query.Append(" ALTER COLUMN ");
            }
            else
            {
                query.Append(" ADD ");
            }
            query.Append(new SqlId(relation.PivotTableFirstColumnName).Value);
            query.Append(" INT NOT NULL");
            if (!col1Exists && !this.Ctx.Qb.TableEmpty(relation.PivotTableName))
            {
                query.Append(" DEFAULT ''");
            }
            query.Append(";\n");

            // Alter or Add Col2
            query.Append("ALTER TABLE ");
            query.Append(new SqlId(this.Ctx.DatabaseName + ".dbo." + relation.PivotTableName).Value);
            var col2Exists = this.Ctx.Qb.ColumnExists(relation.PivotTableName, relation.PivotTableSecondColumnName);
            if (col2Exists)
            {
                query.Append(" ALTER COLUMN ");
            }
            else
            {
                query.Append(" ADD ");
            }
            query.Append(new SqlId(relation.PivotTableSecondColumnName).Value);
            query.Append(" INT NOT NULL");
            if (!col2Exists && !this.Ctx.Qb.TableEmpty(relation.PivotTableName))
            {
                query.Append(" DEFAULT ''");
            }
            query.Append(";\n");

            // Drop the existing composite primary key.
            query.Append("ALTER TABLE ");
            query.Append(new SqlId(this.Ctx.DatabaseName + ".dbo." + relation.PivotTableName).Value);
            query.Append(" DROP CONSTRAINT ");
            query.Append(new SqlId("Pk_" + relation.PivotTableName + "_Id").Value);
            query.Append(";\n");

            // If data loss is allowed, drop any other columns.
            if (DataLossAllowed)
            {
                using (var reader = this.Ctx.Qb
                .SELECT("{0}", new SqlId("COLUMN_NAME"))
                .FROM("INFORMATION_SCHEMA.COLUMNS")
                .WHERE("{0} = {1}", new SqlId("TABLE_NAME"), relation.PivotTableName)
                .Reader
                ){
                    while (reader.Read())
                    {
                        var existingCol = reader.GetString(0);

                        if (existingCol != relation.PivotTableFirstColumnName && existingCol != relation.PivotTableSecondColumnName)
                        {
                            query.Append("ALTER TABLE ");
                            query.Append(new SqlId(this.Ctx.DatabaseName + ".dbo." + relation.PivotTableName).Value);
                            query.Append(" DROP COLUMN ");
                            query.Append(new SqlId(existingCol).Value);
                            query.Append(";\n");
                        }
                    }
                }
            }

            // Recreate the composite primary key.
            query.Append("ALTER TABLE ");
            query.Append(new SqlId(this.Ctx.DatabaseName + ".dbo." + relation.PivotTableName).Value);
            query.Append(" ADD CONSTRAINT ");
            query.Append(new SqlId("Pk_" + relation.PivotTableName + "_Id").Value);
            query.Append(" PRIMARY KEY (");
            query.Append(new SqlId(relation.PivotTableFirstColumnName).Value);
            query.Append(",");
            query.Append(new SqlId(relation.PivotTableSecondColumnName).Value);
            query.Append(");\n");
        }

        /**
         * Adds a new foreign key to the foreign table, that supports a Many:1.
         *
         * > NOTE: This will be deferred until it comes time
         * > to actually create the foreign table.
         */
        protected void AddFKToFT(RelationshipDiscoverer.Relation relation)
        {
            // Create a new ForeignKey, the creation is defered.
            ForeignKeys.Add(new ForeignKey
            {
                TableName = relation.ForeignKeyTableName,
                ColumnName = relation.ForeignKeyColumnName
            });

            // Ensure the foreign key contraint gets added also.
            FkConstraints.Add(new FkConstraint
            {
                LocalTableName = relation.ForeignKeyTableName,
                LocalColumnName = relation.ForeignKeyColumnName,
                ForeignTableName = relation.LocalTableName,
                ForeignColumnName = "Id"
            });
        }

        /**
         * Because the creation of the foreign key is defered,
         * the implemenation at this point is exactly the same.
         */
        protected void UpdateFKToFT(RelationshipDiscoverer.Relation relation)
        {
            this.AddFKToFT(relation);
        }

        /**
         * Adds a new foreign key to the local table, that supports a One:Many.
         */
        protected void AddFKToLTOm(StringBuilder query, RelationshipDiscoverer.Relation relation)
        {
            // Add the column to current table we are about to create.
            query.Append("\t");
            query.Append(new SqlId(relation.ForeignKeyColumnName).Value);
            query.Append(" INT NULL,\n");

            // Ensure the foreign key contraint gets added.
            FkConstraints.Add(new FkConstraint
            {
                LocalTableName = relation.ForeignKeyTableName,
                LocalColumnName = relation.ForeignKeyColumnName,
                ForeignTableName = relation.ForeignTableName,
                ForeignColumnName = "Id"
            });
        }

        protected void UpdateFKToLTOm(StringBuilder query, RelationshipDiscoverer.Relation relation)
        {
            // Add the column to current table we are about to create.
            query.Append("ALTER TABLE ");
            query.Append(new SqlId(this.Ctx.DatabaseName + ".dbo." + relation.ForeignKeyTableName).Value);

            var colExists = this.Ctx.Qb.ColumnExists(relation.ForeignKeyTableName, relation.ForeignKeyColumnName);
            if (colExists)
            {
                query.Append(" ALTER COLUMN ");
            }
            else
            {
                query.Append(" ADD ");
            }

            query.Append(new SqlId(relation.ForeignKeyColumnName).Value);
            query.Append(" INT NULL");

            query.Append(";\n");

            // Ensure the foreign key contraint gets added.
            FkConstraints.Add(new FkConstraint
            {
                LocalTableName = relation.ForeignKeyTableName,
                LocalColumnName = relation.ForeignKeyColumnName,
                ForeignTableName = relation.ForeignTableName,
                ForeignColumnName = "Id"
            });
        }

        /**
         * Adds a new foreign key to the local table, that supports a 1:1.
         */
        protected void AddFKToLTOo(StringBuilder query, RelationshipDiscoverer.Relation relation)
        {
            // Add the column to current table we are about to create.
            query.Append("\t");
            query.Append(new SqlId(relation.ForeignKeyColumnName).Value);
            query.Append(" INT NULL,\n");

            // Ensure the foreign key contraint gets added.
            FkConstraints.Add(new FkConstraint
            {
                LocalTableName = relation.ForeignKeyTableName,
                LocalColumnName = relation.ForeignKeyColumnName,
                ForeignTableName = relation.ForeignTableName,
                ForeignColumnName = "Id"
            });
        }

        protected void UpdateFKToLTOo(StringBuilder query, RelationshipDiscoverer.Relation relation)
        {
            // Add the column to current table we are about to create.
            query.Append("ALTER TABLE ");
            query.Append(new SqlId(this.Ctx.DatabaseName + ".dbo." + relation.ForeignKeyTableName).Value);

            var colExists = this.Ctx.Qb.ColumnExists(relation.ForeignKeyTableName, relation.ForeignKeyColumnName);
            if (colExists)
            {
                query.Append(" ALTER COLUMN ");
            }
            else
            {
                query.Append(" ADD ");
            }

            query.Append(new SqlId(relation.ForeignKeyColumnName).Value);
            query.Append(" INT NULL");

            query.Append(";\n");

            // Ensure the foreign key contraint gets added.
            FkConstraints.Add(new FkConstraint
            {
                LocalTableName = relation.ForeignKeyTableName,
                LocalColumnName = relation.ForeignKeyColumnName,
                ForeignTableName = relation.ForeignTableName,
                ForeignColumnName = "Id"
            });
        }
    }
}
