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

namespace Graceful.Utils
{
    using System;
    using Inflector;
    using System.Linq;
    using System.Reflection;
    using Graceful.Extensions;
    using System.Collections.Generic;

    public class RelationshipDiscoverer
    {
        /**
         * Describes a relationship between 2 models.
         * The IsManyToMany, IsManyToOne, IsOneToMany & IsOneToOne
         * methods will return an instance of this struct.
         */
        public struct Relation
        {
            /**
             * We have 4 types of relationships.
             *
             * 	- MtoM: A Many to Many relationship.
             *
             * 	- MtoO: A Many to One relationship.
             *
             * 	- OtoM: A One to Many relationship.
             *
             * 	- OtoO: A One to One relationship.
             */
            public RelationType Type;
            public enum RelationType { MtoM, MtoO, OtoM, OtoO }

            /**
             * The local type of the relationship.
             */
            public Type LocalType;

            /**
             * The foreign type of the relationship.
             */
            public Type ForeignType;

            /**
             * The local property of the relationship.
             */
            public PropertyInfo LocalProperty;

            /**
             * The foreign property of the relationship.
             */
            public PropertyInfo ForeignProperty;

            /**
             * The local table name of the relationship.
             */
            public string LocalTableName;

            /**
             * The foreign table name of the relationship.
             */
            public string ForeignTableName;

            /**
             * The singular version of the local table name.
             */
            public string LocalTableNameSingular;

            /**
             * The singular version of the foreign table name.
             */
            public string ForeignTableNameSingular;

            /**
             * Only required for One to One relationships.
             * However for convenience we will still set this for
             * MtoO and OtoM relationships.
             */
            public string ForeignKeyTableName;

            /**
             * Name of the foreign key column in OtoM and OtoO relationships.
             */
            public string ForeignKeyColumnName;

            /**
             * The name of the pivot table for a MtoM relationship.
             */
            public string PivotTableName;

            /**
             * The name of the first column in the pivot table.
             */
            public string PivotTableFirstColumnName;

            /**
             *  The name of the second column in the pivot table.
             */
            public string PivotTableSecondColumnName;

            /**
             * Used to provide a unique relationship link when more
             * than one relationship exists between two types.
             *
             * To see what the LinkIdentifier is consider this example:
             *
             * ```
             *  class Customer : Model<Customer>
             *  {
             *      public List<Product> FavoriteProducts { get; set; }
             *      public List<Product> DislikedProducts { get; set; }
             *  }
             *
             *  class Product : Model<Order>
             *  {
             *      public List<Customer> FavoriteCustomers { get; set; }
             *      public List<Customer> DislikedCustomers { get; set; }
             *  }
             * ```
             *
             * We have 2 LinkIdentifier's Favorite & Disliked.
             */
            public string LinkIdentifier;
        }

        /**
         * This represents the discovered relationships.
         */
        public List<Relation> Discovered { get; protected set; }

        /**
         * RelationshipDiscoverer Constructor
         */
        public RelationshipDiscoverer(HashSet<Type> Models)
        {
            this.Discovered = new List<Relation>();

            // Loop through all models in the context.
            Models.ForEach(model =>
            {
                // Loop through all mapped props of the model
                Model.Dynamic(model).MappedProps.ForEach(prop =>
                {
                    // Ignore primative types.
                    if (TypeMapper.IsClrType(prop.PropertyType))
                    {
                        return;
                    }

                    // Get the relationship discriptor.
                    Relation? relation;

                    if (TypeMapper.IsListOfEntities(prop))
                    {
                        if ((relation = this.IsManyToMany(prop)) == null)
                        {
                            if ((relation = this.IsManyToOne(prop)) == null)
                            {
                                throw new UnknownRelationshipException(prop);
                            }
                        }
                    }
                    else
                    {
                        // Make sure the type is a Graceful Model.
                        // If this exception throws, it probably means the
                        // TypeMapper has failed us.
                        if (!prop.PropertyType.IsSubclassOf(typeof(Model)))
                        {
                            throw new UnknownRelationshipException(prop);
                        }

                        if ((relation = this.IsOneToMany(prop)) == null)
                        {
                            if ((relation = this.IsOneToOne(prop)) == null)
                            {
                                throw new UnknownRelationshipException(prop);
                            }
                        }
                    }

                    // Add it to our discovered list.
                    this.Discovered.Add((Relation)relation);
                });
            });
        }

