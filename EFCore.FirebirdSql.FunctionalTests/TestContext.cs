/*
 *          Copyright (c) 2017-2018 Rafael Almeida (ralms@ralms.net)
 *
 *                    EntityFrameworkCore.FirebirdSql
 *
 * THIS MATERIAL IS PROVIDED AS IS, WITH ABSOLUTELY NO WARRANTY EXPRESSED
 * OR IMPLIED.  ANY USE IS AT YOUR OWN RISK.
 *
 * Permission is hereby granted to use or copy this program
 * for any purpose,  provided the above notices are retained on all copies.
 * Permission to modify the code and to distribute modified code is granted,
 * provided the above notices are retained, and a notice that the code was
 * modified is included with the above copyright notice.
 *
 */

using System.IO;
using EFCore.FirebirdSql.FunctionalTests.TestUtilities;
using FirebirdSql.Data.FirebirdClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using FB = FirebirdSql.Data.FirebirdClient;

namespace EFCore.FirebirdSql.FunctionalTests
{
    // Repro: https://github.com/ralmsdeveloper/EntityFrameworkCore.FirebirdSQL/issues/28
    public class FirebirdContext : DbContext
    {
        private static readonly ILoggerFactory loggerFactory = new LoggerFactory()
            .AddConsole((s, l) => l == LogLevel.Debug && !s.EndsWith("Query"));

        public virtual DbSet<People> People { get; set; }
        public virtual DbSet<Repro41> Repro41 { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var directory = Path.Combine(Variables.PathAsssembly, "Issue28.fdb");
            var connectionString = new FB.FbConnectionStringBuilder(
               $@"User=SYSDBA;Password=masterkey;Database={directory};DataSource=localhost;Port=3050;")
            {
                // Dialect = 1
            }.ConnectionString;

            optionsBuilder
                .UseFirebird(connectionString)
                .ConfigureWarnings(c => c.Log(CoreEventId.IncludeIgnoredWarning));

            //var loggerFactory = new LoggerFactory()
            //    .AddConsole()
            //    .AddDebug();

            optionsBuilder.UseLoggerFactory(loggerFactory);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<People>(entity =>
               {
                   entity.ToTable("PEOPLE");

                   entity.Property(e => e.Id).HasColumnName("ID");

                   entity.Property(e => e.Givenname)
                       .HasColumnName("GIVENNAME")
                       .HasColumnType("VARCHAR(60)");

                   entity.Property(e => e.Name)
                       .HasColumnName("NAME")
                       .HasColumnType("VARCHAR(60)");
               });

            modelBuilder.Entity<Repro41>(entity =>
            {
                entity.ToTable("Repro41");

                entity.Property(e => e.Code).ValueGeneratedOnAddOrUpdate().UseFirebirdIdentityColumn();
                entity.Property(e => e.State).ValueGeneratedOnAddOrUpdate().UseFirebirdIdentityColumn();
            });
        }

    }

    public class TestContext : DbContext
    {
        public DbSet<Author> Author { get; set; }
        public DbSet<Book> Book { get; set; }
        public DbSet<Person> Person { get; set; }

        public DbSet<Course> Courses { get; set; }

        private string dbFileName;
        public TestContext(string dbFileName = "EFCoreSample.fdb")
        {
            this.dbFileName = dbFileName;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var directory = Path.Combine(Variables.PathAsssembly, dbFileName);
            var connectionString = new FB.FbConnectionStringBuilder(
                $@"User=SYSDBA;Password=masterkey;Database={directory};DataSource=localhost;Port=3050;")
            {
                //Dialect = 1,
            }.ConnectionString;

            optionsBuilder
                .UseFirebird(connectionString)
                .ConfigureWarnings(c => c.Log(CoreEventId.IncludeIgnoredWarning))
                .EnableSensitiveDataLogging();

            var loggerFactory = new LoggerFactory()
                .AddConsole()
                .AddDebug();

            optionsBuilder.UseLoggerFactory(loggerFactory);
        }

        protected override void OnModelCreating(ModelBuilder modelo)
        {
            base.OnModelCreating(modelo);

            modelo.Entity<Author>()
                .Property(x => x.AuthorId).UseFirebirdSequenceTrigger();

            modelo.Entity<Book>()
                .Property(x => x.BookId).UseFirebirdSequenceTrigger();

            modelo.Entity<Person>()
                .HasKey(person => new { person.Name, person.LastName });

            modelo.Entity<BookAuthor>()
                .HasKey(ba => new { ba.BookId, ba.AuthorId });

            modelo.Entity<BookAuthor>()
                .HasOne(ba => ba.Author)
                .WithMany(a => a.Books)
                .HasForeignKey(ba => ba.AuthorId);

            modelo.Entity<BookAuthor>()
                .HasOne(ba => ba.Book)
                .WithMany(b => b.Authors)
                .HasForeignKey(ba => ba.BookId);

            modelo.Entity<BookAuthor>()
                .HasIndex(ba => new { ba.BookId, ba.AuthorId });
        }
    }
}
