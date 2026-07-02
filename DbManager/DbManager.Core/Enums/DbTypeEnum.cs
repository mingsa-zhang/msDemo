using System.ComponentModel.DataAnnotations;

namespace DbManager.Core.Enums;

public enum DbTypeEnum
{
    [Display(Name = "MySQL")]
    MySql = 1,

    [Display(Name = "MariaDB")]
    MariaDB = 2,

    [Display(Name = "SQL Server")]
    SqlServer = 3,

    [Display(Name = "PostgreSQL")]
    PostgreSQL = 4,

    [Display(Name = "Oracle")]
    Oracle = 5,

    [Display(Name = "SQLite")]
    SQLite = 6,

    [Display(Name = "MongoDB")]
    MongoDB = 7,

    [Display(Name = "Redis")]
    Redis = 8,

    [Display(Name = "DB2")]
    DB2 = 9
}
