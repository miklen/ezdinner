<script setup lang="ts">
const { t } = useI18n()
const { show: showSnackbar } = useSnackbar()
const { isSupported, isIosSafariWithoutPwa, isSubscribed, init, subscribe, unsubscribe } = usePushNotifications()

const loading = ref(false)

onMounted(() => {
  init()
})

async function toggleNotifications(value: boolean | null) {
  if (value === null) return
  if (loading.value) return
  loading.value = true
  try {
    if (value) {
      const result = await subscribe()
      if (result === 'ok') {
        showSnackbar(t('notifications.subscribeSuccess'), { type: 'success' })
      } else if (result === 'denied') {
        showSnackbar(t('notifications.permissionDenied'), { type: 'error', duration: 6000 })
      } else {
        showSnackbar(t('notifications.enableFailed'), { type: 'error' })
      }
    } else {
      const result = await unsubscribe()
      if (result === 'ok') {
        showSnackbar(t('notifications.unsubscribeSuccess'), { type: 'success' })
      } else {
        showSnackbar(t('notifications.disableFailed'), { type: 'error' })
      }
    }
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <template v-if="isSupported">
    <v-list-item class="notifications-item" prepend-icon="mdi-bell-outline" :title="t('notifications.title')">
      <template #append>
        <v-progress-circular v-if="loading" size="20" width="2" indeterminate color="primary" />
        <v-switch
          v-else
          :model-value="isSubscribed"
          :aria-label="t('notifications.title')"
          color="primary"
          density="compact"
          hide-details
          :disabled="loading"
          @update:model-value="toggleNotifications"
        />
      </template>
    </v-list-item>
    <div v-if="isIosSafariWithoutPwa" class="notifications-ios-hint">
      <v-icon size="14">mdi-information-outline</v-icon>
      {{ t('notifications.iosInstallRequired') }}
    </div>
  </template>
</template>

<style scoped>
.notifications-item :deep(.v-list-item__content) {
  min-width: 0;
  overflow: hidden;
}
.notifications-item :deep(.v-list-item-title) {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.notifications-ios-hint {
  display: flex;
  align-items: flex-start;
  gap: 6px;
  padding: 4px 16px 8px;
  font-size: var(--text-xs);
  color: var(--color-text-muted);
  white-space: normal;
  line-height: 1.4;
}
</style>