        /**
         * Given a PropertyInfo we will work out if it is a Many to Many
         * relationship. For it to be a Many to Many relationship, both the
         * current TModel and the related TModel must have a List of each
         * others type.
         */
        protected virtual Relation? IsManyToMany(PropertyInfo localProp)
        {
            // The relationship desciptor we will return
            // if we actually find a valid relationship.
            var relation = new Relation { Type = Relation.RelationType.MtoM };

            // Grab the local and foreign types.
            relation.LocalType = localProp.DeclaringType;
            relation.ForeignType = localProp.PropertyType.GenericTypeArguments[0];

            // Grab the sql table names for both model types.
            relation = this.SetTableNames(relation);

            // Get all local properties that are lists of the foreignType.
            var localProps = Model.Dynamic(relation.LocalType).MappedProps
            .Where(lp => TypeMapper.IsListOfEntities(lp, relation.ForeignType)).ToList();

            // Get all foreign properties that are lists of the localType.
            var foreignProps = Model.Dynamic(relation.ForeignType).MappedProps
            .Where(fp => TypeMapper.IsListOfEntities(fp, relation.LocalType)).ToList();

            // The next part of the logic will determin these values.
            string pivotTableName = null; string pivotTableNameAlt = null;

            // If we can't find any matching properties on the foreign
            // side well we don't have a Many to Many relationship.
            if (foreignProps.Count == 0)
            {
                return null;
            }

            // If there is only 1 local and 1 foreign property that reference
            // each other then we create the pivot table based on the model
            // names. This is a simple every day Many to Many relationship.
            else if (localProps.Count == 1 && foreignProps.Count == 1)
            {
                // Save both navigational properties.
                relation.LocalProperty = localProp;
                relation.ForeignProperty = foreignProps[0];

                // Create the pivot table name candiates.
                pivotTableName = relation.LocalTableName + "To" + relation.ForeignTableName;
                pivotTableNameAlt = relation.ForeignTableName + "To" + relation.LocalTableName;
            }

            // This supports the ability to define multiple relationships to the
            // same type. If there are multiple properties that reference each
            // other, we use the property names to create a unique pivot table.
            else if (localProps.Count > 1 || foreignProps.Count > 1)
            {
                PropertyInfo foreignProp;

                // First lets check for any explictly set InversePropertyAttributes.
                var foreignInverseProp = localProp.GetCustomAttribute<InversePropertyAttribute>(false);
                if (foreignInverseProp != null)
                {
                    // Sweet the local property is telling is
                    // exactly which foreign property to use.
                    foreignProp = foreignProps.Single
                    (
                        fp => fp.Name == foreignInverseProp.Value
                    );
                }
                else
                {
                    // Lets see if any of the foreign properties have an
                    // InversePropertyAttribute that points to our
                    // local property.
                    foreignProp = foreignProps
                    .Where(fp => fp.GetCustomAttribute<InversePropertyAttribute>(false) != null)
                    .SingleOrDefault(fp => fp.GetCustomAttribute<InversePropertyAttribute>(false).Value == localProp.Name);
                }

                // Now lets attempt to find our inverse property by looking at the property names.
                if (foreignProp == null)
                {
                    // Determin the relation link id.
                    relation.LinkIdentifier = localProp.Name.Replace
                    (
                        relation.ForeignTableName, String.Empty
                    );

                    // We should have at least one foreign
                    // property that contains the LinkIdentifier.

                    try
                    {
                        foreignProp = foreignProps.Single
                        (
                            fp => fp.Name.Contains
                            (
                                relation.LinkIdentifier
                            )
                        );
                    }
                    catch
                    {
                        // We couldn't find a matching property
                        // so we don't have a relationship.
                        return null;
                    }
                }
                else
                {
                    // If the forgeign poroperty was found by means of a
                    // InversePropertyAttribute then we still need a
                    // LinkIdentifier so we need to make one.
                    relation.LinkIdentifier = localProp.Name + foreignProp.Name;
                }

                // Save both navigational properties.
                relation.LocalProperty = localProp;
                relation.ForeignProperty = foreignProp;

                // Create the pivot table name candiates.
                pivotTableName = relation.LocalTableName + relation.LinkIdentifier + relation.ForeignTableName;
                pivotTableNameAlt = relation.ForeignTableName + relation.LinkIdentifier + relation.LocalTableName;
            }
            else
            {
                return null;
            }

            // Now there are 2 possible pivot table names.
            // It's a first in, first served situation.
            if (this.PivotTableTaken(pivotTableName))
            {
                relation.PivotTableName = pivotTableName;
                relation.PivotTableFirstColumnName = relation.LocalTableNameSingular + "Id";
                relation.PivotTableSecondColumnName = relation.ForeignTableNameSingular + "Id";
            }
            else if (this.PivotTableTaken(pivotTableNameAlt))
            {
                relation.PivotTableName = pivotTableNameAlt;
                relation.PivotTableFirstColumnName = relation.ForeignTableNameSingular + "Id";
                relation.PivotTableSecondColumnName = relation.LocalTableNameSingular + "Id";
            }
            else
            {
                // No table actually exists yet,
                // so just go with the first option.
                // The Migrator will soon create it :)
                relation.PivotTableName = pivotTableName;
                relation.PivotTableFirstColumnName = relation.LocalTableNameSingular + "Id";
                relation.PivotTableSecondColumnName = relation.ForeignTableNameSingular + "Id";
            }

            // Finally return the relationship discriptor.
            return relation;
        }

