# 用户管理 API 文档

本文档描述了系统内部用户管理系统的核心接口。所有接口必须在头部携带鉴权 Token，格式为 `Authorization: Bearer {token}`。

## 接口列表

### 1. 获取用户列表
- **URL**: `GET /api/users`
- **参数**: 支持分页，如 `?page=1&size=20`。支持模糊搜索 `?keyword=张三`。
- **返回**: 包含用户核心信息的 JSON 数组及总条数 `total_count`。

### 2. 获取单个用户详情
- **URL**: `GET /api/users/{id}`
- **说明**: 通过用户的全局唯一 ID 获取其详细资料（包含脱敏后的密码 Hash 和注册时间）。

### 3. 创建用户
- **URL**: `POST /api/users`
- **Body 参数**:
  ```json
  {
      "name": "张三",
      "email": "zhangsan@example.com",
      "role": "Admin" // 可选: Admin, Editor, Viewer
  }
  ```

### 4. 更新用户信息
- **URL**: `PUT /api/users/{id}`
- **说明**: 仅允许更新 `name` 和 `email` 字段，不支持变更角色。

### 5. 删除用户
- **URL**: `DELETE /api/users/{id}`
- **说明**: 硬删除。删除后无法恢复，且会级联删除其名下的各类日志。
