using Dapper;
using Microsoft.Data.SqlClient;

namespace CoreDemo8
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var dateTime = DateTime.Now;
            var dateTimeStr = dateTime.ToString("yyyyMMddHH");
            if (dateTime.Minute < 2)
            {
                dateTimeStr = dateTime.AddMinutes(-2).ToString("yyyyMMddHH");
            }
            Console.WriteLine(dateTimeStr);

            var connStr = "Data Source=171.221.200.189,63436;Initial Catalog=CR_V11_ERP_XJND;uid=sa;pwd=Hysyyl@123*;PERSIST SECURITY INFO=True;TrustServerCertificate=True;";

            try
            {
                using var connection1 = new SqlConnection(connStr);

                // 👇 关键：显式打开，并捕获异常
                connection1.Open();

                Console.WriteLine("✅ 连接成功！");

                var result = connection1.Query("SELECT GETDATE() AS Now");
                Console.WriteLine($"DB 时间: {result.First().Now}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ 真实错误:");
                Console.WriteLine(ex.ToString());
            }

            var connection = new SqlConnection("Data Source=171.221.200.189,63436;Initial Catalog=CR_V11_ERP_XJND;uid=sa;pwd=Hysyyl@123*;PERSIST SECURITY INFO=True;TrustServerCertificate=True;");
            var list = connection.Query<object>($@"
SELECT top 500 'ERP_YYY_CZ'                                    AS ERPAppCode,
       [货主ID] + '_' + [商品ID] + '_' + IIF([批号] = '' OR [批号] IS NULL, '-', [批号]) + '_' + [批次号] + '_' +
       CONVERT(
           NVARCHAR(10),StockType
       )                                               AS ERPGoodsStockId,
       [商品ID]                                        AS ERPGoodsId                   --ERP商品Id
        ,
       [商品名称]                                      AS ERPGoodsName                 --ERP商品名称
        ,
       IIF([批号] = '' OR [批号] IS NULL, '-', [批号]) AS BatchNumber                  --批号
        ,
       [批次号]                                        AS BatchNo                      --批次号
        ,
       [数量]                                          AS Stock                        --库存量
        ,
       ISNULL([件装量], 0)                             AS PackingCapacity              --件装量
        ,
       CONVERT(
           NVARCHAR(50),
               (
                   CASE

                       WHEN LEN([有效期至]) = 7
                           AND ISDATE([有效期至] + '-01') = 1 THEN
                           DATEADD(DAY, - 1,
                                   DATEADD(MONTH, 1, CONVERT(DATETIME2, CONVERT(datetime, [有效期至] + '-01'))))
                       WHEN LEN([有效期至]) = 10
                           AND ISDATE([有效期至]) = 1 THEN
                           CONVERT(DATETIME2, CONVERT(datetime, [有效期至]))
                       WHEN LEN([有效期至]) = 6
                           AND ISDATE(LEFT([有效期至], 4) + '-' + RIGHT([有效期至], 2) + '-01') = 1 THEN
                           CONVERT(DATETIME2, CONVERT(datetime, LEFT([有效期至], 4) + '-' + RIGHT([有效期至], 2) + '-01'))
                       WHEN LEN([有效期至]) = 8
                           AND ISDATE(LEFT([有效期至], 4) + '-' + SUBSTRING([有效期至], 5, 2) + '-' +
                                      RIGHT([有效期至], 2)) = 1 THEN
                           CONVERT(
                               DATETIME2,
                                   CONVERT(
                                       datetime,
                                           LEFT([有效期至], 4) + '-' + SUBSTRING([有效期至], 5, 2) + '-' +
                                           RIGHT([有效期至], 2)
                                   )
                           )
                       ELSE CONVERT(DATETIME2, '9999-12-31')
                       END
                   ),
               23
       )                                               AS ValidityPeriod               --效期
        ,
       CONVERT(
           NVARCHAR(50),
               (
                   CASE

                       WHEN LEN([生产日期]) = 7
                           AND ISDATE([生产日期] + '-01') = 1 THEN
                           DATEADD(DAY, - 1,
                                   DATEADD(MONTH, 1, CONVERT(DATETIME2, CONVERT(datetime, [生产日期] + '-01'))))
                       WHEN LEN([生产日期]) = 10
                           AND ISDATE([生产日期]) = 1 THEN
                           CONVERT(DATETIME2, CONVERT(datetime, [生产日期]))
                       WHEN LEN([生产日期]) = 6
                           AND ISDATE(LEFT([生产日期], 4) + '-' + RIGHT([生产日期], 2) + '-01') = 1 THEN
                           CONVERT(DATETIME2, CONVERT(datetime, LEFT([生产日期], 4) + '-' + RIGHT([生产日期], 2) + '-01'))
                       WHEN LEN([生产日期]) = 8
                           AND ISDATE(LEFT([生产日期], 4) + '-' + SUBSTRING([生产日期], 5, 2) + '-' +
                                      RIGHT([生产日期], 2)) = 1 THEN
                           CONVERT(
                               DATETIME2,
                                   CONVERT(
                                       datetime,
                                           LEFT([生产日期], 4) + '-' + SUBSTRING([生产日期], 5, 2) + '-' +
                                           RIGHT([生产日期], 2)
                                   )
                           )
                       ELSE CONVERT(DATETIME2, '9999-12-31')
                       END
                   ),
               23
       )                                               AS ManufactureDate              --生产日期
        ,
       CONVERT(NVARCHAR(50), [入库日期], 23)           AS WarehousingDate              --入库日期
        ,
       StockType                                       AS StockType                    --库存类型
        ,
       'ERP_YYY_CZ'                                    AS WarehouseCode                --仓库编号
        ,
       NEWID()                                         AS ERPGoodsStockChangeRecordId, -- 幂等主键ID
       [货主ID]                                        AS ERPHZId                      --货主Id(货主编号)
        ,
       GETDATE()                                       AS [DataCollectionTime]         --数据采集时间

FROM (SELECT [商品ID],
             [商品名称],
             [仓库名称],
             [批次号],
             [货主ID],
             [架位ID],
             [批号],
             [生产日期],
             [有效期至],
             SUM([数量]) AS [数量],
             [入库日期],
             [仓库ID],
             StockType,
             [件装量]
      FROM (SELECT a.[商品ID],
                   a.[商品名称],
                   a.[仓库名称],
                   a.[批次号],
                   a.[货主ID],
                   a.[架位ID],
                   a.[批号],
                   a.[生产日期],
                   a.[有效期至],
                   a.[数量],
                   a.[入库日期],
                   a.[仓库ID],
                   b.StockType AS StockType,
                   [件装量]
            FROM [dbo].[phspkc_center_v] AS a
            INNER JOIN [dbo].[ckhzdzb] b ON a.[仓库ID] = b.ckid AND a.[货主Id] = b.hzid
            WHERE a.[货主Id] IN ('HZZ00000124','HZZ00000183','HZZ00000184','HZZ00000185','HZZ00000193')
            ) t
      GROUP BY [商品ID],
               [商品名称],
               [仓库名称],
               [批次号],
               [货主ID],
               [架位ID],
               [批号],
               [生产日期],
               [有效期至],
               [入库日期],
               [仓库ID],
               StockType,
               [件装量]) GoodsStocks
");
            var aa = list.AsList();
        }
    }
}
