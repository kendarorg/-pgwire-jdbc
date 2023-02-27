﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using PgWireAdo.utils;
using PgWireAdo.wire;
using PgWireAdo.wire.client;
using PgWireAdo.wire.server;

namespace PgWireAdo.ado;

public class PgwConnection : DbConnection
{
    private PgwConnectionString _options;
    private string _connectionString;
    private ConnectionState _state = ConnectionState.Closed;
    private TcpClient _client;
    private ReadSeekableStream _stream;

    public PgwConnection(string connectionString)
    {
        _connectionString = connectionString;
    }

    public PgwConnection()
    {
        
    }

    public override void Open()
    {
        _client = new TcpClient(_options.DataSource, _options.Port);
        _stream = new ReadSeekableStream(_client.GetStream(), 1024);
        _state = ConnectionState.Open;
        var sslNegotiation = new SSLNegotation();
        sslNegotiation.Write(_stream);
        var parameters = new Dictionary<String, String>();
        parameters.Add("database", Database);
        var startup = new StartupMessage(parameters);
        startup.Write(_stream);
    }

    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        Open();
        return Task.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        if (_state != ConnectionState.Closed)
        {
            _state = ConnectionState.Closed;
            if (disposing)
            {
                _stream.Dispose();
                _client.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    public override string ConnectionString {
        get { return _connectionString; }
        set
        {
            if (value == null)
            {
                value = string.Empty;
            }

            _options = new PgwConnectionString(value);
            _connectionString = value;
        }
    }
    public override string Database => _options.Database;
    public override ConnectionState State => _state;
    public override string DataSource => _options.DataSource;
    public override string ServerVersion => _options.ServerVersion;

    public ReadSeekableStream Stream => _stream;


    public override void Close()
    {
        _state = ConnectionState.Closed;
        if (_stream == null)
        {
            return;
        }
        var terminate = new TerminateMessage();
        terminate.Write(_stream);
        _stream.Dispose();
        _client.Dispose();
        _stream = null;
        _client = null;

    }


    protected override DbCommand CreateDbCommand()
    {
        return new PgwCommand(this);
    }

    public override ValueTask DisposeAsync()
    {
        return new ValueTask(Task.Run(Dispose));
    }

    #region TOIMPLEMENT

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        var result= new PgwTransaction(this, isolationLevel);
        var queryMessage = new QueryMessage("JANUS:BEGIN_TRANSACTION");
        queryMessage.Write(Stream);
        var commandComplete = new CommandComplete();
        if (commandComplete.IsMatching(Stream))
        {
            commandComplete.Read(Stream);
        }
        var readyForQuery = new ReadyForQuery();
        if (readyForQuery.IsMatching(Stream))
        {
            readyForQuery.Read(Stream);
        }

        return result;
    }

    public override void ChangeDatabase(string databaseName)
    {
        throw new NotImplementedException();
    }

    #endregion
}

