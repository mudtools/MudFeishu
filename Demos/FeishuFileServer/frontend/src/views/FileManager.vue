<template>
  <div class="file-manager" :class="{ 'dark-mode': appStore.isDark }">
    <div class="main-content">
      <aside class="sidebar" :class="{ collapsed: appStore.sidebarCollapsed }">
        <div class="sidebar-header">
          <div class="logo-container">
            <div class="logo-icon gradient-bg">
              <el-icon size="24">
                <FolderOpened />
              </el-icon>
            </div>
            <transition name="fade">
              <span v-if="!appStore.sidebarCollapsed" class="logo-text gradient-text">文件管理器</span>
            </transition>
          </div>
          <el-button class="collapse-btn" text @click="appStore.toggleSidebar">
            <el-icon :size="20">
              <Fold v-if="!appStore.sidebarCollapsed" />
              <Expand v-else />
            </el-icon>
          </el-button>
        </div>

        <div class="sidebar-content" v-show="!appStore.sidebarCollapsed">
          <div class="quick-actions">
            <button class="action-btn btn-sync" @click="showSyncDialog = true">
              <el-icon>
                <Refresh />
              </el-icon>
              <span>同步云空间</span>
            </button>
            <button class="action-btn btn-primary" @click="handleUpload">
              <el-icon>
                <Upload />
              </el-icon>
              <span>上传文件</span>
            </button>
            <button class="action-btn btn-secondary" @click="handleCreateFolder">
              <el-icon>
                <FolderAdd />
              </el-icon>
              <span>新建文件夹</span>
            </button>
          </div>

          <div class="divider"></div>

          <div class="folder-section">
            <div class="section-header">
              <span class="section-title">文件夹</span>
              <el-button text size="small" @click="loadFolders">
                <el-icon>
                  <Refresh />
                </el-icon>
              </el-button>
            </div>
            <FolderTree :current-folder-token="currentFolderToken" @select="handleFolderSelect" @folder-created="loadFolders" @folder-updated="loadFolders" />
          </div>
        </div>
      </aside>

      <main class="content-area" @touchstart="handleTouchStart" @touchmove="handleTouchMove" @touchend="handleTouchEnd">
        <div class="toolbar glass-effect">
          <div class="toolbar-left">
            <el-button v-if="appStore.sidebarCollapsed" class="expand-btn" text @click="appStore.toggleSidebar">
              <el-icon>
                <Expand />
              </el-icon>
            </el-button>

            <div class="breadcrumb-modern">
              <el-icon class="breadcrumb-icon" @click="navigateToRoot">
                <HomeFilled />
              </el-icon>
              <template v-for="(item, index) in breadcrumbList" :key="index">
                <el-icon class="breadcrumb-separator">
                  <ArrowRight />
                </el-icon>
                <span class="breadcrumb-item" @click="navigateToFolder(item)">{{ item.folderName }}</span>
              </template>
            </div>
          </div>

          <div class="toolbar-right">
            <div class="search-container">
              <el-icon class="search-icon">
                <Search />
              </el-icon>
              <input v-model="searchKeyword" type="text" class="search-input" placeholder="搜索文件..." @input="handleSearch" />
              <transition name="fade">
                <el-icon v-if="searchKeyword" class="clear-icon" @click="clearSearch">
                  <Close />
                </el-icon>
              </transition>
            </div>

            <div class="view-toggle">
              <button class="view-btn" :class="{ active: fileStore.viewMode === 'list' }" @click="fileStore.viewMode = 'list'">
                <el-icon>
                  <List />
                </el-icon>
              </button>
              <button class="view-btn" :class="{ active: fileStore.viewMode === 'grid' }" @click="fileStore.viewMode = 'grid'">
                <el-icon>
                  <Grid />
                </el-icon>
              </button>
            </div>

            <button class="theme-toggle" @click="appStore.toggleTheme">
              <transition name="scale" mode="out-in">
                <el-icon :key="appStore.isDark ? 'dark' : 'light'" :size="20">
                  <Moon v-if="!appStore.isDark" />
                  <Sunny v-else />
                </el-icon>
              </transition>
            </button>

            <el-dropdown @command="handleUserCommand" trigger="click">
              <div class="user-avatar">
                <el-icon>
                  <User />
                </el-icon>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="profile">
                    <el-icon>
                      <UserFilled />
                    </el-icon>
                    <span>个人信息</span>
                  </el-dropdown-item>
                  <el-dropdown-item command="password">
                    <el-icon>
                      <Lock />
                    </el-icon>
                    <span>修改密码</span>
                  </el-dropdown-item>
                  <el-dropdown-item divided command="logout">
                    <el-icon>
                      <SwitchButton />
                    </el-icon>
                    <span>退出登录</span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </div>

        <div class="file-list-container" ref="fileListRef" v-loading="fileStore.loading" element-loading-background="transparent" @scroll="handleScroll" @dragover.prevent="handleDragOver" @dragleave.prevent="handleDragLeave"
             @drop.prevent="handleDrop">
          <transition name="fade">
            <div v-if="isDragging" class="drag-overlay">
              <div class="drag-content">
                <el-icon :size="60">
                  <Upload />
                </el-icon>
                <p>释放文件以上传</p>
              </div>
            </div>
          </transition>

          <transition name="slide">
            <div v-if="fileStore.selectedFiles.length > 0" class="batch-toolbar glass-effect">
              <div class="batch-info">
                <el-checkbox :model-value="isAllSelected" @change="handleSelectAll" />
                <span>已选择 {{ fileStore.selectedFiles.length }} 个文件</span>
              </div>
              <div class="batch-actions">
                <el-button type="primary" size="small" @click="handleBatchDownload">
                  <el-icon>
                    <Download />
                  </el-icon>
                  下载
                </el-button>
                <el-button type="warning" size="small" @click="handleBatchMove">
                  <el-icon>
                    <Rank />
                  </el-icon>
                  移动
                </el-button>
                <el-button type="info" size="small" @click="handleBatchCopy">
                  <el-icon>
                    <CopyDocument />
                  </el-icon>
                  复制
                </el-button>
                <el-button type="danger" size="small" @click="handleBatchDelete">
                  <el-icon>
                    <Delete />
                  </el-icon>
                  删除
                </el-button>
                <el-button size="small" @click="fileStore.clearSelection">
                  取消选择
                </el-button>
              </div>
            </div>
          </transition>

          <FileList :files="fileStore.files" :view-mode="fileStore.viewMode" :selected-files="fileStore.selectedFiles" @select="handleFileSelect" @preview="handleFilePreview" @download="handleFileDownload" @rename="handleFileRename"
                    @move="handleFileMove" @delete="handleFileDelete" @versions="handleVersionHistory" @share="handleFileShare" />

          <transition name="fade">
            <div v-if="!fileStore.loading && fileStore.files.length === 0" class="empty-state">
              <div class="empty-icon">
                <el-icon :size="80">
                  <FolderOpened />
                </el-icon>
              </div>
              <h3>暂无文件</h3>
              <p>拖拽文件到此处或点击上传按钮</p>
              <button class="action-btn btn-primary" @click="handleUpload">
                <el-icon>
                  <Upload />
                </el-icon>
                <span>上传文件</span>
              </button>
            </div>
          </transition>

          <transition name="slide">
            <div v-if="fileStore.totalCount > 0" class="pagination-container">
              <el-pagination v-model:current-page="fileStore.page" v-model:page-size="fileStore.pageSize" :total="fileStore.totalCount" :page-sizes="[10, 20, 50, 100]" layout="total, sizes, prev, pager, next" background
                             @size-change="handleSizeChange" @current-change="handlePageChange" />
            </div>
          </transition>
        </div>

        <transition name="fade">
          <div v-if="isPulling" class="pull-indicator" :style="{ top: pullDistance + 'px' }">
            <el-icon :class="{ 'rotate': pullDistance > 80 }" :size="24">
              <ArrowDown />
            </el-icon>
            <span>{{ pullDistance > 80 ? '释放刷新' : '下拉刷新' }}</span>
          </div>
        </transition>
      </main>
    </div>

    <transition name="scale">
      <FileUpload v-if="appStore.showUploadPanel" :folder-token="currentFolderToken" @close="appStore.setUploadPanel(false)" @success="handleUploadSuccess" />
    </transition>

    <transition name="scale">
      <FolderDialog v-model="showCreateFolderDialog" mode="create" :parent-token="currentFolderToken" @success="handleCreateFolderSuccess" />
    </transition>

    <transition name="scale">
      <RenameDialog v-if="renameDialog.visible" :current-name="renameDialog.currentName" :type="renameDialog.type" @close="renameDialog.visible = false" @confirm="handleRenameConfirm" />
    </transition>

    <transition name="scale">
      <MoveDialog v-if="moveDialog.visible" :item-token="moveDialog.itemToken" :item-type="moveDialog.itemType" @close="moveDialog.visible = false" @success="handleMoveSuccess" />
    </transition>

    <transition name="scale">
      <VersionHistory v-if="versionHistoryVisible" :file-token="currentFileToken" :file-name="currentFileName" @close="versionHistoryVisible = false" />
    </transition>

    <transition name="scale">
      <UserProfileDialog v-if="userProfileVisible" @close="userProfileVisible = false" />
    </transition>

    <transition name="scale">
      <ChangePasswordDialog v-if="changePasswordVisible" @close="changePasswordVisible = false" />
    </transition>

    <ShareDialog v-model="shareDialogVisible" :resource-type="shareResource.type" :resource-token="shareResource.token" :resource-name="shareResource.name" />

    <OperationLogDrawer v-model="operationLogVisible" />

    <el-dialog v-model="showSyncDialog" title="飞书云空间同步" width="500px" :close-on-click-modal="false">
      <SyncPanel @sync-complete="handleSyncComplete" />
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { ElMessage, ElMessageBox } from "element-plus"
import {
  Upload,
  FolderAdd,
  Search,
  List,
  Grid,
  HomeFilled,
  Fold,
  Expand,
  User,
  ArrowDown,
  UserFilled,
  Lock,
  SwitchButton,
  ArrowRight,
  Refresh,
  Close,
  Moon,
  Sunny,
  FolderOpened,
  Download,
  Rank,
  CopyDocument,
  Delete,
} from "@element-plus/icons-vue"
import { useFileStore } from "@/stores/fileStore"
import { useFolderStore } from "@/stores/folderStore"
import { useAppStore } from "@/stores/appStore"
import { useAuthStore } from "@/stores/authStore"
import { folderApi, fileApi } from "@/api"
import FolderTree from "@/components/FolderTree.vue"
import FileList from "@/components/FileList.vue"
import FileUpload from "@/components/FileUpload.vue"
import FolderDialog from "@/components/FolderDialog.vue"
import RenameDialog from "@/components/RenameDialog.vue"
import MoveDialog from "@/components/MoveDialog.vue"
import VersionHistory from "@/components/VersionHistory.vue"
import UserProfileDialog from "@/components/UserProfileDialog.vue"
import ChangePasswordDialog from "@/components/ChangePasswordDialog.vue"
import ShareDialog from "@/components/ShareDialog.vue"
import OperationLogDrawer from "@/components/OperationLogDrawer.vue"
import SyncPanel from "@/components/SyncPanel.vue"

