<script setup lang="ts">
const emit = defineEmits<{
  'edit-name': []
  move: []
  delete: []
}>()
</script>

<template>
  <v-menu location="bottom end" :close-on-content-click="true">
    <template #activator="{ props: menuProps }">
      <v-btn
        v-bind="menuProps"
        icon="mdi-dots-vertical"
        variant="text"
        density="compact"
        size="small"
        aria-label="More actions"
        class="overflow-btn"
        @click.stop
      />
    </template>
    <v-list density="compact" min-width="168">
      <v-list-item
        prepend-icon="mdi-pencil-outline"
        title="Rename"
        @click="emit('edit-name')"
      />
      <v-list-item
        prepend-icon="mdi-transfer-right"
        title="Move occurrences"
        @click="emit('move')"
      />
      <v-divider />
      <v-list-item
        prepend-icon="mdi-delete-outline"
        title="Delete"
        class="text-error"
        @click="emit('delete')"
      />
    </v-list>
  </v-menu>
</template>

<style scoped>
.overflow-btn {
  opacity: 0;
  transition: opacity var(--duration-fast) var(--ease-out);
}

/* Show on keyboard focus always */
.overflow-btn:focus-visible {
  opacity: 1;
}
</style>
