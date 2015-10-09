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

namespace Graceful.Tests
{
    using Xunit;
    using System;
    using Graceful.Utils;
    using System.Collections.Generic;

    public class RelationshipDiscovererTests
    {
        [Fact]
        public void SimpleManyToManyTest()
        {
            var discoverer = new RelationshipDiscoverer(new HashSet<Type>
            {
                typeof(Models.SimpleManyToManyTestModel1),
                typeof(Models.SimpleManyToManyTestModel2)
            });

            Assert.Equal(2, discoverer.Discovered.Count);

            var relation1 = discoverer.Discovered[0];
            Assert.Equal(RelationshipDiscoverer.Relation.RelationType.MtoM, relation1.Type);
            Assert.Equal(typeof(Models.SimpleManyToManyTestModel1), relation1.LocalType);
            Assert.Equal(typeof(Models.SimpleManyToManyTestModel2), relation1.ForeignType);
            Assert.Equal(typeof(Models.SimpleManyToManyTestModel1).GetProperty("Foos"), relation1.LocalProperty);
            Assert.Equal(typeof(Models.SimpleManyToManyTestModel2).GetProperty("Bars"), relation1.ForeignProperty);
            Assert.Equal("SimpleManyToManyTestModel1s", relation1.LocalTableName);
            Assert.Equal("SimpleManyToManyTestModel2s", relation1.ForeignTableName);
            Assert.Equal("SimpleManyToManyTestModel1", relation1.LocalTableNameSingular);
            Assert.Equal("SimpleManyToManyTestModel2", relation1.ForeignTableNameSingular);
            Assert.Equal(null, relation1.ForeignKeyTableName);
            Assert.Equal(null, relation1.ForeignKeyColumnName);
            Assert.Equal("SimpleManyToManyTestModel1sToSimpleManyToManyTestModel2s", relation1.PivotTableName);
            Assert.Equal("SimpleManyToManyTestModel1Id", relation1.PivotTableFirstColumnName);
            Assert.Equal("SimpleManyToManyTestModel2Id", relation1.PivotTableSecondColumnName);
            Assert.Equal(null, relation1.LinkIdentifier);

            var relation2 = discoverer.Discovered[1];
            Assert.Equal(RelationshipDiscoverer.Relation.RelationType.MtoM, relation2.Type);
            Assert.Equal(typeof(Models.SimpleManyToManyTestModel2), relation2.LocalType);
            Assert.Equal(typeof(Models.SimpleManyToManyTestModel1), relation2.ForeignType);
            Assert.Equal(typeof(Models.SimpleManyToManyTestModel2).GetProperty("Bars"), relation2.LocalProperty);
            Assert.Equal(typeof(Models.SimpleManyToManyTestModel1).GetProperty("Foos"), relation2.ForeignProperty);
            Assert.Equal("SimpleManyToManyTestModel2s", relation2.LocalTableName);
            Assert.Equal("SimpleManyToManyTestModel1s", relation2.ForeignTableName);
            Assert.Equal("SimpleManyToManyTestModel2", relation2.LocalTableNameSingular);
            Assert.Equal("SimpleManyToManyTestModel1", relation2.ForeignTableNameSingular);
            Assert.Equal(null, relation2.ForeignKeyTableName);
            Assert.Equal(null, relation2.ForeignKeyColumnName);
            Assert.Equal("SimpleManyToManyTestModel1sToSimpleManyToManyTestModel2s", relation2.PivotTableName);
            Assert.Equal("SimpleManyToManyTestModel1Id", relation2.PivotTableFirstColumnName);
            Assert.Equal("SimpleManyToManyTestModel2Id", relation2.PivotTableSecondColumnName);
            Assert.Equal(null, relation2.LinkIdentifier);
        }

