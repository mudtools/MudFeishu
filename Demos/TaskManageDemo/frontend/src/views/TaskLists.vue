<template>
  <div class="tasklists-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>任务清单</span>
          <el-button type="primary" @click="showCreateDialog">新建清单</el-button>
        </div>
      </template>

      <el-table v-loading="loading" :data="taskLists" stripe>
        <el-table-column prop="name" label="清单名称" min-width="200" />
        <el-table-column prop="description" label="描述" min-width="200">
          <template #default="{ row }">
            {{ row.description || '暂无描述' }}
          </template>
        </el-table-column>
        <el-table-column prop="taskCount" label="任务数量" width="120">
          <template #default="{ row }">
            <el-tag>{{ row.taskCount || 0 }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="创建时间" width="180">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" size="small" text @click="viewTasks(row)">
              查看任务
            </el-button>
            <el-button type="primary" size="small" text @click="handleEdit(row)">
              编辑
            </el-button>
            <el-button type="danger" size="small" text @click="handleDelete(row)">
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="editingList ? '编辑清单' : '新建清单'" width="500px">
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="80px">
        <el-form-item label="清单名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入清单名称" />
        </el-form-item>
        <el-form-item label="描述" prop="description">
          <el-input v-model="form.description" type="textarea" :rows="3" placeholder="请输入描述" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleSubmit">
          {{ editingList ? '保存' : '创建' }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from "vue"
import { useRouter } from "vue-router"
import { ElMessage, ElMessageBox } from "element-plus"
import type { FormInstance, FormRules } from "element-plus"
import { taskListApi } from "../api"
import type { TaskList, CreateTaskListRequest } from "../types"
import dayjs from "dayjs"

const router = useRouter()
const loading = ref(false)
const taskLists = ref<TaskList[]>([])
const dialogVisible = ref(false)
const editingList = ref<TaskList | null>(null)
const submitting = ref(false)
const formRef = ref<FormInstance>()

const form = reactive<CreateTaskListRequest>({
  name: "",
  description: "",
})

const formRules: FormRules = {
  name: [{ required: true, message: "请输入清单名称", trigger: "blur" }],
}

const formatDate = (date: string) => dayjs(date).format("YYYY-MM-DD HH:mm")

const fetchTaskLists = async () => {
  loading.value = true
  try {
    taskLists.value = await taskListApi.getTaskLists()
  } catch {
    ElMessage.error("获取任务清单失败")
  } finally {
    loading.value = false
  }
}

const showCreateDialog = () => {
  editingList.value = null
  Object.assign(form, { name: "", description: "" })
  dialogVisible.value = true
}

const handleEdit = (list: TaskList) => {
  editingList.value = list
  Object.assign(form, { name: list.name, description: list.description || "" })
  dialogVisible.value = true
}

const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    submitting.value = true
    try {
      if (editingList.value) {
        await taskListApi.updateTaskList(editingList.value.id, form)
        ElMessage.success("清单更新成功")
      } else {
        await taskListApi.createTaskList(form)
        ElMessage.success("清单创建成功")
      }
      dialogVisible.value = false
      fetchTaskLists()
    } catch {
      ElMessage.error(editingList.value ? "清单更新失败" : "清单创建失败")
    } finally {
      submitting.value = false
    }
  })
}

const handleDelete = async (list: TaskList) => {
  try {
    await ElMessageBox.confirm("确定要删除此清单吗？", "确认删除", {
      type: "warning",
    })
    await taskListApi.deleteTaskList(list.id)
    ElMessage.success("清单已删除")
    fetchTaskLists()
  } catch {}
}

const viewTasks = (list: TaskList) => {
  router.push({ path: "/tasks", query: { taskListId: list.id } })
}

onMounted(() => fetchTaskLists())
</script>

<style scoped>
.tasklists-page {
  padding: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