const route = useRoute()
const router = useRouter()
const fileStore = useFileStore()
const folderStore = useFolderStore()
const appStore = useAppStore()
const authStore = useAuthStore()

const searchKeyword = ref("")
const showCreateFolderDialog = ref(false)
const currentFolderToken = ref<string | undefined>(undefined)
const currentFileToken = ref("")
const currentFileName = ref("")
const versionHistoryVisible = ref(false)
const userProfileVisible = ref(false)
const changePasswordVisible = ref(false)
const shareDialogVisible = ref(false)
const operationLogVisible = ref(false)
const showSyncDialog = ref(false)
const isDragging = ref(false)
const shareResource = ref<{
  type: "File" | "Folder"
  token: string
  name: string
}>({
  type: "File",
  token: "",
  name: "",
})

const isAllSelected = computed(() => {
  return (
    fileStore.files.length > 0 &&
    fileStore.selectedFiles.length === fileStore.files.length
  )
})

const isPulling = ref(false)
const pullDistance = ref(0)
const touchStartY = ref(0)
const scrollTop = ref(0)

const breadcrumbList = computed(() => folderStore.currentFolderPath)

const renameDialog = ref<{
  visible: boolean
  currentName: string
  type: "file" | "folder"
  token: string
}>({
  visible: false,
  currentName: "",
  type: "file",
  token: "",
})

