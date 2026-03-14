<template>
  <v-app-bar color="white" elevation="1">
    <v-container :fluid="md" class="py-0 fill-height">
      <v-avatar>
        <v-img src="/android-chrome-192x192.png" />
      </v-avatar>
      <v-toolbar-title class="font-weight-black">Dinner Planner</v-toolbar-title>
      <v-spacer />
      <TopbarProfile />
    </v-container>
  </v-app-bar>
</template>

<script setup lang="ts">
const { md } = useDisplay()
</script>
