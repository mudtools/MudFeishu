import { createRouter, createWebHistory } from "vue-router";
import type { RouteRecordRaw } from "vue-router";
import MainLayout from "../layouts/MainLayout.vue";
import { setupRouterGuards } from "./guards";

const routes: RouteRecordRaw[] = [
  {
    path: "/login",
    name: "Login",
    component: () => import("../views/Login.vue"),
    meta: { title: "登录", public: true },
  },
  {
    path: "/register",
    name: "Register",
    component: () => import("../views/Register.vue"),
    meta: { title: "注册", public: true },
  },
  {
    path: "/bind-feishu",
    name: "BindFeishu",
    component: () => import("../views/BindFeishu.vue"),
    meta: { title: "绑定飞书" },
  },
  {
    path: "/",
    component: MainLayout,
    redirect: "/tasks",
    children: [
      {
        path: "tasks",
        name: "TaskList",
        component: () => import("../views/TaskList.vue"),
        meta: { title: "任务列表", icon: "List" },
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
        meta: { title: "任务看板", icon: "Grid" },
      },
      {
        path: "tasklists",
        name: "TaskLists",
        component: () => import("../views/TaskLists.vue"),
        meta: { title: "任务清单", icon: "Folder" },
      },
      {
        path: "templates",
        name: "Templates",
        component: () => import("../views/Templates.vue"),
        meta: { title: "任务模板", icon: "Document" },
      },
      {
        path: "statistics",
        name: "Statistics",
        component: () => import("../views/Statistics.vue"),
        meta: { title: "统计报表", icon: "DataAnalysis" },
      },
      {
        path: "users",
        name: "Users",
        component: () => import("../views/Users.vue"),
        meta: { title: "用户管理", icon: "User", permission: "user:manage" },
      },
      {
        path: "roles",
        name: "Roles",
        component: () => import("../views/Roles.vue"),
        meta: { title: "角色权限", icon: "Key", permission: "user:manage" },
      },
    ],
  },
  {
    path: "/:pathMatch(.*)*",
    name: "NotFound",
    component: () => import("../views/NotFound.vue"),
    meta: { title: "页面不存在", public: true },
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

// 设置路由守卫
setupRouterGuards(router);

export default router;
