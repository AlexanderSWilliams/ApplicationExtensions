using System;
using System.Data.Common;

namespace Application.Data
{
    public interface ITransaction
    {
        Func<DbConnection> GetConnection { get; set; }

        DbTransaction Transaction { get; set; }
    }
}