        [Fact]
        public void MultipleManyToManyTest()
        {
            var discoverer = new RelationshipDiscoverer(new HashSet<Type>
            {
                typeof(Models.MultipleManyToManyTestModel1),
                typeof(Models.MultipleManyToManyTestModel2)
            });

            Assert.Equal(4, discoverer.Discovered.Count);

            var relation1 = discoverer.Discovered[0];
            Assert.Equal(RelationshipDiscoverer.Relation.RelationType.MtoM, relation1.Type);
            Assert.Equal(typeof(Models.MultipleManyToManyTestModel1), relation1.LocalType);
            Assert.Equal(typeof(Models.MultipleManyToManyTestModel2), relation1.ForeignType);
            Assert.Equal(typeof(Models.MultipleManyToManyTestModel1).GetProperty("FooMultipleManyToManyTestModel2s"), relation1.LocalProperty);
            Assert.Equal(typeof(Models.MultipleManyToManyTestModel2).GetProperty("FooMultipleManyToManyTestModel1s"), relation1.ForeignProperty);
            Assert.Equal("MultipleManyToManyTestModel1s", relation1.LocalTableName);
            Assert.Equal("MultipleManyToManyTestModel2s", relation1.ForeignTableName);
            Assert.Equal("MultipleManyToManyTestModel1", relation1.LocalTableNameSingular);
            Assert.Equal("MultipleManyToManyTestModel2", relation1.ForeignTableNameSingular);
            Assert.Equal(null, relation1.ForeignKeyTableName);
            Assert.Equal(null, relation1.ForeignKeyColumnName);
            Assert.Equal("MultipleManyToManyTestModel1sFooMultipleManyToManyTestModel2s", relation1.PivotTableName);
            Assert.Equal("MultipleManyToManyTestModel1Id", relation1.PivotTableFirstColumnName);
            Assert.Equal("MultipleManyToManyTestModel2Id", relation1.PivotTableSecondColumnName);
            Assert.Equal("Foo", relation1.LinkIdentifier);

            var relation2 = discoverer.Discovered[1];
            Assert.Equal(RelationshipDiscoverer.Relation.RelationType.MtoM, relation2.Type);
            Assert.Equal(typeof(Models.MultipleManyToManyTestModel1), relation2.LocalType);
            Assert.Equal(typeof(Models.MultipleManyToManyTestModel2), relation2.ForeignType);
            Assert.Equal(typeof(Models.MultipleManyToManyTestModel1).GetProperty("BarMultipleManyToManyTestModel2s"), relation2.LocalProperty);
            Assert.Equal(typeof(Models.MultipleManyToManyTestModel2).GetProperty("BarMultipleManyToManyTestModel1s"), relation2.ForeignProperty);
            Assert.Equal("MultipleManyToManyTestModel1s", relation2.LocalTableName);
            Assert.Equal("MultipleManyToManyTestModel2s", relation2.ForeignTableName);
            Assert.Equal("MultipleManyToManyTestModel1", relation2.LocalTableNameSingular);
            Assert.Equal("MultipleManyToManyTestModel2", relation2.ForeignTableNameSingular);
            Assert.Equal(null, relation2.ForeignKeyTableName);
            Assert.Equal(null, relation2.ForeignKeyColumnName);
            Assert.Equal("MultipleManyToManyTestModel1sBarMultipleManyToManyTestModel2s", relation2.PivotTableName);
            Assert.Equal("MultipleManyToManyTestModel1Id", relation2.PivotTableFirstColumnName);
            Assert.Equal("MultipleManyToManyTestModel2Id", relation2.PivotTableSecondColumnName);
            Assert.Equal("Bar", relation2.LinkIdentifier);

            var relation3 = discoverer.Discovered[2];
            Assert.Equal(RelationshipDiscoverer.Relation.RelationType.MtoM, relation3.Type);
            Assert.Equal(typeof(Models.MultipleManyToManyTestModel2), relation3.LocalType);
            Assert.Equal(typeof(Models.MultipleManyToManyTestModel1), relation3.ForeignType);
            Assert.Equal(typeof(Models.MultipleManyToManyTestModel2).GetProperty("FooMultipleManyToManyTestModel1s"), relation3.LocalProperty);
            Assert.Equal(typeof(Models.MultipleManyToManyTestModel1).GetProperty("FooMultipleManyToManyTestModel2s"), relation3.ForeignProperty);
            Assert.Equal("MultipleManyToManyTestModel2s", relation3.LocalTableName);
            Assert.Equal("MultipleManyToManyTestModel1s", relation3.ForeignTableName);
            Assert.Equal("MultipleManyToManyTestModel2", relation3.LocalTableNameSingular);
            Assert.Equal("MultipleManyToManyTestModel1", relation3.ForeignTableNameSingular);
            Assert.Equal(null, relation3.ForeignKeyTableName);
            Assert.Equal(null, relation3.ForeignKeyColumnName);
            Assert.Equal("MultipleManyToManyTestModel1sFooMultipleManyToManyTestModel2s", relation3.PivotTableName);
            Assert.Equal("MultipleManyToManyTestModel1Id", relation3.PivotTableFirstColumnName);
            Assert.Equal("MultipleManyToManyTestModel2Id", relation3.PivotTableSecondColumnName);
            Assert.Equal("Foo", relation3.LinkIdentifier);

            var relation4 = discoverer.Discovered[3];
            Assert.Equal(RelationshipDiscoverer.Relation.RelationType.MtoM, relation4.Type);
            Assert.Equal(typeof(Models.MultipleManyToManyTestModel2), relation4.LocalType);
            Assert.Equal(typeof(Models.MultipleManyToManyTestModel1), relation4.ForeignType);
            Assert.Equal(typeof(Models.MultipleManyToManyTestModel2).GetProperty("BarMultipleManyToManyTestModel1s"), relation4.LocalProperty);
            Assert.Equal(typeof(Models.MultipleManyToManyTestModel1).GetProperty("BarMultipleManyToManyTestModel2s"), relation4.ForeignProperty);
            Assert.Equal("MultipleManyToManyTestModel2s", relation4.LocalTableName);
            Assert.Equal("MultipleManyToManyTestModel1s", relation4.ForeignTableName);
            Assert.Equal("MultipleManyToManyTestModel2", relation4.LocalTableNameSingular);
            Assert.Equal("MultipleManyToManyTestModel1", relation4.ForeignTableNameSingular);
            Assert.Equal(null, relation4.ForeignKeyTableName);
            Assert.Equal(null, relation4.ForeignKeyColumnName);
            Assert.Equal("MultipleManyToManyTestModel1sBarMultipleManyToManyTestModel2s", relation4.PivotTableName);
            Assert.Equal("MultipleManyToManyTestModel1Id", relation4.PivotTableFirstColumnName);
            Assert.Equal("MultipleManyToManyTestModel2Id", relation4.PivotTableSecondColumnName);
            Assert.Equal("Bar", relation4.LinkIdentifier);
        }

