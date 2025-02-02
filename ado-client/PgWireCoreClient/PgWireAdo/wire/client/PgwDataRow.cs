﻿using System.Buffers.Binary;
using PgWireAdo.utils;
using System.Collections;
using System.Text;

namespace PgWireAdo.wire.client
{
    public class PgwDataRow:PgwClientMessage
    {
        public override BackendMessageCode BeType => BackendMessageCode.DataRow;
        private List<RowDescriptor> _descriptors;

        private List<Object> _data = new();

        public PgwDataRow()
        {
            
        }

        public List<object> Data => _data;

        public List<RowDescriptor> Descriptors
        {
            get => _descriptors;
            set => _descriptors = value;
        }


        public override void Read(DataMessage stream)
        {
            ConsoleOut.WriteLine("[SERVER] Read: PgwDataRow");
            var colCount = stream.ReadInt16();
            for (var i = 0; i < colCount; i++)
            {
                var descriptor = _descriptors[i];
                var colLength = stream.ReadInt32();
                var data = stream.ReadBytes(colLength);
                if(colLength==0){
                    _data.Add(null);
                }
                else if (descriptor.FormatCode == 0) //text
                {
                    _data.Add(UTF8Encoding.Default.GetString(data));
                }
                else if (descriptor.FormatCode == 1) //binary
                {
                    var type = descriptor.DataTypeObjectId;
                    _data.Add(convertType(type,data));
                }
                else
                {
                    throw new Exception("Invalid format code " + descriptor.FormatCode);
                }
            }
        }

        private object convertType(int type, byte[] data)
        {
            var realType = (TypesOids)type;
            switch (realType)
            {
                case (TypesOids.Bool):
                    return data[0] > 0;
                case (TypesOids.Bytea):
                case (TypesOids.Varbit):
                    return data;
                case (TypesOids.BPChar):
                    return (char)data[0];
                default:
                    break;
            }

            return UTF8Encoding.Default.GetString(data);
        }
    }
}
