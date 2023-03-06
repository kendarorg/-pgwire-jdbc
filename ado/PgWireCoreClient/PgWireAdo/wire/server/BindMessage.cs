﻿using System.Buffers.Binary;
using System.Data;
using System.Data.Common;
using PgWireAdo.ado;
using PgWireAdo.utils;
using TB.ComponentModel;

namespace PgWireAdo.wire.server;

public class BindMessage : PgwServerMessage
{
    private String _destinationPortal;
    private String _sourcePsName;
    private List<PgwParameter> _parameters = new ();
    private List<PgwParameter> _results = new();

    public BindMessage(string statementId, string commandText, DbParameterCollection parameters)
    {
        _sourcePsName = statementId;
        _destinationPortal = commandText;
        foreach (DbParameter dbParameter in parameters)
        {
            if (dbParameter.Direction == ParameterDirection.Input)
            {
                _parameters.Add((PgwParameter)dbParameter);
            }
        }

    }


    public override void Write(PgwByteBuffer stream)
    {
        ConsoleOut.WriteLine("[SERVER] Read: BindMessage " + _sourcePsName+" portal: "+ _destinationPortal);
        if (_sourcePsName == null) throw new InvalidOperationException("Missing query");

        var parsLengths = 0;
        foreach (var pgwParameter in _parameters)
        {
            if (pgwParameter == null || pgwParameter.Value == null)
            {
                parsLengths += 2+4;
            }
            else if (pgwParameter.Value.GetType() == typeof(string))
            {
                var d = EncodingUtils.GetUTF8((String)pgwParameter.Value);
                parsLengths += 2+4+ d.Length;
            }
            else if (pgwParameter.Value.GetType() == typeof(byte[]))
            {
                parsLengths += 2 + 4 + ((byte[])pgwParameter.Value).Length;
            }
            else if (pgwParameter.Value.GetType() == typeof(char[]))
            {
                parsLengths += 2 + 4 + ((char[])pgwParameter.Value).Length;
            }
            else
            {
                var stringValue = pgwParameter.Value.As<string>();
                parsLengths += 2+4 + stringValue.Value.Length;
            }

        }

        int length = 4 + _sourcePsName.Length + 1 + _destinationPortal.Length + 1 +
                     //2 + _parameters.Count * 2 +
                     2 + 2+ parsLengths +
                     2 + _results.Count * 2;
        stream.WriteByte((byte)'B');
        stream.WriteInt32(length);
        stream.WriteASCIIString(_destinationPortal);
        stream.WriteByte(0);
        stream.WriteASCIIString(_sourcePsName);
        stream.WriteByte(0);
        stream.WriteInt16((short)_parameters.Count);

        foreach (var pgwParameter in _parameters)
        {
            if (pgwParameter == null || pgwParameter.Value == null)
            {
                stream.WriteInt16(0);//Text
            }
            else if (pgwParameter.Value.GetType() == typeof(string))
            {
                
                stream.WriteInt16(0);//Text
            }
            else if (pgwParameter.Value.GetType() == typeof(byte[]))
            {
                stream.WriteInt16(1);//Binary
            }
            else if (pgwParameter.Value.GetType() == typeof(char[]))
            {
                stream.WriteInt16(1);//Binary
            }
            else
            {
                stream.WriteInt16(0);//Text
            }

        }
        stream.WriteInt16((short)_parameters.Count);
        foreach (var pgwParameter in _parameters)
        {
            if (pgwParameter == null || pgwParameter.Value == null)
            {
                stream.WriteInt32(0);
            }
            else if(pgwParameter.Value.GetType() == typeof(string))
            {
                var d = EncodingUtils.GetUTF8((String)pgwParameter.Value);
                stream.WriteInt32(d.Length);
                stream.Write(d.Data);
            }
            else if (pgwParameter.Value.GetType() == typeof(byte[]))
            {
                stream.WriteInt32(((byte[])pgwParameter.Value).Length);
                stream.Write((byte[])pgwParameter.Value);
            }
            else if (pgwParameter.Value.GetType() == typeof(char[]))
            {
                var bval = ((char[])pgwParameter.Value).Select(c => (byte)c).ToArray();
                stream.WriteInt32(bval.Length);
                stream.Write(bval);
            }
            else
            {
                var stringValue = pgwParameter.Value.As<string>();
                stream.WriteInt32(stringValue.Value.Length);
                stream.WriteASCIIString(stringValue.Value);
            }

        }
        stream.WriteInt16((short)_results.Count);
        foreach (var pgwParameter in _results)
        {
            if (pgwParameter == null || pgwParameter.Value == null)
            {
                stream.WriteInt16(0);//Text
            }
            else if (pgwParameter.Value.GetType() == typeof(string))
            {
                stream.WriteInt16(0);//Text
            }
            else if (pgwParameter.Value.GetType() == typeof(byte[]))
            {
                stream.WriteInt16(1);//Binary
            }
            else if (pgwParameter.Value.GetType() == typeof(char[]))
            {
                stream.WriteInt16(1);//Binary
            }
            else
            {
                stream.WriteInt16(0);//Text
            }
        }
    }

    
}