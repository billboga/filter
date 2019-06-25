using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using RimDev.Filter.Generic;
using RimDev.Filter.Range.Generic;
using Xunit;

#if !NETCOREAPP2_1

namespace RimDev.Filter.Tests.Generic.Integration
{
    public class EfTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture fixture;

        public EfTests(DatabaseFixture databaseFixture)
        {
            fixture = databaseFixture;
        }

        private readonly IEnumerable<Person> People = new[]
        {
            new Person()
            {
                FavoriteDate = DateTime.Parse("2000-01-01"),
                FavoriteDateTimeOffset = DateTimeOffset.Parse("2010-01-01"),
                FavoriteLetter = 'a',
                FavoriteNumber = 5,
                FirstName = "John",
                LastName = "Doe"
            },
            new Person()
            {
                FavoriteDate = DateTime.Parse("2000-01-02"),
                FavoriteDateTimeOffset = DateTimeOffset.Parse("2010-01-02"),
                FavoriteLetter = 'b',
                FavoriteNumber = 10,
                FirstName = "Tim",
                LastName = "Smith",
                Rating = 4.5m
            },
        };

        [Fact]
        public void Can_filter_nullable_models_via_entity_framework()
        {
            Database.SetInitializer(new DropCreateDatabaseAlways<FilterDbContext>());

            using (var context = new FilterDbContext(fixture.ConnectionString))
            using (var transaction = context.Database.BeginTransaction())
            {
                context.People.AddRange(People);
                context.SaveChanges();

                var @return = context.People.Filter(new
                {
                    Rating = new decimal?(4.5m)
                });

                Assert.Equal(1, @return.Count());

                transaction.Rollback();
            }
        }

        [Fact]
        public void Can_filter_datetimeoffset_via_entity_framework()
        {
            using (var context = new FilterDbContext(fixture.ConnectionString))
            using (var transaction = context.Database.BeginTransaction())
            {
                context.People.AddRange(People);
                context.SaveChanges();

                var @return = context.People.Filter(new
                {
                    FavoriteDateTimeOffset = (Range<DateTimeOffset>)"[2010-01-01,2010-01-02)"
                });

                Assert.Equal(1, @return.Count());

                transaction.Rollback();
            }
        }

        [Fact]
        public void Should_be_able_to_handle_nullable_source()
        {
            using (var context = new FilterDbContext(fixture.ConnectionString))
            using (var transaction = context.Database.BeginTransaction())
            {
                context.People.AddRange(People);
                context.SaveChanges();

                var @return = context.People.Filter(new
                {
                    Rating = (Range<decimal>)"[4.5,5.0]"
                });

                Assert.Equal(1, @return.Count());

                transaction.Rollback();
            }
        }

        [Fact]
        public void Should_not_optimize_arrays_containing_multiple_values()
        {
            var singleParameter = new
            {
                FirstName = "Tim"
            };

            var collectionParameter = new
            {
                FirstName = new[] { "Tim", "John" }
            };

            using (var context = new FilterDbContext(fixture.ConnectionString))
            {
                IQueryable<Person> query = context.People.AsNoTracking();

                var expectedQuery = query.Filter(singleParameter);
                var actualQuery = query.Filter(collectionParameter);

                Assert.NotEqual(expectedQuery.ToString(), actualQuery.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void Should_optimize_arrays_containing_a_single_value()
        {
            var singleParameter = new
            {
                FirstName = "Tim"
            };

            var collectionParameter = new
            {
                FirstName = new[] { "Tim" }
            };

            using (var context = new FilterDbContext(fixture.ConnectionString))
            {
                IQueryable<Person> query = context.People.AsNoTracking();

                var expectedQuery = query.Where(x => x.FirstName == singleParameter.FirstName);
                var actualQuery = query.Filter(collectionParameter);

                Assert.Equal(expectedQuery.ToString(), actualQuery.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void Should_not_optimize_collections_containing_multiple_values()
        {
            var singleParameter = new
            {
                FirstName = "Tim"
            };

            var collectionParameter = new
            {
                FirstName = new List<string> { "Tim", "John" }
            };

            using (var context = new FilterDbContext(fixture.ConnectionString))
            {
                IQueryable<Person> query = context.People.AsNoTracking();

                var expectedQuery = query.Filter(singleParameter);
                var actualQuery = query.Filter(collectionParameter);

                Assert.NotEqual(expectedQuery.ToString(), actualQuery.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void Should_optimize_collections_containing_a_single_value()
        {
            var singleParameter = new
            {
                FirstName = "Tim"
            };

            var collectionParameter = new
            {
                FirstName = new List<string> { "Tim" }
            };

            using (var context = new FilterDbContext(fixture.ConnectionString))
            {
                IQueryable<Person> query = context.People.AsNoTracking();

                var expectedQuery = query.Filter(singleParameter);
                var actualQuery = query.Filter(collectionParameter);

                Assert.Equal(expectedQuery.ToString(), actualQuery.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }

        public sealed class FilterDbContext : DbContext
        {
            public FilterDbContext(string nameOrConnectionString)
                : base(nameOrConnectionString) { }

            public DbSet<Person> People { get; set; }
        }

        public class Person
        {
            public int Id { get; set; }
            public DateTime FavoriteDate { get; set; }
            public DateTimeOffset FavoriteDateTimeOffset { get; set; }
            public char FavoriteLetter { get; set; }
            public int FavoriteNumber { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public decimal? Rating { get; set; }
        }
    }
}

#endif