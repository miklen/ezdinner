<template>
  <v-app-bar :elevation="0" class="topbar-large" height="64">
    <v-container :fluid="md" class="py-0 fill-height">
      <NuxtLink to="/home" class="logotype">
        <span class="logotype__word">Ez</span><span class="logotype__word logotype__word--accent">Dinner</span>
      </NuxtLink>
      <v-spacer />
      <TopbarProfile />
    </v-container>
  </v-app-bar>
</template>

<script setup lang="ts">
const { md } = useDisplay()
</script>

<style scoped>
.topbar-large {
  background-color: var(--color-surface) !important;
  border-bottom: 1px solid var(--color-border) !important;
}

.logotype {
  display: flex;
  align-items: baseline;
  gap: 0;
  text-decoration: none;
  line-height: 1;
}

.logotype__word {
  font-family: var(--font-display);
  font-size: 26px;
  font-weight: 400;
  letter-spacing: -0.02em;
  color: var(--color-text-primary);
  transition: color var(--duration-fast) var(--ease-out);
}

.logotype__word--accent {
  color: var(--color-primary);
}

.logotype:hover .logotype__word {
  color: var(--color-primary-dark);
}
</style>
