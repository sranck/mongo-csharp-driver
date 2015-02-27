/* Copyright 2010-2014 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Linq
{
    [TestFixture]
    public class WithMaxTimeTests
    {
        private class B
        {
            public ObjectId Id;
            public int a;
            public int b;
            public int c;
        }

        private MongoServer _server;
        private MongoServerInstance _primary;
        private MongoDatabase _database;
        private MongoCollection<B> _collection;

        [TestFixtureSetUp]
        public void Setup()
        {
            _server = Configuration.TestServer;
            _primary = _server.Instances.First(x => ReadPreference.Primary.MatchesInstance(x));
            _database = Configuration.TestDatabase;
            _collection = Configuration.GetTestCollection<B>();

            _collection.Drop();
            _collection.Insert(new B { Id = ObjectId.GenerateNewId(), a = 1, b = 10, c = 100 });
            _collection.Insert(new B { Id = ObjectId.GenerateNewId(), a = 2, b = 20, c = 200 });
            _collection.Insert(new B { Id = ObjectId.GenerateNewId(), a = 3, b = 30, c = 300 });
            _collection.Insert(new B { Id = ObjectId.GenerateNewId(), a = 4, b = 40, c = 400 });
        }

        [Test]
        public void TestSimpleQueryHasMaxTime()
        {
            var query = _collection.AsQueryable().WithMaxTime(TimeSpan.FromSeconds(5));
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual(5000, selectQuery.MaxTime.Value.TotalMilliseconds);
        }

        [Test]
        public void TestQueryWithSkipAndTakeHasMaxTime()
        {
            var query = _collection.AsQueryable().WithMaxTime(TimeSpan.FromSeconds(5)).Skip(2).Take(5);
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual(5000, selectQuery.MaxTime.Value.TotalMilliseconds);

            query = _collection.AsQueryable().Skip(2).Take(5).WithMaxTime(TimeSpan.FromSeconds(5));
            selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual(5000, selectQuery.MaxTime.Value.TotalMilliseconds);
        }

        [Test]
        public void TestQueryWithProjectionHasMaxTime()
        {
            var query = _collection.AsQueryable().WithMaxTime(TimeSpan.FromSeconds(5)).Select(o => o.a);
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual(5000, selectQuery.MaxTime.Value.TotalMilliseconds);
            Assert.AreEqual("(B o) => o.a", ExpressionFormatter.ToString(selectQuery.Projection));
        }

        [Test]
        public void TestQueryWithConditionHasMaxTime()
        {
            var query = _collection.AsQueryable().Where(o => o.a == 1 && o.b == 3).WithMaxTime(TimeSpan.FromSeconds(5));
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual(5000, selectQuery.MaxTime.Value.TotalMilliseconds);
            Assert.AreEqual("{ \"a\" : 1, \"b\" : 3 }", selectQuery.BuildQuery().ToJson());
        }

        [Test]
        public void TestQueryWithMaxTimeBeforeConditionHasMaxTime()
        {
            var query = _collection.AsQueryable().WithMaxTime(TimeSpan.FromSeconds(5)).Where(o => o.a == 1 && o.b == 3);
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual(5000, selectQuery.MaxTime.Value.TotalMilliseconds);
            Assert.AreEqual("{ \"a\" : 1, \"b\" : 3 }", selectQuery.BuildQuery().ToJson());
        }

        [Test]
        public void TestMaxTimeIsUsedInQuery()
        {
            if (_primary.Supports(FeatureId.MaxTime))
            {
                using (var failpoint = new FailPoint(FailPointName.MaxTimeAlwaysTimeout, _server, _primary))
                {
                    if (failpoint.IsSupported())
                    {
                        failpoint.SetAlwaysOn();
                        var maxTime = TimeSpan.FromMilliseconds(1);
                        Assert.Throws<ExecutionTimeoutException>(() => _collection.AsQueryable().WithMaxTime(maxTime).ToList());
                    }
                }
            }
        }

        [Test]
        public void TestWithMaxTimeCannotBeBeforeDistinct()
        {
            Assert.Throws<NotSupportedException>(
                () => _collection.AsQueryable().Select(o => o.a).WithMaxTime(TimeSpan.FromSeconds(5)).Distinct().ToList());
        }

        [Test]
        public void TestWithMaxTimeCannotBeAfterDistinct()
        {
            Assert.Throws<NotSupportedException>(() => _collection.AsQueryable().Select(o => o.a).Distinct().WithMaxTime(TimeSpan.FromSeconds(5)).ToList());
        }

        [Test]
        public void TestThereCanOnlyBeOneMaxTime()
        {
            Assert.Throws<NotSupportedException>(() => _collection.AsQueryable().WithMaxTime(TimeSpan.FromSeconds(5)).WithMaxTime(TimeSpan.FromSeconds(10)).ToList());
        }

    }
}