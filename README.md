# 虚拟现场巡检问题标注

本项目依据 Unity 研发实操题的无 AR 设备备选版，构建巡检问题上报和处理的最小闭环。巡检人员在虚拟现场标记问题并提交，管理人员在网页查看问题并更新状态。

## 技术方案

- 巡检端：Unity 6 `6000.0.85f1` LTS、C#，在本机 Unity Editor 或桌面环境运行。
- 管理端：React、TypeScript，计划使用 Docker 在本地运行。
- 后端：Go，计划使用 Docker 在本地运行，持久化保存问题数据。
- Unity 与浏览器管理端访问同一个本地 Go 后端。后端绑定 `http://localhost:18765`，管理页面绑定 `http://localhost:3000`。

## 当前进度

Go 后端、React 管理页面和 Docker Compose 已实现并验证；后端使用 Go 1.23 和 JSON 数据卷，管理端使用 React 18、TypeScript 5.7、Vite 6。Unity 项目正在准备，编辑器和许可证已就绪，但包管理器报错阻止导入；Play 模式、Windows 构建和三端闭环尚待验证。演示视频尚未录制。

## 启动与接口

启动 Docker Desktop，在项目根目录执行 `docker compose up -d --build`，再访问 `http://localhost:3000`。停止服务执行 `docker compose down`；不要添加 `-v`，否则会删除持久化数据卷。

在 Unity Hub 中使用 Unity 6 `6000.0.85f1` 打开 `unity` 目录。编辑器首次导入时会尝试创建巡检场景；可通过 `Inspection > Open inspection scene` 打开场景并进入 Play 模式。该步骤尚未在编辑器中验证。

计划操作为 WASD 移动、按住鼠标右键转动视角、左键点击场景放置标记。填写面板打开时忽略场景点击，取消时移除未保存的标记。

后端接口：`GET /health` 检查服务，`POST /issues` 创建问题，`GET /issues` 查询列表，`PATCH /issues/{id}/status` 修改状态，例如请求体 `{"status":"resolved"}`。创建时提交 `title`、`description`、`priority` 和包含 `x`、`y`、`z` 的 `position`；优先级为 `low`、`medium`、`high`，状态为 `open`、`in_progress`、`resolved`。错误响应包含 `error` 字段。

## 待确认事项

- 演示要求中的“真机”是否包含普通电脑上的 Unity Editor 或桌面程序。
- 按当前决定，填写面板打开时为模态状态，忽略场景点击。

## 交付要求

提交三端源码、本 README、AI_LOG.md，以及两段各 3～5 分钟的演示视频。视频需覆盖现场观察、标记及提交问题、网页查看和修改状态、后端重启后数据保留。Unity 项目应保留 Assets、Packages 和 ProjectSettings；不必提交 Library、Temp 和 Logs 等自动生成目录。

## AR 基础问题

1. 使用 AR Foundation 的 `ARPlaneManager` 检测平面，使用 `ARRaycastManager` 将屏幕点击转换为平面命中位姿，再放置对象；具体能力取决于设备和 AR 提供程序。
2. Unity 坐标属于当前会话的坐标系，重启后原点和方向可能改变。仅保存 xyz 不能保证真实位置不变；需要对应平台的持久化锚点、重定位或其他空间配准方案。
3. 初始化失败或设备不支持时，应说明原因并提供重试、退出或替代方式；Tracking 异常时提示用户改善光照、移动设备或等待恢复，避免把不可靠位置当作准确结果。
