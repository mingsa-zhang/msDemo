using DbManager.Core.Dialects;
using DbManager.Core.Enums;
using Xunit;

namespace DbManager.Tests;

/// <summary>
/// 类型映射（原生类型 → 逻辑类型）单元测试，重点覆盖本会话修正的 CLOB 归类。
/// </summary>
public class DbTypeMapperTests
{
    private static readonly DefaultDbTypeMapper Mapper = DefaultDbTypeMapper.For(DbTypeEnum.Oracle);

    [Theory]
    [InlineData("clob", LogicalTypeEnum.Text)]
    [InlineData("NCLOB", LogicalTypeEnum.Text)]
    [InlineData("varchar(50)", LogicalTypeEnum.Text)]
    [InlineData("text", LogicalTypeEnum.Text)]
    [InlineData("blob", LogicalTypeEnum.Binary)]
    [InlineData("bytea", LogicalTypeEnum.Binary)]
    [InlineData("int", LogicalTypeEnum.Number)]
    [InlineData("decimal(10,2)", LogicalTypeEnum.Number)]
    [InlineData("datetime", LogicalTypeEnum.DateTime)]
    [InlineData("timestamp", LogicalTypeEnum.DateTime)]
    [InlineData("json", LogicalTypeEnum.Json)]
    [InlineData("uuid", LogicalTypeEnum.Guid)]
    [InlineData("uniqueidentifier", LogicalTypeEnum.Guid)]
    [InlineData("bit", LogicalTypeEnum.Boolean)]
    [InlineData("boolean", LogicalTypeEnum.Boolean)]
    public void ToLogicalType_MapsExpected(string native, LogicalTypeEnum expected)
    {
        Assert.Equal(expected, Mapper.ToLogicalType(native));
    }

    [Fact]
    public void ToLogicalType_Clob_IsNotBinary()
    {
        // 回归：CLOB 曾被误判为二进制，应为文本
        Assert.NotEqual(LogicalTypeEnum.Binary, Mapper.ToLogicalType("clob"));
    }
}
