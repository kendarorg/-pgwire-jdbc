package org.kendar.pgwire.server;

import org.kendar.pgwire.commons.PgwByteBuffer;

import java.io.IOException;

public class AuthenticationOk implements PgwServerMessage{
    @Override
    public void write(PgwByteBuffer buffer) throws IOException {
        buffer.writeByte((byte) 'R'); // 'R' for AuthenticationRequest
        buffer.writeInt(8); // Length
        buffer.writeInt(0);
    }
}
