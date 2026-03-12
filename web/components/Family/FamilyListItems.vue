<template>
  <v-list>
    <v-list-item
      v-for="family in familiesStore.familySelectors"
      :key="family.id"
      :prepend-icon="family.id === appStore.activeFamilyId ? 'mdi-star' : 'mdi-star-outline'"
      @click="appStore.setActiveFamilyId(family.id)"
    >
      <v-list-item-title>{{ family.name }}</v-list-item-title>
    </v-list-item>

    <v-list-item v-if="familiesStore.familySelectors.length === 0" to="/families">
      <v-list-item-title>Create or join a family</v-list-item-title>
    </v-list-item>
  </v-list>
</template>

<script setup lang="ts">
const appStore = useAppStore()
const familiesStore = useFamiliesStore()
</script>
