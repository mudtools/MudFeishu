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
  
  if (to.meta.requiresAuth && !userStore.isLoggedIn) {
    next({ name: 'Login', query: { redirect: to.fullPath } })
  } else if (to.name === 'Login' && userStore.isLoggedIn) {
    next({ name: 'Home' })
  } else {
    next()
  }
})

export default router
