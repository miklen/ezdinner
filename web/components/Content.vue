<template>
  <v-row>
    <v-col>
      <slot />
    </v-col>

    <v-col
      v-if="split"
      md="4"
      cols="12"
      :class="desktopOnlySupport ? 'd-none d-md-flex' : ''"
    >
      <slot name="support" />
    </v-col>
  </v-row>
</template>

<script setup lang="ts">
defineProps<{ split?: boolean; desktopOnlySupport?: boolean }>()
</script>
