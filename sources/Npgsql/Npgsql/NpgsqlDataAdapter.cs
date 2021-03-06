// created on 1/8/2002 at 23:02
//
// Npgsql.NpgsqlDataAdapter.cs
//
// Author:
//  Francisco Jr. (fxjrlists@yahoo.com.br)
//
//  Copyright (C) 2002 The Npgsql Development Team
//  npgsql-general@gborg.postgresql.org
//  http://gborg.postgresql.org/project/npgsql/projdisplay.php
//
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

using System;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace Npgsql
{
    /// <summary>
    /// Represents the method that handles the <see cref="Npgsql.NpgsqlDataAdapter.RowUpdated">RowUpdated</see> events.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="NpgsqlRowUpdatedEventArgs">NpgsqlRowUpdatedEventArgs</see> that contains the event data.</param>
    public delegate void NpgsqlRowUpdatedEventHandler(Object sender, NpgsqlRowUpdatedEventArgs e);

    /// <summary>
    /// Represents the method that handles the <see cref="Npgsql.NpgsqlDataAdapter.RowUpdating">RowUpdating</see> events.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="NpgsqlRowUpdatingEventArgs">NpgsqlRowUpdatingEventArgs</see> that contains the event data.</param>
    public delegate void NpgsqlRowUpdatingEventHandler(Object sender, NpgsqlRowUpdatingEventArgs e);

    /// <summary>
    /// This class represents an adapter from many commands: select, update, insert and delete to fill <see cref="System.Data.DataSet">Datasets.</see>
    /// </summary>
    public sealed class NpgsqlDataAdapter : DbDataAdapter
    {
        // Log support
        private static readonly String CLASSNAME = MethodBase.GetCurrentMethod().DeclaringType.Name;

        /// <summary>
        /// Row updated event.
        /// </summary>
        public event NpgsqlRowUpdatedEventHandler RowUpdated;

        /// <summary>
        /// Row updating event.
        /// </summary>
        public event NpgsqlRowUpdatingEventHandler RowUpdating;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public NpgsqlDataAdapter()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="selectCommand"></param>
        public NpgsqlDataAdapter(NpgsqlCommand selectCommand)
        {
            NpgsqlEventLog.LogMethodEnter(LogLevel.Debug, CLASSNAME, CLASSNAME);
            SelectCommand = selectCommand;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="selectCommandText"></param>
        /// <param name="selectConnection"></param>
        public NpgsqlDataAdapter(String selectCommandText, NpgsqlConnection selectConnection)
            : this(new NpgsqlCommand(selectCommandText, selectConnection))
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="selectCommandText"></param>
        /// <param name="selectConnectionString"></param>
        public NpgsqlDataAdapter(String selectCommandText, String selectConnectionString)
            : this(selectCommandText, new NpgsqlConnection(selectConnectionString))
        {
        }

        /// <summary>
        /// Create row updated event.
        /// </summary>
        /// <param name="dataRow"></param>
        /// <param name="command"></param>
        /// <param name="statementType"></param>
        /// <param name="tableMapping"></param>
        /// <returns></returns>
        protected override RowUpdatedEventArgs CreateRowUpdatedEvent(DataRow dataRow, IDbCommand command,
                                                                     StatementType statementType,
                                                                     DataTableMapping tableMapping)
        {
            NpgsqlEventLog.LogMethodEnter(LogLevel.Debug, CLASSNAME, "CreateRowUpdatedEvent");
            return new NpgsqlRowUpdatedEventArgs(dataRow, command, statementType, tableMapping);
        }

        /// <summary>
        /// Create row updating event.
        /// </summary>
        /// <param name="dataRow"></param>
        /// <param name="command"></param>
        /// <param name="statementType"></param>
        /// <param name="tableMapping"></param>
        /// <returns></returns>
        protected override RowUpdatingEventArgs CreateRowUpdatingEvent(DataRow dataRow, IDbCommand command,
                                                                       StatementType statementType,
                                                                       DataTableMapping tableMapping)
        {
            NpgsqlEventLog.LogMethodEnter(LogLevel.Debug, CLASSNAME, "CreateRowUpdatingEvent");
            return new NpgsqlRowUpdatingEventArgs(dataRow, command, statementType, tableMapping);
        }

        /// <summary>
        /// Raise the RowUpdated event.
        /// </summary>
        /// <param name="value"></param>
        protected override void OnRowUpdated(RowUpdatedEventArgs value)
        {
            NpgsqlEventLog.LogMethodEnter(LogLevel.Debug, CLASSNAME, "OnRowUpdated");
            //base.OnRowUpdated(value);
            if ((RowUpdated != null) && (value is NpgsqlRowUpdatedEventArgs))
            {
                RowUpdated(this, (NpgsqlRowUpdatedEventArgs)value);
            }
        }

        /// <summary>
        /// Raise the RowUpdating event.
        /// </summary>
        /// <param name="value"></param>
        protected override void OnRowUpdating(RowUpdatingEventArgs value)
        {
            NpgsqlEventLog.LogMethodEnter(LogLevel.Debug, CLASSNAME, "OnRowUpdating");
            if ((RowUpdating != null) && (value is NpgsqlRowUpdatingEventArgs))
            {
                RowUpdating(this, (NpgsqlRowUpdatingEventArgs)value);
            }
        }

        /// <summary>
        /// Delete command.
        /// </summary>
        public new NpgsqlCommand DeleteCommand
        {
            get
            {
                NpgsqlEventLog.LogPropertyGet(LogLevel.Debug, CLASSNAME, "NpgDataAdapter.DeleteCommand");
                return (NpgsqlCommand)base.DeleteCommand;
            }

            set { base.DeleteCommand = value; }
        }

        /// <summary>
        /// Select command.
        /// </summary>
        public new NpgsqlCommand SelectCommand
        {
            get
            {
                NpgsqlEventLog.LogPropertyGet(LogLevel.Debug, CLASSNAME, "NpgDataAdapter.SelectCommand");
                return (NpgsqlCommand)base.SelectCommand;
            }

            set { base.SelectCommand = value; }
        }

        /// <summary>
        /// Update command.
        /// </summary>
        public new NpgsqlCommand UpdateCommand
        {
            get
            {
                NpgsqlEventLog.LogPropertyGet(LogLevel.Debug, CLASSNAME, "NpgDataAdapter.UpdateCommand");
                return (NpgsqlCommand)base.UpdateCommand;
            }

            set { base.UpdateCommand = value; }
        }

        /// <summary>
        /// Insert command.
        /// </summary>
        public new NpgsqlCommand InsertCommand
        {
            get
            {
                NpgsqlEventLog.LogPropertyGet(LogLevel.Debug, CLASSNAME, "NpgDataAdapter.InsertCommand");
                return (NpgsqlCommand)base.InsertCommand;
            }

            set { base.InsertCommand = value; }
        }
    }
}

#pragma warning disable 1591

public class NpgsqlRowUpdatingEventArgs : RowUpdatingEventArgs
{
    public NpgsqlRowUpdatingEventArgs(DataRow dataRow, IDbCommand command, StatementType statementType,
                                      DataTableMapping tableMapping)
        : base(dataRow, command, statementType, tableMapping)

    {
    }
}

public class NpgsqlRowUpdatedEventArgs : RowUpdatedEventArgs
{
    public NpgsqlRowUpdatedEventArgs(DataRow dataRow, IDbCommand command, StatementType statementType,
                                     DataTableMapping tableMapping)
        : base(dataRow, command, statementType, tableMapping)

    {
    }
}

#pragma warning restore 1591