        [Fact]
        public void SimpleManyToOneTest()
        {
            var discoverer = new RelationshipDiscoverer(new HashSet<Type>
            {
                typeof(Models.SimpleManyToOneTestModel1),
                typeof(Models.SimpleManyToOneTestModel2)
            });

            Assert.Equal(2, discoverer.Discovered.Count);

            var relation1 = discoverer.Discovered[0];
            Assert.Equal(RelationshipDiscoverer.Relation.RelationType.MtoO, relation1.Type);
            Assert.Equal(typeof(Models.SimpleManyToOneTestModel1), relation1.LocalType);
            Assert.Equal(typeof(Models.SimpleManyToOneTestModel2), relation1.ForeignType);
            Assert.Equal(typeof(Models.SimpleManyToOneTestModel1).GetProperty("Foos"), relation1.LocalProperty);
            Assert.Equal(typeof(Models.SimpleManyToOneTestModel2).GetProperty("Bar"), relation1.ForeignProperty);
            Assert.Equal("SimpleManyToOneTestModel1s", relation1.LocalTableName);
            Assert.Equal("SimpleManyToOneTestModel2s", relation1.ForeignTableName);
            Assert.Equal("SimpleManyToOneTestModel1", relation1.LocalTableNameSingular);
            Assert.Equal("SimpleManyToOneTestModel2", relation1.ForeignTableNameSingular);
            Assert.Equal("SimpleManyToOneTestModel2s", relation1.ForeignKeyTableName);
            Assert.Equal("SimpleManyToOneTestModel1Id", relation1.ForeignKeyColumnName);
            Assert.Equal(null, relation1.PivotTableName);
            Assert.Equal(null, relation1.PivotTableFirstColumnName);
            Assert.Equal(null, relation1.PivotTableSecondColumnName);
            Assert.Equal(null, relation1.LinkIdentifier);

            var relation2 = discoverer.Discovered[1];
            Assert.Equal(RelationshipDiscoverer.Relation.RelationType.OtoM, relation2.Type);
            Assert.Equal(typeof(Models.SimpleManyToOneTestModel2), relation2.LocalType);
            Assert.Equal(typeof(Models.SimpleManyToOneTestModel1), relation2.ForeignType);
            Assert.Equal(typeof(Models.SimpleManyToOneTestModel2).GetProperty("Bar"), relation2.LocalProperty);
            Assert.Equal(typeof(Models.SimpleManyToOneTestModel1).GetProperty("Foos"), relation2.ForeignProperty);
            Assert.Equal("SimpleManyToOneTestModel2s", relation2.LocalTableName);
            Assert.Equal("SimpleManyToOneTestModel1s", relation2.ForeignTableName);
            Assert.Equal("SimpleManyToOneTestModel2", relation2.LocalTableNameSingular);
            Assert.Equal("SimpleManyToOneTestModel1", relation2.ForeignTableNameSingular);
            Assert.Equal("SimpleManyToOneTestModel2s", relation2.ForeignKeyTableName);
            Assert.Equal("SimpleManyToOneTestModel1Id", relation2.ForeignKeyColumnName);
            Assert.Equal(null, relation2.PivotTableName);
            Assert.Equal(null, relation2.PivotTableFirstColumnName);
            Assert.Equal(null, relation2.PivotTableSecondColumnName);
            Assert.Equal(null, relation2.LinkIdentifier);

        }

