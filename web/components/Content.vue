<template>
  <v-row>
    <v-col>
      <v-sheet min-height="30vh" rounded="lg" elevation="1">
        <v-container>
          <slot />
        </v-container>
      </v-sheet>
    </v-col>

    <v-col v-if="split" md="4" cols="12">
      <v-sheet min-height="10vh" rounded="lg" elevation="1">
        <v-container>
          <slot name="support" />
        </v-container>
      </v-sheet>
    </v-col>
  </v-row>
</template>

<script setup lang="ts">
defineProps<{ split?: boolean }>()
</script>
