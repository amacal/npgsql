// Npgsql.NpgsqlCopyIn.cs
//
// Author:
//     Kalle Hallivuori <kato@iki.fi>
//
//    Copyright (C) 2007 The Npgsql Development Team
//    npgsql-general@gborg.postgresql.org
//    http://gborg.postgresql.org/project/npgsql/projdisplay.php
//
//  Copyright (c) 2002-2007, The Npgsql Development Team
//
// Permission to use, copy, modify, and distribute this software and its
// documentation for any purpose, without fee, and without a written
// agreement is hereby granted, provided that the above copyright notice
// and this paragraph and the following two paragraphs appear in all copies.
//
// IN NO EVENT SHALL THE NPGSQL DEVELOPMENT TEAM BE LIABLE TO ANY PARTY
// FOR DIRECT, INDIRECT, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES,
// INCLUDING LOST PROFITS, ARISING OUT OF THE USE OF THIS SOFTWARE AND ITS
// DOCUMENTATION, EVEN IF THE NPGSQL DEVELOPMENT TEAM HAS BEEN ADVISED OF
// THE POSSIBILITY OF SUCH DAMAGE.
//
// THE NPGSQL DEVELOPMENT TEAM SPECIFICALLY DISCLAIMS ANY WARRANTIES,
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS FOR A PARTICULAR PURPOSE. THE SOFTWARE PROVIDED HEREUNDER IS
// ON AN "AS IS" BASIS, AND THE NPGSQL DEVELOPMENT TEAM HAS NO OBLIGATIONS
// TO PROVIDE MAINTENANCE, SUPPORT, UPDATES, ENHANCEMENTS, OR MODIFICATIONS.

using System.IO;

namespace Npgsql
{
    /// <summary>
    /// Represents a PostgreSQL COPY FROM STDIN operation with a corresponding SQL statement
    /// to execute against a PostgreSQL database
    /// and an associated stream used to read data from (if provided by user)
    /// or for writing it (when generated by driver).
    /// Eg. new NpgsqlCopyIn("COPY mytable FROM STDIN", connection, streamToRead).Start();
    /// </summary>
    public class NpgsqlCopyIn
    {
        private readonly NpgsqlConnector _context;
        private readonly NpgsqlCommand _cmd;
        private Stream _copyStream;
        private bool _disposeCopyStream; // user did not provide stream, so reset it after use

        /// <summary>
        /// Creates NpgsqlCommand to run given query upon Start(). Data for the requested COPY IN operation can then be written to CopyData stream followed by a call to End() or Cancel().
        /// </summary>
        public NpgsqlCopyIn(string copyInQuery, NpgsqlConnection conn)
            : this(new NpgsqlCommand(copyInQuery, conn), conn)
        {
        }

        /// <summary>
        /// Given command is run upon Start(). Data for the requested COPY IN operation can then be written to CopyData stream followed by a call to End() or Cancel().
        /// </summary>
        public NpgsqlCopyIn(NpgsqlCommand cmd, NpgsqlConnection conn)
            : this(cmd, conn, null)
        {
        }

        /// <summary>
        /// Given command is executed upon Start() and all data from fromStream is passed to it as copy data.
        /// </summary>
        public NpgsqlCopyIn(NpgsqlCommand cmd, NpgsqlConnection conn, Stream fromStream)
        {
            _context = conn.Connector;
            _cmd = cmd;
            _copyStream = fromStream;
        }

        /// <summary>
        /// Returns true if the connection is currently reserved for this operation.
        /// </summary>
        public bool IsActive
        {
            get { return _context != null && _context.CurrentState is NpgsqlCopyInState && _context.Mediator.CopyStream == _copyStream; }
        }

        /// <summary>
        /// The stream provided by user or generated upon Start().
        /// User may provide a stream to constructor; it is used to pass to server all data read from it.
        /// Otherwise, call to Start() sets this to a writable NpgsqlCopyInStream that passes all data written to it to server.
        /// In latter case this is only available while the copy operation is active and null otherwise.
        /// </summary>
        public Stream CopyStream
        {
            get { return _copyStream; }
        }

        /// <summary>
        /// Returns true if this operation is currently active and in binary format.
        /// </summary>
        public bool IsBinary
        {
            get { return IsActive && _context.CurrentState.CopyFormat.IsBinary; }
        }

        /// <summary>
        /// Returns true if this operation is currently active and field at given location is in binary format.
        /// </summary>
        public bool FieldIsBinary(int fieldNumber)
        {
            return IsActive && _context.CurrentState.CopyFormat.FieldIsBinary(fieldNumber);
        }

        /// <summary>
        /// Returns number of fields expected on each input row if this operation is currently active, otherwise -1
        /// </summary>
        public int FieldCount
        {
            get { return IsActive ? _context.CurrentState.CopyFormat.FieldCount : -1; }
        }

        /// <summary>
        /// The Command used to execute this copy operation.
        /// </summary>
        public NpgsqlCommand NpgsqlCommand
        {
            get { return _cmd; }
        }

        /// <summary>
        /// Set before a COPY IN query to define size of internal buffer for reading from given CopyStream.
        /// </summary>
        public int CopyBufferSize
        {
            get { return _context.Mediator.CopyBufferSize; }
            set { _context.Mediator.CopyBufferSize = value; }
        }

        /// <summary>
        /// Command specified upon creation is executed as a non-query.
        /// If CopyStream is set upon creation, it will be flushed to server as copy data, and operation will be finished immediately.
        /// Otherwise the CopyStream member can be used for writing copy data to server and operation finished with a call to End() or Cancel().
        /// </summary>
        public void Start()
        {
            if (_context.CurrentState is NpgsqlReadyState)
            {
                _context.Mediator.CopyStream = _copyStream;
                _cmd.ExecuteNonQuery();
                _disposeCopyStream = _copyStream == null;
                _copyStream = _context.Mediator.CopyStream;
                if (_copyStream == null && !(_context.CurrentState is NpgsqlReadyState))
                {
                    throw new NpgsqlException("Not a COPY IN query: " + _cmd.CommandText);
                }
            }
            else
            {
                throw new NpgsqlException("Copy can only start in Ready state, not in " + _context.CurrentState);
            }
        }

        /// <summary>
        /// Called after writing all data to CopyStream to successfully complete this copy operation.
        /// </summary>
        public void End()
        {
            if (_context != null)
            {
                try
                {
                    if (IsActive)
                    {
                        // Stop Notification thread so we can process this message.
                        // See bug 1010796
                        using (_context.BlockNotificationThread())
                        {
                            _context.CurrentState.SendCopyDone(_context);
                        }
                    }
                }
                finally
                {
                    if (_context.Mediator.CopyStream == _copyStream)
                    {
                        _context.Mediator.CopyStream = null;
                    }
                    if (_disposeCopyStream)
                    {
                        _copyStream = null;
                    }
                }
            }
        }

        /// <summary>
        /// Withdraws an already started copy operation. The operation will fail with given error message.
        /// Will do nothing if current operation is not active.
        /// </summary>
        public void Cancel(string message)
        {
            if (_context != null)
            {
                try
                {
                    if (IsActive)
                    {
                        // Stop Notification thread so we can process this message.
                        // See bug 1010796
                        using (_context.BlockNotificationThread())
                        {
                            _context.CurrentState.SendCopyFail(_context, message);
                        }
                    }
                }
                finally
                {
                    if (_context.Mediator.CopyStream == _copyStream)
                    {
                        _context.Mediator.CopyStream = null;
                    }
                    if (_disposeCopyStream)
                    {
                        _copyStream = null;
                    }
                }
            }
        }
    }
}