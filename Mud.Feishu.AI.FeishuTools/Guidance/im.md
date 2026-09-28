消息（im.*）：`im.get_history_messages` 按会话与时间窗取历史（时间用带时区的 RFC3339），`im.get_message_content` 取单条消息正文（图片/文件只回元数据，不下载）。发送走 `im.send_message`（默认不启用）。