const moveDialog = ref<{
  visible: boolean
  itemToken: string
  itemType: "file" | "folder" | "batch"
}>({
  visible: false,
  itemToken: "",
  itemType: "file",
})

const loadFolders = async () => {
  try {
    folderStore.setLoading(true)
    const response = await folderApi.getList()
    folderStore.setFolders(response.folders)
  } catch (error) {
    ElMessage.error("加载文件夹失败")
  } finally {
    folderStore.setLoading(false)
  }
}

const loadFiles = async () => {
  try {
    fileStore.setLoading(true)
    const response = await fileApi.getList(
      currentFolderToken.value,
      searchKeyword.value,
      fileStore.page,
      fileStore.pageSize
    )
    fileStore.setFiles(response.files, response.totalCount)
  } catch (error) {
    ElMessage.error("加载文件列表失败")
  } finally {
    fileStore.setLoading(false)
  }
}

const handleFolderSelect = (folder: any) => {
  currentFolderToken.value = folder?.folderToken ?? undefined
  folderStore.setCurrentFolder(folder)
  fileStore.page = 1
  loadFiles()
}

const handleFileSelect = (fileToken: string) => {
  fileStore.toggleSelect(fileToken)
}

const handleFilePreview = (file: any) => {
  currentFileToken.value = file.fileToken
}

