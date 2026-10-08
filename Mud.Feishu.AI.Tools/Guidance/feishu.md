# 能力出处与元工具（feishu.*）

## 前置链
能力不在工具集 → `feishu.capability_lookup`；要某域深层避坑 → `feishu.guidance_read(domain, topic)`。

## 避坑
- `capability_lookup` 只回元数据，**不代表能调用**。
- 它报「能力存在但未策展」时**如实告知，禁止臆造 API 调用**。

## 示例
能发卡片吗 → `capability_lookup` → 如实告知结果。