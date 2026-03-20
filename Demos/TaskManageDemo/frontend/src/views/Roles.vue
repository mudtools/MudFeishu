<template>
  <div class="roles-page">
    <div class="page-header">
      <h1>角色权限管理</h1>
      <p class="subtitle">管理系统角色和权限分配</p>
    </div>

    <el-tabs v-model="activeTab" class="page-tabs">
      <el-tab-pane label="角色管理" name="roles">
        <el-card class="table-card">
          <template #header>
            <div class="card-header">
              <span>角色列表</span>
              <el-button type="primary" @click="handleCreateRole">
                <el-icon>
                  <Plus />
                </el-icon>
                新建角色
              </el-button>
            </div>
          </template>

          <el-table :data="roles" v-loading="loading" stripe>
            <el-table-column prop="name" label="角色名称" width="150" />
            <el-table-column prop="code" label="角色代码" width="150">
              <template #default="{ row }">
                <el-tag>{{ row.code }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="description" label="描述" min-width="200" />
            <el-table-column prop="isSystem" label="类型" width="100" align="center">
              <template #default="{ row }">
                <el-tag :type="row.isSystem ? 'warning' : 'info'">
                  {{ row.isSystem ? '系统' : '自定义' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="isEnabled" label="状态" width="100" align="center">
              <template #default="{ row }">
                <el-tag :type="row.isEnabled ? 'success' : 'danger'">
                  {{ row.isEnabled ? '启用' : '禁用' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="userCount" label="用户数" width="100" align="center" />
            <el-table-column label="操作" width="200" fixed="right">
              <template #default="{ row }">
                <el-button text type="primary" @click="handleEditPermissions(row)">
                  <el-icon>
                    <Key />
                  </el-icon>
                  权限
                </el-button>
                <el-button text type="primary" @click="handleEditRole(row)" :disabled="row.isSystem">
                  编辑
                </el-button>
                <el-button text type="danger" @click="handleDeleteRole(row)" :disabled="row.isSystem">
                  删除
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="权限列表" name="permissions">
        <el-card class="table-card">
          <template #header>
            <div class="card-header">
              <span>权限列表</span>
              <el-button type="primary" @click="handleInitPermissions">
                <el-icon>
                  <Refresh />
                </el-icon>
                初始化权限
              </el-button>
            </div>
          </template>

          <div class="permission-groups">
            <div v-for="group in permissionGroups" :key="group.group" class="permission-group">
              <h4 class="group-title">{{ group.group }}</h4>
              <div class="permission-items">
                <el-tag v-for="perm in group.permissions" :key="perm.id" :type="perm.isEnabled ? 'success' : 'info'" class="permission-tag">
                  <el-tooltip :content="perm.description || perm.code" placement="top">
                    <span>{{ perm.name }}</span>
                  </el-tooltip>
                </el-tag>
              </div>
            </div>
          </div>
        </el-card>
      </el-tab-pane>
    </el-tabs>

    <el-dialog v-model="roleDialogVisible" :title="editingRole ? '编辑角色' : '新建角色'" width="500px">
      <el-form :model="roleForm" :rules="roleRules" ref="roleFormRef" label-width="80px">
        <el-form-item label="角色代码" prop="code" v-if="!editingRole">
          <el-input v-model="roleForm.code" placeholder="请输入角色代码" />
        </el-form-item>
        <el-form-item label="角色名称" prop="name">
          <el-input v-model="roleForm.name" placeholder="请输入角色名称" />
        </el-form-item>
        <el-form-item label="描述">
          <el-input v-model="roleForm.description" type="textarea" placeholder="请输入角色描述" />
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="roleForm.sortOrder" :min="0" />
        </el-form-item>
        <el-form-item label="启用状态">
          <el-switch v-model="roleForm.isEnabled" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="roleDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSaveRole" :loading="saving">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="permissionDialogVisible" title="配置角色权限" width="600px">
      <div class="permission-config">
        <div class="role-info">
          <span class="role-name">{{ currentRole?.name }}</span>
          <span class="role-code">{{ currentRole?.code }}</span>
        </div>
        <el-divider />
        <div class="permission-groups-config">
          <div v-for="group in permissionGroups" :key="group.group" class="permission-group">
            <h4 class="group-title">{{ group.group }}</h4>
            <div class="permission-checkboxes">
              <el-checkbox v-for="perm in group.permissions" :key="perm.id" :label="perm.id" v-model="permissionForm.permissionIds">
                {{ perm.name }}
                <el-tooltip v-if="perm.description" :content="perm.description" placement="top">
                  <el-icon class="help-icon">
                    <QuestionFilled />
                  </el-icon>
                </el-tooltip>
              </el-checkbox>
            </div>
          </div>
        </div>
      </div>
      <template #footer>
        <el-button @click="permissionDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSavePermissions" :loading="saving">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from "vue"
import {
  ElMessage,
  ElMessageBox,
  type FormInstance,
  type FormRules,
} from "element-plus"
import { Plus, Key, Refresh, QuestionFilled } from "@element-plus/icons-vue"
import {
  getRoles,
  createRole,
  updateRole,
  deleteRole,
  getRolePermissions,
  assignRolePermissions,
  getPermissionGroups,
  initializePermissions,
} from "../api/role"
import type { Role, PermissionGroup } from "../types"

const activeTab = ref("roles")
const loading = ref(false)
const saving = ref(false)
const roles = ref<Role[]>([])
const permissionGroups = ref<PermissionGroup[]>([])

const roleDialogVisible = ref(false)
const permissionDialogVisible = ref(false)
const editingRole = ref<Role | null>(null)
const currentRole = ref<Role | null>(null)

const roleFormRef = ref<FormInstance>()
const roleForm = reactive({
  code: "",
  name: "",
  description: "",
  sortOrder: 0,
  isEnabled: true,
})

const permissionForm = reactive({
  permissionIds: [] as number[],
})

const roleRules: FormRules = {
  code: [
    { required: true, message: "请输入角色代码", trigger: "blur" },
    {
      pattern: /^[a-z_]+$/,
      message: "角色代码只能包含小写字母和下划线",
      trigger: "blur",
    },
  ],
  name: [{ required: true, message: "请输入角色名称", trigger: "blur" }],
}

const pagination = reactive({
  page: 1,
  pageSize: 100,
  total: 0,
})

onMounted(async () => {
  await Promise.all([loadRoles(), loadPermissions()])
})

async function loadRoles() {
  loading.value = true
  try {
    const response = await getRoles({
      page: pagination.page,
      pageSize: pagination.pageSize,
    })
    if (response.success) {
      roles.value = response.data.items
      pagination.total = response.data.total
    }
  } catch (error) {
    console.error("加载角色列表失败:", error)
    ElMessage.error("加载角色列表失败")
  } finally {
    loading.value = false
  }
}

async function loadPermissions() {
  try {
    const response = await getPermissionGroups()
    if (response.success) {
      permissionGroups.value = response.data
    }
  } catch (error) {
    console.error("加载权限列表失败:", error)
  }
}

function handleCreateRole() {
  editingRole.value = null
  roleForm.code = ""
  roleForm.name = ""
  roleForm.description = ""
  roleForm.sortOrder = 0
  roleForm.isEnabled = true
  roleDialogVisible.value = true
}

function handleEditRole(role: Role) {
  editingRole.value = role
  roleForm.code = role.code
  roleForm.name = role.name
  roleForm.description = role.description || ""
  roleForm.sortOrder = role.sortOrder
  roleForm.isEnabled = role.isEnabled
  roleDialogVisible.value = true
}

async function handleSaveRole() {
  const valid = await roleFormRef.value?.validate()
  if (!valid) return

  saving.value = true
  try {
    if (editingRole.value) {
      const response = await updateRole(editingRole.value.id, {
        name: roleForm.name,
        description: roleForm.description,
        sortOrder: roleForm.sortOrder,
        isEnabled: roleForm.isEnabled,
      })
      if (response.success) {
        ElMessage.success("角色更新成功")
        roleDialogVisible.value = false
        loadRoles()
      }
    } else {
      const response = await createRole({
        code: roleForm.code,
        name: roleForm.name,
        description: roleForm.description,
        sortOrder: roleForm.sortOrder,
      })
      if (response.success) {
        ElMessage.success("角色创建成功")
        roleDialogVisible.value = false
        loadRoles()
      }
    }
  } catch (error) {
    console.error("保存角色失败:", error)
    ElMessage.error("保存角色失败")
  } finally {
    saving.value = false
  }
}

async function handleDeleteRole(role: Role) {
  try {
    await ElMessageBox.confirm(
      `确定要删除角色 "${role.name}" 吗？删除后不可恢复。`,
      "确认删除",
      { type: "warning" }
    )
    const response = await deleteRole(role.id)
    if (response.success) {
      ElMessage.success("角色删除成功")
      loadRoles()
    }
  } catch (error) {
    if (error !== "cancel") {
      console.error("删除角色失败:", error)
      ElMessage.error("删除角色失败")
    }
  }
}

async function handleEditPermissions(role: Role) {
  currentRole.value = role
  try {
    const response = await getRolePermissions(role.id)
    if (response.success) {
      permissionForm.permissionIds = response.data.map((p) => p.id)
    }
  } catch (error) {
    permissionForm.permissionIds = []
  }
  permissionDialogVisible.value = true
}

async function handleSavePermissions() {
  if (!currentRole.value) return

  saving.value = true
  try {
    const response = await assignRolePermissions(
      currentRole.value.id,
      permissionForm.permissionIds
    )
    if (response.success) {
      ElMessage.success("权限配置成功")
      permissionDialogVisible.value = false
      loadRoles()
    }
  } catch (error) {
    console.error("保存权限失败:", error)
    ElMessage.error("保存权限失败")
  } finally {
    saving.value = false
  }
}

async function handleInitPermissions() {
  try {
    await ElMessageBox.confirm(
      "确定要初始化权限数据吗？这将添加缺失的权限定义。",
      "确认初始化"
    )
    const response = await initializePermissions()
    if (response.success) {
      ElMessage.success("权限初始化成功")
      loadPermissions()
    }
  } catch (error) {
    if (error !== "cancel") {
      console.error("初始化权限失败:", error)
      ElMessage.error("初始化权限失败")
    }
  }
}
</script>

<style scoped>
.roles-page {
  padding: 0;
}

.page-header {
  margin-bottom: 24px;
}

.page-header h1 {
  font-size: 24px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0 0 8px 0;
}

.subtitle {
  color: var(--text-secondary);
  margin: 0;
}

.page-tabs {
  background: var(--bg-card);
  border-radius: var(--radius-lg);
  padding: 16px;
}

.table-card {
  border: none;
  box-shadow: none;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.permission-groups {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.permission-group {
  background: var(--bg-secondary);
  border-radius: var(--radius-md);
  padding: 16px;
}

.group-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0 0 12px 0;
  padding-bottom: 8px;
  border-bottom: 1px solid var(--border-light);
}

.permission-items {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.permission-tag {
  cursor: pointer;
}

.permission-config {
  max-height: 500px;
  overflow-y: auto;
}

.role-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.role-name {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
}

.role-code {
  font-size: 12px;
  color: var(--text-muted);
  background: var(--bg-tertiary);
  padding: 2px 8px;
  border-radius: var(--radius-sm);
}

.permission-groups-config {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.permission-checkboxes {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

.permission-checkboxes .el-checkbox {
  margin-right: 0;
}

.help-icon {
  margin-left: 4px;
  color: var(--text-muted);
  cursor: help;
}
</style>
