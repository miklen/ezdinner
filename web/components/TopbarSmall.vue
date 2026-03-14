<template>
  <v-app-bar :elevation="0" class="topbar-small" height="56">
    <NuxtLink to="/home" class="logotype ml-4">
      <span class="logotype__word">Ez</span><span class="logotype__word logotype__word--accent">Dinner</span>
    </NuxtLink>
    <v-spacer />
    <TopbarProfile class="mr-4" />
  </v-app-bar>
</template>

<script setup lang="ts">
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
