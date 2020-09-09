using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using System.Text.RegularExpressions;

namespace Happy_Apps_Core.Database
{
    public class SqliteInterceptor : IDbCommandInterceptor
    {
        private static readonly Regex CharIndexReplaceRegex = new Regex(@"\(CHARINDEX\((.*?),\s?(.*?)\)\)\s*?>\s*?0");

        public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
        }

        public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
        }

        public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
        }

        public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            ReplaceCharIndexFunc(command);
        }

        public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
        }

        public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            ReplaceCharIndexFunc(command);
        }

        private void ReplaceCharIndexFunc(DbCommand command)
        {
            //todo can remove?
            bool isMatch = false;
            var text = CharIndexReplaceRegex.Replace(command.CommandText, (match) =>
            {
	            if (!match.Success) return match.Value;
	            string paramsKey = match.Groups[1].Value;
	            string paramsColumnName = match.Groups[2].Value;
	            //replaceParams
	            /*foreach (DbParameter param in command.Parameters)
                    {
                        if (param.ParameterName == paramsKey.Substring(1))
                        {
                            param.Value = string.Format("%{0}%", param.Value);
                            break;
                        }
                    }*/
	            isMatch = true;
	            //return string.Format("{0} LIKE {1}", paramsColumnName, paramsKey);
	            return $"(INSTR({paramsColumnName},{paramsKey})) > 0";
            });
            if (isMatch)
                command.CommandText = text;
        }
    }
}
