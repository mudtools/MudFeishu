<template>
  <div class="department-container">
    <!-- 工具栏 -->
    <el-card class="toolbar-card" shadow="never">
      <div class="toolbar">
        <div class="toolbar-left">
          <el-input
            v-model="searchDepartmentId"
            placeholder="输入部门ID（0为根部门）"
            style="width: 320px"
            clearable
            @keyup.enter="handleSearchChildren"
          >
            <template #prefix>
              <el-icon><Search /></el-icon>
            </template>
          </el-input>
          <el-button type="primary" :loading="loadingTable" @click="handleSearchChildren">
            <el-icon><Search /></el-icon>
            查询子部门
          </el-button>
          <el-button type="success" @click="openCreateDialog">
            <el-icon><Plus /></el-icon>
            创建部门
          </el-button>
          <el-button @click="openBatchDialog">
            <el-icon><DocumentCopy /></el-icon>
            批量查询
          </el-button>
        </div>
        <div class="toolbar-right">
          <el-tag v-if="currentDepartmentId" type="info" size="large">
            当前部门: {{ currentDepartmentId }}
          </el-tag>
        </div>
      </div>
    </el-card>

    <!-- 部门列表 -->
    <el-card shadow="never" class="table-card">
      <el-table
        :data="departmentList"
        v-loading="loadingTable"
        border
        stripe
        style="width: 100%"
        empty-text="暂无子部门数据，请输入部门ID查询"
      >
        <el-table-column label="部门名称" prop="name" min-width="180" show-overflow-tooltip />
        <el-table-column label="部门ID" prop="department_id" width="200" show-overflow-tooltip />
        <el-table-column label="开放部门ID" prop="open_department_id" width="220" show-overflow-tooltip />
        <el-table-column label="父部门ID" prop="parent_department_id" width="150" show-overflow-tooltip />
        <el-table-column label="负责人" prop="leader_user_id" width="150" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.leader_user_id || '—' }}
          </template>
        </el-table-column>
        <el-table-column label="成员数" prop="member_count" width="90" align="center" />
        <el-table-column label="排序" prop="order" width="80" align="center" />
        <el-table-column label="状态" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="row.status?.is_deleted ? 'danger' : 'success'" size="small">
              {{ row.status?.is_deleted ? '已删除' : '正常' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="360" fixed="right">
          <template #default="{ row }">
            <el-button size="small" @click="handleViewDetail(row)">详情</el-button>
            <el-button size="small" type="warning" @click="openEditDialog(row)">编辑</el-button>
            <el-button size="small" type="primary" @click="handleViewChildren(row)">子部门</el-button>
            <el-dropdown trigger="click" @command="(cmd: string) => handleMoreAction(cmd, row)">
              <el-button size="small">
                更多<el-icon class="el-icon--right"><ArrowDown /></el-icon>
              </el-button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="parents">父部门链</el-dropdown-item>
                  <el-dropdown-item command="updateId">更新部门ID</el-dropdown-item>
                  <el-dropdown-item command="unbindChat" :disabled="!row.chat_id">解绑群聊</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-wrapper" v-if="hasMore || pageToken">
        <el-button :disabled="!hasMore" :loading="loadingMore" type="primary" plain @click="handleLoadMore">
          加载更多
        </el-button>
        <el-tag v-if="!hasMore && departmentList.length > 0" type="info">已加载全部</el-tag>
      </div>
    </el-card>

    <!-- 部门详情对话框 -->
    <el-dialog v-model="detailDialogVisible" title="部门详情" width="700px" destroy-on-close>
      <el-descriptions v-if="detailData" :column="2" border>
        <el-descriptions-item label="部门名称">{{ detailData.name }}</el-descriptions-item>
        <el-descriptions-item label="部门ID">{{ detailData.department_id }}</el-descriptions-item>
        <el-descriptions-item label="开放部门ID">{{ detailData.open_department_id }}</el-descriptions-item>
        <el-descriptions-item label="父部门ID">{{ detailData.parent_department_id }}</el-descriptions-item>
        <el-descriptions-item label="负责人">{{ detailData.leader_user_id || '—' }}</el-descriptions-item>
        <el-descriptions-item label="群聊ID">{{ detailData.chat_id || '—' }}</el-descriptions-item>
        <el-descriptions-item label="成员数">{{ detailData.member_count }}</el-descriptions-item>
        <el-descriptions-item label="主要成员数">{{ detailData.primary_member_count ?? '—' }}</el-descriptions-item>
        <el-descriptions-item label="排序">{{ detailData.order || '—' }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="detailData.status?.is_deleted ? 'danger' : 'success'" size="small">
            {{ detailData.status?.is_deleted ? '已删除' : '正常' }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="单位ID列表" :span="2">
          {{ detailData.unit_ids?.length ? detailData.unit_ids.join(', ') : '—' }}
        </el-descriptions-item>
        <el-descriptions-item label="HRBP列表" :span="2">
          {{ detailData.department_hrbps?.length ? detailData.department_hrbps.join(', ') : '—' }}
        </el-descriptions-item>
        <el-descriptions-item label="群聊人员类型" :span="2">
          {{ detailData.group_chat_employee_types?.length ? detailData.group_chat_employee_types.join(', ') : '—' }}
        </el-descriptions-item>
      </el-descriptions>
      <el-skeleton v-else :rows="6" animated />
    </el-dialog>

    <!-- 创建部门对话框 -->
    <el-dialog v-model="createDialogVisible" title="创建部门" width="600px" destroy-on-close>
      <el-form :model="createForm" label-width="130px" ref="createFormRef" :rules="createRules">
        <el-form-item label="部门名称" prop="name">
          <el-input v-model="createForm.name" placeholder="请输入部门名称（不可包含 /）" />
        </el-form-item>
        <el-form-item label="父部门ID" prop="parent_department_id">
          <el-input v-model="createForm.parent_department_id" placeholder="根部门填 0" />
        </el-form-item>
        <el-form-item label="自定义部门ID">
          <el-input v-model="createForm.department_id" placeholder="可选，留空则自动生成" />
        </el-form-item>
        <el-form-item label="负责人用户ID">
          <el-input v-model="createForm.leader_user_id" placeholder="可选" />
        </el-form-item>
        <el-form-item label="排序">
          <el-input v-model="createForm.order" placeholder="可选，数值越小越靠前" />
        </el-form-item>
        <el-form-item label="创建部门群">
          <el-switch v-model="createForm.create_group_chat" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleCreate">确认创建</el-button>
      </template>
    </el-dialog>

    <!-- 编辑部门对话框 -->
    <el-dialog v-model="editDialogVisible" title="编辑部门（部分更新）" width="600px" destroy-on-close>
      <el-form :model="editForm" label-width="130px" ref="editFormRef" :rules="editRules">
        <el-form-item label="部门名称" prop="name">
          <el-input v-model="editForm.name" placeholder="请输入部门名称" />
        </el-form-item>
        <el-form-item label="父部门ID" prop="parent_department_id">
          <el-input v-model="editForm.parent_department_id" placeholder="根部门填 0" />
        </el-form-item>
        <el-form-item label="负责人用户ID">
          <el-input v-model="editForm.leader_user_id" placeholder="可选" />
        </el-form-item>
        <el-form-item label="排序">
          <el-input v-model="editForm.order" placeholder="可选" />
        </el-form-item>
        <el-form-item label="创建部门群">
          <el-switch v-model="editForm.create_group_chat" />
        </el-form-item>
      </el-form>
      <div class="edit-tip">
        <el-alert type="info" :closable="false" show-icon>
          <template #default>
            此操作使用 PATCH 部分更新接口，仅更新填写的字段。如需完全更新请使用 PUT 接口。
          </template>
        </el-alert>
      </div>
      <template #footer>
        <el-button @click="editDialogVisible = false">取消</el-button>
        <el-button type="warning" :loading="submitting" @click="handleEditPart">部分更新 (PATCH)</el-button>
        <el-button type="danger" :loading="submitting" @click="handleEditFull">完全更新 (PUT)</el-button>
      </template>
    </el-dialog>

    <!-- 更新部门ID对话框 -->
    <el-dialog v-model="updateIdDialogVisible" title="更新部门ID" width="500px" destroy-on-close>
      <el-form :model="updateIdForm" label-width="130px" ref="updateIdFormRef" :rules="updateIdRules">
        <el-form-item label="当前部门ID">
          <el-input :model-value="updateIdForm.currentId" disabled />
        </el-form-item>
        <el-form-item label="新部门ID" prop="new_department_id">
          <el-input v-model="updateIdForm.new_department_id" placeholder="请输入新的自定义部门ID" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="updateIdDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleUpdateId">确认更新</el-button>
      </template>
    </el-dialog>

    <!-- 批量查询对话框 -->
    <el-dialog v-model="batchDialogVisible" title="批量查询部门信息" width="600px" destroy-on-close>
      <el-form label-width="120px">
        <el-form-item label="部门ID列表">
          <el-input
            v-model="batchInputIds"
            type="textarea"
            :rows="5"
            placeholder="输入多个部门ID，每行一个或用英文逗号分隔"
          />
        </el-form-item>
      </el-form>
      <el-table
        v-if="batchResults.length > 0"
        :data="batchResults"
        border
        stripe
        max-height="300"
        style="margin-top: 16px"
      >
        <el-table-column label="部门名称" prop="name" min-width="150" show-overflow-tooltip />
        <el-table-column label="部门ID" prop="department_id" width="180" show-overflow-tooltip />
        <el-table-column label="开放部门ID" prop="open_department_id" width="200" show-overflow-tooltip />
        <el-table-column label="成员数" prop="member_count" width="80" align="center" />
      </el-table>
      <template #footer>
        <el-button @click="batchDialogVisible = false">关闭</el-button>
        <el-button type="primary" :loading="loadingBatch" @click="handleBatchQuery">查询</el-button>
      </template>
    </el-dialog>

    <!-- 父部门链对话框 -->
    <el-dialog v-model="parentDialogVisible" title="父部门链" width="700px" destroy-on-close>
      <el-table :data="parentList" border stripe v-loading="loadingParents">
        <el-table-column label="部门名称" prop="name" min-width="150" show-overflow-tooltip />
        <el-table-column label="部门ID" prop="department_id" width="180" show-overflow-tooltip />
        <el-table-column label="开放部门ID" prop="open_department_id" width="200" show-overflow-tooltip />
        <el-table-column label="成员数" prop="member_count" width="80" align="center" />
      </el-table>
      <template #footer>
        <el-button @click="parentDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Plus, DocumentCopy, ArrowDown } from '@element-plus/icons-vue'
import {
  departmentApi,
  extractErrorMessage,
  type GetDepartmentInfo,
  type DepartmentCreateRequest,
  type DepartmentPartUpdateRequest,
  type DepartmentUpdateRequest
} from '@/api/department'

// ===================== 部门列表 =====================

const searchDepartmentId = ref('0')
const currentDepartmentId = ref('')
const departmentList = ref<GetDepartmentInfo[]>([])
const loadingTable = ref(false)
const hasMore = ref(false)
const pageToken = ref<string | null>(null)
const loadingMore = ref(false)

const fetchSubDepartments = async (departmentId: string, token?: string | null) => {
  const result = await departmentApi.getSubDepartments(
    departmentId,
    false,
    50,
    token ?? null
  )
  return result
}

const handleSearchChildren = async () => {
  const deptId = searchDepartmentId.value.trim() || '0'
  currentDepartmentId.value = deptId
  loadingTable.value = true
  departmentList.value = []
  pageToken.value = null
  hasMore.value = false
  try {
    const result = await fetchSubDepartments(deptId)
    departmentList.value = result.items || []
    hasMore.value = result.has_more
    pageToken.value = result.page_token
    if (departmentList.value.length === 0) {
      ElMessage.info('未查询到子部门')
    }
  } catch (error: any) {
    ElMessage.error(extractErrorMessage(error))
  } finally {
    loadingTable.value = false
  }
}

const handleLoadMore = async () => {
  if (!hasMore.value || !pageToken.value) return
  loadingMore.value = true
  try {
    const result = await fetchSubDepartments(currentDepartmentId.value, pageToken.value)
    departmentList.value.push(...(result.items || []))
    hasMore.value = result.has_more
    pageToken.value = result.page_token
  } catch (error: any) {
    ElMessage.error(extractErrorMessage(error))
  } finally {
    loadingMore.value = false
  }
}

const handleViewChildren = (row: GetDepartmentInfo) => {
  searchDepartmentId.value = row.department_id || row.open_department_id
  handleSearchChildren()
}

// ===================== 部门详情 =====================

const detailDialogVisible = ref(false)
const detailData = ref<GetDepartmentInfo | null>(null)

const handleViewDetail = async (row: GetDepartmentInfo) => {
  detailDialogVisible.value = true
  detailData.value = null
  const deptId = row.department_id || row.open_department_id
  try {
    const result = await departmentApi.getDepartment(deptId)
    detailData.value = result.department
  } catch (error: any) {
    ElMessage.error(extractErrorMessage(error))
  }
}

// ===================== 创建部门 =====================

const createDialogVisible = ref(false)
const createFormRef = ref<FormInstance>()
const submitting = ref(false)
const createForm = reactive<DepartmentCreateRequest>({
  name: '',
  parent_department_id: '0',
  department_id: '',
  leader_user_id: '',
  order: '',
  create_group_chat: false
})

const createRules: FormRules = {
  name: [{ required: true, message: '请输入部门名称', trigger: 'blur' }],
  parent_department_id: [{ required: true, message: '请输入父部门ID', trigger: 'blur' }]
}

const openCreateDialog = () => {
  createForm.name = ''
  createForm.parent_department_id = currentDepartmentId.value || '0'
  createForm.department_id = ''
  createForm.leader_user_id = ''
  createForm.order = ''
  createForm.create_group_chat = false
  createDialogVisible.value = true
}

const handleCreate = async () => {
  if (!createFormRef.value) return
  await createFormRef.value.validate(async (valid) => {
    if (!valid) return
    submitting.value = true
    try {
      const result = await departmentApi.createDepartment(createForm)
      if (result.code === 0) {
        ElMessage.success('部门创建成功')
        createDialogVisible.value = false
        // 刷新列表
        if (currentDepartmentId.value) {
          handleSearchChildren()
        }
      } else {
        ElMessage.error(result.msg || `创建失败 (code: ${result.code})`)
      }
    } catch (error: any) {
      ElMessage.error(extractErrorMessage(error))
    } finally {
      submitting.value = false
    }
  })
}

// ===================== 编辑部门 =====================

const editDialogVisible = ref(false)
const editFormRef = ref<FormInstance>()
const editingDepartmentId = ref('')
const editForm = reactive<DepartmentPartUpdateRequest>({
  name: '',
  parent_department_id: '',
  leader_user_id: '',
  order: '',
  create_group_chat: false
})

const editRules: FormRules = {
  name: [{ required: true, message: '请输入部门名称', trigger: 'blur' }],
  parent_department_id: [{ required: true, message: '请输入父部门ID', trigger: 'blur' }]
}

const openEditDialog = (row: GetDepartmentInfo) => {
  editingDepartmentId.value = row.department_id || row.open_department_id
  editForm.name = row.name
  editForm.parent_department_id = row.parent_department_id
  editForm.leader_user_id = row.leader_user_id || ''
  editForm.order = row.order || ''
  editForm.create_group_chat = false
  editDialogVisible.value = true
}

const handleEditPart = async () => {
  if (!editFormRef.value) return
  await editFormRef.value.validate(async (valid) => {
    if (!valid) return
    submitting.value = true
    try {
      const result = await departmentApi.updatePartDepartment(editingDepartmentId.value, editForm)
      if (result.code === 0) {
        ElMessage.success('部门部分更新成功')
        editDialogVisible.value = false
        handleSearchChildren()
      } else {
        ElMessage.error(result.msg || `更新失败 (code: ${result.code})`)
      }
    } catch (error: any) {
      ElMessage.error(extractErrorMessage(error))
    } finally {
      submitting.value = false
    }
  })
}

const handleEditFull = async () => {
  if (!editFormRef.value) return
  await editFormRef.value.validate(async (valid) => {
    if (!valid) return
    submitting.value = true
    try {
      const fullForm: DepartmentUpdateRequest = {
        name: editForm.name,
        parent_department_id: editForm.parent_department_id,
        leader_user_id: editForm.leader_user_id,
        order: editForm.order,
        create_group_chat: editForm.create_group_chat
      }
      const result = await departmentApi.updateDepartment(editingDepartmentId.value, fullForm)
      if (result.code === 0) {
        ElMessage.success('部门完全更新成功')
        editDialogVisible.value = false
        handleSearchChildren()
      } else {
        ElMessage.error(result.msg || `更新失败 (code: ${result.code})`)
      }
    } catch (error: any) {
      ElMessage.error(extractErrorMessage(error))
    } finally {
      submitting.value = false
    }
  })
}

// ===================== 更新部门ID =====================

const updateIdDialogVisible = ref(false)
const updateIdFormRef = ref<FormInstance>()
const updateIdForm = reactive({
  currentId: '',
  new_department_id: ''
})

const updateIdRules: FormRules = {
  new_department_id: [{ required: true, message: '请输入新的部门ID', trigger: 'blur' }]
}

const openUpdateIdDialog = (row: GetDepartmentInfo) => {
  updateIdForm.currentId = row.department_id || row.open_department_id
  updateIdForm.new_department_id = ''
  updateIdDialogVisible.value = true
}

const handleUpdateId = async () => {
  if (!updateIdFormRef.value) return
  await updateIdFormRef.value.validate(async (valid) => {
    if (!valid) return
    submitting.value = true
    try {
      const result = await departmentApi.updateDepartmentId(
        updateIdForm.currentId,
        updateIdForm.new_department_id
      )
      if (result.code === 0) {
        ElMessage.success('部门ID更新成功')
        updateIdDialogVisible.value = false
        handleSearchChildren()
      } else {
        ElMessage.error(result.msg || `更新失败 (code: ${result.code})`)
      }
    } catch (error: any) {
      ElMessage.error(extractErrorMessage(error))
    } finally {
      submitting.value = false
    }
  })
}

// ===================== 解绑群聊 =====================

const handleUnbindChat = async (row: GetDepartmentInfo) => {
  const deptId = row.department_id || row.open_department_id
  try {
    await ElMessageBox.confirm(
      `确定要解绑部门「${row.name}」的群聊吗？此操作将把部门群转为普通群。`,
      '解绑确认',
      { type: 'warning', confirmButtonText: '确认解绑', cancelButtonText: '取消' }
    )
    const result = await departmentApi.unbindDepartmentChat(deptId)
    if (result.code === 0) {
      ElMessage.success('部门群聊解绑成功')
      handleSearchChildren()
    } else {
      ElMessage.error(result.msg || `解绑失败 (code: ${result.code})`)
    }
  } catch (error: any) {
    if (error === 'cancel') return
    ElMessage.error(extractErrorMessage(error))
  }
}

// ===================== 批量查询 =====================

const batchDialogVisible = ref(false)
const batchInputIds = ref('')
const batchResults = ref<GetDepartmentInfo[]>([])
const loadingBatch = ref(false)

const openBatchDialog = () => {
  batchInputIds.value = ''
  batchResults.value = []
  batchDialogVisible.value = true
}

const handleBatchQuery = async () => {
  const ids = batchInputIds.value
    .split(/[,\n\r]/)
    .map((s) => s.trim())
    .filter((s) => s.length > 0)

  if (ids.length === 0) {
    ElMessage.warning('请输入至少一个部门ID')
    return
  }

  loadingBatch.value = true
  try {
    const result = await departmentApi.getDepartmentsByIds(ids)
    batchResults.value = result.items || []
    if (batchResults.value.length === 0) {
      ElMessage.info('未查询到任何部门信息')
    }
  } catch (error: any) {
    ElMessage.error(extractErrorMessage(error))
  } finally {
    loadingBatch.value = false
  }
}

// ===================== 父部门链 =====================

const parentDialogVisible = ref(false)
const parentList = ref<GetDepartmentInfo[]>([])
const loadingParents = ref(false)

const handleViewParents = async (row: GetDepartmentInfo) => {
  const deptId = row.department_id || row.open_department_id
  parentDialogVisible.value = true
  parentList.value = []
  loadingParents.value = true
  try {
    const result = await departmentApi.getParentDepartments(deptId, 50)
    parentList.value = result.items || []
    if (parentList.value.length === 0) {
      ElMessage.info('未查询到父部门信息')
    }
  } catch (error: any) {
    ElMessage.error(extractErrorMessage(error))
  } finally {
    loadingParents.value = false
  }
}

// ===================== 更多操作路由 =====================

const handleMoreAction = (command: string, row: GetDepartmentInfo) => {
  switch (command) {
    case 'parents':
      handleViewParents(row)
      break
    case 'updateId':
      openUpdateIdDialog(row)
      break
    case 'unbindChat':
      handleUnbindChat(row)
      break
  }
}

// ===================== 初始化 =====================

// 页面加载时自动查询根部门
handleSearchChildren()
</script>

<style scoped>
.department-container {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.toolbar-card {
  border-radius: 8px;
}

.toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
}

.toolbar-left {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.toolbar-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.table-card {
  border-radius: 8px;
}

.pagination-wrapper {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 12px;
  margin-top: 16px;
}

.edit-tip {
  margin-top: 12px;
}
</style>
