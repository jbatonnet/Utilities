using System;
using System.Collections.Generic;
using System.Text;

namespace System.Data.Common
{
    public static class DbConnectionExtensions
    {
        public static DbCommand CreateCommand(this DbConnection me, string commandText)
        {
            DbCommand command = me.CreateCommand();
            command.CommandText = commandText;
            return command;
        }
    }
}