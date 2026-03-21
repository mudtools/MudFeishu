<template>
  <el-dialog v-model="visible" title="新建任务" width="600px" destroy-on-close class="create-task-dialog">
    <el-form ref="formRef" :model="form" :rules="rules" label-position="top" class="task-form">
      <el-form-item label="任务标题" prop="summary">
        <el-input v-model="form.summary" placeholder="输入任务标题" size="large" />
      </el-form-item>

      <el-form-item label="任务描述" prop="description">
        <el-input v-model="form.description" type="textarea" :rows="3" placeholder="输入任务描述（可选）" />
      </el-form-item>

      <div class="form-row">
        <el-form-item label="任务清单" prop="taskListId" class="form-col">
          <el-select v-model="form.taskListId" placeholder="选择任务清单" style="width: 100%">
            <el-option v-for="list in taskLists" :key="list.id" :label="list.name" :value="list.id" />
          </el-select>
        </el-form-item>

        <el-form-item label="优先级" prop="priority" class="form-col">
          <el-select v-model="form.priority" placeholder="选择优先级" style="width: 100%">
            <el-option label="低" :value="1" />
            <el-option label="中" :value="2" />
            <el-option label="高" :value="3" />
            <el-option label="紧急" :value="4" />
          </el-select>
        </el-form-item>
      </div>

      <div class="form-row">
        <el-form-item label="开始时间" prop="startTime" class="form-col">
          <el-date-picker v-model="form.startTime" type="datetime" placeholder="选择开始时间" style="width: 100%" />
        </el-form-item>

        <el-form-item label="截止时间" prop="dueTime" class="form-col">
          <el-date-picker v-model="form.dueTime" type="datetime" placeholder="选择截止时间" style="width: 100%" />
        </el-form-item>
      </div>

      <el-form-item label="负责人" prop="assignees">
        <el-select v-model="form.assignees" multiple placeholder="选择负责人" style="width: 100%">
          <el-option v-for="user in users" :key="user.id" :label="user.name" :value="user.feishuId">
            <div class="user-option">
              <el-avatar :size="24" :src="user.avatarUrl">
                {{ user.name.charAt(0) }}
              </el-avatar>
              <span>{{ user.name }}</span>
            </div>
          </el-option>
        </el-select>
      </el-form-item>

      <el-form-item label="标签" prop="tags">
        <el-select v-model="form.tags" multiple allow-create filterable placeholder="添加标签" style="width: 100%">
          <el-option v-for="tag in availableTags" :key="tag" :label="tag" :value="tag" />
        </el-select>
      </el-form-item>
    </el-form>

    <template #footer>
      <div class="dialog-footer">
        <el-button @click="visible = false">取消</el-button>
        <el-button type="primary" :loading="loading" @click="handleSubmit">
          创建任务
        </el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { ElMessage } from "element-plus"
import type { FormInstance, FormRules } from "element-plus"
import { taskApi } from "../api/task"
import { taskListApi } from "../api/taskList"
import { getUsers } from "../api/user"
import type { TaskList, User, CreateTaskRequest } from "../types"

const props = defineProps<{
  modelValue: boolean
}>()

const emit = defineEmits<{
  "update:modelValue": [value: boolean]
  success: []
}>()

const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit("update:modelValue", val),
})

const formRef = ref<FormInstance>()
const loading = ref(false)

const form = ref({
  summary: "",
  description: "",
  taskListId: undefined as number | undefined,
  priority: 2,
  startTime: undefined as Date | undefined,
  dueTime: undefined as Date | undefined,
  assignees: [] as string[],
  tags: [] as string[],
})

const rules: FormRules = {
  summary: [{ required: true, message: "请输入任务标题", trigger: "blur" }],
  priority: [{ required: true, message: "请选择优先级", trigger: "change" }],
}

const taskLists = ref<TaskList[]>([])
const users = ref<User[]>([])
const availableTags = ref(["重要", "紧急", "设计", "开发", "测试", "文档"])

const loadData = async () => {
  try {
    const [listsRes, usersRes] = await Promise.all([
      taskListApi.getTaskLists(),
      getUsers({ page: 1, pageSize: 100 }),
    ])
    taskLists.value = listsRes
    users.value = usersRes.data?.items || []
  } catch (error) {
    console.error("加载数据失败:", error)
  }
}

watch(visible, (val) => {
  if (val) {
    loadData()
    form.value = {
      summary: "",
      description: "",
      taskListId: undefined,
      priority: 2,
      startTime: undefined,
      dueTime: undefined,
      assignees: [],
      tags: [],
    }
  }
})

const handleSubmit = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (valid) {
      loading.value = true
      try {
        const selectedList = taskLists.value.find(
          (l) => l.id === form.value.taskListId
        )
        const requestData: CreateTaskRequest = {
          summary: form.value.summary,
          description: form.value.description,
          priority: form.value.priority,
          assignees: form.value.assignees,
        }
        if (selectedList?.taskListGuid) {
          requestData.taskListGuid = selectedList.taskListGuid
        }
        if (form.value.startTime) {
          requestData.startTime = form.value.startTime.toISOString()
        }
        if (form.value.dueTime) {
          requestData.dueTime = form.value.dueTime.toISOString()
        }
        await taskApi.createTask(requestData)
        ElMessage.success("任务创建成功")
        visible.value = false
        emit("success")
      } catch (error) {
        ElMessage.error("任务创建失败")
      } finally {
        loading.value = false
      }
    }
  })
}
</script>

<style scoped>
.create-task-dialog :deep(.el-dialog__header) {
  padding: 20px 24px;
  border-bottom: 1px solid var(--border-light);
}

.create-task-dialog :deep(.el-dialog__body) {
  padding: 24px;
}

.create-task-dialog :deep(.el-dialog__footer) {
  padding: 16px 24px;
  border-top: 1px solid var(--border-light);
}

.task-form :deep(.el-form-item__label) {
  font-weight: 500;
  color: var(--text-primary);
  padding-bottom: 8px;
}

.form-row {
  display: flex;
  gap: 16px;
}

.form-col {
  flex: 1;
}

.user-option {
  display: flex;
  align-items: center;
  gap: 8px;
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}
</style>
