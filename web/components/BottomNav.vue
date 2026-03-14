<template>
  <v-bottom-navigation
    :elevation="0"
    class="bottom-nav"
    grow
  >
    <v-btn
      v-for="item in links"
      :key="item.to"
      :to="item.to"
      class="bottom-nav__btn"
    >
      <v-icon>{{ currentRoute === item.to ? item.iconActive : item.icon }}</v-icon>
      <span class="bottom-nav__label">{{ item.title }}</span>
    </v-btn>
  </v-bottom-navigation>
</template>

<script setup lang="ts">
defineProps<{
  links: { icon: string; iconActive: string; title: string; to: string }[]
}>()

const route = useRoute()
const currentRoute = computed(() => route.path)
</script>

<style scoped>
.bottom-nav {
  background-color: var(--color-surface) !important;
  border-top: 1px solid var(--color-border) !important;
  box-shadow: var(--shadow-lg) !important;
  height: 64px !important;
}

.bottom-nav__btn {
  min-width: 0 !important;
  min-height: 64px !important;
  flex-direction: column !important;
  gap: 2px !important;
}

.bottom-nav__btn.v-btn--active {
  color: var(--color-primary) !important;
}

.bottom-nav__btn:not(.v-btn--active) {
  color: var(--color-text-muted) !important;
}

.bottom-nav__label {
  font-family: var(--font-body);
  font-size: 10px;
  font-weight: 500;
  letter-spacing: 0.02em;
  line-height: 1;
}
</style>
