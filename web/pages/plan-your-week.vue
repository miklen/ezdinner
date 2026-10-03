<script setup lang="ts">
import Workspace from '~/components/WeekPlanning/Workspace.vue'
defineOptions({ name: 'WeekPlanningPage' })
definePageMeta({ keepalive: true })
const { t } = useI18n()
useHead(() => ({ title: t('weekPlanning.title') }))
</script>

<template>
  <Workspace />
</template>