        /**
         * Given a PropertyInfo we will work out if it is a Many to One
         * relationship. For it to be a Many to One relationship, the current
         * TModel must have a property that is a List of a foreign type.
         */
        protected virtual Relation? IsManyToOne(PropertyInfo localProp)
        {
            // The relationship desciptor we will return
            // if we actually find a valid relationship.
            var relation = new Relation { Type = Relation.RelationType.MtoO };

            // Grab the local and foreign types.
            relation.LocalType = localProp.DeclaringType;
            relation.ForeignType = localProp.PropertyType.GenericTypeArguments[0];

            // Grab the sql table names for both model types.
            relation = this.SetTableNames(relation);

            // Many to One relationships will always store
            // the foreign key in the foreign table.
            relation.ForeignKeyTableName = relation.ForeignTableName;

            // Get all local properties that are lists of the foreignType.
            var localProps = Model.Dynamic(relation.LocalType).MappedProps
            .Where(lp => TypeMapper.IsListOfEntities(lp, relation.ForeignType)).ToList();

            // Get all foreign properties that are of the localType.
            var foreignProps = Model.Dynamic(relation.ForeignType).MappedProps
            .Where(fp => fp.PropertyType == relation.LocalType).ToList();

            // If we can't find any matching properties on the foreign
            // side well we don't have a Many to One relationship.
            if (foreignProps.Count == 0)
            {
                return null;
            }

            // If there is 1 local and 1 foreign property that reference each
            // other, we have a simple, everyday, Many to One relationship.
            if (localProps.Count == 1 && foreignProps.Count == 1)
            {
                relation.LocalProperty = localProp;
                relation.ForeignProperty = foreignProps[0];
                relation.ForeignKeyColumnName = relation.LocalTableNameSingular + "Id";
            }

            // If we have a single local property but no foreign property.
            // We have a "Lazy" or "One Way" Many to One relationship.
            //
            // > NOTE: I think I have decided that lazy relationships are
            // > a pain in the arse and should not be allowed...
            /*else if (localProps.Count == 1 && foreignProps.Count == 0)
            {
                relation.LocalProperty = localProp;
                relation.ForeignProperty = null;
                relation.ForeignKeyColumnName = relation.LocalTableNameSingular + "Id";
            }*/

            // If there are multiple properties that reference each other,
            // we need to create the foreign key based on the property name
            // instead. So that the relationships remains unique.
            else if (localProps.Count > 1 || foreignProps.Count > 1)
            {
                PropertyInfo foreignProp;

                // First lets check for any explictly set InversePropertyAttributes.
                var foreignInverseProp = localProp.GetCustomAttribute<InversePropertyAttribute>(false);
                if (foreignInverseProp != null)
                {
                    // Sweet the local property is telling is
                    // exactly which foreign property to use.
                    foreignProp = foreignProps.Single
                    (
                        fp => fp.Name == foreignInverseProp.Value
                    );
                }
                else
                {
                    // Lets see if any of the foreign properties have an
                    // InversePropertyAttribute that points to our
                    // local property.
                    foreignProp = foreignProps
                    .Where(fp => fp.GetCustomAttribute<InversePropertyAttribute>(false) != null)
                    .SingleOrDefault(fp => fp.GetCustomAttribute<InversePropertyAttribute>(false).Value == localProp.Name);
                }

                // Now lets attempt to find our inverse property by looking at the property names.
                if (foreignProp == null)
                {
                    // Determin the relationship link id.
                    relation.LinkIdentifier = localProp.Name.Replace
                    (
                        relation.ForeignTableName, String.Empty
                    );

                    // We should have at least one foreign
                    // property that contains the LinkIdentifier.
                    try
                    {
                        foreignProp = foreignProps.Single
                        (
                            fp => fp.Name.Contains
                            (
                                relation.LinkIdentifier
                            )
                        );
                    }
                    catch
                    {
                        // Okay so we couldn't find a matching property.
                        // We still have a ManyToOne relationship, its a Lazy One,
                        // that requires a unique foreign key column name.
                        //
                        // > NOTE: I think I have decided that lazy relationships
                        // > are a pain in the arse and should not be allowed...
                        //foreignProp = null;

                        // We couldn't find a matching property
                        // so we don't have a relationship.
                        return null;
                    }
                }
                else
                {
                    // If the forgeign poroperty was found by means of a
                    // InversePropertyAttribute then we still need a
                    // LinkIdentifier so we need to make one.
                    relation.LinkIdentifier = localProp.Name + foreignProp.Name;
                }

                // Save both navigational properties.
                relation.LocalProperty = localProp;
                relation.ForeignProperty = foreignProp;

                // Set the foreign key column name
                relation.ForeignKeyColumnName = relation.LocalTableNameSingular + relation.LinkIdentifier + "Id";
            }
            else
            {
                return null;
            }

            // Finally return the relationship descriptor.
            return relation;
        }

