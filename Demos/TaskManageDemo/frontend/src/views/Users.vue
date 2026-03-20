<template>
  <div class="users-page">
    <div class="page-header">
      <h1>用户管理</h1>
      <p class="subtitle">管理系统用户、分配角色和权限</p>
    </div>

    <div class="stats-cards">
      <el-card class="stat-card">
        <div class="stat-content">
          <el-icon :size="32" class="stat-icon"><User /></el-icon>
          <div class="stat-info">
            <span class="stat-value">{{ statistics.totalUsers }}</span>
            <span class="stat-label">总用户数</span>
          </div>
        </div>
      </el-card>
      <el-card class="stat-card">
        <div class="stat-content">
          <el-icon :size="32" class="stat-icon success"><CircleCheck /></el-icon>
          <div class="stat-info">
            <span class="stat-value">{{ statistics.activeUsers }}</span>
            <span class="stat-label">活跃用户</span>
          </div>
        </div>
      </el-card>
      <el-card class="stat-card">
        <div class="stat-content">
          <el-icon :size="32" class="stat-icon warning"><UserFilled /></el-icon>
          <div class="stat-info">
            <span class="stat-value">{{ statistics.adminUsers }}</span>
            <span class="stat-label">管理员</span>
          </div>
        </div>
      </el-card>
      <el-card class="stat-card">
        <div class="stat-content">
          <el-icon :size="32" class="stat-icon primary"><Plus /></el-icon>
          <div class="stat-info">
            <span class="stat-value">{{ statistics.newUsersThisMonth }}</span>
            <span class="stat-label">本月新增</span>
          </div>
        </div>
      </el-card>
    </div>

    <el-card class="table-card">
      <template #header>
        <div class="card-header">
          <div class="search-area">
            <el-input
              v-model="searchParams.keyword"
              placeholder="搜索用户名、邮箱"
              clearable
              @clear="handleSearch"
              @keyup.enter="handleSearch"
            >
              <template #prefix>
                <el-icon><Search /></el-icon>
              </template>
            </el-input>
            <el-select v-model="searchParams.role" placeholder="角色筛选" clearable @change="handleSearch">
              <el-option label="全部角色" value="" />
              <el-option v-for="role in roles" :key="role.id" :label="role.name" :value="role.code" />
            </el-select>
            <el-select v-model="searchParams.isActive" placeholder="状态筛选" clearable @change="handleSearch">
              <el-option label="全部状态" :value="undefined" />
              <el-option label="已激活" :value="true" />
              <el-option label="已禁用" :value="false" />
            </el-select>
          </div>
          <el-button type="primary" @click="handleSearch">
            <el-icon><Search /></el-icon>
            搜索
          </el-button>
        </div>
      </template>

      <el-table :data="users" v-loading="loading" stripe>
        <el-table-column label="用户" min-width="200">
          <template #default="{ row }">
            <div class="user-cell">
              <el-avatar :size="40" :src="row.avatarUrl">
                {{ row.name.charAt(0).toUpperCase() }}
              </el-avatar>
              <div class="user-info">
                <span class="user-name">{{ row.name }}</span>
                <span class="user-email">{{ row.email || '-' }}</span>
              </div>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="role" label="角色" width="120">
          <template #default="{ row }">
            <el-tag :type="getRoleTagType(row.role)">{{ getRoleName(row.role) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="departmentName" label="部门" width="150">
          <template #default="{ row }">
            {{ row.departmentId || '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="isActive" label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'danger'">
              {{ row.isActive ? '活跃' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="lastLoginAt" label="最后登录" width="180">
          <template #default="{ row }">
            {{ row.lastLoginAt ? formatDate(row.lastLoginAt) : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="创建时间" width="180">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button text type="primary" @click="handleEditRoles(row)">
              <el-icon><Key /></el-icon>
              分配角色
            </el-button>
            <el-dropdown @command="(cmd: string) => handleCommand(cmd, row)">
              <el-button text>
                <el-icon><More /></el-icon>
              </el-button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="activate" :disabled="row.isActive">
                    激活用户
                  </el-dropdown-item>
                  <el-dropdown-item command="deactivate" :disabled="!row.isActive">
                    禁用用户
                  </el-dropdown-item>
                  <el-dropdown-item command="permissions">
                    查看权限
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-container">
        <el-pagination
          v-model:current-page="pagination.page"
          v-model:page-size="pagination.pageSize"
          :page-sizes="[10, 20, 50, 100]"
          :total="pagination.total"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="loadUsers"
          @current-change="loadUsers"
        />
      </div>
    </el-card>

    <el-dialog v-model="roleDialogVisible" title="分配角色" width="500px">
      <el-form :model="roleForm" label-width="80px">
        <el-form-item label="用户">
          <el-input :value="currentUser?.name" disabled />
        </el-form-item>
        <el-form-item label="角色">
          <el-checkbox-group v-model="roleForm.roleIds">
            <el-checkbox v-for="role in roles" :key="role.id" :label="role.id">
              {{ role.name }}
            </el-checkbox>
          </el-checkbox-group>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="roleDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSaveRoles" :loading="saving">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="permissionDialogVisible" title="用户权限详情" width="700px">
      <div v-if="userPermissionDetail" class="permission-detail">
        <div class="detail-section">
          <h4>用户角色</h4>
          <div class="role-tags">
            <el-tag v-for="role in userPermissionDetail.roles" :key="role.id" class="role-tag">
              {{ role.name }}
            </el-tag>
            <span v-if="userPermissionDetail.roles.length === 0" class="empty-text">暂无角色</span>
          </div>
        </div>
        <div class="detail-section">
          <h4>有效权限 ({{ userPermissionDetail.effectivePermissions.length }})</h4>
          <div class="permission-list">
            <el-tag
              v-for="perm in userPermissionDetail.effectivePermissions"
              :key="perm"
              size="small"
              class="permission-tag"
            >
              {{ perm }}
            </el-tag>
          </div>
        </div>
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  User,
  CircleCheck,
  UserFilled,
  Plus,
  Search,
  Key,
  More,
} from '@element-plus/icons-vue'
import {
  getUsers,
  activateUser,
  deactivateUser,
  getUserStatistics,
} from '../api/user'
import {
  getAllRoles,
  getUserRoles,
  assignUserRoles,
  getUserPermissionDetail,
} from '../api/role'
import type {
  User as UserType,
  Role,
  UserStatisticsDto,
  UserPermissionDetail,
} from '../types'

const loading = ref(false)
const saving = ref(false)
const users = ref<UserType[]>([])
const roles = ref<Role[]>([])
const statistics = ref<UserStatisticsDto>({
  totalUsers: 0,
  activeUsers: 0,
  adminUsers: 0,
  newUsersThisMonth: 0,
})

const pagination = reactive({
  page: 1,
  pageSize: 20,
  total: 0,
})

const searchParams = reactive({
  keyword: '',
  role: '',
  isActive: undefined as boolean | undefined,
})

const roleDialogVisible = ref(false)
const permissionDialogVisible = ref(false)
const currentUser = ref<UserType | null>(null)
const userPermissionDetail = ref<UserPermissionDetail | null>(null)
const roleForm = reactive({
  roleIds: [] as number[],
})

onMounted(async () => {
  await Promise.all([loadUsers(), loadRoles(), loadStatistics()])
})

async function loadUsers() {
  loading.value = true
  try {
    const response = await getUsers({
      page: pagination.page,
      pageSize: pagination.pageSize,
      keyword: searchParams.keyword || undefined,
      role: searchParams.role || undefined,
      isActive: searchParams.isActive,
    })
    if (response.success) {
      users.value = response.data.items
      pagination.total = response.data.total
    }
  } catch (error) {
    console.error('加载用户列表失败:', error)
    ElMessage.error('加载用户列表失败')
  } finally {
    loading.value = false
  }
}

async function loadRoles() {
  try {
    const response = await getAllRoles()
    if (response.success) {
      roles.value = response.data
    }
  } catch (error) {
    console.error('加载角色列表失败:', error)
  }
}

async function loadStatistics() {
  try {
    const response = await getUserStatistics()
    if (response.success) {
      statistics.value = response.data
    }
  } catch (error) {
    console.error('加载统计数据失败:', error)
  }
}

function handleSearch() {
  pagination.page = 1
  loadUsers()
}

async function handleEditRoles(user: UserType) {
  currentUser.value = user
  try {
    const response = await getUserRoles(user.id)
    if (response.success) {
      roleForm.roleIds = response.data.map((r) => r.id)
    }
  } catch (error) {
    roleForm.roleIds = []
  }
  roleDialogVisible.value = true
}

async function handleSaveRoles() {
  if (!currentUser.value) return

  saving.value = true
  try {
    const response = await assignUserRoles({
      userId: currentUser.value.id,
      roleIds: roleForm.roleIds,
    })
    if (response.success) {
      ElMessage.success('角色分配成功')
      roleDialogVisible.value = false
      loadUsers()
    }
  } catch (error) {
    console.error('保存角色失败:', error)
    ElMessage.error('保存角色失败')
  } finally {
    saving.value = false
  }
}

async function handleCommand(command: string, user: UserType) {
  switch (command) {
    case 'activate':
      await handleActivate(user)
      break
    case 'deactivate':
      await handleDeactivate(user)
      break
    case 'permissions':
      await handleViewPermissions(user)
      break
  }
}

async function handleActivate(user: UserType) {
  try {
    await ElMessageBox.confirm(`确定要激活用户 "${user.name}" 吗？`, '确认激活')
    const response = await activateUser(user.id)
    if (response.success) {
      ElMessage.success('用户已激活')
      loadUsers()
      loadStatistics()
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('激活用户失败:', error)
      ElMessage.error('激活用户失败')
    }
  }
}

async function handleDeactivate(user: UserType) {
  try {
    await ElMessageBox.confirm(`确定要禁用用户 "${user.name}" 吗？`, '确认禁用')
    const response = await deactivateUser(user.id)
    if (response.success) {
      ElMessage.success('用户已禁用')
      loadUsers()
      loadStatistics()
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('禁用用户失败:', error)
      ElMessage.error('禁用用户失败')
    }
  }
}

async function handleViewPermissions(user: UserType) {
  try {
    const response = await getUserPermissionDetail(user.id)
    if (response.success) {
      userPermissionDetail.value = response.data
      permissionDialogVisible.value = true
    }
  } catch (error) {
    console.error('获取用户权限失败:', error)
    ElMessage.error('获取用户权限失败')
  }
}

function getRoleTagType(role: string): 'danger' | 'warning' | 'success' | 'info' {
  switch (role) {
    case 'admin':
      return 'danger'
    case 'manager':
      return 'warning'
    case 'department_admin':
      return 'success'
    default:
      return 'info'
  }
}

function getRoleName(role: string): string {
  const roleMap: Record<string, string> = {
    admin: '管理员',
    manager: '经理',
    user: '普通用户',
    department_admin: '部门管理员',
  }
  return roleMap[role] || role
}

function formatDate(date: string): string {
  return new Date(date).toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}
</script>

<style scoped>
.users-page {
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

.stats-cards {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  margin-bottom: 24px;
}

.stat-card {
  border-radius: var(--radius-lg);
}

.stat-content {
  display: flex;
  align-items: center;
  gap: 16px;
}

.stat-icon {
  color: var(--text-muted);
}

.stat-icon.success {
  color: var(--success-color);
}

.stat-icon.warning {
  color: var(--warning-color);
}

.stat-icon.primary {
  color: var(--primary-color);
}

.stat-info {
  display: flex;
  flex-direction: column;
}

.stat-value {
  font-size: 24px;
  font-weight: 600;
  color: var(--text-primary);
}

.stat-label {
  font-size: 14px;
  color: var(--text-secondary);
}

.table-card {
  border-radius: var(--radius-lg);
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.search-area {
  display: flex;
  gap: 12px;
}

.search-area .el-input {
  width: 250px;
}

.search-area .el-select {
  width: 150px;
}

.user-cell {
  display: flex;
  align-items: center;
  gap: 12px;
}

.user-info {
  display: flex;
  flex-direction: column;
}

.user-name {
  font-weight: 500;
  color: var(--text-primary);
}

.user-email {
  font-size: 12px;
  color: var(--text-muted);
}

.pagination-container {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}

.permission-detail {
  max-height: 500px;
  overflow-y: auto;
}

.detail-section {
  margin-bottom: 24px;
}

.detail-section h4 {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0 0 12px 0;
}

.role-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.role-tag {
  margin: 0;
}

.empty-text {
  color: var(--text-muted);
  font-size: 14px;
}

.permission-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.permission-tag {
  margin: 0;
}

@media (max-width: 1200px) {
  .stats-cards {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 768px) {
  .stats-cards {
    grid-template-columns: 1fr;
  }

  .search-area {
    flex-direction: column;
  }

  .search-area .el-input,
  .search-area .el-select {
    width: 100%;
  }
}
</style>
