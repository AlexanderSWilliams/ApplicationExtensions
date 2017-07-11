using System.Data;

namespace Application.Data.ITransactionExtensions
{
    public static class ITransactionExtensions
    {
        public static void Commit(this ITransaction trans)
        {
            if (trans.Transaction != null)
                trans.Transaction.Commit();
            trans.Transaction = null;
        }

        public static void CreateOrContinueTransaction(this ITransaction trans, IsolationLevel level = IsolationLevel.ReadCommitted)
        {
            if (trans.Transaction == null)
            {
                var Connection = trans.GetConnection();
                Connection.VerifyOpenConnection();
                trans.Transaction = Connection.BeginTransaction(level);
            }
        }

        public static void Rollback(this ITransaction trans)
        {
            if (trans.Transaction != null)
                trans.Transaction.Rollback();
            trans.Transaction = null;
        }
    }
}