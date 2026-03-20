import { createRouter, createWebHistory } from "vue-router";
import type { RouteRecordRaw } from "vue-router";
import MainLayout from "../layouts/MainLayout.vue";

const routes: RouteRecordRaw[] = [
  {
    path: "/",
    component: MainLayout,
    redirect: "/tasks",
    children: [
      {
        path: "tasks",
        name: "TaskList",
        component: () => import("../views/TaskList.vue"),
        meta: { title: "任务列表" },
      },
      {
        path: "tasks/:id",
        name: "TaskDetail",
        component: () => import("../views/TaskDetail.vue"),
        meta: { title: "任务详情" },
      },
      {
        path: "kanban",
        name: "Kanban",
        component: () => import("../views/Kanban.vue"),
        meta: { title: "任务看板" },
      },
      {
        path: "tasklists",
        name: "TaskLists",
        component: () => import("../views/TaskLists.vue"),
        meta: { title: "任务清单" },
      },
      {
        path: "templates",
        name: "Templates",
        component: () => import("../views/Templates.vue"),
        meta: { title: "任务模板" },
      },
      {
        path: "statistics",
        name: "Statistics",
        component: () => import("../views/Statistics.vue"),
        meta: { title: "统计报表" },
      },
    ],
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

router.beforeEach((to, _from, next) => {
  document.title = `${to.meta.title || "任务管理"} - TaskManage`;
  next();
});

export default router;
