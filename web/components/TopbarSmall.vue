<template>
  <div>
    <v-app-bar :elevation="0" class="topbar-small" height="56">
      <NuxtLink to="/home" class="logotype ml-4">
        <span class="logotype__word">Ez</span><span class="logotype__word logotype__word--accent">Dinner</span>
      </NuxtLink>
      <v-spacer />
      <TopbarProfile class="mr-2" />
      <v-app-bar-nav-icon class="mr-1" @click="drawer = !drawer" />
    </v-app-bar>

    <v-navigation-drawer v-model="drawer" temporary>
      <v-list-item class="py-4">
        <span class="logotype">
          <span class="logotype__word">Ez</span><span class="logotype__word logotype__word--accent">Dinner</span>
        </span>
        <template #append>
          <v-icon @click="drawer = false">mdi-chevron-left</v-icon>
        </template>
      </v-list-item>
      <v-divider />
      <v-list nav>
        <v-list-item
          v-for="item in links"
          :key="item.to"
          :prepend-icon="item.icon"
          :title="item.title"
          :to="item.to"
          exact
          @click="drawer = false"
        />
      </v-list>
    </v-navigation-drawer>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  links: { icon: string; title: string; to: string }[]
}>()

const drawer = ref(false)
</script>

<style scoped>
.topbar-small {
  background-color: var(--color-surface) !important;
  border-bottom: 1px solid var(--color-border) !important;
}

.logotype {
  display: flex;
  align-items: baseline;
  text-decoration: none;
  line-height: 1;
}

.logotype__word {
  font-family: var(--font-display);
  font-size: 22px;
  font-weight: 400;
  letter-spacing: -0.02em;
  color: var(--color-text-primary);
}

.logotype__word--accent {
  color: var(--color-primary);
}
</style>
