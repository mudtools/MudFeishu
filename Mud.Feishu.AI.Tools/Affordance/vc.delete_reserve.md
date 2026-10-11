## WHEN

- 预约不再需要（会议取消、时段改期），需要释放会议室与时段。

## Avoid when

- 会议已开始、只想提前结束——用 `vc.end_meeting`。
- 只是想换会议室——先确认新时段可用（`vc.list_rooms`）再删旧预约，避免时段被他人占走。

## Prerequisites

- 先用 `vc.list_reserves` 拿到目标 `reserve_id`。
- 需预约写权限。

## Tips

- 预约对应的会议号**随即失效且不可恢复**，已分享会议号的参会者会受影响。
- **务必先 `dry_run=true` 预演**，并用 `vc.list_reserves` 复核对象后再真正删除。
