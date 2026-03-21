import { defineStore } from "pinia";
import { ref, computed } from "vue";

/**
 * 任务UI状态管理
 * 负责UI相关的状态，如对话框显示、选择状态、视图模式等
 */
export const useTaskUIStore = defineStore("taskUI", () => {
  // ==================== State ====================

  // 对话框状态
  const createDialogVisible = ref(false);
  const editDialogVisible = ref(false);
  const deleteConfirmVisible = ref(false);
  const detailDrawerVisible = ref(false);

  // 视图模式
  type ViewMode = "list" | "kanban" | "calendar";
  const viewMode = ref<ViewMode>("list");

  // 侧边栏状态
  const sidebarCollapsed = ref(false);

  // 任务选择状态
  const selectedTaskIds = ref<number[]>([]);
  const lastSelectedTaskId = ref<number | null>(null);

  // 拖拽状态
  const isDragging = ref(false);
  const draggedTaskId = ref<number | null>(null);

  // 批量操作状态
  const batchMode = ref(false);

  // 通知状态
  const notifications = ref<Array<{
    id: string;
    type: "success" | "error" | "warning" | "info";
    message: string;
    duration?: number;
  }>>([]);

  // ==================== Getters ====================

  const hasSelection = computed(() => selectedTaskIds.value.length > 0);
  const selectedCount = computed(() => selectedTaskIds.value.length);
  const isTaskSelected = computed(() => (taskId: number) =>
    selectedTaskIds.value.includes(taskId)
  );

  // ==================== Actions ====================

  // 对话框操作
  function openCreateDialog() {
    createDialogVisible.value = true;
  }

  function closeCreateDialog() {
    createDialogVisible.value = false;
  }

  function openEditDialog() {
    editDialogVisible.value = true;
  }

  function closeEditDialog() {
    editDialogVisible.value = false;
  }

  function openDeleteConfirm() {
    deleteConfirmVisible.value = true;
  }

  function closeDeleteConfirm() {
    deleteConfirmVisible.value = false;
  }

  function openDetailDrawer() {
    detailDrawerVisible.value = true;
  }

  function closeDetailDrawer() {
    detailDrawerVisible.value = false;
  }

  // 视图模式操作
  function setViewMode(mode: ViewMode) {
    viewMode.value = mode;
  }

  // 侧边栏操作
  function toggleSidebar() {
    sidebarCollapsed.value = !sidebarCollapsed.value;
  }

  function setSidebarCollapsed(collapsed: boolean) {
    sidebarCollapsed.value = collapsed;
  }

  // 选择操作
  function selectTask(taskId: number, multiSelect = false) {
    if (multiSelect) {
      if (selectedTaskIds.value.includes(taskId)) {
        selectedTaskIds.value = selectedTaskIds.value.filter((id) => id !== taskId);
      } else {
        selectedTaskIds.value.push(taskId);
      }
    } else {
      selectedTaskIds.value = [taskId];
    }
    lastSelectedTaskId.value = taskId;
  }

  function selectRange(startId: number, endId: number, allTaskIds: number[]) {
    const startIndex = allTaskIds.indexOf(startId);
    const endIndex = allTaskIds.indexOf(endId);

    if (startIndex === -1 || endIndex === -1) return;

    const rangeStart = Math.min(startIndex, endIndex);
    const rangeEnd = Math.max(startIndex, endIndex);

    const rangeIds = allTaskIds.slice(rangeStart, rangeEnd + 1);

    // 合并当前选择范围
    const newSelection = new Set([...selectedTaskIds.value, ...rangeIds]);
    selectedTaskIds.value = Array.from(newSelection);
  }

  function clearSelection() {
    selectedTaskIds.value = [];
    lastSelectedTaskId.value = null;
  }

  function selectAll(taskIds: number[]) {
    selectedTaskIds.value = [...taskIds];
  }

  // 批量操作
  function enterBatchMode() {
    batchMode.value = true;
  }

  function exitBatchMode() {
    batchMode.value = false;
    clearSelection();
  }

  // 拖拽操作
  function startDrag(taskId: number) {
    isDragging.value = true;
    draggedTaskId.value = taskId;
  }

  function endDrag() {
    isDragging.value = false;
    draggedTaskId.value = null;
  }

  // 通知操作
  function showNotification(
    type: "success" | "error" | "warning" | "info",
    message: string,
    duration = 3000
  ) {
    const id = Date.now().toString();
    notifications.value.push({ id, type, message, duration });

    if (duration > 0) {
      setTimeout(() => {
        removeNotification(id);
      }, duration);
    }

    return id;
  }

  function removeNotification(id: string) {
    const index = notifications.value.findIndex((n) => n.id === id);
    if (index !== -1) {
      notifications.value.splice(index, 1);
    }
  }

  function clearAllNotifications() {
    notifications.value = [];
  }

  // 重置所有UI状态
  function resetUI() {
    createDialogVisible.value = false;
    editDialogVisible.value = false;
    deleteConfirmVisible.value = false;
    detailDrawerVisible.value = false;
    selectedTaskIds.value = [];
    lastSelectedTaskId.value = null;
    isDragging.value = false;
    draggedTaskId.value = null;
    batchMode.value = false;
    notifications.value = [];
  }

  return {
    // State
    createDialogVisible,
    editDialogVisible,
    deleteConfirmVisible,
    detailDrawerVisible,
    viewMode,
    sidebarCollapsed,
    selectedTaskIds,
    lastSelectedTaskId,
    isDragging,
    draggedTaskId,
    batchMode,
    notifications,
    // Getters
    hasSelection,
    selectedCount,
    isTaskSelected,
    // Actions
    openCreateDialog,
    closeCreateDialog,
    openEditDialog,
    closeEditDialog,
    openDeleteConfirm,
    closeDeleteConfirm,
    openDetailDrawer,
    closeDetailDrawer,
    setViewMode,
    toggleSidebar,
    setSidebarCollapsed,
    selectTask,
    selectRange,
    clearSelection,
    selectAll,
    enterBatchMode,
    exitBatchMode,
    startDrag,
    endDrag,
    showNotification,
    removeNotification,
    clearAllNotifications,
    resetUI,
  };
});
