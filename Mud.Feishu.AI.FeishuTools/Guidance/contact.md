通讯录（contact.*）：把姓名/邮箱/手机号解析成 open_id 是绝大多数写操作的前置步骤——`contact.resolve_user` / `contact.search_user` / `contact.get_user` / `contact.batch_get`。解析不到时先确认该用户在当前应用可见范围内，不要反复换关键字重试。
