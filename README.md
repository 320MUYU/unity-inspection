# 虚拟现场巡检问题标注

本项目依据 Unity 研发实操题的无 AR 设备备选版，构建巡检问题上报和处理的最小闭环。巡检人员在虚拟现场标记问题并提交，管理人员在网页查看问题并更新状态。

## 技术方案

- 巡检端：Unity 6 `6000.0.85f1` LTS、C#，在本机 Unity Editor 或桌面环境运行。
- 管理端：React `18.3.1`、TypeScript `5.7.3`、Vite `6.0.7`，通过 Docker 在本地运行。
- 后端：Go 1.23，通过 Docker 在本地运行，使用 JSON 文件持久化保存问题数据。
- Unity 与浏览器管理端访问同一个本地 Go 后端。后端绑定 `http://localhost:18765`，管理页面绑定 `http://localhost:3000`。

## 当前进度

Go 后端、React 管理页面、Unity 固定机房场景和问题上报已实现。巡检端支持鼠标直接转动视角、中心准星标记、移动碰撞、设备识别和未解决标记恢复；管理端支持查看设备、更新状态和手动删除已解决的问题。机房入口已补充隐藏碰撞边界，阻止角色退入未建模区域。

已有 Unity、React 测试和隔离环境中的三端联调记录；最新边界回归为 Play Mode 9 项通过、1 项隔离网络测试未运行。Windows 构建已通过启动检查。两段演示录像通过 [GitHub Releases](https://github.com/320MUYU/unity-inspection/releases/tag/demo-videos-2026-10-30) 提供。

## 项目目录

```text
backend/       Go 接口、问题持久化与测试
web/           React 管理页面与测试
unity/         Unity 场景、巡检交互与测试
视频讲解/      本机原始演示视频，在线下载见 Releases
compose.yaml   后端和管理端的本地容器配置
AI_LOG.md      既有 Prompt 原文、结果及末尾总结与审查实例
```

## 本地启动

### 1. 启动后端和管理端

启动 Docker Desktop，在项目根目录执行：

```powershell
docker compose up -d --build
```

- 管理页面：[http://localhost:3000](http://localhost:3000)
- 后端健康检查：[http://localhost:18765/health](http://localhost:18765/health)
- 问题列表接口：[http://localhost:18765/issues](http://localhost:18765/issues)
- 查看容器状态：`docker compose ps`

后端代码编译进容器镜像；修改 Go 代码后，在项目根目录重新构建并更新后端：

```powershell
docker compose up -d --build --no-deps backend
```

问题文件保存于 `inspection-data` 数据卷中的 `/data/issues.json`，通过 `DATA_FILE` 环境变量指定。已有记录会在后端启动时加载。

停止服务时执行 `docker compose down`。不要添加 `-v`，因为该参数会删除保存问题数据的 Docker 数据卷。

### 2. 启动 Unity 巡检端

在 Unity Hub 中使用 Unity 6 `6000.0.85f1` 打开项目根目录下的 `unity` 文件夹，通过菜单 `Inspection > Open inspection scene` 打开巡检场景，再进入 Play 模式。

本机已生成的 Windows 测试构建位于 `unity/Builds/Inspection/Inspection.exe`。运行时应保留同目录下的其他构建文件；`Builds` 不纳入源码提交，其他机器需自行构建。

Unity 场景和 UI 在 Play 模式启动时创建。运行后端和管理页面不会自动启动 Unity，需单独进入 Play 模式或启动 Windows 构建。

## 巡检操作

1. 使用 `WASD` 移动，直接移动鼠标观察场景。
2. 将屏幕中央准星对准设备或场景表面，点击鼠标左键放置标记。
3. 在弹出的表单中确认标记对象，填写必填标题、可选描述和优先级，点击“提交问题”。机柜、配电箱会显示对应名称；地面和墙面显示“场景表面”。提交失败时表单会保留内容，可检查后端后重试。
4. 在管理页面刷新问题列表，查看设备和上报记录并修改处理状态。问题变为“已解决”后，Unity 中对应标记会在下一次同步时移除；若重新改回待处理或处理中，标记会恢复。
5. 已解决问题可在管理页面点击“删除”，确认后永久删除；其他状态的问题不能删除。

按 `Esc` 或窗口失焦会释放鼠标；点击画面可恢复巡检，该次点击不会创建标记。表单打开时会释放鼠标并忽略场景点击；取消时移除未保存标记。角色移动会受到场景碰撞体阻挡；入口隐藏边界覆盖中央及两侧角落，不响应巡检标注射线。

### 固定场景与标记恢复

进入场景时 Unity 立即请求问题列表，之后每次请求结束等待 3 秒再同步。`open` 和 `in_progress` 问题显示青绿色球形标记，`resolved` 或已从列表删除的问题移除对应标记；网络或数据异常时保留现有标记，并在界面提示同步失败。

恢复位置直接使用问题的 `position.x/y/z` 世界坐标，问题 ID 用于避免重复创建标记；`deviceId` 和 `deviceName` 用于记录设备归属。当前使用同一个固定机房布局，修改模型位置或尺寸后，已有坐标需要重新核对。旧问题若没有记录设备信息，管理页面显示“未记录设备”。配电箱优化前保存的坐标可能不再紧贴其表面。

当前后端入口包含演示数据初始化：**启动时问题列表为空**，就创建一条“机柜 A-01 温度异常”，位置为 `(-2.8, 1.3, 5.01)`，初始状态为 `open`。Unity 成功同步后，左侧 A-01 机柜门中部会出现标记。列表非空时跳过初始化，不会覆盖已有记录；删除全部问题后再重启后端，会重新生成演示记录。

## 检查与测试

以下结果依据已有测试报告和 AI_LOG 中的验证记录，文档更新时未重新运行 Unity 或完整三端联调：

- Unity Edit Mode：已有报告为 13 项通过。
- 最近边界回归：Play Mode 共 10 项，9 项通过、0 项失败、1 项隔离网络测试未运行。入口中央、两侧角落阻挡和隐藏边界不被标注的测试通过。
- 此前完整联调：隔离环境中验证 Unity 上报 A-01 问题、网页解决后对应标记消失、网页手动删除及后端重启后该记录不存在；网页请求被路由至隔离后端。该结果与最近边界回归分别记录。
- 管理端：已有 6 项测试及生产构建通过记录。在 `web` 目录安装依赖后，执行 `npm test` 和 `npm run build`。
- Windows 构建：边界修复后重新构建成功，并通过启动日志检查；实际鼠标手感可在桌面运行时体验。

后端本地检查需要 Go 1.23 或兼容版本，在 `backend` 目录执行：

```powershell
go test ./...
go vet ./...
```

通过 Docker 启动项目不要求本机安装 Go。Unity 中打开 `Window > General > Test Runner` 可运行 Edit Mode 和 Play Mode 测试。隔离网络测试另需设置 `INSPECTION_RUN_INTEGRATION=1`，并启动监听 `18766` 端口的隔离后端；未启用时该项显示跳过。

已有 Unity 批处理测试环境曾需要在启动 Unity 前临时设置 `ALLUSERSPROFILE` 为 `ProgramData`。若遇到 Package Manager 报 `The "path" argument must be of type string. Received undefined`，可检查启动进程是否缺少该变量；该历史环境问题不代表所有机器都需要修改此变量。

## 后端接口

| 方法与路径 | 用途 |
| --- | --- |
| `GET /health` | 检查后端服务状态 |
| `POST /issues` | 创建巡检问题 |
| `GET /issues` | 获取问题列表 |
| `PATCH /issues/{id}/status` | 更新指定问题的处理状态 |
| `DELETE /issues/{id}` | 永久删除已解决问题；其他状态返回冲突错误 |

创建问题时提交 `title`、`description`、`priority` 和包含 `x`、`y`、`z` 的 `position`，并可提交 `deviceId`、`deviceName`；优先级为 `low`、`medium`、`high`。状态为 `open`、`in_progress`、`resolved`，例如更新请求体为 `{"status":"resolved"}`。错误响应包含 `error` 字段。

## 演示视频

两段原始 MP4 可在 [演示录像（2026-10-30）](https://github.com/320MUYU/unity-inspection/releases/tag/demo-videos-2026-10-30) 的 Assets 中下载。

| 视频 | 时长 | 下载 |
| --- | --- | --- |
| 演示视频 1 | 约 1 分 35 秒 | [下载 MP4](https://github.com/320MUYU/unity-inspection/releases/download/demo-videos-2026-10-30/Desktop.2026.10.30.-.19.47.29.03.mp4) |
| 演示视频 2 | 3 分 58 秒 | [下载 MP4](https://github.com/320MUYU/unity-inspection/releases/download/demo-videos-2026-10-30/Desktop.2026.10.30.-.20.05.43.06.mp4) |

视频作为 Release 附件提供，Git 克隆和自动生成的源码压缩包不包含视频；本机 `视频讲解/` 保留原始文件。

## AR 基础问题

1. 使用 AR Foundation 的 `ARPlaneManager` 检测平面，使用 `ARRaycastManager` 将屏幕点击转换为平面命中位姿，再放置对象；具体能力取决于设备和 AR 提供程序。
2. Unity 坐标属于当前会话的坐标系，重启后原点和方向可能改变。仅保存 xyz 不能保证真实位置不变；需要对应平台的持久化锚点、重定位或其他空间配准方案。
3. 初始化失败或设备不支持时，应说明原因并提供重试、退出或替代方式；Tracking 异常时提示用户改善光照、移动设备或等待恢复，避免把不可靠位置当作准确结果。