        [Fact]
        public void MultipleManyToOneTest()
        {
            var discoverer = new RelationshipDiscoverer(new HashSet<Type>
            {
                typeof(Models.MultipleManyToOneTestModel1),
                typeof(Models.MultipleManyToOneTestModel2)
            });

            Assert.Equal(4, discoverer.Discovered.Count);

            var relation1 = discoverer.Discovered[0];
            Assert.Equal(RelationshipDiscoverer.Relation.RelationType.MtoO, relation1.Type);
            Assert.Equal(typeof(Models.MultipleManyToOneTestModel1), relation1.LocalType);
            Assert.Equal(typeof(Models.MultipleManyToOneTestModel2), relation1.ForeignType);
            Assert.Equal(typeof(Models.MultipleManyToOneTestModel1).GetProperty("FooMultipleManyToOneTestModel2s"), relation1.LocalProperty);
            Assert.Equal(typeof(Models.MultipleManyToOneTestModel2).GetProperty("FooMultipleManyToOneTestModel1"), relation1.ForeignProperty);
            Assert.Equal("MultipleManyToOneTestModel1s", relation1.LocalTableName);
            Assert.Equal("MultipleManyToOneTestModel2s", relation1.ForeignTableName);
            Assert.Equal("MultipleManyToOneTestModel1", relation1.LocalTableNameSingular);
            Assert.Equal("MultipleManyToOneTestModel2", relation1.ForeignTableNameSingular);
            Assert.Equal("MultipleManyToOneTestModel2s", relation1.ForeignKeyTableName);
            Assert.Equal("MultipleManyToOneTestModel1FooId", relation1.ForeignKeyColumnName);
            Assert.Equal(null, relation1.PivotTableName);
            Assert.Equal(null, relation1.PivotTableFirstColumnName);
            Assert.Equal(null, relation1.PivotTableSecondColumnName);
            Assert.Equal("Foo", relation1.LinkIdentifier);

            var relation2 = discoverer.Discovered[1];
            Assert.Equal(RelationshipDiscoverer.Relation.RelationType.MtoO, relation2.Type);
            Assert.Equal(typeof(Models.MultipleManyToOneTestModel1), relation2.LocalType);
            Assert.Equal(typeof(Models.MultipleManyToOneTestModel2), relation2.ForeignType);
            Assert.Equal(typeof(Models.MultipleManyToOneTestModel1).GetProperty("BarMultipleManyToOneTestModel2s"), relation2.LocalProperty);
            Assert.Equal(typeof(Models.MultipleManyToOneTestModel2).GetProperty("BarMultipleManyToOneTestModel1"), relation2.ForeignProperty);
            Assert.Equal("MultipleManyToOneTestModel1s", relation2.LocalTableName);
            Assert.Equal("MultipleManyToOneTestModel2s", relation2.ForeignTableName);
            Assert.Equal("MultipleManyToOneTestModel1", relation2.LocalTableNameSingular);
            Assert.Equal("MultipleManyToOneTestModel2", relation2.ForeignTableNameSingular);
            Assert.Equal("MultipleManyToOneTestModel2s", relation2.ForeignKeyTableName);
            Assert.Equal("MultipleManyToOneTestModel1BarId", relation2.ForeignKeyColumnName);
            Assert.Equal(null, relation2.PivotTableName);
            Assert.Equal(null, relation2.PivotTableFirstColumnName);
            Assert.Equal(null, relation2.PivotTableSecondColumnName);
            Assert.Equal("Bar", relation2.LinkIdentifier);

            var relation3 = discoverer.Discovered[2];
            Assert.Equal(RelationshipDiscoverer.Relation.RelationType.OtoM, relation3.Type);
            Assert.Equal(typeof(Models.MultipleManyToOneTestModel2), relation3.LocalType);
            Assert.Equal(typeof(Models.MultipleManyToOneTestModel1), relation3.ForeignType);
            Assert.Equal(typeof(Models.MultipleManyToOneTestModel2).GetProperty("FooMultipleManyToOneTestModel1"), relation3.LocalProperty);
            Assert.Equal(typeof(Models.MultipleManyToOneTestModel1).GetProperty("FooMultipleManyToOneTestModel2s"), relation3.ForeignProperty);
            Assert.Equal("MultipleManyToOneTestModel2s", relation3.LocalTableName);
            Assert.Equal("MultipleManyToOneTestModel1s", relation3.ForeignTableName);
            Assert.Equal("MultipleManyToOneTestModel2", relation3.LocalTableNameSingular);
            Assert.Equal("MultipleManyToOneTestModel1", relation3.ForeignTableNameSingular);
            Assert.Equal("MultipleManyToOneTestModel2s", relation3.ForeignKeyTableName);
            Assert.Equal("MultipleManyToOneTestModel1FooId", relation3.ForeignKeyColumnName);
            Assert.Equal(null, relation3.PivotTableName);
            Assert.Equal(null, relation3.PivotTableFirstColumnName);
            Assert.Equal(null, relation3.PivotTableSecondColumnName);
            Assert.Equal("Foo", relation3.LinkIdentifier);

            var relation4 = discoverer.Discovered[3];
            Assert.Equal(RelationshipDiscoverer.Relation.RelationType.OtoM, relation4.Type);
            Assert.Equal(typeof(Models.MultipleManyToOneTestModel2), relation4.LocalType);
            Assert.Equal(typeof(Models.MultipleManyToOneTestModel1), relation4.ForeignType);
            Assert.Equal(typeof(Models.MultipleManyToOneTestModel2).GetProperty("BarMultipleManyToOneTestModel1"), relation4.LocalProperty);
            Assert.Equal(typeof(Models.MultipleManyToOneTestModel1).GetProperty("BarMultipleManyToOneTestModel2s"), relation4.ForeignProperty);
            Assert.Equal("MultipleManyToOneTestModel2s", relation4.LocalTableName);
            Assert.Equal("MultipleManyToOneTestModel1s", relation4.ForeignTableName);
            Assert.Equal("MultipleManyToOneTestModel2", relation4.LocalTableNameSingular);
            Assert.Equal("MultipleManyToOneTestModel1", relation4.ForeignTableNameSingular);
            Assert.Equal("MultipleManyToOneTestModel2s", relation4.ForeignKeyTableName);
            Assert.Equal("MultipleManyToOneTestModel1BarId", relation4.ForeignKeyColumnName);
            Assert.Equal(null, relation4.PivotTableName);
            Assert.Equal(null, relation4.PivotTableFirstColumnName);
            Assert.Equal(null, relation4.PivotTableSecondColumnName);
            Assert.Equal("Bar", relation4.LinkIdentifier);
        }