        /**
         * Given a PropertyInfo we will work out if it is a One to Many
         * relationship. For it to be a One to Many relationship, the current
         * "TModel" must have a property that refers to a single foreign type
         * and the foreign type must have a property that is a List of "TModel".
         */
        public Relation? IsOneToMany(PropertyInfo localProp)
        {
            // The relationship desciptor we will return
            // if we actually find a valid relationship.
            var relation = new Relation { Type = Relation.RelationType.OtoM };

            // Grab the local and foreign types.
            relation.LocalType = localProp.DeclaringType;
            relation.ForeignType = localProp.PropertyType;

            // Grab the sql table names for both model types.
            relation = this.SetTableNames(relation);

            // One to Many relationships will always store
            // the foreign key in the local table.
            relation.ForeignKeyTableName = relation.LocalTableName;

            // Get all local properties that are of the foreign type.
            var localProps = Model.Dynamic(relation.LocalType).MappedProps
            .Where(lp => lp.PropertyType == relation.ForeignType).ToList();

            // Get all foreign properties that are lists of the local type.
            var foreignProps = Model.Dynamic(relation.ForeignType).MappedProps
            .Where(fp => TypeMapper.IsListOfEntities(fp, relation.LocalType)).ToList();

            // If we can't find any matching properties on the foreign
            // side well we don't have a One to Many relationship.
            if (foreignProps.Count == 0)
            {
                return null;
            }

            // If there is only 1 local and 1 foreign property that refrence
            // each other then we create the foreign key based on the model
            // names. This is a simple every day One to Many relationship.
            else if (localProps.Count == 1 && foreignProps.Count == 1)
            {
                // Save both navigational properties.
                relation.LocalProperty = localProp;
                relation.ForeignProperty = foreignProps[0];

                // Set the foreign key column name
                relation.ForeignKeyColumnName = relation.ForeignTableNameSingular + "Id";
            }

            // If there are multiple properties that reference each other,
            // we need to create the foreign key based on the property name
            // instead. So that the relationships remains unique.
            else if (localProps.Count > 1 || foreignProps.Count > 1)
            {
                PropertyInfo foreignProp;

                // First lets check for any explictly set InversePropertyAttributes.
                var foreignInverseProp = localProp.GetCustomAttribute<InversePropertyAttribute>(false);
                if (foreignInverseProp != null)
                {
                    // Sweet the local property is telling is
                    // exactly which foreign property to use.
                    foreignProp = foreignProps.Single
                    (
                        fp => fp.Name == foreignInverseProp.Value
                    );
                }
                else
                {
                    // Lets see if any of the foreign properties have an
                    // InversePropertyAttribute that points to our
                    // local property.
                    foreignProp = foreignProps
                    .Where(fp => fp.GetCustomAttribute<InversePropertyAttribute>(false) != null)
                    .SingleOrDefault(fp => fp.GetCustomAttribute<InversePropertyAttribute>(false).Value == localProp.Name);
                }

                // Now lets attempt to find our inverse property by looking at the property names.
                if (foreignProp == null)
                {
                    // Determin the relationship link id.
                    relation.LinkIdentifier = localProp.Name.Replace
                    (
                        relation.ForeignTableNameSingular, String.Empty
                    );

                    // We should have at least one foreign
                    // property that contains the LinkIdentifier.
                    try
                    {
                        foreignProp = foreignProps.Single
                        (
                            fp => fp.Name.Contains
                            (
                                relation.LinkIdentifier
                            )
                        );
                    }
                    catch
                    {
                        // We couldn't find a matching property
                        // so we don't have a relationship.
                        return null;
                    }
                }
                else
                {
                    // If the forgeign poroperty was found by means of a
                    // InversePropertyAttribute then we still need a
                    // LinkIdentifier so we need to make one.
                    relation.LinkIdentifier = localProp.Name + foreignProp.Name;
                }

                // Save both navigational properties.
                relation.LocalProperty = localProp;
                relation.ForeignProperty = foreignProp;

                // Set the foreign key column name
                relation.ForeignKeyColumnName = relation.ForeignTableNameSingular + relation.LinkIdentifier + "Id";
            }
            else
            {
                return null;
            }

            // Finally return the relationship descriptor.
            return relation;
        }

