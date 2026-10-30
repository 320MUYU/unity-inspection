# 虚拟现场巡检问题标注

本项目依据 Unity 研发实操题的无 AR 设备备选版，构建巡检问题上报和处理的最小闭环。巡检人员在虚拟现场标记问题并提交，管理人员在网页查看问题并更新状态。

## 技术方案

- 巡检端：Unity 6 `6000.0.85f1` LTS、C#，在本机 Unity Editor 或桌面环境运行。
- 管理端：React、TypeScript，通过 Docker 在本地运行。
- 后端：Go，通过 Docker 在本地运行，持久化保存问题数据。
- Unity 与浏览器管理端访问同一个本地 Go 后端。后端绑定 `http://localhost:18765`，管理页面绑定 `http://localhost:3000`。

## 当前进度

Go 后端、React 管理页面、Unity 机房场景及问题上报已实现；后端使用 Go 1.23 和 JSON 数据卷，管理端使用 React 18、TypeScript 5.7、Vite 6。Unity Edit Mode 和 Play Mode 测试已通过；隔离联调中，同一条 Unity 提交的问题经管理页修改状态后，在后端重启后仍然存在。联调使用浏览器请求路由切换到隔离后端。Windows 构建已通过启动检查。完整流程仍需人工操作验收，演示视频尚未录制。

## 启动与接口

启动 Docker Desktop，在项目根目录执行 `docker compose up -d --build`，再访问 `http://localhost:3000`。停止服务执行 `docker compose down`；不要添加 `-v`，否则会删除持久化数据卷。

在 Unity Hub 中使用 Unity 6 `6000.0.85f1` 打开 `unity` 目录，通过 `Inspection > Open inspection scene` 打开场景并进入 Play 模式。已在本机生成的 Windows 测试构建位于 `unity/Builds/Inspection/Inspection.exe`；该目录属于构建产物，不纳入源码提交，其他机器需自行构建。

操作为 WASD 移动、直接移动鼠标转动视角、左键在屏幕中心准星命中的位置放置标记。按 `Esc` 或窗口失焦会释放鼠标；点击画面可恢复巡检，该次点击不会创建标记。填写面板打开时释放鼠标并忽略场景点击，取消时移除未保存的标记；提交失败会保留填写内容以便重试。已提交标记只保留在当前场景会话内；从后端恢复历史标记属于未实现的加分项。

机柜为落地高柜，配电箱为带高压标识、开关和线缆的较小箱体。此次调整保留了配电箱的中心位置，但表面尺寸发生变化，调整前保存的坐标可能不再紧贴其表面。

本次批处理测试环境需要在启动 Unity 前临时设置 `ALLUSERSPROFILE` 为 `ProgramData`；若遇到 Package Manager 报 `The "path" argument must be of type string. Received undefined`，可检查启动进程是否缺少该变量。该问题与实际桌面环境是否复现需分别确认。

后端接口：`GET /health` 检查服务，`POST /issues` 创建问题，`GET /issues` 查询列表，`PATCH /issues/{id}/status` 修改状态，例如请求体 `{"status":"resolved"}`。创建时提交 `title`、`description`、`priority` 和包含 `x`、`y`、`z` 的 `position`；优先级为 `low`、`medium`、`high`，状态为 `open`、`in_progress`、`resolved`。错误响应包含 `error` 字段。

## 待确认事项

- 演示要求中的“真机”是否包含普通电脑上的 Unity Editor 或桌面程序。
- 是否需要恢复场景中历史问题标记：任务书将此列为可选加分项，当前未实现。

## 交付要求

提交三端源码、本 README、AI_LOG.md，以及两段各 3～5 分钟的演示视频。视频需覆盖现场观察、标记及提交问题、网页查看和修改状态、后端重启后数据保留。Unity 项目应保留 Assets、Packages 和 ProjectSettings；不必提交 Library、Temp 和 Logs 等自动生成目录。

## AR 基础问题

1. 使用 AR Foundation 的 `ARPlaneManager` 检测平面，使用 `ARRaycastManager` 将屏幕点击转换为平面命中位姿，再放置对象；具体能力取决于设备和 AR 提供程序。
2. Unity 坐标属于当前会话的坐标系，重启后原点和方向可能改变。仅保存 xyz 不能保证真实位置不变；需要对应平台的持久化锚点、重定位或其他空间配准方案。
3. 初始化失败或设备不支持时，应说明原因并提供重试、退出或替代方式；Tracking 异常时提示用户改善光照、移动设备或等待恢复，避免把不可靠位置当作准确结果。