        [Fact]
        public void SimpleOneToOneTest()
        {
            var discoverer = new RelationshipDiscoverer(new HashSet<Type>
            {
                typeof(Models.SimpleOneToOneTestModel1),
                typeof(Models.SimpleOneToOneTestModel2)
            });

            Assert.Equal(2, discoverer.Discovered.Count);

            var relation1 = discoverer.Discovered[0];
            Assert.Equal(RelationshipDiscoverer.Relation.RelationType.OtoO, relation1.Type);
            Assert.Equal(typeof(Models.SimpleOneToOneTestModel1), relation1.LocalType);
            Assert.Equal(typeof(Models.SimpleOneToOneTestModel2), relation1.ForeignType);
            Assert.Equal(typeof(Models.SimpleOneToOneTestModel1).GetProperty("Foo"), relation1.LocalProperty);
            Assert.Equal(typeof(Models.SimpleOneToOneTestModel2).GetProperty("Bar"), relation1.ForeignProperty);
            Assert.Equal("SimpleOneToOneTestModel1s", relation1.LocalTableName);
            Assert.Equal("SimpleOneToOneTestModel2s", relation1.ForeignTableName);
            Assert.Equal("SimpleOneToOneTestModel1", relation1.LocalTableNameSingular);
            Assert.Equal("SimpleOneToOneTestModel2", relation1.ForeignTableNameSingular);
            Assert.Equal("SimpleOneToOneTestModel1s", relation1.ForeignKeyTableName);
            Assert.Equal("SimpleOneToOneTestModel2Id", relation1.ForeignKeyColumnName);
            Assert.Equal(null, relation1.PivotTableName);
            Assert.Equal(null, relation1.PivotTableFirstColumnName);
            Assert.Equal(null, relation1.PivotTableSecondColumnName);
            Assert.Equal(null, relation1.LinkIdentifier);

            var relation2 = discoverer.Discovered[1];
            Assert.Equal(RelationshipDiscoverer.Relation.RelationType.OtoO, relation2.Type);
            Assert.Equal(typeof(Models.SimpleOneToOneTestModel2), relation2.LocalType);
            Assert.Equal(typeof(Models.SimpleOneToOneTestModel1), relation2.ForeignType);
            Assert.Equal(typeof(Models.SimpleOneToOneTestModel2).GetProperty("Bar"), relation2.LocalProperty);
            Assert.Equal(typeof(Models.SimpleOneToOneTestModel1).GetProperty("Foo"), relation2.ForeignProperty);
            Assert.Equal("SimpleOneToOneTestModel2s", relation2.LocalTableName);
            Assert.Equal("SimpleOneToOneTestModel1s", relation2.ForeignTableName);
            Assert.Equal("SimpleOneToOneTestModel2", relation2.LocalTableNameSingular);
            Assert.Equal("SimpleOneToOneTestModel1", relation2.ForeignTableNameSingular);
            Assert.Equal("SimpleOneToOneTestModel1s", relation2.ForeignKeyTableName);
            Assert.Equal("SimpleOneToOneTestModel2Id", relation2.ForeignKeyColumnName);
            Assert.Equal(null, relation2.PivotTableName);
            Assert.Equal(null, relation2.PivotTableFirstColumnName);
            Assert.Equal(null, relation2.PivotTableSecondColumnName);
            Assert.Equal(null, relation2.LinkIdentifier);
        }

