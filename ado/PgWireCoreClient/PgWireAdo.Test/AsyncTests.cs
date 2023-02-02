﻿using PgWireAdo.Test.Utils;
using System.Data;
using PgWireAdo.ado;
using static PgWireAdo.Test.Utils.TestUtils;

namespace PgWireAdo.Test
{
    public class AsyncTests:TestBase
    {
        [Test]
        public async Task NonQuery()
        {
            await using var conn = await OpenConnectionAsync();
            var tableName = await CreateTempTable(conn, "intf int");
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"INSERT INTO {tableName} (intf) VALUES (4)";
            await cmd.ExecuteNonQueryAsync();
            Assert.That(await conn.ExecuteScalarAsync($"SELECT intf FROM {tableName}"), Is.EqualTo(4));
        }



        [Test]
        public async Task Scalar()
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = new PgwCommand("SELECT 1", conn);
            Assert.That(await cmd.ExecuteScalarAsync(), Is.EqualTo(1));
        }

        [Test]
        public async Task Reader()
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = new PgwCommand("SELECT 1", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            Assert.That(reader[0], Is.EqualTo(1));
        }

        [Test]
        public async Task Columnar()
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = new PgwCommand("SELECT NULL, 2, 'Some Text'", conn);
            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
            await reader.ReadAsync();
            Assert.That(await reader.IsDBNullAsync(0), Is.True);
            Assert.That(await reader.GetFieldValueAsync<string>(2), Is.EqualTo("Some Text"));
        }
    }
}
