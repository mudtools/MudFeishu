import { createRouter, createWebHistory } from 'vue-router'
import { useUserStore } from '@/stores/user'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'Login',
      component: () => import('@/views/LoginView.vue'),
      meta: { requiresAuth: false }
    },
    {
      path: '/auth/feishu/callback',
      name: 'Callback',
      component: () => import('@/views/CallbackView.vue'),
      meta: { requiresAuth: false }
    },
    {
      path: '/',
      name: 'Home',
      component: () => import('@/views/HomeView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/spaces',
      name: 'Spaces',
      component: () => import('@/views/SpacesView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/spaces/:spaceId',
      name: 'SpaceDetail',
      component: () => import('@/views/SpaceDetailView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/spaces/:spaceId/nodes/:nodeToken',
      name: 'NodeDetail',
      component: () => import('@/views/NodeDetailView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/search',
      name: 'Search',
      component: () => import('@/views/SearchView.vue'),
      meta: { requiresAuth: true }
    }
  ]
})

router.beforeEach(async (to, _from, next) => {
  const userStore = useUserStore()
  
  // 如果有 token 但还在加载用户信息，等待加载完成
  if (userStore.token && userStore.loading) {
    await new Promise(resolve => {
      const unwatch = userStore.$subscribe(() => {
        if (!userStore.loading) {
          unwatch()
          resolve(void 0)
        }
      })
      // 如果已经在加载中，设置一个短超时
      setTimeout(() => {
        unwatch()
        resolve(void 0)
      }, 3000)
    })
  }
  
  // 判断登录状态：有 token 且有用户信息
  const isLoggedIn = !!userStore.token && !!userStore.user
  
  if (to.meta.requiresAuth && !isLoggedIn) {
    next({ name: 'Login', query: { redirect: to.fullPath } })
  } else if (to.name === 'Login' && isLoggedIn) {
    next({ name: 'Home' })
  } else {
    next()
  }
})

export default router