const handleFileDownload = async (file: any) => {
  try {
    const response = await fileApi.download(file.fileToken)
    const blob = new Blob([response as any])
    const link = document.createElement("a")
    link.href = window.URL.createObjectURL(blob)
    link.download = file.fileName
    link.click()
    ElMessage.success("下载成功")
  } catch (error) {
    ElMessage.error("下载失败")
  }
}

const handleFileRename = (file: any) => {
  renameDialog.value = {
    visible: true,
    currentName: file.fileName,
    type: "file",
    token: file.fileToken,
  }
}

const handleFileMove = (file: any) => {
  moveDialog.value = {
    visible: true,
    itemToken: file.fileToken,
    itemType: "file",
  }
}

const handleFileDelete = async (file: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除 "${file.fileName}" 吗？`, "提示", {
      type: "warning",
    })
    await fileApi.delete(file.fileToken)
    fileStore.removeFile(file.fileToken)
    ElMessage.success("删除成功")
  } catch (error: any) {
    if (error !== "cancel") {
      ElMessage.error("删除失败")
    }
  }
}

const handleVersionHistory = (file: any) => {
  currentFileToken.value = file.fileToken
  currentFileName.value = file.fileName
  versionHistoryVisible.value = true
}

const handleFileShare = (file: any) => {
  shareResource.value = {
    type: "File",
    token: file.fileToken,
    name: file.fileName,
  }
  shareDialogVisible.value = true
}

const handleSelectAll = (val: boolean) => {
  if (val) {
    fileStore.files.forEach((f) => {
      if (!fileStore.selectedFiles.includes(f.fileToken)) {
        fileStore.selectedFiles.push(f.fileToken)
      }
    })
  } else {
    fileStore.clearSelection()
  }
}

const handleBatchDownload = async () => {
  if (fileStore.selectedFiles.length === 0) {
    ElMessage.warning("请先选择文件")
    return
  }
  try {
    const { batchApi } = await import("@/api")
    const response = await batchApi.download({
      fileTokens: fileStore.selectedFiles,
      folderTokens: [],
    })
    const blob = response.data as Blob
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement("a")
    a.href = url
    a.download = `download_${new Date()
      .toISOString()
      .slice(0, 19)
      .replace(/[:-]/g, "")}.zip`
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    window.URL.revokeObjectURL(url)
    fileStore.clearSelection()
    ElMessage.success("批量下载成功")
  } catch (error) {
    ElMessage.error("批量下载失败")
  }
}

const handleBatchMove = () => {
  if (fileStore.selectedFiles.length === 0) {
    ElMessage.warning("请先选择文件")
    return
  }
  moveDialog.value = {
    visible: true,
    itemToken: fileStore.selectedFiles[0],
    itemType: "batch",
  }
}

const handleBatchCopy = () => {
  ElMessage.info("批量复制功能开发中")
}

const handleBatchDelete = async () => {
  if (fileStore.selectedFiles.length === 0) {
    ElMessage.warning("请先选择文件")
    return
  }
  try {
    await ElMessageBox.confirm(
      `确定要删除选中的 ${fileStore.selectedFiles.length} 个文件吗？`,
      "提示",
      {
        type: "warning",
      }
    )
    const { batchApi } = await import("@/api")
    await batchApi.delete({
      fileTokens: fileStore.selectedFiles,
      folderTokens: [],
    })
    fileStore.clearSelection()
    loadFiles()
    ElMessage.success("批量删除成功")
  } catch (error: any) {
    if (error !== "cancel") {
      ElMessage.error("批量删除失败")
    }
  }
}

const handleDragOver = (_e: DragEvent) => {
  isDragging.value = true
}

const handleDragLeave = (_e: DragEvent) => {
  isDragging.value = false
}

const handleDrop = async (e: DragEvent) => {
  isDragging.value = false
  const files = e.dataTransfer?.files
  if (!files || files.length === 0) return

  for (const file of Array.from(files)) {
    const formData = new FormData()
    formData.append("file", file)
    try {
      await fileApi.upload(formData, currentFolderToken.value)
    } catch (error) {
      ElMessage.error(`上传 ${file.name} 失败`)
    }
  }
  loadFiles()
  ElMessage.success(`成功上传 ${files.length} 个文件`)
}

const handleUpload = () => {
  appStore.setUploadPanel(true)
}

const handleCreateFolder = () => {
  showCreateFolderDialog.value = true
}

const handleUploadSuccess = () => {
  loadFiles()
}

const handleCreateFolderSuccess = () => {
  loadFolders()
}

const handleRenameConfirm = async (newName: string) => {
  try {
    if (renameDialog.value.type === "file") {
      await fileApi.rename(renameDialog.value.token, newName)
    } else {
      await folderApi.update(renameDialog.value.token, { name: newName })
    }
    renameDialog.value.visible = false
    ElMessage.success("重命名成功")
    loadFiles()
    loadFolders()
  } catch (error) {
    ElMessage.error("重命名失败")
  }
}

const handleMoveSuccess = () => {
  moveDialog.value.visible = false
  ElMessage.success("移动成功")
  loadFiles()
  loadFolders()
}

const handleSyncComplete = () => {
  showSyncDialog.value = false
  loadFiles()
  loadFolders()
}

const handleSearch = () => {
  fileStore.page = 1
  loadFiles()
}

const clearSearch = () => {
  searchKeyword.value = ""
  handleSearch()
}

const handleSizeChange = () => {
  loadFiles()
}

const handlePageChange = () => {
  loadFiles()
}

const navigateToRoot = () => {
  currentFolderToken.value = undefined
  folderStore.setCurrentFolder(null)
  fileStore.page = 1
  loadFiles()
}

const navigateToFolder = (folder: any) => {
  currentFolderToken.value = folder.folderToken
  folderStore.setCurrentFolder(folder)
  fileStore.page = 1
  loadFiles()
}

const handleUserCommand = async (command: string) => {
  switch (command) {
    case "profile":
      userProfileVisible.value = true
      break
    case "password":
      changePasswordVisible.value = true
      break
    case "logout":
      try {
        await ElMessageBox.confirm("确定要退出登录吗？", "提示", {
          type: "warning",
        })
        await authStore.logout()
        router.push("/login")
      } catch (error: any) {
        if (error !== "cancel") {
          console.error("退出登录失败:", error)
        }
      }
      break
  }
}

const handleScroll = (e: Event) => {
  const target = e.target as HTMLElement
  scrollTop.value = target.scrollTop
}

const handleTouchStart = (e: TouchEvent) => {
  if (scrollTop.value === 0) {
    touchStartY.value = e.touches[0].clientY
  }
}

const handleTouchMove = (e: TouchEvent) => {
  if (scrollTop.value === 0 && touchStartY.value > 0) {
    const currentY = e.touches[0].clientY
    const diff = currentY - touchStartY.value
    if (diff > 0) {
      isPulling.value = true
      pullDistance.value = Math.min(diff * 0.5, 100)
    }
  }
}

const handleTouchEnd = async () => {
  if (pullDistance.value > 80) {
    appStore.startRefreshing()
    await loadFiles()
    appStore.stopRefreshing()
    ElMessage.success("刷新成功")
  }
  isPulling.value = false
  pullDistance.value = 0
  touchStartY.value = 0
}

watch(
  () => route.params.folderToken,
  (newToken) => {
    currentFolderToken.value = newToken as string | undefined
    loadFiles()
  }
)

onMounted(() => {
  loadFolders()
  loadFiles()
})
</script>

<style scoped lang="scss">
.file-manager {
  width: 100%;
  height: 100vh;
  display: flex;
  flex-direction: column;
  background: var(--bg-color);
  transition: background-color var(--transition-normal);
}

.sidebar {
  width: 280px;
  min-width: 280px;
  background: var(--bg-secondary);
  border-right: 1px solid var(--border-color);
  overflow: hidden;
  transition: all var(--transition-normal);
  position: relative;
  z-index: 10;
  display: flex;
  flex-direction: column;
  color: var(--text-primary);
}

.sidebar.collapsed {
  width: 64px;
  min-width: 64px;
}

.sidebar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-md);
  border-bottom: 1px solid var(--border-light);
}

.logo-container {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

.logo-icon {
  width: 40px;
  height: 40px;
  border-radius: var(--radius-md);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  flex-shrink: 0;
}

.logo-text {
  font-size: 18px;
  font-weight: 700;
  white-space: nowrap;
}

.collapse-btn {
  color: var(--text-secondary);

  &:hover {
    color: var(--primary-color);
  }
}

.sidebar-content {
  flex: 1;
  overflow-y: auto;
  padding: var(--spacing-md);
  color: var(--text-primary);
}

.quick-actions {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.divider {
  height: 1px;
  background: var(--border-color);
  margin: var(--spacing-md) 0;
}

.folder-section {
  color: var(--text-primary);
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--spacing-sm);
}

.section-title {
  font-size: 12px;
  font-weight: 600;
  color: var(--text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.action-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-sm) var(--spacing-md);
  border-radius: var(--radius-md);
  font-weight: 500;
  font-size: 14px;
  transition: all var(--transition-fast);
  cursor: pointer;
  border: none;
  width: 100%;

  &:active {
    transform: scale(0.98);
  }
}

.btn-primary {
  background: linear-gradient(
    135deg,
    var(--primary-color) 0%,
    var(--primary-dark) 100%
  );
  color: white;
  box-shadow: var(--shadow-md), 0 4px 12px rgba(99, 102, 241, 0.25);

  &:hover {
    box-shadow: var(--shadow-lg), 0 6px 20px rgba(99, 102, 241, 0.35);
    transform: translateY(-1px);
  }
}

.btn-secondary {
  background: var(--bg-secondary);
  color: var(--text-primary);
  border: 1px solid var(--border-color);

  &:hover {
    background: var(--bg-tertiary);
    border-color: var(--primary-color);
    color: var(--primary-color);
  }
}

.btn-sync {
  background: linear-gradient(
    135deg,
    #10b981 0%,
    #059669 100%
  );
  color: white;
  box-shadow: var(--shadow-md), 0 4px 12px rgba(16, 185, 129, 0.25);

  &:hover {
    box-shadow: var(--shadow-lg), 0 6px 20px rgba(16, 185, 129, 0.35);
    transform: translateY(-1px);
  }
}

.content-area {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  position: relative;
}

.toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-sm) var(--spacing-lg);
  border-bottom: 1px solid var(--border-light);
  position: sticky;
  top: 0;
  z-index: var(--z-sticky);
  flex-shrink: 0;
}

.toolbar-left,
.toolbar-right {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  flex-shrink: 0;
}

.breadcrumb-modern {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);

  .breadcrumb-icon {
    cursor: pointer;
    color: var(--text-secondary);
    transition: color var(--transition-fast);

    &:hover {
      color: var(--primary-color);
    }
  }

  .breadcrumb-separator {
    color: var(--text-tertiary);
    font-size: 12px;
  }

  .breadcrumb-item {
    color: var(--text-secondary);
    font-size: 14px;
    cursor: pointer;
    transition: color var(--transition-fast);

    &:hover {
      color: var(--primary-color);
    }
  }
}

.search-container {
  position: relative;
  display: flex;
  align-items: center;
}

.search-icon {
  position: absolute;
  left: var(--spacing-sm);
  color: var(--text-tertiary);
}

.search-input {
  width: 220px;
  padding: var(--spacing-sm) var(--spacing-md);
  padding-left: 36px;
  padding-right: 36px;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-full);
  font-size: 14px;
  color: var(--text-primary);
  transition: all var(--transition-fast);
  outline: none;

  &:focus {
    border-color: var(--primary-color);
    box-shadow: 0 0 0 3px var(--primary-bg);
    width: 280px;
  }

  &::placeholder {
    color: var(--text-tertiary);
  }
}

.clear-icon {
  position: absolute;
  right: var(--spacing-sm);
  color: var(--text-tertiary);
  cursor: pointer;
  transition: color var(--transition-fast);

  &:hover {
    color: var(--text-primary);
  }
}

.view-toggle {
  display: flex;
  background: var(--bg-tertiary);
  border-radius: var(--radius-md);
  padding: 2px;
}

.view-btn {
  padding: var(--spacing-xs) var(--spacing-sm);
  border: none;
  background: transparent;
  color: var(--text-secondary);
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: all var(--transition-fast);

  &.active {
    background: var(--bg-secondary);
    color: var(--primary-color);
    box-shadow: var(--shadow-sm);
  }

  &:hover:not(.active) {
    color: var(--text-primary);
  }
}

.theme-toggle {
  width: 40px;
  height: 40px;
  border: none;
  background: var(--bg-tertiary);
  border-radius: var(--radius-full);
  color: var(--text-secondary);
  cursor: pointer;
  transition: all var(--transition-fast);
  display: flex;
  align-items: center;
  justify-content: center;

  &:hover {
    background: var(--primary-bg);
    color: var(--primary-color);
  }
}

.user-avatar {
  width: 40px;
  height: 40px;
  border-radius: var(--radius-full);
  background: linear-gradient(
    135deg,
    var(--primary-color) 0%,
    var(--primary-light) 100%
  );
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all var(--transition-fast);
  flex-shrink: 0;

  &:hover {
    transform: scale(1.05);
    box-shadow: var(--shadow-md);
  }
}

.file-list-container {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  padding: var(--spacing-lg);
  position: relative;
  display: flex;
  flex-direction: column;
}

.file-grid {
  display: grid;
  gap: var(--spacing-md);

  &.list {
    grid-template-columns: 1fr;
  }

  &.grid {
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  }
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-2xl);
  text-align: center;

  .empty-icon {
    color: var(--text-tertiary);
    margin-bottom: var(--spacing-lg);
    animation: float 3s ease-in-out infinite;
  }

  h3 {
    font-size: 20px;
    font-weight: 600;
    color: var(--text-primary);
    margin-bottom: var(--spacing-sm);
  }

  p {
    color: var(--text-secondary);
    margin-bottom: var(--spacing-lg);
  }
}

@keyframes float {
  0%,
  100% {
    transform: translateY(0);
  }
  50% {
    transform: translateY(-10px);
  }
}

.pagination-container {
  display: flex;
  justify-content: center;
  padding: var(--spacing-lg) 0;
}

.pull-indicator {
  position: absolute;
  left: 50%;
  transform: translateX(-50%);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-xs);
  color: var(--text-secondary);
  font-size: 14px;

  .el-icon {
    transition: transform var(--transition-fast);

    &.rotate {
      transform: rotate(180deg);
    }
  }
}

.expand-btn {
  color: var(--text-secondary);

  &:hover {
    color: var(--primary-color);
  }
}

.batch-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-md);
  margin-bottom: var(--spacing-md);
  border-radius: var(--radius-lg);
  position: sticky;
  top: 0;
  z-index: 10;
}

.batch-info {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);

  span {
    font-weight: 500;
    color: var(--text-primary);
  }
}

.batch-actions {
  display: flex;
  gap: var(--spacing-sm);
}

.drag-overlay {
  position: absolute;
  inset: 0;
  background: rgba(var(--primary-rgb), 0.1);
  border: 2px dashed var(--primary-color);
  border-radius: var(--radius-lg);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 100;
  backdrop-filter: blur(4px);
}

.drag-content {
  text-align: center;
  color: var(--primary-color);

  .el-icon {
    margin-bottom: var(--spacing-md);
  }

  p {
    font-size: 18px;
    font-weight: 500;
  }
}
</style>
