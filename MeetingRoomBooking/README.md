# 会议室预约管理系统

基于 .NET 6 + SQLite + 自定义前端框架的会议室预约管理系统。

## 功能特性

### 用户认证
- ✅ 用户注册/登录
- ✅ JWT Token 认证
- ✅ 角色权限管理（普通员工/行政管理员）
- ✅ 登录状态持久化

### 会议室管理（行政管理员）
- ✅ 新增/编辑/删除会议室
- ✅ 设置会议室容量、设备信息
- ✅ 配置可预约时间段
- ✅ 查看会议室使用记录

### 会议预约（普通员工）
- ✅ 查看所有会议室列表
- ✅ 提交预约申请
- ✅ 查看个人预约记录
- ✅ 取消预约（会议开始前2小时内不可取消）

### 审批与冲突控制
- ✅ 管理员审批预约
- ✅ 自动检测时间冲突
- ✅ 实时查看会议室空闲时间

### 数据统计（管理员）
- ✅ 会议室使用率统计
- ✅ 高频使用部门排行
- ✅ 可视化数据展示

## 技术栈

- **后端框架**: ASP.NET Core 6.0
- **数据库**: SQLite + Entity Framework Core
- **认证**: JWT Bearer Token
- **API文档**: Swagger/OpenAPI
- **前端**: 原生 JavaScript + CSS + HTML
- **密码加密**: BCrypt.Net

## 项目结构

```
MeetingRoomBooking/
├── Controllers/           # API控制器
│   ├── AuthController.cs         # 认证相关API
│   ├── MeetingRoomsController.cs # 会议室管理API
│   ├── BookingsController.cs     # 预约管理API
│   └── AdminController.cs        # 管理员功能API
├── Models/                # 数据模型
│   ├── Entities.cs               # 实体类
│   └── DTOs.cs                   # 数据传输对象
├── Data/                  # 数据层
│   └── ApplicationDbContext.cs    # EF上下文
├── Services/              # 业务服务
│   └── JwtService.cs             # JWT服务
├── Extensions/            # 扩展方法
│   └── ServiceExtensions.cs       # 服务配置
├── wwwroot/               # 静态文件
│   ├── index.html                 # 登录/注册页面
│   ├── dashboard.html             # 管理后台
│   └── js/
│       └── dashboard.js           # 前端逻辑
├── Program.cs             # 程序入口
└── appsettings.json      # 配置文件
```

## 快速开始

### 1. 还原依赖

```bash
cd MeetingRoomBooking
dotnet restore
```

### 2. 运行项目

```bash
dotnet run
```

项目将在 `http://localhost:5000` 启动。

### 3. 访问系统

- **前端页面**: http://localhost:5000
- **Swagger API文档**: http://localhost:5000

### 4. 默认账号

系统会自动创建一个管理员账号：
- **用户名**: admin
- **密码**: admin123
- **角色**: 行政管理员

## API 端点

### 认证相关
- `POST /api/auth/register` - 用户注册
- `POST /api/auth/login` - 用户登录
- `GET /api/auth/me` - 获取当前用户信息

### 会议室管理
- `GET /api/meetingrooms` - 获取所有会议室
- `GET /api/meetingrooms/{id}` - 获取单个会议室
- `POST /api/meetingrooms` - 创建会议室（管理员）
- `PUT /api/meetingrooms/{id}` - 更新会议室（管理员）
- `DELETE /api/meetingrooms/{id}` - 删除会议室（管理员）
- `GET /api/meetingrooms/{id}/usage` - 查看使用记录（管理员）

### 预约管理
- `GET /api/bookings/availability` - 查看空闲时间
- `POST /api/bookings` - 创建预约
- `GET /api/bookings/my` - 我的预约
- `GET /api/bookings/{id}` - 获取预约详情
- `POST /api/bookings/{id}/cancel` - 取消预约

### 管理员功能
- `GET /api/admin/bookings/pending` - 获取待审批预约
- `GET /api/admin/bookings` - 获取所有预约
- `POST /api/admin/bookings/{id}/approve` - 审批预约
- `GET /api/admin/statistics` - 数据统计

## 配置说明

### JWT 配置 (appsettings.json)

```json
{
  "Jwt": {
    "Key": "YourSecretKeyHere",
    "Issuer": "MeetingRoomBookingSystem"
  }
}
```

### 数据库配置

系统使用 SQLite 数据库，数据库文件 `meetingroom.db` 会自动创建在项目根目录。

## 业务规则

1. **时间冲突检测**: 提交预约时自动检测时间冲突
2. **取消限制**: 会议开始前2小时内不可取消预约
3. **审批流程**: 员工提交预约后需管理员审批
4. **预约时间**: 不可预约过去的时间
5. **状态管理**: 预约状态包括待审批、已批准、已拒绝、已取消

## 数据库设计

### 主要实体

- **User**: 用户表（用户名、邮箱、密码哈希、部门、角色）
- **MeetingRoom**: 会议室表（名称、位置、容量、设备、可预约时间）
- **Booking**: 预约表（用户、会议室、时间、状态、审批人）
- **RoomUsageLog**: 使用记录表（用于统计分析）

## 安全特性

- ✅ 密码使用 BCrypt 加密存储
- ✅ JWT Token 认证
- ✅ 基于角色的访问控制（RBAC）
- ✅ API 接口权限验证
- ✅ CORS 配置

## 开发说明

### 添加新功能

1. 在 `Models/Entities.cs` 中定义实体
2. 在 `Data/ApplicationDbContext.cs` 中添加 DbSet
3. 创建对应的 Controller
4. 在前端添加相应的页面和交互逻辑

### 代码规范

- 遵循 SOLID 原则
- 使用 DTO 进行数据传输
- 统一的 API 响应格式
- 详细的代码注释

## 许可证

MIT License

## 作者

Created with Claude Code
