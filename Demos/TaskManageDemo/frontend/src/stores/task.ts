import { defineStore } from "pinia";
import { ref, computed } from "vue";
import { taskApi } from "../api";
import type {
  Task,
  TaskSearchParams,
  CreateTaskRequest,
  UpdateTaskRequest,
  PagedResponse,
} from "../types";

export const useTaskStore = defineStore("task", () => {
  const tasks = ref<Task[]>([]);
  const currentTask = ref<Task | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const total = ref(0);
  const page = ref(1);
  const pageSize = ref(20);

  const searchParams = ref<TaskSearchParams>({
    keyword: "",
    status: "all",
    includeCompleted: true,
    page: 1,
    pageSize: 20,
    sortBy: "createdAt",
    sortDescending: true,
  });

  const hasMore = computed(() => tasks.value.length < total.value);

  async function fetchTasks(params?: Partial<TaskSearchParams>) {
    loading.value = true;
    error.value = null;
    try {
      const mergedParams = { ...searchParams.value, ...params };
      const response: PagedResponse<Task> =
        await taskApi.getTasks(mergedParams);
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

  async function fetchTask(id: number) {
    loading.value = true;
    error.value = null;
    try {
      currentTask.value = await taskApi.getTask(id);
    } catch (e) {
      error.value = e instanceof Error ? e.message : "获取任务详情失败";
    } finally {
      loading.value = false;
    }
  }

  async function createTask(request: CreateTaskRequest) {
    loading.value = true;
    error.value = null;
    try {
      const task = await taskApi.createTask(request);
      tasks.value.unshift(task);
      total.value++;
      return task;
    } catch (e) {
      error.value = e instanceof Error ? e.message : "创建任务失败";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function updateTask(id: number, request: UpdateTaskRequest) {
    loading.value = true;
    error.value = null;
    try {
      const task = await taskApi.updateTask(id, request);
      const index = tasks.value.findIndex((t) => t.id === id);
      if (index !== -1) {
        tasks.value[index] = task;
      }
      if (currentTask.value?.id === id) {
        currentTask.value = task;
      }
      return task;
    } catch (e) {
      error.value = e instanceof Error ? e.message : "更新任务失败";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function deleteTask(id: number) {
    loading.value = true;
    error.value = null;
    try {
      await taskApi.deleteTask(id);
      tasks.value = tasks.value.filter((t) => t.id !== id);
      total.value--;
      if (currentTask.value?.id === id) {
        currentTask.value = null;
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : "删除任务失败";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function completeTask(id: number) {
    loading.value = true;
    error.value = null;
    try {
      const task = await taskApi.completeTask(id);
      const index = tasks.value.findIndex((t) => t.id === id);
      if (index !== -1) {
        tasks.value[index] = task;
      }
      if (currentTask.value?.id === id) {
        currentTask.value = task;
      }
      return task;
    } catch (e) {
      error.value = e instanceof Error ? e.message : "完成任务失败";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function searchTasks(params: Partial<TaskSearchParams>) {
    loading.value = true;
    error.value = null;
    try {
      const mergedParams = { ...searchParams.value, ...params };
      const response = await taskApi.searchTasks(mergedParams);
      tasks.value = response.items;
      total.value = response.total;
      page.value = response.page;
      searchParams.value = mergedParams;
    } catch (e) {
      error.value = e instanceof Error ? e.message : "搜索任务失败";
    } finally {
      loading.value = false;
    }
  }

  function clearCurrentTask() {
    currentTask.value = null;
  }

  return {
    tasks,
    currentTask,
    loading,
    error,
    total,
    page,
    pageSize,
    searchParams,
    hasMore,
    fetchTasks,
    fetchTask,
    createTask,
    updateTask,
    deleteTask,
    completeTask,
    searchTasks,
    clearCurrentTask,
  };
});