        [Fact]
        public void MultipleOneToOneTest()
        {
            var discoverer = new RelationshipDiscoverer(new HashSet<Type>
            {
                typeof(Models.MultipleOneToOneTestModel1),
                typeof(Models.MultipleOneToOneTestModel2)
            });

            Assert.Equal(4, discoverer.Discovered.Count);

            var relation1 = discoverer.Discovered[0];
            Assert.Equal(RelationshipDiscoverer.Relation.RelationType.OtoO, relation1.Type);
            Assert.Equal(typeof(Models.MultipleOneToOneTestModel1), relation1.LocalType);
            Assert.Equal(typeof(Models.MultipleOneToOneTestModel2), relation1.ForeignType);
            Assert.Equal(typeof(Models.MultipleOneToOneTestModel1).GetProperty("FooMultipleOneToOneTestModel2"), relation1.LocalProperty);
            Assert.Equal(typeof(Models.MultipleOneToOneTestModel2).GetProperty("FooMultipleOneToOneTestModel1"), relation1.ForeignProperty);
            Assert.Equal("MultipleOneToOneTestModel1s", relation1.LocalTableName);
            Assert.Equal("MultipleOneToOneTestModel2s", relation1.ForeignTableName);
            Assert.Equal("MultipleOneToOneTestModel1", relation1.LocalTableNameSingular);
            Assert.Equal("MultipleOneToOneTestModel2", relation1.ForeignTableNameSingular);
            Assert.Equal("MultipleOneToOneTestModel1s", relation1.ForeignKeyTableName);
            Assert.Equal("MultipleOneToOneTestModel2FooId", relation1.ForeignKeyColumnName);
            Assert.Equal(null, relation1.PivotTableName);
            Assert.Equal(null, relation1.PivotTableFirstColumnName);
            Assert.Equal(null, relation1.PivotTableSecondColumnName);
            Assert.Equal("Foo", relation1.LinkIdentifier);

            var relation2 = discoverer.Discovered[1];
            Assert.Equal(RelationshipDiscoverer.Relation.RelationType.OtoO, relation2.Type);
            Assert.Equal(typeof(Models.MultipleOneToOneTestModel1), relation2.LocalType);
            Assert.Equal(typeof(Models.MultipleOneToOneTestModel2), relation2.ForeignType);
            Assert.Equal(typeof(Models.MultipleOneToOneTestModel1).GetProperty("BarMultipleOneToOneTestModel2"), relation2.LocalProperty);
            Assert.Equal(typeof(Models.MultipleOneToOneTestModel2).GetProperty("BarMultipleOneToOneTestModel1"), relation2.ForeignProperty);
            Assert.Equal("MultipleOneToOneTestModel1s", relation2.LocalTableName);
            Assert.Equal("MultipleOneToOneTestModel2s", relation2.ForeignTableName);
            Assert.Equal("MultipleOneToOneTestModel1", relation2.LocalTableNameSingular);
            Assert.Equal("MultipleOneToOneTestModel2", relation2.ForeignTableNameSingular);
            Assert.Equal("MultipleOneToOneTestModel1s", relation2.ForeignKeyTableName);
            Assert.Equal("MultipleOneToOneTestModel2BarId", relation2.ForeignKeyColumnName);
            Assert.Equal(null, relation2.PivotTableName);
            Assert.Equal(null, relation2.PivotTableFirstColumnName);
            Assert.Equal(null, relation2.PivotTableSecondColumnName);
            Assert.Equal("Bar", relation2.LinkIdentifier);

            var relation3 = discoverer.Discovered[2];
            Assert.Equal(RelationshipDiscoverer.Relation.RelationType.OtoO, relation3.Type);
            Assert.Equal(typeof(Models.MultipleOneToOneTestModel2), relation3.LocalType);
            Assert.Equal(typeof(Models.MultipleOneToOneTestModel1), relation3.ForeignType);
            Assert.Equal(typeof(Models.MultipleOneToOneTestModel2).GetProperty("FooMultipleOneToOneTestModel1"), relation3.LocalProperty);
            Assert.Equal(typeof(Models.MultipleOneToOneTestModel1).GetProperty("FooMultipleOneToOneTestModel2"), relation3.ForeignProperty);
            Assert.Equal("MultipleOneToOneTestModel2s", relation3.LocalTableName);
            Assert.Equal("MultipleOneToOneTestModel1s", relation3.ForeignTableName);
            Assert.Equal("MultipleOneToOneTestModel2", relation3.LocalTableNameSingular);
            Assert.Equal("MultipleOneToOneTestModel1", relation3.ForeignTableNameSingular);
            Assert.Equal("MultipleOneToOneTestModel1s", relation3.ForeignKeyTableName);
            Assert.Equal("MultipleOneToOneTestModel2FooId", relation3.ForeignKeyColumnName);
            Assert.Equal(null, relation3.PivotTableName);
            Assert.Equal(null, relation3.PivotTableFirstColumnName);
            Assert.Equal(null, relation3.PivotTableSecondColumnName);
            Assert.Equal("Foo", relation3.LinkIdentifier);

            var relation4 = discoverer.Discovered[3];
            Assert.Equal(RelationshipDiscoverer.Relation.RelationType.OtoO, relation4.Type);
            Assert.Equal(typeof(Models.MultipleOneToOneTestModel2), relation4.LocalType);
            Assert.Equal(typeof(Models.MultipleOneToOneTestModel1), relation4.ForeignType);
            Assert.Equal(typeof(Models.MultipleOneToOneTestModel2).GetProperty("BarMultipleOneToOneTestModel1"), relation4.LocalProperty);
            Assert.Equal(typeof(Models.MultipleOneToOneTestModel1).GetProperty("BarMultipleOneToOneTestModel2"), relation4.ForeignProperty);
            Assert.Equal("MultipleOneToOneTestModel2s", relation4.LocalTableName);
            Assert.Equal("MultipleOneToOneTestModel1s", relation4.ForeignTableName);
            Assert.Equal("MultipleOneToOneTestModel2", relation4.LocalTableNameSingular);
            Assert.Equal("MultipleOneToOneTestModel1", relation4.ForeignTableNameSingular);
            Assert.Equal("MultipleOneToOneTestModel1s", relation4.ForeignKeyTableName);
            Assert.Equal("MultipleOneToOneTestModel2BarId", relation4.ForeignKeyColumnName);
            Assert.Equal(null, relation4.PivotTableName);
            Assert.Equal(null, relation4.PivotTableFirstColumnName);
            Assert.Equal(null, relation4.PivotTableSecondColumnName);
            Assert.Equal("Bar", relation4.LinkIdentifier);
        }
    }
}
