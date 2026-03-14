<template>
  <v-app>
    <v-main>
      <v-container>
        <v-row justify="center">
          <v-col cols="auto">
            <v-avatar size="400">
              <v-img src="/images/error.jpg" />
            </v-avatar>
          </v-col>
        </v-row>
        <v-row justify="center">
          <v-col class="text-center">
            <h1>{{ error?.statusCode === 404 ? '404 Not Found' : 'An error occurred' }}</h1>
            <NuxtLink to="/">Home page</NuxtLink>
          </v-col>
        </v-row>
      </v-container>
    </v-main>
  </v-app>
</template>

<script setup lang="ts">
const props = defineProps<{
  error: { statusCode: number; message: string } | null
}>()

useHead({ title: props.error?.statusCode === 404 ? '404 Not Found' : 'Error' })
</script>

<style scoped>
h1 { font-size: 20px; }
</style>
