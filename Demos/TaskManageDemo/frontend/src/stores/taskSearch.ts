import { defineStore } from "pinia";
import { ref, computed } from "vue";
import { taskApi } from "../api";
import type { Task, TaskSearchParams, PagedResponse } from "../types";

/**
 * 任务搜索状态管理
 * 负责搜索相关的状态和操作
 */
export const useTaskSearchStore = defineStore("taskSearch", () => {
  // ==================== State ====================
  const tasks = ref<Task[]>([]);
  const total = ref(0);
  const page = ref(1);
  const pageSize = ref(20);
  const loading = ref(false);
  const error = ref<string | null>(null);

  const searchParams = ref<TaskSearchParams>({
    keyword: "",
    status: "all",
    priority: undefined,
    assigneeId: undefined,
    taskListId: undefined,
    dueDateFrom: undefined,
    dueDateTo: undefined,
    includeCompleted: true,
    page: 1,
    pageSize: 20,
    sortBy: "createdAt",
    sortDescending: true,
  });

  // ==================== Getters ====================
  const hasMore = computed(() => tasks.value.length < total.value);
  const isEmpty = computed(() => tasks.value.length === 0 && !loading.value);
  const currentFilters = computed(() => {
    const filters: string[] = [];
    if (searchParams.value.keyword) filters.push(`关键词: ${searchParams.value.keyword}`);
    if (searchParams.value.status && searchParams.value.status !== "all") {
      filters.push(`状态: ${searchParams.value.status}`);
    }
    if (searchParams.value.priority !== undefined) {
      filters.push(`优先级: ${searchParams.value.priority}`);
    }
    return filters;
  });

  // ==================== Actions ====================

  /**
   * 搜索任务
   */
  async function searchTasks(params?: Partial<TaskSearchParams>) {
    loading.value = true;
    error.value = null;
    try {
      const mergedParams = { ...searchParams.value, ...params, page: 1 };
      const response: PagedResponse<Task> = await taskApi.searchTasks(mergedParams);
      tasks.value = response.items;
      total.value = response.total;
      page.value = response.page;
      pageSize.value = response.pageSize;
      searchParams.value = mergedParams;
    } catch (e) {
      error.value = e instanceof Error ? e.message : "搜索任务失败";
      tasks.value = [];
    } finally {
      loading.value = false;
    }
  }

  /**
   * 获取任务列表（分页）
   */
  async function fetchTasks(params?: Partial<TaskSearchParams>) {
    loading.value = true;
    error.value = null;
    try {
      const mergedParams = { ...searchParams.value, ...params };
      const response: PagedResponse<Task> = await taskApi.getTasks(mergedParams);
      tasks.value = response.items;
      total.value = response.total;
      page.value = response.page;
      pageSize.value = response.pageSize;
      searchParams.value = mergedParams;
    } catch (e) {
      error.value = e instanceof Error ? e.message : "获取任务列表失败";
    } finally {
      loading.value = false;
    }
  }

  /**
   * 加载更多任务
   */
  async function loadMore() {
    if (!hasMore.value || loading.value) return;

    loading.value = true;
    try {
      const nextPage = page.value + 1;
      const response: PagedResponse<Task> = await taskApi.getTasks({
        ...searchParams.value,
        page: nextPage,
      });
      tasks.value.push(...response.items);
      page.value = nextPage;
    } catch (e) {
      error.value = e instanceof Error ? e.message : "加载更多任务失败";
    } finally {
      loading.value = false;
    }
  }

  /**
   * 更新搜索参数
   */
  function updateSearchParams(params: Partial<TaskSearchParams>) {
    searchParams.value = { ...searchParams.value, ...params };
  }

  /**
   * 重置搜索参数
   */
  function resetSearchParams() {
    searchParams.value = {
      keyword: "",
      status: "all",
      priority: undefined,
      assigneeId: undefined,
      taskListId: undefined,
      dueDateFrom: undefined,
      dueDateTo: undefined,
      includeCompleted: true,
      page: 1,
      pageSize: 20,
      sortBy: "createdAt",
      sortDescending: true,
    };
  }

  /**
   * 清空搜索结果
   */
  function clearResults() {
    tasks.value = [];
    total.value = 0;
    page.value = 1;
    error.value = null;
  }

  /**
   * 根据ID更新任务（用于实时更新）
   */
  function updateTaskInList(updatedTask: Task) {
    const index = tasks.value.findIndex((t) => t.id === updatedTask.id);
    if (index !== -1) {
      tasks.value[index] = updatedTask;
    }
  }

  /**
   * 从列表中移除任务
   */
  function removeTaskFromList(taskId: number) {
    const index = tasks.value.findIndex((t) => t.id === taskId);
    if (index !== -1) {
      tasks.value.splice(index, 1);
      total.value--;
    }
  }

  /**
   * 添加任务到列表
   */
  function addTaskToList(task: Task) {
    tasks.value.unshift(task);
    total.value++;
  }

  return {
    // State
    tasks,
    total,
    page,
    pageSize,
    loading,
    error,
    searchParams,
    // Getters
    hasMore,
    isEmpty,
    currentFilters,
    // Actions
    searchTasks,
    fetchTasks,
    loadMore,
    updateSearchParams,
    resetSearchParams,
    clearResults,
    updateTaskInList,
    removeTaskFromList,
    addTaskToList,
  };
});