        /**
         * Given a PropertyInfo we will work out if it is a One to One
         * relationship. For it to be a One to One relationship, the current
         * "TModel" must have a property that refers to a single foreign Type.
         */
        protected virtual Relation? IsOneToOne(PropertyInfo localProp)
        {
            // The relationship desciptor we will return
            // if we actually find a valid relationship.
            var relation = new Relation { Type = Relation.RelationType.OtoO };

            // Grab the local and foreign types.
            relation.LocalType = localProp.DeclaringType;
            relation.ForeignType = localProp.PropertyType;

            // Grab the sql table names for both model types.
            relation = this.SetTableNames(relation);

            // Get all local properties that are of the foreign type.
            var localProps = Model.Dynamic(relation.LocalType).MappedProps
            .Where(lp => lp.PropertyType == relation.ForeignType).ToList();

            // Get all foreign properties that are of the local type.
            var foreignProps = Model.Dynamic(relation.ForeignType).MappedProps
            .Where(fp => fp.PropertyType == relation.LocalType).ToList();

            // If we can't find any matching properties on the foreign
            // side well we don't have a One to One relationship.
            if (foreignProps.Count == 0)
            {
                return null;
            }

            // If there is only 1 local and 1 foreign property that reference
            // each other then we create the foreign key based on the model
            // names. This is a simple every day One to One relationship.
            if (localProps.Count == 1 && foreignProps.Count == 1)
            {
                // Save both navigational properties.
                relation.LocalProperty = localProp;
                relation.ForeignProperty = foreignProps[0];

                // One to One relationships may have their foreign keys stored
                // in either table, it make no real diffrence, it is simply a
                // first in, first served situation.
                if (this.ForeignKeyColumnTaken(relation.LocalTableName, relation.ForeignTableNameSingular + "Id"))
                {
                    relation.ForeignKeyTableName = relation.LocalTableName;
                    relation.ForeignKeyColumnName = relation.ForeignTableNameSingular + "Id";
                }
                else if (this.ForeignKeyColumnTaken(relation.ForeignTableName, relation.LocalTableNameSingular + "Id"))
                {
                    relation.ForeignKeyTableName = relation.ForeignTableName;
                    relation.ForeignKeyColumnName = relation.LocalTableNameSingular + "Id";
                }
                else
                {
                    // Table or Column does not yet exist.
                    // So lets just go with our first prefrence.
                    // The migrator will soon create the table and / or column.
                    relation.ForeignKeyTableName = relation.LocalTableName;
                    relation.ForeignKeyColumnName = relation.ForeignTableNameSingular + "Id";
                }
            }

            // If there are 0 foreignProps, we still have a one to one
            // relationship, the other side just doesn't know anything about it.
            //
            // > NOTE: I think I have decided that lazy relationships are
            // > a pain in the arse and should not be allowed...
            /*else if (localProps.Count == 1 && foreignProps.Count == 0)
            {
                // Save both navigational properties.
                relation.LocalProperty = localProp;
                relation.ForeignProperty = null;

                // Because the other side knows nothing about us we can
                // be confident about where the foreign key will live.
                relation.ForeignKeyTableName = relation.LocalTableName;
                relation.ForeignKeyColumnName = relation.ForeignTableNameSingular + "Id";
            }*/

            // If there are multiple properties that reference each other,
            // we need to create the foreign key based on the property names
            // instead. So that the relationships remains unique.
            else if (localProps.Count > 1 || foreignProps.Count > 1)
            {
                PropertyInfo foreignProp;

                // First lets check for any explictly set InversePropertyAttributes.
                var foreignInverseProp = localProp.GetCustomAttribute<InversePropertyAttribute>(false);
                if (foreignInverseProp != null)
                {
                    // Sweet the local property is telling is
                    // exactly which foreign property to use.
                    foreignProp = foreignProps.Single
                    (
                        fp => fp.Name == foreignInverseProp.Value
                    );
                }
                else
                {
                    // Lets see if any of the foreign properties have an
                    // InversePropertyAttribute that points to our
                    // local property.
                    foreignProp = foreignProps
                    .Where(fp => fp.GetCustomAttribute<InversePropertyAttribute>(false) != null)
                    .SingleOrDefault(fp => fp.GetCustomAttribute<InversePropertyAttribute>(false).Value == localProp.Name);
                }

                // Now lets attempt to find our inverse property by looking at the property names.
                if (foreignProp == null)
                {
                    // Determin the relationship link id.
                    relation.LinkIdentifier = localProp.Name.Replace
                    (
                        relation.ForeignTableNameSingular, String.Empty
                    );

                    // We should have at least one foreign
                    // property that contains the LinkIdentifier.
                    try
                    {
                        foreignProp = foreignProps.Single
                        (
                            fp => fp.Name.Contains
                            (
                                relation.LinkIdentifier
                            )
                        );
                    }
                    catch
                    {
                        // Okay so we couldn't find a matching property.
                        // We still have a OneToOne relationship, its a Lazy One,
                        // that requires a unique foreign key column name.
                        //
                        // > NOTE: I think I have decided that lazy relationships
                        // > are a pain in the arse and should not be allowed...
                        //foreignProp = null;

                        // We couldn't find a matching property
                        // so we don't have a relationship.
                        return null;
                    }
                }
                else
                {
                    // If the forgeign poroperty was found by means of a
                    // InversePropertyAttribute then we still need a
                    // LinkIdentifier so we need to make one.
                    relation.LinkIdentifier = localProp.Name + foreignProp.Name;
                }

                // Save both navigational properties.
                relation.LocalProperty = localProp;
                relation.ForeignProperty = foreignProp;

                // One to One relationships may have their foreign keys stored
                // in either table, it make no real diffrence, it is simply a
                // first in, first served situation.
                if (this.ForeignKeyColumnTaken(relation.LocalTableName, relation.ForeignTableNameSingular + relation.LinkIdentifier + "Id"))
                {
                    relation.ForeignKeyTableName = relation.LocalTableName;
                    relation.ForeignKeyColumnName = relation.ForeignTableNameSingular + relation.LinkIdentifier + "Id";
                }
                else if (this.ForeignKeyColumnTaken(relation.ForeignTableName, relation.LocalTableNameSingular + relation.LinkIdentifier + "Id"))
                {
                    relation.ForeignKeyTableName = relation.ForeignTableName;
                    relation.ForeignKeyColumnName = relation.LocalTableNameSingular + relation.LinkIdentifier + "Id";
                }
                else
                {
                    // Table or Column does not yet exist.
                    // So lets just go with our first prefrence.
                    // The migrator will soon create the table and / or column.
                    relation.ForeignKeyTableName = relation.LocalTableName;
                    relation.ForeignKeyColumnName = relation.ForeignTableNameSingular + relation.LinkIdentifier + "Id";
                }
            }
            else
            {
                return null;
            }

            // Finally return the relationship descriptor.
            return relation;
        }

        /**
         * Given a relation, that has had it's local and foreign types set.
         * We will grab the SqlTable Names for both types, and set the table
         * name fields of the relation struct.
         */
        protected virtual Relation SetTableNames(Relation relation)
        {
            relation.LocalTableName = Model.Dynamic(relation.LocalType).SqlTableName;
            relation.ForeignTableName = Model.Dynamic(relation.ForeignType).SqlTableName;
            relation.LocalTableNameSingular = relation.LocalTableName.Singularize();
            relation.ForeignTableNameSingular = relation.ForeignTableName.Singularize();
            return relation;
        }

        /**
         * Given a pivot table name we will check to see if it has already
         * been taken by another model. ie: The other side of the relationship.
         */
        protected virtual bool PivotTableTaken(string TableName)
        {
            return this.Discovered.Any(r => r.PivotTableName == TableName);
        }

        /**
         * Given a table name and a column name for a foreign key
         * we will check to see if it has already been taken or not.
         */
        protected virtual bool ForeignKeyColumnTaken(string TableName, string ColumnName)
        {
            return this.Discovered.Any
            (
                r => r.ForeignKeyTableName == TableName &&
                r.ForeignKeyColumnName == ColumnName
            );
        }
    }
}
