<template>
  <v-row>
    <v-col>
      <slot />
    </v-col>

    <v-col v-if="split" md="4" cols="12">
      <slot name="support" />
    </v-col>
  </v-row>
</template>

<script setup lang="ts">
defineProps<{ split?: boolean }>()
</script>
