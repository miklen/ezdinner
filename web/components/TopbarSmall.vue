<template>
  <div>
    <v-app-bar color="white" elevation="1">
      <v-avatar class="ml-2">
        <v-img src="/android-chrome-192x192.png" />
      </v-avatar>
      <v-app-bar-nav-icon @click="drawer = !drawer" />
      <v-spacer />
      <TopbarProfile class="mr-4" />
    </v-app-bar>

    <v-navigation-drawer v-model="drawer" temporary>
      <v-list-item title="Dinner Planner" prepend-icon="mdi-silverware-fork-knife">
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
