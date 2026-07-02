# CoreDemo3 - 访客管理系统

基于.NET 6 + SQLite的访客信息登记和通行码生成系统，适用于自助终端场景。

## 项目概述

配合前台自助终端，实现访客信息登记和通行码生成功能，访客凭通行码可通过门禁系统。

## 功能特性

### 核心功能
- ✅ **访客登记**：保存访客信息（姓名、手机号、身份证、来访事由、被访人）
- ✅ **通行码生成**：生成唯一的6位数字通行码，有效期为当天23:59:59
- ✅ **通行码验证**：门禁系统调用，验证通行码是否有效
- ✅ **记录查询**：查询某时间段内的来访记录

### 系统特性
- 📱 **响应式设计**：支持桌面和移动设备
- 🔒 **数据验证**：完整的输入验证和错误处理
- 📊 **统计报表**：实时统计访客数据
- 🖨️ **打印功能**：支持通行码打印
- 📥 **数据导出**：支持Excel格式导出

## 技术栈

- **后端**：.NET 6, Entity Framework Core, SQLite
- **前端**：HTML5, CSS3, JavaScript, Bootstrap 5
- **API文档**：Swagger/OpenAPI

## 项目结构

```
CoreDemo3/
├── Controllers/          # API控制器
│   └── VisitorController.cs
├── Data/                # 数据访问层
│   └── VisitorDbContext.cs
├── Models/              # 数据模型
│   ├── Visitor.cs
│   ├── VisitorRegisterRequest.cs
│   ├── VisitorRegisterResponse.cs
│   ├── AccessCodeValidationRequest.cs
│   └── AccessCodeValidationResponse.cs
├── Services/            # 业务逻辑层
│   ├── IVisitorService.cs
│   └── VisitorService.cs
└── wwwroot/            # 前端资源
    ├── index.html      # 访客登记页面
    ├── validate.html   # 通行码验证页面
    ├── records.html    # 记录查询页面
    ├── css/
    │   └── style.css   # 样式文件
    └── js/
        ├── register.js # 登记页面脚本
        ├── validate.js # 验证页面脚本
        └── records.js  # 查询页面脚本
```

## API接口

### 1. 访客登记
```http
POST /api/visitor/register
Content-Type: application/json

{
  "name": "张三",
  "phone": "13800138000",
  "idCard": "110101199001011234",
  "visitReason": "商务洽谈",
  "visitedPerson": "李四"
}
```

### 2. 通行码验证
```http
POST /api/visitor/validate
Content-Type: application/json

{
  "accessCode": "123456"
}
```

### 3. 记录查询
```http
GET /api/visitor/records?startTime=2023-01-01T00:00:00&endTime=2023-01-02T23:59:59
```

### 4. 统计信息
```http
GET /api/visitor/statistics?date=2023-01-01
```

## 数据库设计

### Visitor表（访客信息）
| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 主键 |
| Name | string | 姓名 |
| Phone | string | 手机号 |
| IdCard | string | 身份证号 |
| VisitReason | string | 来访事由 |
| VisitedPerson | string | 被访人 |
| AccessCode | string | 6位通行码 |
| ExpiryTime | datetime | 有效期 |
| Status | int | 状态(0:已登记,1:已入场,2:已离场,3:已过期) |
| CheckInTime | datetime? | 入场时间 |
| CheckOutTime | datetime? | 离场时间 |
| CreatedTime | datetime | 创建时间 |
| UpdatedTime | datetime | 更新时间 |

## 运行方式

### 1. 环境要求
- .NET 6.0 SDK
- Visual Studio 2022 或 Visual Studio Code

### 2. 启动项目
```bash
cd CoreDemo3/CoreDemo3
dotnet run
```

### 3. 访问应用
- 主页：http://localhost:5000 或 http://localhost:5001
- API文档：http://localhost:5000/swagger

## 使用指南

### 访客登记流程
1. 访客在自助终端填写基本信息
2. 系统验证输入信息格式
3. 生成6位数字通行码
4. 显示成功页面，包含通行码信息
5. 可选择打印通行码

### 通行码验证流程
1. 门禁系统输入6位数字通行码
2. 系统验证通行码有效性
3. 检查通行码是否过期
4. 更新访客状态为"已入场"
5. 返回验证结果和访客信息

### 记录查询功能
1. 设置时间范围查询条件
2. 支持按姓名、手机号、身份证号搜索
3. 查看详细访客信息和状态
4. 导出查询结果为Excel文件

## 部署说明

### 1. 发布应用
```bash
dotnet publish -c Release -o ./publish
```

### 2. 配置数据库
- SQLite数据库文件自动创建（visitor.db）
- 确保应用有数据库文件的读写权限

### 3. 配置端口
修改 `appsettings.json`：
```json
{
  "Urls": "http://0.0.0.0:80"
}
```

## 注意事项

1. **安全性**
   - 身份证号等信息在前端进行脱敏显示
   - API接口具备输入验证和错误处理
   - 通行码具有有效期限制

2. **性能考虑**
   - 数据库查询已添加索引优化
   - 前端搜索采用防抖处理
   - 支持分页查询大量数据

3. **扩展性**
   - 采用分层架构，易于扩展
   - 接口设计遵循RESTful规范
   - 支持与其他系统集成

## 开发说明

### 添加新功能
1. 在 `Models/` 中定义数据模型
2. 在 `Services/` 中实现业务逻辑
3. 在 `Controllers/` 中添加API接口
4. 在 `wwwroot/` 中添加前端页面

### 数据库迁移
如需修改数据库结构：
```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

## 联系方式

如有问题或建议，请联系开发团队。

---

**注意**：这是一个演示项目，生产环境使用请确保：
1. 加强安全验证措施
2. 添加用户认证和授权
3. 配置HTTPS协议
4. 添加日志记录和监控
5. 进行性能测试和优化