---
name: inspection-ai-log
description: Maintain the inspection project's AI_LOG.md when recording project-related user prompts, follow-ups, corrections, and their outcomes. Do not use for unrelated conversations.
---

# 巡检项目 AI 使用记录

在本项目中记录用户主动发送给 AI 的项目相关问题和指令，包括追问和纠错。将记录按实际顺序追加到项目根目录的 `AI_LOG.md`；这里的记录是本地文件维护，不代表获得向外部服务上传或发送信息的授权。

每条使用下一个连续的 `P-编号`，包含目标、`Prompt 原文`和结果。逐字保留可见的用户原文，包括错字、换行及代码标记；不能用摘要代替原文。记录结果的采用、修改后采用或未采用状态，并简述实际验证方式。正在进行或尚未验证的内容应如实标明，不得编造。

检查已有记录，避免重复；如果无法访问早期对话原文，明确说明缺失，不能推测补写。IDE 逐字自动补全不需要记录。对 API Key、Token、账号、隐私和其他敏感信息使用明确的 `[REDACTED: ...]` 占位符，不写入秘密。

至少记录一次实际检查、修改或放弃 AI 结果的过程，说明原因和验证；如尚未发生，保留待补充状态。后续真实发生时更新相应记录。遵守项目现行的审查和 Git 工作流程。
