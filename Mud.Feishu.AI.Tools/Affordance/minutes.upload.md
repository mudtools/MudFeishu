## WHEN

- 录音或会议录像已在云盘上，用户想把它变成妙记以便总结与检索。
- 文件格式、时长与大小都满足导入条件，只差从云盘引用触发生成。

## Avoid when

- 文件还没进云盘——先 `drive.upload_file` 传上去拿 file_token，本工具不搬运字节。
- 源对象已是妙记、只要其中一段——那是 `minutes.clip`，不是重新导入。
- 想读已有妙记的内容——用 `minutes.get_artifacts`，导入与读取是两条路。

## Prerequisites

- `file_token` 是云盘音视频文件的 token，且当前用户对该文件有下载权限。
- 宿主放行 user 身份、授权该写工具；需 minutes:minutes.upload:write。

## Tips

- 时长不超过 6 小时、大小不超过 6GB，音视频各自有格式白名单，超限由平台拒绝。
- 成功回执返回新妙记链接，后续按该妙记的 token 走读取类工具。
